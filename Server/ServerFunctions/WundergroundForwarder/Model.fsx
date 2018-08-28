#load "../Database.fsx"

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

[<Measure>]
type volts

[<Measure>]
type celcius

[<Measure>]
type percent

[<Measure>]
type pascal

[<Measure>]
type metersPerSecond

[<Measure>]
type sixteenths

[<Measure>]
type degrees

[<Measure>]
type seconds

let degreesPerSixteenth = 22.5<degrees/sixteenths>

type ReadingValues =
    | ReadingTime of DateTime
    | DeviceTime of DateTime
    | SupplyVoltage of decimal<volts>
    | BatteryChargeVoltage of decimal<volts>
    | PanelVoltage of decimal<volts>
    | TemperatureCelciusHydrometer of decimal<celcius>
    | TemperatureCelciusBarometer of decimal<celcius>
    | HumidityPercentHydrometer of decimal<percent>
    | HumidityPercentBarometer of decimal<percent>
    | PressurePascal of decimal<pascal>
    | SpeedMetersPerSecond of decimal<metersPerSecond>
    | GustMetersPerSecond of decimal<metersPerSecond>
    | DirectionSixteenths of int<sixteenths>
    | RefreshInterval of int<seconds>

type DeviceReadings = {
    DeviceId : string
    Readings : ReadingValues list
}    

let applyReading (reading : Reading) value =

    let toDouble (v : decimal<'T>) = 
        let cleanV = (v / LanguagePrimitives.DecimalWithMeasure<'T> 1.0m)
        new Nullable<double>(double cleanV)

    match value with
    | RefreshInterval seconds -> reading.RefreshIntervalSeconds <- seconds / 1<seconds>
    | ReadingTime time -> reading.ReadingTime <- time.ToUniversalTime()
    | DeviceTime time -> reading.DeviceTime <- time.ToUniversalTime()
    | SupplyVoltage voltage -> reading.SupplyVoltage <- toDouble(voltage)
    | BatteryChargeVoltage voltage -> reading.BatteryChargeVoltage <- toDouble(voltage)
    | PanelVoltage voltage -> reading.PanelVoltage <- toDouble(voltage)
    | TemperatureCelciusBarometer temp -> reading.TemperatureCelciusBarometer <- toDouble(temp)
    | TemperatureCelciusHydrometer temp -> reading.TemperatureCelciusHydrometer <- toDouble(temp)
    | HumidityPercentHydrometer perc -> reading.HumidityPercentHydrometer <- toDouble(perc)
    | HumidityPercentBarometer perc -> reading.HumidityPercentBarometer <- toDouble(perc)
    | PressurePascal perc -> reading.PressurePascal <- toDouble(perc)
    | SpeedMetersPerSecond speed -> reading.SpeedMetersPerSecond <- toDouble(speed)
    | GustMetersPerSecond speed -> reading.GustMetersPerSecond <- toDouble(speed)
    | DirectionSixteenths direction -> reading.DirectionSixteenths <- new Nullable<double>(double direction)

let createReading deviceReading = 
    let reading = new Reading(RowKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks))
    deviceReading.Readings |> List.iter (applyReading reading)
    if reading.DeviceTime = DateTime.MinValue then reading.DeviceTime <- reading.ReadingTime    
    reading.PartitionKey <- deviceReading.DeviceId
    reading.SourceDevice <- deviceReading.DeviceId
    reading