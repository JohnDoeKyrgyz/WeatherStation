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
    open WeatherStation.Shared
    open Logic
    open Model
    open Microsoft.AspNetCore.Http

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

    let getStationDetails key = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! readingsCount = SystemSettings.readingsCount systemSettingsRepository.GetSettingWithDefault
        let! stationDetails =
            weatherStationDetails connectionString readingsCount key
            |> getWeatherStationDetails
        return
            match stationDetails with
            | Some details -> Ok details
            | None -> Error (sprintf "No device %A" key) }

    let getSettings key = async {
        let! settings = weatherStationSettings connectionString key
        let settings =
            match settings with
            | Some null -> String.Empty
            | None -> String.Empty
            | Some v -> v
        return Ok settings
    }

    let setSettings key next (ctx : HttpContext) = task {
        let! formContents = ctx.ReadBodyFromRequestAsync()
        let settings = ParticleSettings.Parse formContents
        let! response = updateParticleDeviceSettings key settings
        let saveSettings = string settings
        do! updateWeatherStationSettings connectionString key saveSettings
        return! Successful.OK response next ctx
    }

    let webApp =
        choose [        
            GET >=> route "/api/stations" >=> (read getStations)
            GET >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}" (getStationDetails >> read)
            GET >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}/settings" (getSettings >> read)
            POST >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}/settings" setSettings
        ]            

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