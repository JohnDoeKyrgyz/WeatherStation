namespace WeatherStations

module Server =
    open System.IO
    open System.Threading.Tasks

    open Microsoft.Extensions.DependencyInjection
    open Giraffe
    open Saturn

    open FSharp.Control.Tasks

    open WeatherStations.Shared
    open WeatherStations.AzureStorage

    open Giraffe.Serialization

    let publicPath = Path.GetFullPath "../Client/public"
    let port = 8085us

    let getInitCounter() : Task<Counter> = task { return 42 }

    let webApp = router {
        get "/api/init" (fun next ctx ->
            task {
                let! counter = getInitCounter()
                return! Successful.OK counter next ctx
            })
            
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