namespace WeatherStations.Shared

type Counter = int

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
    Location : Location
    Status : Status
}

