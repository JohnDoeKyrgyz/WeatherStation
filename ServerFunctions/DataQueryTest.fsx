#load "Preamble.fsx"
#load "Database.fsx"

open System

open Microsoft.WindowsAzure.Storage

open FSharp.Data

open Database

let connect connectionString =
    let storageAccount = CloudStorageAccount.Parse connectionString
    storageAccount.CreateCloudTableClient()

[<Literal>]
let SettingsJson = __SOURCE_DIRECTORY__ + @"\local.settings.json"
type LocalSettings = JsonProvider< SettingsJson >

let settings = LocalSettings.GetSample()
let connectionString = settings.ConnectionStrings.WeatherStationStorage.ConnectionString

let connection = connect connectionString
let readingsTableRef = connection.GetTableReference("Readings")
let readingsQuery = readingsTableRef.CreateQuery<Reading>()

let recentReadings =
    let tenMinutesAgo = DateTime.Now.ToUniversalTime().Subtract(TimeSpan.FromDays(4.0))
    query {
        for reading in readingsQuery do
        where (reading.ReadingTime > tenMinutesAgo )
    }

printfn "Time, BatteryVoltage, PanelVoltage"
for reading in recentReadings do
    printfn "%A, %A, %A" reading.ReadingTime reading.BatteryChargeVoltage reading.PanelVoltage
