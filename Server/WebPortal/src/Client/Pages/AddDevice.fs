namespace WeatherStation.Client.Pages

module AddDevice =

    open System

    open WeatherStation.Client
    open WeatherStation.Shared
    open Elmish

    open Fable
    open Fable.React.Props
    open Fable.React
    open Browser.Types
    open Fable.Core.JsInterop

    open Fulma
    open Fetch

    open Client

    type Model = {
        SaveResult : Loadable<StationKey>
        Station : StationKey    }

    type Msg =
        | Save of Loadable<StationKey>
        | ClearSaveResult
        | StationUpdate of StationKey


    let init : Model * Cmd<Msg> =
        let initialModel = {SaveResult = NotLoading; Station = {DeviceId = null; DeviceType = Particle}}
        initialModel, Cmd.none

    module P = Props
    module R = Helpers

    let createStation key =
        let url = (sprintf "/api/stations/%s/%s" (string key.DeviceType) key.DeviceId)
        Cmd.OfPromise.either
            (fetchAs url)
            [Method HttpMethod.POST]
            (Ok >> Loaded >> Save)
            (Error >> Loaded >> Save)

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | Save Loading ->
            {currentModel with SaveResult = Loading}, createStation currentModel.Station
        | Save saveResult ->
            {currentModel with SaveResult = saveResult}, Cmd.none
        | ClearSaveResult -> {currentModel with SaveResult = NotLoading}, Cmd.none
        | StationUpdate station -> {currentModel with Station = station}, Cmd.none

    let view dispatch model =

        let updater builder value =
            let newModel = builder value
            StationUpdate newModel
            |> dispatch

        let deviceTypesSelector =
            let deviceTypeOption v = option [Value v] [str (string v)]
            let onSelect (event: Event) =
                let value : DeviceType = event.target?value
                StationUpdate {model.Station with DeviceType = value}
                |> dispatch

            (Select.select [] [
                    select [Value model.Station.DeviceType; OnChange onSelect] [
                        deviceTypeOption Particle
                        deviceTypeOption Test]])
        [
            yield!
                match model.SaveResult with
                | Loading -> [spinner "Creating Device..."]
                | Loaded result ->
                    let color, header, message =
                        match result with
                        | Ok response -> IsSuccess, "Success", (sprintf "Created station with id %s" response.DeviceId)
                        | Error exn -> IsDanger, "Error", exn.Message
                    [Message.message [Message.Option.Color color][
                        Message.header [] [
                            str header
                            Delete.delete [Delete.OnClick (fun _ -> dispatch ClearSaveResult)][]]
                        Message.body [] [str message]]]
                | NotLoading -> []


            yield div [] [
                    formControl "Device Type"
                        deviceTypesSelector
                        []

                    formControl "DeviceId"
                        (textInput
                            (Some model.Station.DeviceId)
                            (updater (fun v -> {model.Station with DeviceId = v})))
                        []

                    fullButton "Save" (fun _ -> dispatch (Save Loading)) FontAwesome.Free.Fa.Solid.Save]]