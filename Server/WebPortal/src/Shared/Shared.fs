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
[<CLIMutable>]
type StationKey = {
    DeviceId : string
    DeviceType : string
}

type StationDetails = {
    Key : StationKey
    Name : string
    WundergroundId : string
    Location : Location
    LastReading : DateTime option
    Readings : Reading list
}

type Station = {
    Key : StationKey
    Name : string
    WundergroundId : string
    Location : Location
    Status : Status
}

