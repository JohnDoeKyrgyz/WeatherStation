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
    open Fulma.FontAwesome

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
        PageSize : TimeSpan
        CurrentPage : DateTime
    }

    type Msg =
        | Station of Loadable<StationDetails>
        | Readings of StationDetails * DateTime * Loadable<Reading list>
        | Settings of Loadable<StationSettings option>
        | SelectTab of Tab
        | UpdateSettings
        | SettingsUpdated of Loadable<string>
        | SettingsChanged of StationSettings option
        | ClearUpdateResult

    let loadReadingsCmd (key : StationKey) stationDetails fromDate tooDate =
        let url =
            let formattedFromDate = UrlDateTime.toUrlDate fromDate
            let formattedTooDate = UrlDateTime.toUrlDate tooDate
            sprintf "/api/stations/%s/%s/%s/%s" key.DeviceType key.DeviceId formattedTooDate formattedFromDate
        let result response = Readings (stationDetails, fromDate, response)
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
        let initialModel = {Device = Loading; Key = key; ActiveTab = Data; Settings = Loading; UpdateResult = NotLoading; PageSize = TimeSpan.FromDays 2.0; CurrentPage = DateTime.Now}    
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
        | Station (result) ->
            let currentPage, pageSize =
                match result with
                | Loaded (Ok stationDetails) -> defaultArg stationDetails.LastReading DateTime.Now, TimeSpan.FromHours stationDetails.PageSizeHours
                | _ -> DateTime.Now, currentModel.PageSize
            { currentModel with Device = result; PageSize = pageSize; CurrentPage = currentPage}, Cmd.none
        | Readings (stationDetails, fromDate, readings) ->
            match readings with
            | Loading ->
                let tooDate = fromDate - currentModel.PageSize
                {currentModel with Device = Loading}, loadReadingsCmd currentModel.Key stationDetails fromDate tooDate
            | Loaded (Ok readings) ->
                let deviceInfo = {stationDetails with Readings = readings}
                {currentModel with Device = Loaded (Ok deviceInfo); CurrentPage = fromDate}, Cmd.none
            | _ -> currentModel, Cmd.none
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

    let paginator (firstPage : DateTime) currentPage pageSize onNavigate =
        let previousDate = currentPage + pageSize
        let previousPage = if previousDate < firstPage then previousDate else firstPage
        let nextPage = currentPage - pageSize
        let buttonDefinitions = [
            FontAwesome.Fa.I.FastBackward, firstPage
            FontAwesome.Fa.I.Backward, previousPage
            FontAwesome.Fa.I.Refresh, currentPage
            FontAwesome.Fa.I.Forward, nextPage ]
        Level.level [] [
            Level.left [][
                for icon, key in buttonDefinitions do
                    yield Button.button [
                        Button.Color IsPrimary
                        Button.OnClick (onNavigate key)] [Icon.faIcon [] [Fa.icon icon]]]
            Level.item [][
                str (sprintf "%A - %A" currentPage nextPage)]]

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
   
    let graph dispatch currentPage pageSize data =
        let nextPage fromDate _ =
            dispatch (Readings (data, fromDate, Loading))
        let voltageData = [|for reading in data.Readings -> {time = date reading.ReadingTime; battery = reading.BatteryChargeVoltage; panel = reading.PanelVoltage}|]        
        let firstPage = defaultArg data.LastReading DateTime.Now
        div [] [
            paginator firstPage currentPage pageSize nextPage
            h2 [] [str "Voltage"]
            voltageChart voltageData]        

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
                    {Name = "Graph"; Key = Graph; Content = loader model.Device (graph dispatch model.CurrentPage model.PageSize); Icon = Some FontAwesome.Fa.I.LineChart}
                    {Name = "Settings"; Key = Tab.Settings; Content = settings dispatch model; Icon = Some FontAwesome.Fa.I.Gear}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]