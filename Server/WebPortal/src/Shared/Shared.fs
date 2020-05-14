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

type BatteryState =
    | Unknown = 0
    | NotCharging = 1
    | Charging = 2
    | Charged = 3
    | Discharging = 4
    | Faulted = 5
    | Disconnected = 6

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
    BatteryState : BatteryState

}

type StatusMessage = {
    Message : string
    CreatedOn : DateTime
}

type DeviceType =
    | Particle
    | Test
    with
        static member Parse value =
            match value with
            | "Particle" -> Particle
            | "Test" -> Test
            | _ -> failwithf "%s is not a valid DeviceType" value

[<CLIMutable>]
type StationKey = {
    DeviceId : string
    DeviceType : DeviceType
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
    StatusMessages : StatusMessage list
}

type Station = {
    Key : StationKey
    Name : string
    WundergroundId : string option
    Location : Location
    Status : Status
}

type FirmwareSettings = {
    Version : int
    Brownout : bool
    BrownoutPercentage : decimal
    ResumePercentage : decimal
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
        ResumePercentage = 0.8m
        BrownoutMinutes = 2880
        SleepTime = 360
        DiagnosticCycles = 0
        UseDeepSleep = true
        Version = 1
        PanelOffMinutes = 120}

type DataPage = {
    Readings : list<Reading>
    Messages : list<StatusMessage>
}

module UrlDateTime =
    open System.Globalization

    let UrlDateTimeFormat = "yyyyMMddTHHmmssZ"
    let toUrlDate (value : DateTime) = value.ToString(UrlDateTimeFormat)
    let fromUrlDate (value : string) = DateTime.ParseExact(value, UrlDateTimeFormat, CultureInfo.InvariantCulture)