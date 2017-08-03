#load "Database.fsx"

open Database
open System
open System.IO
open System.Collections.Generic

open Microsoft.WindowsAzure.Storage // Namespace for CloudStorageAccount
open Microsoft.WindowsAzure.Storage.Table // Namespace for Table storage types

[<Literal>]
let WeatherStationStorageConnection = "DefaultEndpointsProtocol=https;AccountName=weatherstationsstorage;AccountKey=GQTMcwjw39JA7cY3wrdaRHdt6ApfafgiXrozLE27J5WpDiTEFs7yPlCtolWR6dEq3bfw4gccvwaeglzYOcpWmA==;BlobEndpoint=https://weatherstationsstorage.blob.core.windows.net/;QueueEndpoint=https://weatherstationsstorage.queue.core.windows.net/;TableEndpoint=https://weatherstationsstorage.table.core.windows.net/;FileEndpoint=https://weatherstationsstorage.file.core.windows.net/;"
let storageAccount = CloudStorageAccount.Parse(WeatherStationStorageConnection)
let tableClient = storageAccount.CreateCloudTableClient()


let readings = tableClient.GetTableReference("Readings")
let query = readings.CreateQuery<Reading>()
query.SelectColumns <- new List<string>(["ReadingTime"; "BatteryVoltage"])
let results =
    query.Execute()
    |> Seq.toList
    |> List.sortByDescending (fun reading -> reading.ReadingTime )
    |> List.take 20

for result in results do
    printfn "%A %d" result.ReadingTime (result.BatteryVoltage.GetValueOrDefault(-1))

(*
let reading = 
    Reading(
        PartitionKey = "TEST",
        RowKey = "2",
        BatteryVoltage = nullable (Some 1),
        RefreshIntervalSeconds = 1,
        DeviceTime = DateTime.MaxValue,
        ReadingTime = DateTime.MaxValue,
        SupplyVoltage = nullable (Some 1),
        ChargeVoltage = nullable (Some 1),
        TemperatureCelciusHydrometer = nullable (Some (double 1.1m)),
        TemperatureCelciusBarometer = nullable (Some (double 1.1m)),
        HumidityPercent = nullable (Some (double 1.1m)),
        PressurePascal = nullable (Some (double 1.1m)),
        SpeedMetersPerSecond = nullable (Some (double 1.1m)),
        DirectionSixteenths = nullable (Some (double 1.1m)),
        SourceDevice = "source")
let table = tableClient.GetTableReference("Readings")
table.CreateIfNotExists()

let insert = TableOperation.Insert(reading)

let result = table.Execute(insert)

printfn "%A" result
*)