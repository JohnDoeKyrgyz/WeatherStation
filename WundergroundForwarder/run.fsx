#I __SOURCE_DIRECTORY__
#load "../Preamble.fsx"
#load "../Database.fsx"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System
open System.Net
open System.Net.Http
open System.Linq
open System.Text

open Newtonsoft.Json
open FSharp.Data

open Microsoft.Azure.WebJobs.Host

open Database

[<Literal>]
let Sample = __SOURCE_DIRECTORY__ + "/StatusUpdate.json"
type Payload = JsonProvider<Sample, SampleIsList = true>

let parseRaw body =
    let data = 
        Convert.FromBase64String body
        |> Encoding.UTF8.GetString

    let data = data.Split([|":"|], StringSplitOptions.None)

    (*
        {"refreshIntervalSeconds":"60","temperatureCelciusHydrometer":57,"humidityPercent":26.79999,"temperatureCelciusBarometer":28.09,
        "pressurePascal":99089.77,"supplyVoltage":369,"batteryVoltage":216,"chargeVoltage":420,"speedMetersPerSecond":0,"directionSixteenths":7,"time":"2017-7-20 21:28:0"}
        uint16_t year;    /*!< Range from 1970 to 2099.*/
    *)

    let readOptional reader i =
        let value = data.[i]
        if String.IsNullOrWhiteSpace( value ) |> not then Some (reader value) else None
    let readOptionalDecimal = readOptional Convert.ToDecimal
    let readInt i = int (Convert.ToInt32(data.[i]) )
    let readOptionalInt = readOptional Convert.ToInt32

    let year = readInt 10
    let month = readInt 12
    let day = readInt 13
    let hour = readInt 13
    let minute = readInt 14
    let second = readInt 15

    let time = DateTime(int year, int month, int day, int hour, int minute, int second)
    Payload.Body(
        readInt 0,
        readOptionalDecimal 1,
        readOptionalDecimal 2,
        readOptionalDecimal 3,
        readOptionalDecimal 4,
        readOptionalInt 5,
        readOptionalInt 6,
        readOptionalInt 7,
        readOptionalDecimal 8,
        readOptionalInt 9,
        time )

let parseBody (payload : Payload.Root) =
    match payload.Body.Record with
    | Some v -> v
    | None -> parseRaw payload.Body.String.Value

let postToWunderground wundergroundId wundergroundPassword (payload : Payload.Root) (log : TraceWriter) =
    async {
        let reading = parseBody payload
        let dateUtc = reading.Time.ToUniversalTime().ToString("yyyy-mm-dd hh:mm:ss")
        let windSpeed = (defaultArg reading.SpeedMetersPerSecond 0.0m) * 2.23694m
        let windDirection = decimal (defaultArg reading.DirectionSixteenths 0) * (360.0m / 16.0m)
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
    let reading = parseBody payload
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
                let body = Payload.StringOrBody( parseBody payload )
                let payload = Payload.Root(body, payload.SourceDevice, payload.Datetime)

                let deviceSerialNumber = string payload.SourceDevice        

                let weatherStation = 
                    weatherStationsTable
                        .Where( fun station -> station.PartitionKey = DefaultPartition && station.RowKey = deviceSerialNumber )
                        .ToArray()
                        .Single()

                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword payload log

                let reading = buildReading payload
                
                log.Info(sprintf "%A" reading)

                return (Some reading, req.CreateResponse(wundergroundResponse))
            } 
            |> Async.RunSynchronously

    if readingResponse.IsSome then 
        storedReading <- readingResponse.Value
    httpResponse