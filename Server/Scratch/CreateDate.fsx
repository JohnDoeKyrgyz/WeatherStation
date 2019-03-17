#load "Preamble.fsx"
#load @"..\DataModel\Model.fs"

open Microsoft.WindowsAzure.Storage

open FSharp.Azure.Storage.Table
open System

open WeatherStation.Model

[<Literal>]
let SettingsJson = __SOURCE_DIRECTORY__ + @"\..\ServerFunctions\local.settings.json"
type LocalSettings = FSharp.Data.JsonProvider< SettingsJson >

let connect connectionString =
    let storageAccount = CloudStorageAccount.Parse connectionString
    storageAccount.CreateCloudTableClient()

let connection = connect (LocalSettings.GetSample()).ConnectionStrings.WeatherStationStorage.ConnectionString

let findCreateDate deviceId =
    let readings =
        Query.all<Reading>
        |> Query.where <@ fun reading key -> key.PartitionKey = deviceId @>
        |> fromTableAsync connection "Readings"
        |> Async.RunSynchronously

    match [for reading, _ in readings -> reading.DeviceTime] with
    | [] -> None
    | dates -> dates |> Seq.min |> Some
let weatherStations =
    Query.all<WeatherStation>
    |> fromTableAsync connection "WeatherStations"
    |> Async.RunSynchronously

for weatherStation, _ in weatherStations do
    let createDate = defaultArg (findCreateDate weatherStation.DeviceId) DateTime.Now
    printfn "%s, %A" weatherStation.DeviceId createDate
    InsertOrReplace {weatherStation with CreatedOn = createDate}
    |> inTableAsync connection "WeatherStations"
    |> Async.RunSynchronously
    |> ignore

    