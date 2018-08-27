#load "Preamble.fsx"

#r @"packages\Google.DataTable.Net.Wrapper/lib/Google.DataTable.Net.Wrapper.dll"
#r @"packages\XPlot.GoogleCharts/lib/net45/XPlot.GoogleCharts.dll"

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
    let tenMinutesAgo = DateTime.Now.ToUniversalTime().Subtract(TimeSpan.FromDays(10.0))
    query {
        for reading in readingsQuery do
        where (reading.ReadingTime > tenMinutesAgo )
    }
    |> Seq.toList
    |> List.sortByDescending (fun v -> v.ReadingTime)

printfn "Time, BatteryVoltage, PanelVoltage"
for reading in recentReadings do
    printfn "%A, %A, %A" reading.ReadingTime reading.BatteryChargeVoltage reading.PanelVoltage

open XPlot.GoogleCharts

let dateLabel (date : DateTime) = 
    let localTime = date.ToLocalTime()
    let localizedDate = new DateTime(date.Year, date.Month, date.Day, localTime.Hour, localTime.Minute, localTime.Second)
    localizedDate.ToString("MM/dd hh:mm:ss")

let batteryVoltage = [for reading in recentReadings -> dateLabel reading.ReadingTime, reading.BatteryChargeVoltage.GetValueOrDefault()]
let panelVoltages = [for reading in recentReadings -> dateLabel reading.ReadingTime, reading.PanelVoltage.GetValueOrDefault()]

let options =
  Options
    ( title = "Power", curveType = "function",
      legend = Legend(position = "bottom", alignment = "vertical") )
            
[batteryVoltage; panelVoltages]
|> Chart.Line
|> Chart.WithOptions options
|> Chart.WithLabels ["Battery"; "Panel"]
|> Chart.Show