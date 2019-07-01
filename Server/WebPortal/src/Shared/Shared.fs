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
    BatteryChargeVoltage : double
    PanelVoltage : double
    PanelMilliamps : double
    TemperatureCelciusHydrometer : double option
    TemperatureCelciusBarometer : double option
    HumidityPercentHydrometer : double option
    HumidityPercentBarometer : double option
    PressurePascal : double option
    GustMetersPerSecond : double option
    SpeedMetersPerSecond : double option
    DirectionDegrees : double option
}

type StatusMessage = {
    Message : string
    CreatedOn : DateTime
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
    CreatedOn : DateTime
    Readings : Reading list
    PageSizeHours : float
}

type Station = {
    Key : StationKey
    Name : string
    WundergroundId : string option
    Location : Location
    Status : Status
}

type StationSettings = {
    Version : int
    Brownout : bool
    BrownoutPercentage : decimal
    BrownoutMinutes : int
    SleepTime : int
    DiagnosticCycles : int
    UseDeepSleep : bool
    PanelOffMinutes : int
}
with
    static member Default = {
        Brownout = true
        BrownoutPercentage = 0.2m
        BrownoutMinutes = 2880
        SleepTime = 360
        DiagnosticCycles = 0
        UseDeepSleep = true
        Version = 1
        PanelOffMinutes = 120}

module UrlDateTime =
    open System.Globalization

    let UrlDateTimeFormat = "yyyyMMddTHHmmssZ"
    let toUrlDate (value : DateTime) = value.ToString(UrlDateTimeFormat)
    let fromUrlDate (value : string) = DateTime.ParseExact(value, UrlDateTimeFormat, CultureInfo.InvariantCulture)