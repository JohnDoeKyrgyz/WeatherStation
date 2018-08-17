namespace WeatherStations

module Server =
    open System.IO
    open System.Threading.Tasks

    open Microsoft.AspNetCore.Builder
    open Microsoft.Extensions.DependencyInjection
    open Giraffe
    open Saturn
    open Shared

    open Giraffe.Serialization

    let publicPath = Path.GetFullPath "../Client/public"
    let port = 8085us

    let getInitCounter() : Task<Counter> = task { return 42 }

    let getStations() =
        task { 
            return [
                {Name = "Main"; WundergroundId = "KVTWESTR7"; Status = Active; Location = {Latitude = 12.0m; Longitude = 12.0m}}
                {Name = "Secondary"; WundergroundId = "abcdxyx"; Status = Offline; Location = {Latitude = 12.0m; Longitude = 12.0m}}]}

    let webApp = router {
        get "/api/init" (fun next ctx ->
            task {
                let! counter = getInitCounter()
                return! Successful.OK counter next ctx
            })
            
        get "/api/stations" (fun next ctx ->
            task {
                let! stations = getStations()
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
