namespace WeatherStation

module Server =
    open System
    open System.IO    
    open System.Configuration

    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.DependencyInjection

    open Saturn
    open Giraffe
    open Giraffe.Serialization

    open FSharp.Control.Tasks    

    open WeatherStation.Data
    open Logic
    open Model

    let publicPath = Path.GetFullPath "../Client/public"
    let port = 8085us

    #if DEBUG
    Console.Beep()
    #endif
    
    let connectionString = ConfigurationManager.ConnectionStrings.["WeatherStationStorage"].ConnectionString

    let read reader next ctx =
        task {
            let! data = reader |> Async.StartAsTask
            match data with
            | Result.Ok data -> return! Successful.OK data next ctx
            | Result.Error error -> return! RequestErrors.NOT_FOUND (string error) next ctx
        }

    let getStations = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! activeThreshold = SystemSettings.activeThreshold systemSettingsRepository.GetSettingWithDefault
        let! stations =
            allStations connectionString
            |> getWeatherStations activeThreshold
        return Ok stations
    }

    let getStationDetails deviceType deviceId = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! readingsCount = SystemSettings.readingsCount systemSettingsRepository.GetSettingWithDefault
        let! stationDetails =
            weatherStationDetails connectionString readingsCount deviceType deviceId
            |> getWeatherStationDetails
        return
            match stationDetails with
            | Some details -> Ok details
            | None -> Error (sprintf "No device %A %s" deviceType deviceId) }

    let setSettings deviceId settings next ctx = task {
        let! response = updateSettings deviceId settings
        return! Successful.OK response next ctx
    }

    let webApp = router {            
        get "/api/stations" (read getStations)
        getf "/api/stations/%s/%s" (fun (deviceType, deviceId) -> (getStationDetails (parseDeviceType deviceType) deviceId) |> read)
        postf "/api/stations/%s/settings" (fun deviceId -> bindJson (fun settings -> setSettings deviceId settings))
    }

    let configureSerialization (services:IServiceCollection) =
        let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
        fableJsonSettings.Converters.Add(Fable.JsonConverter())
        services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

    let app = application {
        url ("http://0.0.0.0:" + port.ToString() + "/")
        use_router webApp
        memory_cache
        use_static publicPath
        service_config configureSerialization
        use_gzip
    }

    run app