#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs.dll"

[<CLIMutable>]
type WeatherStation = {
    PartitionKey: string
    RowKey: string
    WundergroundStationId: string
    WundergroundPassword: string
}