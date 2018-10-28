namespace WeatherStation.Client.Pages
open Fable.Recharts
open Fable.Recharts.Props
module Device =

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

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

    let margin t r b l =
        Chart.Margin { top = t; bottom = b; right = r; left = l }

    
    module R = Fable.Helpers.React
    module P = R.Props                        

    let graph model =       
        match model.Device with
        | Loaded (Error error) -> str (string error)
        | Loading -> str "Loading..."
        | Loaded (Ok deviceDetails) ->
            let data = [|for reading in deviceDetails.Readings -> reading.BatteryChargeVoltage|] 
            lineChart
                [ margin 5. 20. 5. 0.
                  Chart.Width 600.
                  Chart.Height 300.
                  Chart.Data data ]
                [ line
                    [ Cartesian.Type Monotone
                      Cartesian.DataKey "uv"
                      P.Stroke "#8884d8"
                      P.StrokeWidth 2. ]
                    []
                  cartesianGrid
                    [ P.Stroke "#ccc"
                      P.StrokeDasharray "5 5" ]
                    []
                  xaxis [Cartesian.DataKey "name"] []
                  yaxis [] []
                  tooltip [] [] ]

    let readingsTable model =
        match model.Device with
        | Loaded (Ok deviceDetails) -> showDeviceDetails deviceDetails
        | Loaded (Error error) -> str (string error)
        | Loading -> str "Loading..."

    let view dispatch model = [
        Client.tabs
            (SelectTab >> dispatch) [
                {Name = "Data"; Key = Data; Content = [readingsTable model]; Icon = Some FontAwesome.Fa.I.Table}
                {Name = "Graph"; Key = Graph; Content = [graph model]; Icon = Some FontAwesome.Fa.I.LineChart}
                {Name = "Settings"; Key = Settings; Content = [str "Settings"]; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]