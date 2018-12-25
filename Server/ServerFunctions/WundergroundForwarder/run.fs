namespace WeatherStation.Functions

module WundergroundForwarder =
    open System
    open System.Threading.Tasks    
    
    open Microsoft.Extensions.Logging
    open Microsoft.Azure.WebJobs 

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

    let processEventHubMessage 
        (log: ILogger) 
        postToWunderground 
        getWeatherStation
        saveWeatherStation
        saveReading
        getReadings
        settingsGetter
        eventHubMessage =

        log.LogInformation(eventHubMessage)

        let parseAttempts = [
            for (key, parser) in parsers do
                yield key, (tryParse (parser log) eventHubMessage)]

        let successfulAttempts = [
            for (key, attempt) in parseAttempts do
                match attempt with
                | Choice1Of2 result -> yield key, result
                | _ -> ()]

        if successfulAttempts.Length > 0 
        then
            [for (deviceType, deviceReading) in successfulAttempts do
                yield async {
                    log.LogInformation(sprintf "%A" deviceReading)
                    log.LogInformation(sprintf "Searching for device %A %s in registry" deviceType deviceReading.DeviceId)
                        
                    let! (weatherStation : WeatherStation option) = 
                        async {
                            match! getWeatherStation deviceType deviceReading.DeviceId with
                            | None -> 
                                log.LogInformation(sprintf "%A %s not found. Searching for device %A %s in registry" deviceType deviceReading.DeviceId DeviceType.Test deviceReading.DeviceId)
                                return! getWeatherStation DeviceType.Test deviceReading.DeviceId
                            | value -> return value
                        }                        

                    if weatherStation.IsNone then
                        log.LogError(sprintf "Device [%s] is not provisioned" deviceReading.DeviceId)
                    else
                        let weatherStation = weatherStation.Value
                        let! settingsRepository = settingsGetter
                        let! readingsWindow = SystemSettings.averageReadingsWindow settingsRepository
                        let readingCutOff = DateTime.Now.Subtract(readingsWindow)
                            
                        let! recentReadings = getReadings deviceReading.DeviceId readingCutOff
                        let values = fixReadings recentReadings weatherStation deviceReading.Readings
                        log.LogInformation(sprintf "Fixed Values %A" values)
                    
                        if weatherStation.WundergroundStationId <> null then
                            try
                                let valuesSeq = values |> Seq.ofList
                                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log
                                log.LogInformation(sprintf "%A" wundergroundResponse)
                            with
                            | ex -> log.LogError("Error while posting to Wunderground", ex)
                        else
                            log.LogWarning("No WundergroundId. No data posted to Wunderground.")                        
                            
                        let deviceReading = {deviceReading with Readings = values}
                        let reading = Model.createReading deviceReading                        

                        log.LogInformation(sprintf "Saving Reading %A" reading)
                        do! saveReading reading

                        let updatedWeatherStation = { weatherStation with LastReading = Some( reading.ReadingTime ) }
                        do! saveWeatherStation updatedWeatherStation }]
                |> Async.Parallel
                |> Async.Ignore        
        else
            async {
                log.LogInformation("No values parsed")
                for (key, attempt) in parseAttempts do
                    let message =
                        match attempt with
                        | Choice2Of2 ex -> 
                            string (innerMostException ex)
                        | _ -> "no exception"

                    log.LogInformation(string key)
                    log.LogError(message)}

    let processEventHubMessageWithAzureStorage postToWunderground log eventHubMessage =
        let connectionString = Environment.GetEnvironmentVariable("WeatherStationStorage")
        let getWeatherStation deviceType id = async { 
            let! repo = AzureStorage.weatherStationRepository connectionString
            return! repo.Get deviceType id}
        let saveWeatherStation weatherStation = async {
            let! repo = AzureStorage.weatherStationRepository connectionString
            do! repo.Save weatherStation
        }
        let saveReading reading = async {
            let! repo = AzureStorage.readingsRepository connectionString
            do! repo.Save reading
        }
        let getReadings deviceId cutOff = async {
            let! repo = AzureStorage.readingsRepository connectionString
            return! repo.GetHistory deviceId cutOff
        }
        let settingsGetter = async {
            let! repo = AzureStorage.settingsRepository connectionString
            return repo.GetSettingWithDefault
        }
        processEventHubMessage 
            log 
            postToWunderground
            getWeatherStation
            saveWeatherStation
            saveReading
            getReadings
            settingsGetter
            eventHubMessage

    [<FunctionName("WundergroundForwarder")>]
    let Run ([<EventHubTrigger("weatherstationsiot", Connection="WeatherStationsIoT")>] eventHubMessage: string) (log: ILogger) =        
        processEventHubMessageWithAzureStorage
            postToWunderground
            log
            eventHubMessage        
        |> Async.StartAsTask :> Task