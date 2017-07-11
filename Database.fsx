#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs.dll"

[<CLIMutable>]
type WeatherStation = {
    DeviceSerialNumber: string
    WundergroundStationId: string
    WundergroundPassword: string
}