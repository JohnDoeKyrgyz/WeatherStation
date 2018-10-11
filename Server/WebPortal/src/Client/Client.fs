namespace WeatherStation.Client
module Client =

    open Fable.Helpers.React
    open Fable.PowerPack.Fetch

    open Fulma
    open Thoth.Json
    
    type Loadable<'T> =
        | Loading
        | Loaded of Result<'T, exn>

    let fetchAs<'TItem> url = fetchAs<'TItem> url Decode.Auto.unsafeFromString


    let button txt onClick =
        Button.button
            [ Button.IsFullWidth
              Button.Color IsPrimary
              Button.OnClick onClick ]
            [ str txt ]