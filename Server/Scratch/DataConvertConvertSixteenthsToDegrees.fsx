#load "Preamble.fsx"

open Microsoft.WindowsAzure.Storage

open FSharp.Data
open FSharp.Azure.Storage.Table
open System

type CurrentReading = {
    RefreshIntervalSeconds : int
    DeviceTime : DateTime
    ReadingTime : DateTime
    SupplyVoltage : double
    BatteryChargeVoltage : double
    PanelVoltage : double
    TemperatureCelciusHydrometer : double
    TemperatureCelciusBarometer : double
    HumidityPercentHydrometer : double
    HumidityPercentBarometer : double
    PressurePascal : double
    GustMetersPerSecond : double
    SpeedMetersPerSecond : double
    DirectionDegrees : double
    [<PartitionKey>]
    SourceDevice : string
    [<RowKey>]
    RowKey : string
}

type OldReading = {
    RefreshIntervalSeconds : int
    DeviceTime : DateTime
    ReadingTime : DateTime
    SupplyVoltage : double
    BatteryChargeVoltage : double
    PanelVoltage : double
    TemperatureCelciusHydrometer : double
    TemperatureCelciusBarometer : double
    HumidityPercentHydrometer : double
    HumidityPercentBarometer : double
    PressurePascal : double
    GustMetersPerSecond : double
    SpeedMetersPerSecond : double
    DirectionSixteenths : double
    [<PartitionKey>]
    SourceDevice : string
    [<RowKey>]
    RowKey : string
}

let convert oldReading =
    {
        RefreshIntervalSeconds = oldReading.RefreshIntervalSeconds
        DeviceTime = oldReading.DeviceTime
        ReadingTime = oldReading.ReadingTime
        SupplyVoltage = oldReading.SupplyVoltage
        BatteryChargeVoltage = oldReading.BatteryChargeVoltage
        PanelVoltage = oldReading.PanelVoltage
        TemperatureCelciusHydrometer = oldReading.TemperatureCelciusHydrometer
        TemperatureCelciusBarometer = oldReading.TemperatureCelciusBarometer
        HumidityPercentHydrometer = oldReading.HumidityPercentHydrometer
        HumidityPercentBarometer = oldReading.HumidityPercentBarometer
        PressurePascal = oldReading.PressurePascal
        GustMetersPerSecond = oldReading.GustMetersPerSecond
        SpeedMetersPerSecond = oldReading.SpeedMetersPerSecond
        DirectionDegrees = oldReading.DirectionSixteenths * (360.0 / 16.0)
        SourceDevice = oldReading.SourceDevice
        RowKey = oldReading.RowKey
    }

[<Literal>]
let SettingsJson = __SOURCE_DIRECTORY__ + @"\..\ServerFunctions\local.settings.json"
type LocalSettings = FSharp.Data.JsonProvider< SettingsJson >

let connect connectionString =
    let storageAccount = CloudStorageAccount.Parse connectionString
    storageAccount.CreateCloudTableClient()

let connection = connect (LocalSettings.GetSample()).ConnectionStrings.WeatherStationStorage.ConnectionString

let processSegment processor =
    let rec doProcess token =
        async{
            let! results, nextToken = 
                Query.all<OldReading>
                |> fromTableSegmentedAsync connection "Readings" token

            do! processor results

            if nextToken.IsSome then do! doProcess nextToken
        }
    doProcess None

let saveToNewTable tableName data =
    let insertBatch values =
        values
        |> Seq.chunkBySize 100
        |> Seq.map (fun batch ->
            batch
            |> Seq.map (convert >> InsertOrReplace)
            |> inTableAsBatchAsync connection tableName
            |> Async.Ignore)
        |> Async.Parallel
        |> Async.Ignore
    
    printfn "Batch Size %d" (data |> Seq.length)
    data |> Seq.map fst |> Seq.groupBy (fun (v : OldReading) -> v.SourceDevice) |> Seq.map (snd >> insertBatch)
    |> Async.Parallel
    |> Async.Ignore
        

async {
    let tableName = "ConvertedReadings"
    let tableReference = connection.GetTableReference(tableName)

    let! deleteResult = tableReference.DeleteIfExistsAsync() |> Async.AwaitTask
    printfn "DeleteTable %s %A" tableName deleteResult

    if(deleteResult) then do! Async.Sleep 5000

    let! createResult = tableReference.CreateIfNotExistsAsync() |> Async.AwaitTask
    printfn "CreateTable %s %A" tableName createResult

    do! processSegment (saveToNewTable tableName)
}
|> Async.RunSynchronously