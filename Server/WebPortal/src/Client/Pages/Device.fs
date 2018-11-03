namespace WeatherStation.Client.Pages
module Device =
    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.Recharts
    open Fable.Recharts.Props
    open Fable.Helpers.React
    
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

    module P = Fable.Helpers.React.Props
    module R = Fable.Helpers.React

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

    type Point = {x: DateTime; y: float}
    
    let graph data  =
        let data = [|for reading in data.Readings -> {x = reading.ReadingTime; y = reading.BatteryChargeVoltage}|]
        let width = 900.0
        //responsiveContainer [Chart.Height 300.] [
        readingsChart data

    let view dispatch model = [
        yield
            Client.tabs
                (SelectTab >> dispatch) [
                    {Name = "Data"; Key = Data; Content = [loader model.Device showDeviceDetails]; Icon = Some FontAwesome.Fa.I.Table}
                    {Name = "Graph"; Key = Graph; Content = [loader model.Device graph]; Icon = Some FontAwesome.Fa.I.LineChart}
                    {Name = "Settings"; Key = Settings; Content = [str "Settings"]; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]