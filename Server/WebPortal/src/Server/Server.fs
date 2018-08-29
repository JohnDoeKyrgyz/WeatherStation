namespace WeatherStation

module Server =
    open System.IO
    open System.Threading.Tasks

    open Microsoft.Extensions.DependencyInjection
    open Giraffe
    open Saturn

    open FSharp.Control.Tasks

    open WeatherStation.AzureStorage

    open Giraffe.Serialization

    let publicPath = Path.GetFullPath "../Client/public"
    let port = 8085us
    
    let webApp = router {
            
        get "/api/stations" (fun next ctx ->
            task {
                let! stations = getWeatherStations()
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
