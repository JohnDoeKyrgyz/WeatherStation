namespace WeatherStation.Client.Pages
module Pages =

    open Elmish.Browser.UrlParser

    /// The different pages of the application. If you add a new page, then add an entry here.
    [<RequireQualifiedAccess>]
    type Page =
        | Home
        | Device of string * string

    let toPath =
        function
        | Page.Home -> "/"
        | Page.Device (deviceType, deviceId) -> sprintf "/device/%s/%s" deviceType deviceId        

    /// The URL is turned into a Result.
    let pageParser : Parser<Page -> Page,_> =
        oneOf
            [ map Page.Home (s "")
              map (fun deviceType deviceId -> Page.Device(deviceType, deviceId)) (s "device" </> str </> str)]

    let urlParser location = parsePath pageParser location