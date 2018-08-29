#I @"C:\Users\jatwood\.nuget\packages\"
//#r @"bin\Debug\netstandard2.0\DataModel.dll"
#r @"windowsazure.storage\9.3.1\lib\netstandard1.3\Microsoft.WindowsAzure.Storage.dll"
#r @"fsharp.data\2.4.6\lib\net45\FSharp.Data.dll"
#r @"newtonsoft.json\11.0.2\lib\netstandard2.0\Newtonsoft.Json.dll"
#r @"fsharp.azure.storage\3.0.0\lib\netstandard2.0\FSharp.Azure.Storage.dll"
#load "Model.fs"

open System

open Microsoft.WindowsAzure.Storage

open FSharp.Data
open FSharp.Azure.Storage.Table
open WeatherStation.Model

let connect connectionString =
    let storageAccount = CloudStorageAccount.Parse connectionString
    storageAccount.CreateCloudTableClient()

[<Literal>]
let SettingsJson = __SOURCE_DIRECTORY__ + @"\..\ServerFunctions\local.settings.json"
type LocalSettings = JsonProvider< SettingsJson >

let settings = LocalSettings.GetSample()
let connectionString = settings.ConnectionStrings.WeatherStationStorage.ConnectionString

let connection = connect connectionString

async {
    let! results =
        Query.all<WeatherStation>
        |> fromTableAsync connection "WeatherStations"

    for entity, metadata in results do printfn "Entity: %A, Metadata: %A" entity metadata
}
|> Async.RunSynchronously

let refreshTestTable tableName =
    let testTable = connection.GetTableReference(tableName)
    let deleted = testTable.DeleteIfExistsAsync() |> Async.AwaitTask |> Async.RunSynchronously
    printfn "Delete %s %O" tableName deleted
    if deleted then Async.Sleep(3000) |> Async.RunSynchronously
    testTable.CreateAsync() |> Async.AwaitTask |> Async.RunSynchronously

refreshTestTable "TestWeatherStations"
let weatherStation = {
    DeviceType = Hologram
    DeviceId = "1234"
    WundergroundStationId = "abc"
    WundergroundPassword = "asd"
    DirectionOffsetDegrees = Some 0
    Latitude = 0.0
    Longitude = 0.0
    LastReading = DateTime.Now
}

Insert weatherStation
|> inTableAsync connection "TestWeatherStations"
|> Async.RunSynchronously


refreshTestTable "TestSettings"
InsertOrReplace {Group = "Default"; Key = "Foo"; Value = "Foo"}
|> inTableAsync connection "TestSettings"
|> Async.RunSynchronously