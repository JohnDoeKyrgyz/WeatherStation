#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs.dll"

let DefaultPartition = "Devices"

[<CLIMutable>]
type WeatherStation = {
    PartitionKey: string
    RowKey: string
    WundergroundStationId: string
    WundergroundPassword: string
}