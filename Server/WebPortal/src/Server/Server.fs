namespace WeatherStation
open Microsoft.Extensions.Configuration

module Server =
    open System
    open System.IO
    open System.Configuration

    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.AspNetCore.Http

    open Saturn
    open Giraffe
    open Giraffe.Serialization

    open FSharp.Control.Tasks

    open WeatherStation.Data
    open WeatherStation.Shared
    open Logic

    let port = 8085us

    #if DEBUG
    Console.Beep()
    #endif

    let getConnectionString (ctx : HttpContext) =
        let configuration = ctx.RequestServices.GetService<IConfiguration>()
        let connectionString = configuration.GetConnectionString("DefaultConnection")
        connectionString

    let read reader next ctx =
        let connectionString = getConnectionString ctx
        task {
            let! data = reader connectionString |> Async.StartAsTask
            match data with
            | Result.Ok data -> return! Successful.OK data next ctx
            | Result.Error error -> return! RequestErrors.NOT_FOUND (string error) next ctx
        }

    let getStations connectionString = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! activeThreshold = SystemSettings.activeThreshold systemSettingsRepository.GetSettingWithDefault
        let! stations =
            allStations connectionString
            |> getWeatherStations activeThreshold
        return Ok stations
    }

    let getStationDetails (key : StationKey) connectionString = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! defaultPageSize = SystemSettings.defaultPageSize systemSettingsRepository.GetSettingWithDefault
        let! stationDetails =
            weatherStationDetails connectionString defaultPageSize key
            |> getWeatherStationDetails defaultPageSize.TotalHours
        return
            match stationDetails with
            | Some details -> Ok details
            | None -> Error (sprintf "No device %A" key) }

    let getReadingsPage key fromDate toDate connectionString = async {
        let! readings = readings connectionString key fromDate toDate
        return Ok (readings |> List.map createReading) }

    let getMessagesPage key fromDate toDate connectionString = async {
        let! messages = messages connectionString key fromDate toDate
        return Ok (messages |> List.map createStatusMessage) }

    let getSettings key connectionString = async {
        let! settings = weatherStationSettings connectionString key
        return Ok settings
    }

    let createStation (key : StationKey) next (ctx : HttpContext) = task {
        let station = Logic.createStation key
        let connectionString = getConnectionString ctx
        do! Data.createStation connectionString station
        return! Successful.OK key next ctx
    }

    type SetSettingsResponse = {
        ParticleRespose : Result<bool, string>
        UpdatedSettings : FirmwareSettings
    }

    let setSettings (key : StationKey) settings next (ctx : HttpContext) = task {
        let connectionString = getConnectionString ctx
        let! updatedSettings = updateWeatherStationSettings connectionString key settings
        match updatedSettings with
        | Some updatedSettings ->
            let! particleResponse = updateParticleDeviceSettings key updatedSettings
            let detailedResponse = {
                ParticleRespose = particleResponse
                UpdatedSettings = updatedSettings
            }
            return! Successful.OK detailedResponse next ctx
        | None -> return! Successful.OK "Cleared settings" next ctx
    }

    [<CLIMutable>]
    type PageKey = {
        DeviceType : DeviceType
        DeviceId : string
        FromDate : string
        TooDate : string
    }

    let webApp =
        choose [
            GET >=> route "/api/stations" >=> (read getStations)
            GET >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}" (getStationDetails >> read)
            POST >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}" (createStation)
            GET >=>
                routeBind<PageKey>
                    "/api/stations/{DeviceType}/{DeviceId}/readings/{FromDate}/{TooDate}"
                    (fun key -> getReadingsPage {DeviceType = key.DeviceType; DeviceId = key.DeviceId} (UrlDateTime.fromUrlDate key.FromDate) (UrlDateTime.fromUrlDate key.TooDate) |> read)
            GET >=>
                routeBind<PageKey>
                    "/api/stations/{DeviceType}/{DeviceId}/messages/{FromDate}/{TooDate}"
                    (fun key -> getMessagesPage {DeviceType = key.DeviceType; DeviceId = key.DeviceId} (UrlDateTime.fromUrlDate key.FromDate) (UrlDateTime.fromUrlDate key.TooDate) |> read)
            GET >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}/settings" (getSettings >> read)
            POST >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}/settings" (setSettings >> bindJson)
        ]

    let configureSerialization (services:IServiceCollection) =
        let fableJsonSettings = Newtonsoft.Json.JsonSerializerSettings()
        fableJsonSettings.Converters.Add(Fable.JsonConverter())
        services.AddSingleton<IJsonSerializer>(NewtonsoftJsonSerializer fableJsonSettings)

    let tryGetEnv = System.Environment.GetEnvironmentVariable >> function null | "" -> None | x -> Some x

    let publicPath = tryGetEnv "public_path" |> Option.defaultValue "../Client/public" |> Path.GetFullPath

    let configureAzure (services:IServiceCollection) =
        tryGetEnv "APPINSIGHTS_INSTRUMENTATIONKEY"
        |> Option.map services.AddApplicationInsightsTelemetry
        |> Option.defaultValue services

    let app = application {
        url ("http://0.0.0.0:" + port.ToString() + "/")
        use_router webApp
        memory_cache
        use_static publicPath
        service_config configureSerialization
        service_config configureAzure
        use_gzip
    }

    run app