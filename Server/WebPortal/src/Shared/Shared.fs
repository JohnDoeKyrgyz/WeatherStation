namespace WeatherStation.Shared

open System

type Location = {
    Latitude : decimal
    Longitude : decimal
}

type Status =
    | Active
    | Offline
    with
        override this.ToString() =
            match this with
            | Active -> "Active"
            | Offline -> "Offline"

type Reading = {
    DeviceTime : DateTime
    ReadingTime : DateTime
    SupplyVoltage : double
    BatteryChargeVoltage : double
    PanelVoltage : double
    TemperatureCelciusHydrometer : double
    TemperatureCelciusBarometer : double
    HumidityPercentHydrometer : double
    HumidityPercentBarometer : double
    PressurePascal : double
    GustMetersPerSecond : double
    SpeedMetersPerSecond : double
    DirectionDegrees : double
}

type StationDetails = {
    Name : string
    WundergroundId : string
    DeviceId : string
    Location : Location
    LastReading : DateTime option
    Readings : Reading list
}

type Station = {
    Name : string
    WundergroundId : string
    DeviceType : string
    DeviceId : string
    Location : Location
    Status : Status
}

