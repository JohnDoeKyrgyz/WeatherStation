namespace WeatherStation.Client
module Client =

    open Elmish
    open Elmish.React

    open Fable.Helpers.React
    open Fable.Helpers.React.Props
    open Fable.PowerPack.Fetch

    open WeatherStation.Client.Application

    open Fulma
    open Thoth.Json
    
    type Loadable<'T> =
        | Loading
        | Loaded of Result<'T, exn>

    let fetchAs<'TItem> url = fetchAs<'TItem> url Decode.Auto.unsafeFromString

    let safeComponents =
        let components =
            span [ ]
               [
                 a [ Href "https://saturnframework.github.io" ] [ str "Saturn" ]
                 str ", "
                 a [ Href "http://fable.io" ] [ str "Fable" ]
                 str ", "
                 a [ Href "https://elmish.github.io/elmish/" ] [ str "Elmish" ]
                 str ", "
                 a [ Href "https://mangelmaxime.github.io/Fulma" ] [ str "Fulma" ]
               ]

        p [ ]
            [ strong [] [ str "SAFE Template" ]
              str " powered by: "
              components ]

    let button txt onClick =
        Button.button
            [ Button.IsFullWidth
              Button.Color IsPrimary
              Button.OnClick onClick ]
            [ str txt ]    

    let view (model : Model) (dispatch : Msg -> unit) =
        div []
            [ Navbar.navbar [ Navbar.Color IsPrimary ]
                [ Navbar.Item.div [ ]
                    [ Heading.h2 [ ]
                        [ str "Weather Stations" ] ] ]

              
                  

              Footer.footer [ ]
                    [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                        [ safeComponents ] ] ]


