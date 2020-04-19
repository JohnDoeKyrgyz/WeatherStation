namespace WeatherStation.Client.Pages
module Pages =

    open Elmish.UrlParser
    open WeatherStation.Shared

    /// The different pages of the application. If you add a new page, then add an entry here.
    [<RequireQualifiedAccess>]
    type Page =
        | Home
        | Device of StationKey
        | AddDevice

    let toPath =
        function
        | Page.Home -> "#home"
        | Page.AddDevice -> "#adddevice"
        | Page.Device key -> sprintf "#device/%s/%s" (string key.DeviceType) key.DeviceId

    let parseDeviceType =
        custom "DeviceType" <| fun segment ->
            match segment.ToLowerInvariant() with
            | "particle" -> Ok Particle
            | "test" -> Ok Test
            | _ -> Error (sprintf "Could not parse DeviceType %s" segment)

    /// The URL is turned into a Result.
    let pageParser : Parser<Page -> Page,_> =
        oneOf [
            map Page.Home (s "home")
            map Page.AddDevice (s "adddevice")
            map (fun deviceType deviceId -> Page.Device({DeviceType = deviceType; DeviceId = deviceId})) (s "device" </> parseDeviceType </> str)]

    let urlParser location =
        let result = parsePath pageParser location
        result