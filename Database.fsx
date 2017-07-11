#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs.dll"
#r "Microsoft.WindowsAzure.Storage.dll"
open Microsoft.WindowsAzure.Storage.Table;

let DefaultPartition = "Devices"

type WeatherStation() =
    inherit TableEntity()
    member val WundergroundStationId = "" with get, set
    member val WundergroundPassword = "" with get, set