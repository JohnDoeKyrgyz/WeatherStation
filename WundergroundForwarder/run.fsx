#if INTERACTIVE
#I @"..\node_modules\azure-functions-core-tools\bin"

#r "Microsoft.Azure.Webjobs.Host.dll"
open Microsoft.Azure.WebJobs.Host
open System

#r "System.Net.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "System.Web.Http.dll"
#r "Newtonsoft.Json.dll"
#r @"..\packages\FSharp.Data\lib\net40\FSharp.Data.dll"
#else
#r "FSharp.Data.dll"
#endif

#r "System.Net.Http"
#r "Newtonsoft.Json"



open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data

[<Literal>]
let Sample = __SOURCE_DIRECTORY__ + "/StatusUpdate.json"
type Payload = JsonProvider<Sample, SampleIsList = true>

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        log.Info("Function invoked")

        let! content = req.Content.ReadAsStringAsync() |> Async.AwaitTask

        let payload = Payload.Parse content        
        let reading = payload.Body
        let dateUtc = reading.Time.ToUniversalTime().ToString("yyyy-mm-dd hh:mm:ss")
        let windSpeed = (defaultArg reading.SpeedMetersPerSecond 0.0m) * 2.23694m
        let windDirection = (defaultArg reading.DirectionSixteenths 0.0m) * (360.0m / 16.0m)
        let temperature = (defaultArg reading.TemperatureCelciusHydrometer 0.0m) * 9.0m/5.0m  + 32.0m
        let barometer = (defaultArg reading.PressurePascal 0.0m) * 0.0002953m

        let url = "https://weatherstation.wunderground.com/weatherstation/updateweatherstation.php"
        let queryParameters = [ 
            "ID", payload.Body.StationId
            "PASSWORD", payload.Body.Password
            "action", "updateraw"
            "dateutc", dateUtc
            "realtime", "1"
            "windspeedmph", string windSpeed
            "winddir", string windDirection
            "tempf", string temperature
            "humidity", defaultArg reading.HumidityPercent 0.0m |> string
            "baromin", string barometer]

        log.Info( sprintf "Wunderground Request: %A" queryParameters )

        let! wundergroundResponse = Http.AsyncRequest( url, queryParameters )

        log.Info( sprintf "Wunderground Response: %A" wundergroundResponse )

        return req.CreateResponse(wundergroundResponse)
    } |> Async.RunSynchronously
