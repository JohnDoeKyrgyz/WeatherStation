namespace WeatherStation.Tests.Functions
module DataSetup =
    open System
    open WeatherStation
    open WeatherStation.Model
    open FSharp.Azure.Storage
    open FSharp.Azure.Storage.Table
    open Microsoft.WindowsAzure.Storage.Table

    let connectionString = "UseDevelopmentStorage=true"

    let initialize() =
        Environment.SetEnvironmentVariable("WeatherStationStorage", connectionString)

    let internal doClear<'TRecord> (connection : CloudTableClient) tableName =
        let tableReference = connection.GetTableReference tableName
        async {
            let! exists = tableReference.ExistsAsync() |> Async.AwaitTask
            if  exists then
                let! recordsToDelete =
                    Query.all<'TRecord>
                    |> Table.fromTableAsync connection tableName
                let recordsToDelete = recordsToDelete |> Seq.toList
                if recordsToDelete.Length > 0 then
                    do!                    
                        [for entity, metadata in recordsToDelete -> Table.Operation.Delete(entity, metadata.Etag)]
                        |> Table.inTableAsBatchAsync connection tableName
                        |> Async.Ignore
        }

    let clear<'TRecord> tableName =
        let connection = AzureStorage.createConnection connectionString
        doClear<'TRecord> connection tableName        

    let load<'TRecord> tableName records =
        let connection = AzureStorage.createConnection connectionString
        let tableReference = connection.GetTableReference tableName
        async {
            let! exists = tableReference.ExistsAsync() |> Async.AwaitTask
            if not exists then do! tableReference.CreateAsync() |> Async.AwaitTask |> Async.Ignore
            else do! doClear<'TRecord> connection tableName
            
            let! results = Table.inTableAsBatchAsync connection tableName [for record : 'TRecord in records -> Insert record]
            return results
        }    

    let loadWeatherStations records =
        async {
            let! results =
                load<WeatherStation> "WeatherStations" records
                |> Async.Catch
            printfn "%A" results
        }
        
    let clearReadings = clear<Reading> "Readings"        