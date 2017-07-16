#I __SOURCE_DIRECTORY__
#load "../Preamble.fsx"
#load "../Database.fsx"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System
open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data
open Microsoft.Azure.WebJobs.Host
open System.Linq

open Database

[<Literal>]
let Sample = __SOURCE_DIRECTORY__ + "/StatusUpdate.json"
type Payload = JsonProvider<Sample, SampleIsList = true>

let postToWunderground wundergroundId wundergroundPassword (payload : Payload.Root) (log : TraceWriter) =
    async {
        let reading = payload.Body
        let dateUtc = reading.Time.ToUniversalTime().ToString("yyyy-mm-dd hh:mm:ss")
        let windSpeed = (defaultArg reading.SpeedMetersPerSecond 0.0m) * 2.23694m
        let windDirection = (defaultArg reading.DirectionSixteenths 0.0m) * (360.0m / 16.0m)
        let temperature = (defaultArg reading.TemperatureCelciusBarometer 0.0m) * 9.0m/5.0m  + 32.0m
        let barometer = (defaultArg reading.PressurePascal 0.0m) * 0.0002953m

        //http://wiki.wunderground.com/index.php/PWS_-_Upload_Protocol
        let url = "https://weatherstation.wunderground.com/weatherstation/updateweatherstation.php"
        let queryParameters = [ 
            "ID", wundergroundId
            "PASSWORD", wundergroundPassword
            "action", "updateraw"
            "dateutc", dateUtc
            "realtime", "1"
            "windspeedmph", string windSpeed
            "winddir", string windDirection
            "tempf", string temperature
            "humidity", defaultArg reading.HumidityPercent 0.0m |> string
            "baromin", string barometer
            "rtfreq", string reading.RefreshIntervalSeconds]

        log.Info( sprintf "Wunderground Request: %A" queryParameters )
    
        let! wundergroundResponse = Http.AsyncRequest( url, queryParameters )
        log.Info( sprintf "Wunderground Response: %A" wundergroundResponse )

        return wundergroundResponse
    }

let buildReading (payload : Payload.Root) =
    let reading = payload.Body
    Reading(
        PartitionKey = string payload.SourceDevice,
        RowKey = string (payload.Datetime.ToFileTimeUtc()),
        BatteryVoltage = nullable reading.BatteryVoltage,
        RefreshIntervalSeconds = reading.RefreshIntervalSeconds,
        DeviceTime = payload.Datetime,
        ReadingTime = reading.Time,
        SupplyVoltage = nullable reading.SupplyVoltage,
        ChargeVoltage = nullable reading.ChargeVoltage,
        TemperatureCelciusHydrometer = nullable (reading.TemperatureCelciusHydrometer |> Option.map double),
        TemperatureCelciusBarometer = nullable (reading.TemperatureCelciusBarometer |> Option.map double),
        HumidityPercent = nullable (reading.HumidityPercent |> Option.map double),
        PressurePascal = nullable (reading.PressurePascal |> Option.map double),
        SpeedMetersPerSecond = nullable (reading.SpeedMetersPerSecond |> Option.map double),
        DirectionSixteenths = nullable (reading.DirectionSixteenths |> Option.map double),
        SourceDevice = string payload.SourceDevice)

let Run(req: HttpRequestMessage, weatherStationsTable: IQueryable<WeatherStation>, storedReading : byref<Reading>, log: TraceWriter) =
    let readingResponse, httpResponse =
        async {
            let! content = req.Content.ReadAsStringAsync() |> Async.AwaitTask
            if String.IsNullOrWhiteSpace(content) then
                return None, req.CreateErrorResponse(HttpStatusCode.BadRequest, "Expected request data")
            else
                let payload = Payload.Parse content
                let deviceSerialNumber = string payload.SourceDevice        

                let weatherStation = 
                    weatherStationsTable
                        .Where( fun station -> station.PartitionKey = DefaultPartition && station.RowKey = deviceSerialNumber )
                        .ToArray()
                        .Single()

                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword payload log

                let reading = buildReading payload

                return (Some reading, req.CreateResponse(wundergroundResponse))
            } 
            |> Async.RunSynchronously

    if readingResponse.IsSome then 
        storedReading <- readingResponse.Value
    httpResponse