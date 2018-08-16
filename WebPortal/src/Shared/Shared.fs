namespace Shared

type Counter = int

type Location = {
    Latitude : decimal
    Longitude : decimal
}
type Station = {
    Name : string
    WundergroundId : string
    Location : Location
}

