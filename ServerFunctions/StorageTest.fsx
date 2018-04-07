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


let reIndexTable sourceTableName destinationTableName =

    let sourceTable = tableClient.GetTableReference(sourceTableName)
    let desitinationTable = tableClient.GetTableReference(destinationTableName)
    
    if desitinationTable.CreateIfNotExists() then printfn "Created table %s" sourceTableName

    let query = sourceTable.CreateQuery<Reading>()

    let rec loop (cont: TableContinuationToken) = async {
        let! result = sourceTable.ExecuteQuerySegmentedAsync(query, cont) |> Async.AwaitTask
        
        let batches =
            result.Results
            |> Seq.chunkBySize 100

        for batch in batches do
            let batchOperation = TableBatchOperation()
            for reading in batch do
                let newRowKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - reading.DeviceTime.Ticks)
                printfn "%s %d %s" (string reading.Timestamp) reading.Timestamp.Ticks newRowKey
                reading.RowKey <- newRowKey
                batchOperation.Insert(reading)
        
            let! insertResults = desitinationTable.ExecuteBatchAsync( batchOperation ) |> Async.AwaitTask
            for insertResult in insertResults do printfn "%A" insertResult

        if (isNull >> not) result.ContinuationToken then
            printfn "Next page."
            do! loop result.ContinuationToken
    }

    loop null

reIndexTable "EndOfOctoberReadings" "Readings" |> Async.RunSynchronously  
