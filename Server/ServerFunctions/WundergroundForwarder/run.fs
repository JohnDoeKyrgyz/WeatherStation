namespace WeatherStation.Functions

module WundergroundForwarder =
    open System
    open System.Threading.Tasks
    
    open Microsoft.Azure.WebJobs
    open Microsoft.Azure.WebJobs.Host    
    open Microsoft.Azure.WebJobs.ServiceBus

    open WeatherStation.Model
    open ProcessReadings
    open WundergroundPost
    open WeatherStation    

    let tryParse parser content =
        try
            Choice1Of2 (parser content)
        with ex -> Choice2Of2 ex

    let parsers = 
        [Particle, Particle.parseValues; Hologram, Hologram.parseValues]

    let rec innerMostException (ex : exn) =
        printfn "%A" ex
        if ex.InnerException <> null then innerMostException ex.InnerException else ex

    [<FunctionName("WundergroundForwarder")>]
    let Run ([<EventHubTrigger("weatherstationsiot", Connection="WeatherStationsIoT")>] eventHubMessage: string) (log: TraceWriter) =    
        async {
            log.Info(eventHubMessage)

            let parseAttempts = [
                for (key, parser) in parsers do
                    yield key, (tryParse (parser log) eventHubMessage)]

            let successfulAttempts = [
                for (key, attempt) in parseAttempts do
                    match attempt with
                    | Choice1Of2 result -> yield key, result
                    | _ -> ()]

            do!
                match successfulAttempts with
                | (deviceType, deviceReading) :: _ ->
                    async {
                        log.Info(sprintf "%A" deviceReading)

                        log.Info(sprintf "Searching for device %A %s in registry" deviceType deviceReading.DeviceId)

                        let! weatherStationRepository = AzureStorage.weatherStationRepository
                        let! weatherStation = weatherStationRepository.Get deviceType deviceReading.DeviceId
                
                        let! settingsRepository = AzureStorage.settingsRepository
                        let! readingsWindow = SystemSettings.averageReadingsWindow settingsRepository
                        let readingCutOff = DateTime.Now.Subtract(readingsWindow)
                        let! readingsRepository = AzureStorage.readingsRepository
                        let! recentReadings = readingsRepository.GetHistory deviceReading.DeviceId readingCutOff

                        let values = fixReadings recentReadings weatherStation deviceReading.Readings
                        log.Info(sprintf "Fixed Values %A" values)

                        let valuesSeq = values |> Seq.ofList
                        let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log

                        log.Info(sprintf "%A" wundergroundResponse)
                
                        let reading = Model.createReading deviceReading
                        
                        log.Info(sprintf "Saving Reading %A" reading)
                        do! readingsRepository.Save reading
                    }                    
                | _ ->
                    async {
                        log.Info("No values parsed")
                        for (key, attempt) in parseAttempts do
                            let message =
                                match attempt with
                                | Choice2Of2 ex -> 
                                    string (innerMostException ex)
                                | _ -> "no exception"

                            log.Info(string key)
                            log.Error(message)}}
        |> Async.StartAsTask :> Task