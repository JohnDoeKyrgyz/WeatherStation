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

let Run(req: HttpRequestMessage, weatherStationsTable: ICollector<WeatherStation>, log: TraceWriter) =
    async {

        log.Info( sprintf "%A" (req.Content.ReadAsStringAsync() |> Async.AwaitTask |> Async.RunSynchronously) )

        let! formData = req.Content.ReadAsFormDataAsync() |> Async.AwaitTask

        let device = {
            PartitionKey = "Devices"
            RowKey = formData.["DeviceSerialNumber"]
            WundergroundStationId = formData.["WundergroundStationId"]
            WundergroundPassword = formData.["WundergroundPassword"]
        }
        
        weatherStationsTable.Add( device )

        log.Info( sprintf "Added record: %A" device )

        return req.CreateResponse(HttpStatusCode.OK)
            
    } |> Async.StartAsTask
