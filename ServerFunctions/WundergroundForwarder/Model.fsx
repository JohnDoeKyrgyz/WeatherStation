#load @"..\Database.fsx"

open System
open Database

type DevicePlatform =
    | Particle
    | Hologram
    with
        override this.ToString() =
            match this with
            | Particle -> "Particle"
            | Hologram -> "Hologram"

type ReadingValues =
    | ReadingTime of DateTime
    | DeviceTime of DateTime
    | SupplyVoltage of double
    | BatteryChargeVoltage of double
    | PanelVoltage of double
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
    | ReadingTime time -> reading.ReadingTime <- time.ToUniversalTime()
    | DeviceTime time -> reading.DeviceTime <- time.ToUniversalTime()
    | SupplyVoltage voltage -> reading.SupplyVoltage <- new Nullable<double>(voltage)
    | BatteryChargeVoltage voltage -> reading.BatteryChargeVoltage <- new Nullable<double>(voltage)
    | PanelVoltage voltage -> reading.PanelVoltage <- new Nullable<double>(voltage)
    | TemperatureCelciusBarometer temp -> reading.TemperatureCelciusBarometer <- new Nullable<double>(temp)
    | TemperatureCelciusHydrometer temp -> reading.TemperatureCelciusHydrometer <- new Nullable<double>(temp)
    | HumidityPercent perc -> reading.HumidityPercent <- new Nullable<double>(perc)
    | PressurePascal perc -> reading.PressurePascal <- new Nullable<double>(perc)
    | SpeedMetersPerSecond speed -> reading.SpeedMetersPerSecond <- new Nullable<double>(speed)
    | DirectionSixteenths direction -> reading.DirectionSixteenths <- new Nullable<double>(double direction)
    | DeviceId id -> 
        reading.PartitionKey <- id
        reading.SourceDevice <- id

let createReading values = 
    let reading = new Reading(RowKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks))
    values |> List.iter (applyReading reading)
    if reading.DeviceTime = DateTime.MinValue then reading.DeviceTime <- reading.ReadingTime
    reading