namespace WeatherStation.Shared

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

type Station = {
    Name : string
    WundergroundId : string
    DeviceId : string
    Location : Location
    Status : Status
}

