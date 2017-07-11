#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"
open Microsoft.WindowsAzure.Storage.Table;

let DefaultPartition = "Devices"

type WeatherStation() =
    inherit TableEntity()
    member val WundergroundStationId = "" with get, set
    member val WundergroundPassword = "" with get, set