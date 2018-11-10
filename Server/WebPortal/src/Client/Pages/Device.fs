namespace WeatherStation.Client.Pages
open Fulma
open Fulma
open Fable.Recharts.Props
open Fable.Recharts
open Fable.Import.React
open System.Drawing
module Device =
    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.PowerPack
    open Fable.Import.Browser
    open Fable.Helpers.React
    open Fable.PowerPack.Fetch
    
    open Fulma

    open Client

    type Tab =
        | Data
        | Graph
        | Settings

    type Model = {
        UpdateResult : Result<Response, exn> option
        Key : StationKey
        Device : Loadable<StationDetails>
        Settings : Loadable<StationSettings>
        ActiveTab : Tab
    }

    type Msg =
        | Station of Loadable<StationDetails>
        | Settings of Loadable<StationSettings>
        | SelectTab of Tab
        | UpdateSettings
        | SettingsUpdated of Result<Response, exn>
        | SettingsChanged of StationSettings
        | ClearUpdateResult

    let loadStationCmd key =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Station)
            (Error >> Loaded >> Station)

    let updateSettings key settings =
        Cmd.ofPromise
            (postRecord<StationSettings> (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId) settings) 
            []
            (Ok >> SettingsUpdated)
            (Error >> SettingsUpdated)

    let loadSettings key =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Settings)
            (Error >> Loaded >> Settings)

    // defines the initial state and initial command (= side-effect) of the application
    let init key : Model * Cmd<Msg> =
        let initialModel = { Device = Loading; Key = key; ActiveTab = Data; Settings = Loading; UpdateResult = None }    
        initialModel, loadStationCmd key

    module P = Fable.Helpers.React.Props
    module R = Fable.Helpers.React

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | SelectTab Tab.Settings ->
            {currentModel with ActiveTab = Tab.Settings}, loadSettings currentModel.Key
        | SelectTab tab ->
            {currentModel with ActiveTab = tab}, Cmd.none                    
        | Station Loading ->
            let nextModel = { currentModel with Device = Loading }
            nextModel, loadStationCmd currentModel.Key
        | Station result ->
            let nextModel = { currentModel with Device = result }
            nextModel, Cmd.none
        | Settings settings ->
            {currentModel with Settings = settings}, Cmd.none
        | SettingsChanged contents ->
            {currentModel with Settings = Loaded (Result.Ok contents)}, Cmd.none
        | UpdateSettings ->
            match currentModel.Settings with
            | Loaded (Result.Ok settings) ->
                currentModel, updateSettings currentModel.Key settings
            | _ -> failwith "Settings not loaded"
        | SettingsUpdated result ->
            {currentModel with UpdateResult = Some result}, Cmd.none
        | ClearUpdateResult ->
            {currentModel with UpdateResult = None}, Cmd.none                            

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
        readingsChart data

    let settings dispatch model = [
        match model.UpdateResult with
        | Some result ->
            let color, header, message =
                match result with
                | Ok _ -> Color.IsSuccess, "Success", "Settings updated"
                | Error exn -> Color.IsDanger, "Error", exn.Message
            yield Message.message [Message.Option.Color color][
                Message.header [] [
                    str header
                    Delete.delete [Delete.OnClick (fun _ -> dispatch ClearUpdateResult)][]]
                Message.body [] [str message]]
        | None -> ()                

        yield loader model.Settings (fun settings ->
            let onChange builder value = builder value |> SettingsChanged |> dispatch
            form [] [
                simpleFormControl "Brownout" <| checkBoxInput settings.Brownout (onChange (fun value -> {settings with Brownout = value}))
                simpleFormControl "Brownout Minutes" <| numberInput settings.BrownoutMinutes (onChange (fun value -> {settings with BrownoutMinutes = value}))
                formControl 
                    "Sleep Time" 
                    (numberInput settings.SleepTime (onChange (fun value -> {settings with SleepTime = value}))) [
                        Help.help [][str "Measured in seconds"]]
                simpleFormControl "Diagnostic Cycles" <| (numberInput settings.DiagnosticCycles (onChange (fun value -> {settings with DiagnosticCycles = value})))
                simpleFormControl "Use Deep Sleep" <| Checkbox.checkbox [] [ Checkbox.input [ ]]
                button "Save" (fun _ -> dispatch UpdateSettings)])
    ]
    
    let view dispatch model = [
        yield
            Client.tabs
                (SelectTab >> dispatch) [
                    {Name = "Data"; Key = Data; Content = [loader model.Device showDeviceDetails]; Icon = Some FontAwesome.Fa.I.Table}
                    {Name = "Graph"; Key = Graph; Content = [loader model.Device graph]; Icon = Some FontAwesome.Fa.I.LineChart}
                    {Name = "Settings"; Key = Tab.Settings; Content = settings dispatch model; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]