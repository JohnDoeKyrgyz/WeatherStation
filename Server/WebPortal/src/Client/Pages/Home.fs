namespace WeatherStation.Client.Pages
module Home =
    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.Helpers.React
    open Fable.Helpers.React.Props
    open Fulma

    open Client

    type Model = {
        Stations : Loadable<Station list>
        SelectedStation : Station option
    }

    type Msg =
        | Stations of Loadable<Station list>
        | Select of Station

    let loadStationsCmd =
        Cmd.ofPromise
            (fetchAs "/api/stations")
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

    let view dispatch model =
        Container.container
            []
            [Content.content [] []]
            (*
            [ Content.content
                [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                //        [ Heading.h3 [] [ show dispatch model ] ]
                    Columns.columns []
                        [ Column.column [] [ button "Reload" (fun _ -> dispatch (Stations Loading))]]]
                        *)

