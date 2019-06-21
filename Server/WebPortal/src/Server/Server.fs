namespace WeatherStation

module Server =
    open System
    open System.IO
    open System.Configuration

    open Microsoft.Extensions.Logging
    open Microsoft.Extensions.DependencyInjection
    open Microsoft.AspNetCore.Builder
    open Microsoft.AspNetCore.Http

    open Saturn
    open Giraffe
    open Giraffe.Serialization

    open FSharp.Control.Tasks

    open WeatherStation.Data
    open WeatherStation.Shared
    open Logic
    open Newtonsoft.Json.Linq

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

    let getStationDetails (key : StationKey) = async {
        let! systemSettingsRepository = AzureStorage.settingsRepository connectionString
        let! defaultPageSize = SystemSettings.defaultPageSize systemSettingsRepository.GetSettingWithDefault
        let! stationDetails =
            weatherStationDetails connectionString defaultPageSize key
            |> getWeatherStationDetails defaultPageSize.TotalHours
        return
            match stationDetails with
            | Some details -> Ok details
            | None -> Error (sprintf "No device %A" key) }

    let getReadingsPage key fromDate toDate = async {
        let! readings = readings connectionString key fromDate toDate
        return Ok (readings |> List.map createReading) }

    let getSettings key = async {
        let! settings = weatherStationSettings connectionString key
        return Ok settings
    }

    type SetSettingsResponse = {
        ParticleRespose : Result<bool, string>
        UpdatedSettings : StationSettings
    }

    let setSettings (key : StationKey) settings next (ctx : HttpContext) = task {
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
        DeviceType : string
        DeviceId : string
        FromDate : string
        TooDate : string
    }

    let webApp =
        choose [
            GET >=> route "/api/stations" >=> (read getStations)
            GET >=> routeBind<StationKey> "/api/stations/{DeviceType}/{DeviceId}" (getStationDetails >> read)
            GET >=>
                routeBind<PageKey>
                    "/api/stations/{DeviceType}/{DeviceId}/{FromDate}/{TooDate}"
                    (fun key -> getReadingsPage {DeviceType = key.DeviceType; DeviceId = key.DeviceId} (UrlDateTime.fromUrlDate key.FromDate) (UrlDateTime.fromUrlDate key.TooDate) |> read)
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