namespace WeatherStation.Client.Pages
module Home =
    open WeatherStation.Client
    open WeatherStation.Shared

    open Fable.React
    open Fable.React.Props

    open Elmish

    open Fulma

    open Client

    module Icons = Fable.FontAwesome.Free.Fa.Solid

    type Model = {
        Stations : Loadable<Station list>
    }

    type Msg =
        | Stations of Loadable<Station list>
        | Select of Station
        | Add
        | Settings

    let loadStationsCmd =
        Cmd.OfPromise.either
            (fetchAs "/api/stations")
            []
            (Ok >> Loaded >> Stations)
            (Error >> Loaded >> Stations)
    let init () : Model * Cmd<Msg> =
        let initialModel = { Stations = Loading }
        initialModel, loadStationsCmd

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | Stations Loading ->
            let nextModel = { currentModel with Stations = Loading }
            nextModel, loadStationsCmd
        | Stations result ->
            let nextModel = { currentModel with Stations = result }
            nextModel, Cmd.none
        //These message is actually handled by the parent Application
        | _ -> currentModel, Cmd.none

    let stationsList dispatch stations =
        Table.table [] [
            thead [] [
                tr [] [
                    th [] [str "Name"]
                    th [] [str "Status"]
                    th [] [
                        div[Style [Float FloatOptions.Left; Width "50%"]][
                            button [Button.IsFullWidth] "Add" (fun _ -> dispatch Add) Icons.Plus
                        ]
                        div [Style [MarginLeft "50%"]][
                            fullButton "Reload" (fun _ -> dispatch (Stations Loading)) Icons.Redo]]]
                        ]
            tbody []
                [for station in stations do
                    let statusColor =
                        match station.Status with
                        | Active -> Color.IsSuccess
                        | Offline -> Color.IsWarning
                    yield
                        tr [] [
                            td [] [
                                if station.WundergroundId.IsSome
                                then yield a [Href (sprintf "https://www.wunderground.com/personal-weather-station/dashboard?ID=%s" station.WundergroundId.Value) ] [str station.Name]
                                else yield str station.Name]
                            td [] [
                                Tag.tag [Tag.Color statusColor] [str (string station.Status)]]
                            td [] [fullButton "Details" (fun _ -> dispatch (Select station)) Icons.Table]]]]

    let view dispatch model = [
        yield! loader model.Stations (stationsList dispatch)]