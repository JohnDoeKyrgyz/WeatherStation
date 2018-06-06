#load @"..\Database.fsx"

open System
open Database

type DevicePlatform =
    | Particle
    | Hologram

type ReadingValues =
    | ReadingTime of DateTime
    | SupplyVoltage of int
    | BatteryChargeVoltage of int
    | PanelVoltage of int
    | TemperatureCelciusHydrometer of double
    | TemperatureCelciusBarometer of double
    | HumidityPercent of double
    | PressurePascal of double
    | SpeedMetersPerSecond of double
    | DirectionSixteenths of int
    | DeviceId of string
    | RefreshInterval of int

let applyReading (reading : Reading) value =
    match value with
    | RefreshInterval seconds -> reading.RefreshIntervalSeconds <- seconds
    | ReadingTime time -> reading.ReadingTime <- time
    | SupplyVoltage voltage -> reading.SupplyVoltage <- new Nullable<int>(voltage)
    | BatteryChargeVoltage voltage -> reading.BatteryChargeVoltage <- new Nullable<int>(voltage)
    | PanelVoltage voltage -> reading.PanelVoltage <- new Nullable<int>(voltage)
    | TemperatureCelciusBarometer temp -> reading.TemperatureCelciusBarometer <- new Nullable<double>(temp)
    | TemperatureCelciusHydrometer temp -> reading.TemperatureCelciusHydrometer <- new Nullable<double>(temp)
    | HumidityPercent perc -> reading.HumidityPercent <- new Nullable<double>(perc)
    | PressurePascal perc -> reading.PressurePascal <- new Nullable<double>(perc)
    | SpeedMetersPerSecond speed -> reading.SpeedMetersPerSecond <- new Nullable<double>(speed)
    | DirectionSixteenths direction -> reading.DirectionSixteenths <- new Nullable<double>(double direction)
    | DeviceId id -> reading.PartitionKey <- id

let createReading values = 
    let reading = new Reading(RowKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks))
    values |> List.iter (applyReading reading)
    reading