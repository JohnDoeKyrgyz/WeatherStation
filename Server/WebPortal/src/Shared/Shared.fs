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
    WundergroundId : string option
    Location : Location
    LastReading : DateTime option
    Readings : Reading list
}

type Station = {
    Key : StationKey
    Name : string
    WundergroundId : string option
    Location : Location
    Status : Status
}

type StationSettings = {
    Brownout : bool
    BrownoutVoltage : decimal
    BrownoutMinutes : int
    SleepTime : int
    DiagnosticCycles : int
    UseDeepSleep : bool    
}
with
    static member Default = {Brownout = true; BrownoutVoltage = 4.7m; BrownoutMinutes = 2880; SleepTime = 360; DiagnosticCycles = 0; UseDeepSleep = true}
