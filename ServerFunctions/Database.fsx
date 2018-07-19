#load "Preamble.fsx"

open System
open Microsoft.WindowsAzure.Storage.Table;

type WeatherStation() =
    inherit TableEntity()
    member val WundergroundStationId = "" with get, set
    member val WundergroundPassword = "" with get, set

    member val DirectionOffsetDegrees = new Nullable<int>() with get, set

let nullable v =
    match v with
    | Some v -> new Nullable<'T>(v)
    | None -> new Nullable<'T>()

type Reading() =
    inherit TableEntity()
    member val RefreshIntervalSeconds = 0 with get, set
    member val DeviceTime = DateTime.MinValue with get, set
    member val ReadingTime = DateTime.MinValue with get, set
    member val SupplyVoltage = new Nullable<double>() with get, set
    member val BatteryChargeVoltage = new Nullable<double>() with get, set
    member val PanelVoltage = new Nullable<double>() with get, set
    member val TemperatureCelciusHydrometer = new Nullable<double>() with get, set
    member val TemperatureCelciusBarometer = new Nullable<double>() with get, set
    member val HumidityPercentHydrometer = new Nullable<double>() with get, set
    member val HumidityPercentBarometer = new Nullable<double>() with get, set
    member val PressurePascal = new Nullable<double>() with get, set
    member val GustMetersPerSecond = new Nullable<double>() with get, set
    member val SpeedMetersPerSecond = new Nullable<double>() with get, set
    member val DirectionSixteenths = new Nullable<double>() with get, set
    member val SourceDevice = "" with get, set