#load "Preamble.fsx"

#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"

open System
open Microsoft.WindowsAzure.Storage.Table;

let DefaultPartition = "Devices"

type WeatherStation() =
    inherit TableEntity()
    member val WundergroundStationId = "" with get, set
    member val WundergroundPassword = "" with get, set

let nullable v =
    match v with
    | Some v -> new Nullable<'T>(v)
    | None -> new Nullable<'T>()

type Reading() =
    inherit TableEntity()
    member val RefreshIntervalSeconds = 0 with get, set
    member val DeviceTime = DateTime.MinValue with get, set
    member val ReadingTime = DateTime.MinValue with get, set
    member val SupplyVoltage = new Nullable<int>() with get, set
    member val BatteryVoltage = new Nullable<int>() with get, set
    member val ChargeVoltage = new Nullable<int>() with get, set
    member val TemperatureCelciusHydrometer = new Nullable<double>() with get, set
    member val TemperatureCelciusBarometer = new Nullable<double>() with get, set
    member val HumidityPercent = new Nullable<double>() with get, set
    member val PressurePascal = new Nullable<double>() with get, set
    member val SpeedMetersPerSecond = new Nullable<double>() with get, set
    member val DirectionSixteenths = new Nullable<double>() with get, set
    member val SourceDevice = "" with get, set