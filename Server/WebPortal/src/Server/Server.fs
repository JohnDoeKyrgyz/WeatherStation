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

    open Logic

    let publicPath = Path.GetFullPath "../Client/public"
    let port = 8085us

    #if DEBUG
    Console.Beep()
    #endif
    
    let connectionString = ConfigurationManager.ConnectionStrings.["WeatherStationStorage"].ConnectionString

    let read reader next ctx =
        task {
            let! data = reader |> Async.StartAsTask
            return! Successful.OK data next ctx
        }

    let getStations = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! activeThreshold = SystemSettings.activeThreshold systemSettingsRepository.GetSettingWithDefault
        let! stations =
            allStations connectionString
            |> getWeatherStations activeThreshold
        return stations
    }

    let setSettings deviceId settings next ctx = task {
        let! response = updateSettings deviceId settings
        return! Successful.OK response next ctx
    }

    let webApp = router {            
        get "/api/stations" (read getStations)
        postf "api/stations/%s/settings" (fun deviceId -> bindJson (fun settings -> setSettings deviceId settings))
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