namespace WeatherStation.Client.Pages

module Device =

    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable
    open Fable.React

    open Fulma
    open Thoth.Json
    open Fetch

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
            sprintf "/api/stations/%s/%s/readings/%s/%s" key.DeviceType key.DeviceId formattedTooDate formattedFromDate
        let result response = Readings (stationDetails, fromDate, response)
        Cmd.OfPromise.either
            (fetchAs url)
            []
            (Ok >> Loaded >> result)
            (Error >> Loaded >> result)

    let loadStationCmd (key : StationKey) =
        Cmd.OfPromise.either
            (fetchAs (sprintf "/api/stations/%s/%s" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Station)
            (Error >> Loaded >> Station)

    let updateSettings key settings =
        (*
            this code is ported from commented out code in:
            https://github.com/fable-compiler/fable-fetch
            This code was commented out because it takes a dependancy on Thoth
        *)
        let url = (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId)
        let extraCoders = Extra.empty |> Extra.withDecimal
        let json = Encode.Auto.toString<StationSettings>(0, settings, extra = extraCoders)
        let jsonBody = Body ( BodyInit.Case2(json) )
        Cmd.OfPromise.either
            (fun _ -> promise {
                let! result =
                    [
                        Method HttpMethod.POST
                        requestHeaders [ContentType "application/json"]
                        jsonBody
                    ]
                    |> fetch url
                return! result.text()
            })
            []
            (Ok >> Loaded >> SettingsUpdated)
            (Error >> Loaded >> SettingsUpdated)

    let loadSettings key =
        Cmd.OfPromise.either
            (fetchAs (sprintf "/api/stations/%s/%s/settings" key.DeviceType key.DeviceId))
            []
            (Ok >> Loaded >> Settings)
            (Error >> Loaded >> Settings)

    let init key : Model * Cmd<Msg> =
        let initialModel = {Device = Loading; Key = key; ActiveTab = Data; Settings = Loading; UpdateResult = NotLoading; PageSize = TimeSpan.FromDays 2.0; CurrentPage = DateTime.Now}
        initialModel, loadStationCmd key

    module P = Props
    module R = Helpers

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
                let fromDate = fromDate.ToUniversalTime()
                let tooDate = (fromDate - currentModel.PageSize).ToUniversalTime()
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
            | Loaded (Ok (Some settings)) ->
                {currentModel with  UpdateResult = Loading}, updateSettings currentModel.Key settings
            | _ -> failwith "Settings not loaded"
        | SettingsUpdated result ->
            {currentModel with UpdateResult = result}, Cmd.none
        | ClearUpdateResult ->
            {currentModel with UpdateResult = NotLoading}, Cmd.none

    type Voltage = {time: string; battery: float; panel: float}

    let voltageChart readings =
        let data = [|for reading in readings -> {time = date reading.ReadingTime; battery = reading.BatteryChargeVoltage; panel = reading.PanelVoltage}|]
        readingsChart data [
            "battery", "red"
            "panel", "orange"]

    type Temperature = {time: string; hydrometer: float option; barometer: float option}

    let temperatureChart readings =
        let data = [|for reading in readings -> {time = date reading.ReadingTime; hydrometer = reading.TemperatureCelciusHydrometer; barometer = reading.TemperatureCelciusBarometer}|]
        readingsChart data [
            "hydrometer", "blue"
            "barometer", "green"]

    type WindSpeed = {time: string; speed: float option}

    let windSpeedChart readings =
        let data = [|for reading in readings -> {time = date reading.ReadingTime; speed = reading.SpeedMetersPerSecond}|]
        readingsChart data [
            "speed", "blue"]

    type WindDirection = {time: string; direction: float option}

    let windDirectionChart readings =
        let data = [|for reading in readings -> {time = date reading.ReadingTime; direction = reading.DirectionDegrees}|]
        readingsChart data [
            "direction", "blue"]

    let view dispatch model =

        let paginator data =
            let onNavigate fromDate _ = dispatch (Readings (data, fromDate, Loading))
            let firstPage = defaultArg data.LastReading DateTime.Now
            let previousDate = model.CurrentPage + model.PageSize
            let previousPage = if previousDate < firstPage then previousDate else firstPage
            let nextPage = model.CurrentPage.Subtract(model.PageSize)
            let currentPageFormatted = date model.CurrentPage
            let nextPageFormatted = date nextPage
            let buttonDefinitions = [
                FontAwesome.Free.Fa.Solid.FastBackward, firstPage
                FontAwesome.Free.Fa.Solid.Backward, previousPage
                FontAwesome.Free.Fa.Solid.Redo, model.CurrentPage
                FontAwesome.Free.Fa.Solid.Forward, nextPage ]
            Level.level [] [
                Level.left [][
                    for icon, key in buttonDefinitions do
                        yield Button.button [
                            Button.Color IsPrimary
                            Button.OnClick (onNavigate key)] [Icon.icon [] [FontAwesome.Fa.i [icon] []]]]
                Level.item [][
                    str (sprintf "%s - %s" currentPageFormatted nextPageFormatted)]]

        let showDeviceDetails deviceDetails =
            div [] [
                paginator deviceDetails
                table
                    ["Time"; "Battery"; "Panel"; "Charge Current"; "Speed"; "Direction"; "Temp"]
                    deviceDetails.Readings
                    (fun reading -> [
                        date (reading.ReadingTime.ToLocalTime())
                        number reading.BatteryChargeVoltage
                        number reading.PanelVoltage
                        number reading.PanelMilliamps
                        numberOptional reading.SpeedMetersPerSecond
                        numberOptional reading.DirectionDegrees
                        numberOptional reading.TemperatureCelciusBarometer])]

        let graphs data =
            div [] [
                paginator data
                h2 [] [str "Voltage"]
                voltageChart data.Readings
                h2 [] [str "Temperature (Celcius)"]
                temperatureChart data.Readings
                h2 [] [str "Wind Speed (Meters / Second)"]
                windSpeedChart data.Readings
                h2 [] [str "Wind Direction (Degrees)"]
                windDirectionChart data.Readings]

        let settings = [
            yield!
                match model.UpdateResult with
                | Loading -> [spinner "Saving Settings..."]
                | Loaded result ->
                    let color, header, message =
                        match result with
                        | Ok response -> IsSuccess, "Success", response
                        | Error exn -> IsDanger, "Error", exn.Message
                    [Message.message [Message.Option.Color color][
                        Message.header [] [
                            str header
                            Delete.delete [Delete.OnClick (fun _ -> dispatch ClearUpdateResult)][]]
                        Message.body [] [str message]]]
                | NotLoading -> []

            yield! loader model.Settings (fun settings ->
                let readValue f = Option.map f settings
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
                        (readValue (fun settings -> settings.BrownoutPercentage))
                        (setValue (fun settings value -> {settings with BrownoutPercentage = value}))
                    |> simpleFormControl "Brownout Percentage"

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

                    intInput
                        (readValue (fun settings -> settings.PanelOffMinutes))
                        (setValue (fun settings value -> {settings with PanelOffMinutes = value}))
                    |> simpleFormControl "Panel Off Minutes"

                    button "Save" (fun _ -> dispatch UpdateSettings) FontAwesome.Free.Fa.Solid.Save])]

        [Client.tabs
            (SelectTab >> dispatch) [
                {Name = "Data"; Key = Data; Content = loader model.Device showDeviceDetails; Icon = Some FontAwesome.Free.Fa.Solid.Table}
                {Name = "Graph"; Key = Graph; Content = loader model.Device graphs; Icon = Some FontAwesome.Free.Fa.Solid.ChartLine}
                {Name = "Settings"; Key = Tab.Settings; Content = settings; Icon = Some FontAwesome.Free.Fa.Solid.Cog}
            ]
            model.ActiveTab
            [Tabs.IsFullWidth; Tabs.IsBoxed]]