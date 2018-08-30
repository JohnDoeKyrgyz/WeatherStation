namespace WeatherStation

module Server =
    open System
    open System.IO

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
    
    let webApp = router {
            
        get "/api/stations" (fun next ctx ->
            task {
                let! systemSettingsRepository = AzureStorage.settingsRepository
                let! activeThreshold = SystemSettings.activeThreshold systemSettingsRepository
                let! stations = getWeatherStations activeThreshold
                return! ctx.WriteJsonAsync stations
            })        
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
