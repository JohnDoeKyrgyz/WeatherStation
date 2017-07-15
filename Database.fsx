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

type Reading() =
    inherit TableEntity()
    member val RefreshIntervalSeconds = 0 with get, set
    member val DeviceTime = DateTime.MinValue with get, set
    member val ReadingTime = DateTime.MinValue with get, set
    member val SupplyVoltage = Option<int>.None with get, set
    member val BatteryVoltage = Option<int>.None with get, set
    member val ChargeVoltage = Option<int>.None with get, set
    member val TemperatureCelciusHydrometer = Option<decimal>.None with get, set
    member val TemperatureCelciusBarometer = Option<decimal>.None with get, set
    member val HumidityPercent = Option<decimal>.None with get, set
    member val PressurePascal = Option<decimal>.None with get, set
    member val SpeedMetersPerSecond = Option<decimal>.None with get, set
    member val DirectionSixteenths = Option<decimal>.None with get, set
    member val SourceDevice = "" with get, set