namespace WeatherStation.Client.Pages
module Device =

    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    
    open Fable.Import.React
    open Fable.Helpers.React
    open Fable.Helpers.React.Props
    open Fulma

    open Client

    type Tab =
        | Data
        | Graph
        | Settings

    type Model = {
        DeviceType : string
        DeviceId : string
        Device : Loadable<StationDetails>
        ActiveTab : Tab
    }

    type Msg =
        | Station of Loadable<StationDetails>
        | SelectTab of Tab

    let loadStationCmd deviceType deviceId =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s" deviceType deviceId))
            []
            (Ok >> Loaded >> Station)
            (Error >> Loaded >> Station)    

    // defines the initial state and initial command (= side-effect) of the application
    let init deviceType deviceId : Model * Cmd<Msg> =
        let initialModel = { Device = Loading; DeviceId = deviceId; DeviceType = deviceType; ActiveTab = Data }    
        initialModel, loadStationCmd deviceType deviceId

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | SelectTab tab ->
            {currentModel with ActiveTab = tab}, Cmd.none
        | Station Loading ->
            let nextModel = { currentModel with Device = Loading }
            nextModel, loadStationCmd currentModel.DeviceType currentModel.DeviceId
        | Station result ->
            let nextModel = { currentModel with Device = result }
            nextModel, Cmd.none

    let showDeviceDetails deviceDetails =
        table
            ["Time"; "Battery"; "Panel"; "Speed"; "Direction"; "Temp"]
            deviceDetails.Readings
            (fun reading -> [
                date reading.ReadingTime
                number reading.BatteryChargeVoltage
                number reading.PanelVoltage
                number reading.SpeedMetersPerSecond
                string reading.DirectionDegrees
                number reading.TemperatureCelciusBarometer])

    let readingsTable model =
        match model.Device with
        | Loaded (Ok deviceDetails) -> showDeviceDetails deviceDetails
        | Loaded (Error error) -> str (string error)
        | Loading -> str "Loading..."

    let view dispatch model = [
        Heading.h3 [] [str model.DeviceId]
        Client.tabs
            (SelectTab >> dispatch) [
                {Name = "Data"; Key = Data; Content = [readingsTable model]; Icon = Some FontAwesome.Fa.I.Table}
                {Name = "Graph"; Key = Graph; Content = [str "Graph"]; Icon = Some FontAwesome.Fa.I.LineChart}
                {Name = "Settings"; Key = Settings; Content = [str "Settings"]; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]