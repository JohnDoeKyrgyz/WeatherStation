namespace WeatherStation.Client.Pages
module Device =
    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.Helpers.React
    open Fulma

    open Client

    type Model = {
        DeviceType : string
        DeviceId : string
        Device : Loadable<Station>
    }

    type Msg =
        | Station of Loadable<Station>

    let loadStationCmd deviceType deviceId =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s" deviceType deviceId))
            []
            (Ok >> Loaded >> Station)
            (Error >> Loaded >> Station)    

    // defines the initial state and initial command (= side-effect) of the application
    let init deviceType deviceId : Model * Cmd<Msg> =
        let initialModel = { Device = Loading; DeviceId = deviceId; DeviceType = deviceType }    
        initialModel, loadStationCmd deviceType deviceId

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | Station Loading ->
            let nextModel = { currentModel with Device = Loading }
            nextModel, loadStationCmd currentModel.DeviceType currentModel.DeviceId
        | Station result ->
            let nextModel = { currentModel with Device = result }
            nextModel, Cmd.none


    let view dispatch model =
        Container.container
            []
            [Content.content [] [h1 [] [str "Hi!"]]]
            (*
            [ Content.content
                [ Content.Modifiers [ Modifier.TextAlignment (Screen.All, TextAlignment.Centered) ] ]
                //        [ Heading.h3 [] [ show dispatch model ] ]
                    Columns.columns []
                        [ Column.column [] [ button "Reload" (fun _ -> dispatch (Stations Loading))]]]
                        *)

