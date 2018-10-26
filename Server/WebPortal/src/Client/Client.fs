namespace WeatherStation.Client
module Client =

    open Fable.Helpers.React
    open Fable.PowerPack
    open Fable.PowerPack.Fetch

    open Fulma
    open Thoth.Json
    
    type Loadable<'T> =
        | Loading
        | Loaded of Result<'T, exn>

    let inline fetchAs url parameters =
        promise {
            let! response = fetch url parameters
            let! text = response.text()
            return Decode.Auto.unsafeFromString text
        }

    let button txt onClick =
        Button.button
            [ Button.IsFullWidth
              Button.Color IsPrimary
              Button.OnClick onClick ]
            [ str txt ]