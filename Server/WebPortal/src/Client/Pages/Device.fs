namespace WeatherStation.Client.Pages
module Device =
    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.Helpers.React
    open Fulma

    open Client
    open Fulma

    type Model = {
        DeviceType : string
        DeviceId : string
        Device : Loadable<StationDetails>
    }

    type Msg =
        | Station of Loadable<StationDetails>

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

    let showDeviceDetails deviceDetails =
        div [] [
            Table.table [] [
                thead [] [
                    th [] [str "Time"]
                    th [] [str "BatteryVoltage"]]
                tbody [] [
                    for reading in deviceDetails.Readings do
                        yield
                            tr [] [
                                td [] [str (string reading.ReadingTime)]
                                td [] [str (string reading.BatteryChargeVoltage)]]]]]


    let view dispatch model =
        div [] [
            yield Heading.h3 [] [str model.DeviceId]
            yield
                match model.Device with
                | Loaded (Ok deviceDetails) -> showDeviceDetails deviceDetails
                | Loaded (Error error) -> str (string error)
                | Loading -> str "Loading..."]

