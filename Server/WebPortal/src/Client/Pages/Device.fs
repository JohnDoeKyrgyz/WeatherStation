namespace WeatherStation.Client.Pages
module Device =

    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable.PowerPack
    open Fable.Helpers.React
    open Fable.PowerPack.Fetch
    
    open Fulma

    open Client

    type Tab =
        | Data
        | Graph
        | Settings

    type Model = {
        UpdateResult : Loadable<string>
        Key : StationKey
        Device : Loadable<StationDetails>
        Settings : Loadable<StationSettings option>
        ActiveTab : Tab
        Page : PageKey<DateTime>
    }

    type Msg =
        | Station of Loadable<StationDetails>
        | Readings of DateTime * StationDetails * Loadable<Reading list>
        | Settings of Loadable<StationSettings option>
        | SelectTab of Tab
        | UpdateSettings
        | SettingsUpdated of Loadable<string>
        | SettingsChanged of StationSettings option
        | ClearUpdateResult

    let loadReadingsCmd (key : StationKey) stationDetails fromDate =
        let url =
            let formattedDate = UrlDateTime.toUrlDate fromDate
            sprintf "/api/stations/%s/%s/%s" key.DeviceType key.DeviceId formattedDate
        let result response = Readings (fromDate, stationDetails, response)
        Cmd.ofPromise
            (fetchAs url)
            []
            (Ok >> Loaded >> result)
            (Error >> Loaded >> result)

    let loadStationCmd (key : StationKey) =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Station)
            (Error >> Loaded >> Station)

    let updateSettings key settings =
        Cmd.ofPromise
            (fun args -> promise {
                let! result = postRecord<StationSettings> (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId) settings args
                return! result.text()
            }) 
            []
            (Ok >> Loaded >> SettingsUpdated)
            (Error >> Loaded >> SettingsUpdated)

    let loadSettings key =
        Cmd.ofPromise
            (fetchAs (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Settings)
            (Error >> Loaded >> Settings)

    let init key : Model * Cmd<Msg> =
        let initialModel = {
            Device = Loading; Key = key; ActiveTab = Data; Settings = Loading; UpdateResult = NotLoading;
            Page = PageKey<DateTime>.Default }    
        initialModel, loadStationCmd key

    module P = Fable.Helpers.React.Props
    module R = Fable.Helpers.React

    let updatePagePosition currentPosition (currentModel : Model) =
        match currentModel.Device with
        | Loaded (Ok stationDetails) ->
            let nextPosition = (stationDetails.Readings |> List.last).ReadingTime
            {currentModel with Page = {currentModel.Page with Current = currentPosition; Next = Some nextPosition; Previous = currentModel.Page.Current}}
        | _ -> currentModel

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | SelectTab Tab.Settings ->
            {currentModel with ActiveTab = Tab.Settings}, loadSettings currentModel.Key
        | SelectTab tab ->
            {currentModel with ActiveTab = tab}, Cmd.none                    
        | Station Loading ->
            let nextModel = { currentModel with Device = Loading }
            nextModel, loadStationCmd currentModel.Key
        | Readings (fromDate, stationDetails, Loading) ->
            {currentModel with Device = Loading}, loadReadingsCmd currentModel.Key stationDetails fromDate
        | Readings (fromDate, stationDetails, result) ->
            let nextModel =
                match result with
                | Loaded (Ok readings) ->
                    let deviceInfo = {stationDetails with Readings = readings}
                    {currentModel with Device = Loaded (Ok deviceInfo)}
                    |> updatePagePosition (Some fromDate)
                | _ -> currentModel
            nextModel, Cmd.none
        | Station (result) ->
            let nextModel =
                { currentModel with Device = result }
                |> updatePagePosition None
            nextModel, Cmd.none
        | Settings settings ->
            {currentModel with Settings = settings}, Cmd.none
        | SettingsChanged contents ->
            {currentModel with Settings = Loaded (Result.Ok contents)}, Cmd.none
        | UpdateSettings ->
            match currentModel.Settings with
            | Loaded (Result.Ok (Some settings)) ->
                {currentModel with  UpdateResult = Loading}, updateSettings currentModel.Key settings
            | _ -> failwith "Settings not loaded"
        | SettingsUpdated result ->
            {currentModel with UpdateResult = result}, Cmd.none
        | ClearUpdateResult ->
            {currentModel with UpdateResult = NotLoading}, Cmd.none                            

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
   
    let graph dispatch pageKey data  =
        let voltageData = [|for reading in data.Readings -> {time = date reading.ReadingTime; battery = reading.BatteryChargeVoltage; panel = reading.PanelVoltage}|]        
        div [] [
            h2 [] [str "Voltage"]
            voltageChart voltageData
            paginator pageKey (fun fromDate _ -> (dispatch (Readings (fromDate, data, Loading))))
        ]        

    let settings dispatch model = [
        yield!
            match model.UpdateResult with
            | Loading -> [spinner "Saving Settings..."]
            | Loaded result ->
                let color, header, message =
                    match result with
                    | Ok response -> Color.IsSuccess, "Success", response
                    | Error exn -> Color.IsDanger, "Error", exn.Message
                [Message.message [Message.Option.Color color][
                    Message.header [] [
                        str header
                        Delete.delete [Delete.OnClick (fun _ -> dispatch ClearUpdateResult)][]]
                    Message.body [] [str message]]]
            | NotLoading -> []

        yield! loader model.Settings (fun settings ->
            let readValue f = FSharp.Core.Option.map f settings
            let setValue builder value =
                match settings with
                | Some settings -> Some (builder settings value)
                | None -> Some (builder StationSettings.Default value)
                |> SettingsChanged
                |> dispatch                
            div [] [
                checkBoxInput 
                    (readValue (fun settings -> settings.Brownout)) 
                    (setValue (fun settings value -> {settings with Brownout = value}))
                |> simpleFormControl "Brownout"

                decimalInput 
                    (readValue (fun settings -> settings.BrownoutVoltage))
                    (setValue (fun settings value -> {settings with BrownoutVoltage = value}))
                |> simpleFormControl "Brownout Voltage"

                intInput 
                    (readValue (fun settings -> settings.BrownoutMinutes))
                    (setValue (fun settings value -> {settings with BrownoutMinutes = value}))
                |> simpleFormControl "Brownout Minutes"

                formControl 
                    "Sleep Time" 
                    (intInput 
                        (readValue (fun settings -> settings.SleepTime))
                        (setValue (fun settings value -> {settings with SleepTime = value})))[                         
                        Help.help [][str "Measured in seconds"]]

                (intInput 
                    (readValue (fun settings -> settings.DiagnosticCycles))
                    (setValue (fun settings value -> {settings with DiagnosticCycles = value})))
                |> simpleFormControl "Diagnostic Cycles"

                checkBoxInput
                    (readValue (fun settings -> settings.UseDeepSleep))
                    (setValue (fun settings value -> {settings with UseDeepSleep = value}))
                |> simpleFormControl "Use Deep Sleep"

                button "Save" (fun _ -> dispatch UpdateSettings)])
    ]

    let view dispatch model = [
        yield
            Client.tabs
                (SelectTab >> dispatch) [
                    {Name = "Data"; Key = Data; Content = loader model.Device showDeviceDetails; Icon = Some FontAwesome.Fa.I.Table}
                    {Name = "Graph"; Key = Graph; Content = loader model.Device (graph dispatch model.Page); Icon = Some FontAwesome.Fa.I.LineChart}
                    {Name = "Settings"; Key = Tab.Settings; Content = settings dispatch model; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]