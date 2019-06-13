namespace WeatherStation.Client.Pages
module Pages =

    open Elmish.UrlParser
    open WeatherStation.Shared

    /// The different pages of the application. If you add a new page, then add an entry here.
    [<RequireQualifiedAccess>]
    type Page =
        | Home
        | Device of StationKey

    let toPath =
        function
        | Page.Home -> "/"
        | Page.Device key -> sprintf "/device/%s/%s" key.DeviceType key.DeviceId

    /// The URL is turned into a Result.
    let pageParser : Parser<Page -> Page,_> =
        oneOf
            [ map Page.Home (s "")
              map (fun deviceType deviceId -> Page.Device({DeviceType = deviceType; DeviceId = deviceId})) (s "device" </> str </> str)]

    let urlParser location = parsePath pageParser location