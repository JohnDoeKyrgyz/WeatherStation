namespace WeatherStation.Client.Pages

module AddDevice =

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

    type Model = {
        SaveResult : Loadable<StationKey>
        Station : Station
    }

    type Msg =
        | Save of Loadable<StationKey>
        | ClearSaveResult
        | StationUpdate of Station


    let init : Model * Cmd<Msg> =
        let initialStation = {
            Key = {DeviceId = null; DeviceType = null}
            Name = "Name"
            WundergroundId = None
            Location = {Latitude = 0.0m; Longitude = 0.0m}
            Status  = Offline
        }
        let initialModel = {SaveResult = Loadable.NotLoading; Station = initialStation}
        initialModel, Cmd.none

    module P = Props
    module R = Helpers

    let update (msg : Msg) (currentModel : Model) : Model * Cmd<Msg> =
        match msg with
        | Save _ -> currentModel, Cmd.none
        | ClearSaveResult -> {currentModel with SaveResult = NotLoading}, Cmd.none
        | StationUpdate station -> {currentModel with Station = station}, Cmd.none

    let view dispatch model =
        let updater builder value =
            let newModel = builder value
            StationUpdate newModel
            |> dispatch
        [
            yield!
                match model.SaveResult with
                | Loading -> [spinner "Creating Device..."]
                | Loaded result ->
                    let color, header, message =
                        match result with
                        | Ok response -> IsSuccess, "Success", (sprintf "Creatd station with id %s" response.DeviceId)
                        | Error exn -> IsDanger, "Error", exn.Message
                    [Message.message [Message.Option.Color color][
                        Message.header [] [
                            str header
                            Delete.delete [Delete.OnClick (fun _ -> dispatch ClearSaveResult)][]]
                        Message.body [] [str message]]]
                | NotLoading -> []


            yield div [] [
                    formControl "Name"
                        (textInput
                            (Some model.Station.Name)
                            (updater (fun v -> {model.Station with Name = v})))
                        []

                    formControl "Latitude"
                        (decimalInput
                            (Some model.Station.Location.Latitude)
                            (updater (fun v -> {model.Station with Location = {model.Station.Location with Latitude = v}})))
                        []

                    formControl "Longitude"
                        (decimalInput
                            (Some model.Station.Location.Longitude)
                            (updater (fun v -> {model.Station with Location = {model.Station.Location with Longitude = v}})))
                        []

                    formControl "WundergroundId"
                        (textInput
                            model.Station.WundergroundId
                            (updater (fun v ->
                                let value = if String.IsNullOrWhiteSpace(v) then None else Some v
                                {model.Station with WundergroundId = value})))
                        []


                    fullButton "Save" (fun _ -> dispatch (Save Loading)) FontAwesome.Free.Fa.Solid.Save]]