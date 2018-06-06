#load @"../Preamble.fsx"
#load "Particle.fsx"
#load "../Database.fsx"

open System.Diagnostics
open System.IO

open Microsoft.WindowsAzure.Storage.Table
open Microsoft.WindowsAzure.Storage
open Microsoft.Azure.WebJobs.Host

open Model
open Particle
open FSharp.Data

let connect connectionString =
    let storageAccount = CloudStorageAccount.Parse connectionString
    storageAccount.CreateCloudTableClient()

[<Literal>]
let SettingsJson = __SOURCE_DIRECTORY__ + @"\..\local.settings.json"
type LocalSettings = JsonProvider< SettingsJson >

let log = {
    new TraceWriter(TraceLevel.Info) with
        override this.Trace traceEvent = printf "%A" traceEvent
        override this.Flush() = ()
 }


let content = File.ReadAllText "ParticleStatusUpdate.json"
let values = parseValues log content

let reading = createReading values

let settings = LocalSettings.GetSample()
let connectionString = settings.ConnectionStrings.WeatherStationStorage.ConnectionString

let connection = connect connectionString
let readingsTableRef = connection.GetTableReference("Readings")

let insertOperation = TableOperation.Insert(reading);

// Execute the insert operation.
readingsTableRef.Execute(insertOperation);
