module WeatherStations.Client

open Elmish
open Elmish.React

open Fable.Helpers.React
open Fable.Helpers.React.Props
open Fable.PowerPack.Fetch

open WeatherStations.Shared

open Fulma

type Loadable<'T> =
    | Loading
    | Loaded of Result<'T, exn>

type Model = {
    Stations : Loadable<Station list>
    SelectedStation : Station option
}

type Msg =
    | Stations of Loadable<Station list>
    | Select of Station

let loadStationsCmd =
    Cmd.ofPromise
        (fetchAs<Station list> "/api/stations")
        []
        (Ok >> Loaded >> Stations)
        (Error >> Loaded >> Stations)    

// defines the initial state and initial command (= side-effect) of the application
let init () : Model * Cmd<Msg> =
    let initialModel = { Stations = Loading; SelectedStation = None}    
    initialModel, loadStationsCmd

let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
    match msg with
    | Stations Loading ->
        let nextModel = { currentModel with Stations = Loading }
        nextModel, loadStationsCmd
    | Stations result ->
        let nextModel = { currentModel with Stations = result }
        nextModel, Cmd.none
    | Select station ->
        let nextModel = { currentModel with SelectedStation = Some station }
        nextModel, Cmd.none        

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

let stationsList dispatch stations =
    table [] [
        thead [] [
            th [] [str "Name"]
            th [] [str "Status"]
            th [] [button "Reload" (fun _ -> dispatch (Stations Loading))]]
        tbody [] 
            [for station in stations do
                yield tr [] [
                    td [] [a [Href (sprintf "https://www.wunderground.com/personal-weather-station/dashboard?ID=%s" station.WundergroundId) ] [str station.Name]]
                    td [] [str (string station.Status)]
                    td [] [
                        Button.button [
                            Button.IsFullWidth
                            Button.Color IsPrimary] [str "Details"]]]]]
    
let show dispatch model = 
    match model.Stations with
    | Loading -> str "Loading..."
    | Loaded (Ok data) -> stationsList dispatch data
    | Loaded (Error error) -> str error.Message

let view (model : Model) (dispatch : Msg -> unit) =
    div []
        [ Navbar.navbar [ Navbar.Color IsPrimary ]
            [ Navbar.Item.div [ ]
                [ Heading.h2 [ ]
                    [ str "Weather Stations" ] ] ]

          Container.container []
              [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ Heading.h3 [] [ show dispatch model ] ]
                Columns.columns []
                    [ Column.column [] [ button "Reload" (fun _ -> dispatch (Stations Loading))]]]

          Footer.footer [ ]
                [ Content.content [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                    [ safeComponents ] ] ]


#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

Program.mkProgram init update view
#if DEBUG
|> Program.withConsoleTrace
|> Program.withHMR
#endif
|> Program.withReact "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run
