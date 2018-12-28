namespace WeatherStation.Client.Pages
module Home =
    open WeatherStation.Client
    open WeatherStation.Shared

    open Fable.Helpers.React
    open Fable.Helpers.React.Props

    open Elmish

    open Fulma
    open FontAwesome.Fa.I

    open Client

    type Model = {
        Stations : Loadable<Station list>
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
        | Select _ ->
            //This message is actually handled by the parent Application
            currentModel, Cmd.none

    let stationsList dispatch stations =
        Table.table [] [
            thead [] [
                tr [] [
                    td [] [str "Name"]
                    td [] [str "Status"]
                    td [] [button "Reload" (fun _ -> dispatch (Stations Loading)) Refresh]]]
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
                            td [] [button "Details" (fun _ -> dispatch (Select station)) Table]]]]

    let view dispatch model = [
        yield! loader model.Stations (stationsList dispatch)]