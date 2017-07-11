#I __SOURCE_DIRECTORY__
#load "../Preamble.fsx"
#load "../Database.fsx"

open System
open System.Net
open System.Net.Http
open Newtonsoft.Json
open FSharp.Data
open Microsoft.Azure.WebJobs.Host
open Microsoft.Azure.WebJobs

open Database

let isValid v = String.IsNullOrWhiteSpace v |> not

let Run(req: HttpRequestMessage, weatherStationsTable: ICollector<WeatherStation>, log: TraceWriter) =
    async {

        let! formData = req.Content.ReadAsFormDataAsync() |> Async.AwaitTask

        let device = 
            WeatherStation(
                PartitionKey = DefaultPartition,
                RowKey = formData.["DeviceSerialNumber"],
                WundergroundStationId = formData.["WundergroundStationId"],
                WundergroundPassword = formData.["WundergroundPassword"])

        if isValid device.PartitionKey && isValid device.RowKey && isValid device.WundergroundStationId && isValid device.WundergroundPassword then
            weatherStationsTable.Add( device )
            log.Info( sprintf "Added record: %A" device )
            return req.CreateResponse(HttpStatusCode.OK)
        else
            return req.CreateErrorResponse(HttpStatusCode.BadRequest, "Invalid data")
            
    } |> Async.StartAsTask
