namespace WeatherStation.Functions

module WundergroundForwarder =
    open System
    open System.Threading.Tasks
    
    open Microsoft.Azure.WebJobs
    open Microsoft.Azure.WebJobs.Host    

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

    let processEventHubMessage eventHubMessage (log: TraceWriter) postToWunderground =
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
                        
                        let connectionString = Environment.GetEnvironmentVariable("WeatherStationStorage")                        
                        let! weatherStationRepository = AzureStorage.weatherStationRepository connectionString
                        let! weatherStation = weatherStationRepository.Get deviceType deviceReading.DeviceId

                        if weatherStation.IsNone then
                            log.Error(sprintf "Device [%s] is not provisioned" deviceReading.DeviceId)
                        else
                            let! readingsRepository = AzureStorage.readingsRepository connectionString
                            let weatherStation = weatherStation.Value
                            try
                                let! settingsRepository = AzureStorage.settingsRepository connectionString
                                let! readingsWindow = SystemSettings.averageReadingsWindow settingsRepository
                                let readingCutOff = DateTime.Now.Subtract(readingsWindow)
                            
                                let! recentReadings = readingsRepository.GetHistory deviceReading.DeviceId readingCutOff

                                let values = fixReadings recentReadings weatherStation deviceReading.Readings
                                log.Info(sprintf "Fixed Values %A" values)

                                let valuesSeq = values |> Seq.ofList
                                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log

                                log.Info(sprintf "%A" wundergroundResponse)
                            with
                            | ex -> log.Error("Error while posting to Wunderground", ex)
                
                            let reading = Model.createReading deviceReading                        
                        
                            log.Info(sprintf "Saving Reading %A" reading)
                            do! readingsRepository.Save reading

                            let updatedWeatherStation = { weatherStation with LastReading = Some( DateTime.Now ) }
                            do! weatherStationRepository.Save updatedWeatherStation
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

    [<FunctionName("WundergroundForwarder")>]
    let Run ([<EventHubTrigger("weatherstationsiot", Connection="WeatherStationsIoT")>] eventHubMessage: string) (log: TraceWriter) =    
        processEventHubMessage eventHubMessage log postToWunderground