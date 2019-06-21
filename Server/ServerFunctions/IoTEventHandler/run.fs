namespace WeatherStation.Functions
open Particle

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

    let handleDeviceReading 
        (log: ILogger) 
        postToWunderground 
        getWeatherStation
        saveWeatherStation
        saveReading
        getReadings
        settingsGetter 
        deviceType 
        (deviceReading : Readings.DeviceReadings) =
        async {
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
            
                if not (isNull weatherStation.WundergroundStationId) then
                    try
                        let valuesSeq = values |> Seq.ofList
                        let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log
                        log.LogInformation(sprintf "%A" wundergroundResponse)
                    with
                    | ex -> log.LogError("Error while posting to Wunderground", ex)
                else
                    log.LogWarning("No WundergroundId. No data posted to Wunderground.")                        
                    
                let deviceReading = {deviceReading with Readings = values}
                let reading = Readings.createReading deviceReading deviceReading.Message                      

                log.LogInformation(sprintf "Saving Reading %A" reading)
                do! saveReading reading

                let updatedWeatherStation = { weatherStation with LastReading = Some( reading.ReadingTime ) }
                do! saveWeatherStation updatedWeatherStation
        }

    let handleStatusMessage 
        (log: ILogger) 
        saveStatusMessage
        (statusMessage : StatusMessage) =
        async {
            log.LogInformation(sprintf "Saving StatusMessage %s for device %s" statusMessage.StatusMessage statusMessage.DeviceId)
            do! saveStatusMessage statusMessage
        }                

    let parsers = [
        Particle.parseParticleEvent]

    let rec innerMostException (ex : exn) =
        printfn "%A" ex
        if not (isNull ex.InnerException) then innerMostException ex.InnerException else ex

    let processEventHubMessage 
        (log: ILogger) 
        postToWunderground 
        getWeatherStation
        saveWeatherStation
        saveReading
        getReadings
        settingsGetter
        saveStatusMessage
        eventHubMessage =

        log.LogInformation(eventHubMessage)

        let parseAttempts = [
            for parser in parsers -> tryParse (parser log) eventHubMessage]

        let successfulAttempts = [
            for attempt in parseAttempts do
                match attempt with
                | Choice1Of2 result -> yield result
                | _ -> ()]

        let handleDeviceReading = handleDeviceReading log postToWunderground getWeatherStation saveWeatherStation saveReading getReadings settingsGetter
        let handleStatusMessage = handleStatusMessage log saveStatusMessage

        if successfulAttempts.Length > 0 
        then
            [for parsedEvent in successfulAttempts do
                yield
                    match parsedEvent with
                    | Particle.ParticeEvent.Reading deviceReading -> handleDeviceReading DeviceType.Particle deviceReading
                    | Particle.ParticeEvent.StatusMessage statusMessage -> handleStatusMessage statusMessage
                ]
                |> Async.Parallel
                |> Async.Ignore        
        else
            async {
                log.LogInformation("No values parsed")
                for attempt in parseAttempts do
                    let message =
                        match attempt with
                        | Choice2Of2 ex -> 
                            string (innerMostException ex)
                        | _ -> "no exception"

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
        let saveStatusMessage statusMessage = async {
            let! repo = AzureStorage.statusMessageRepository connectionString
            do! repo.Save statusMessage
        }
        processEventHubMessage 
            log 
            postToWunderground
            getWeatherStation
            saveWeatherStation
            saveReading
            getReadings
            settingsGetter
            saveStatusMessage
            eventHubMessage

    [<FunctionName("WundergroundForwarder")>]
    let Run ([<EventHubTrigger("weatherstationsiot", Connection="WeatherStationsIoT")>] eventHubMessage: string) (log: ILogger) =        
        processEventHubMessageWithAzureStorage
            postToWunderground
            log
            eventHubMessage        
        |> Async.StartAsTask :> Task