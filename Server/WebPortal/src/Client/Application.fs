namespace WeatherStation.Client

module Application =
    open Elmish
    open Elmish.React
    open Elmish.HMR
    open Elmish.Navigation

    open Fable.React
    open Fable.React.Props

    open WeatherStation.Client.Pages

    /// The composed model for the different possible page states of the application
    type PageModel =
        | HomeModel of Home.Model
        | DeviceModel of Device.Model
        | AddModel of AddDevice.Model

    /// The composed model for the application, which is a single page state plus login information
    type Model = {
        PageModel : PageModel }

    /// The composed set of messages that update the state of the application
    type Msg =
        | HomeMsg of Home.Msg
        | DeviceMsg of Device.Msg
        | AddDeviceMsg of AddDevice.Msg
        | Navigate of Pages.Page

    // VIEW
    open Fulma

    /// Constructs the view for a page given the model and dispatcher.
    let viewPage model dispatch =
        match model.PageModel with
        | HomeModel m -> Home.view (HomeMsg >> dispatch) m
        | DeviceModel m -> Device.view (DeviceMsg >> dispatch) m
        | AddModel m -> AddDevice.view (AddDeviceMsg >> dispatch) m

    let navigate page model =
        let url = Pages.toPath page
        let gotoPage message toPageModel (pageModel, pageCommand) =
            let command =
                [
                    Navigation.newUrl url
                    pageCommand]
                |> List.map (Cmd.map message)
                |> Cmd.batch
            {model with PageModel = toPageModel pageModel}, command
        match page with
        | Pages.Page.Home -> gotoPage HomeMsg HomeModel (Home.init())
        | Pages.Page.AddDevice -> gotoPage AddDeviceMsg AddModel (AddDevice.init)
        | Pages.Page.Device key -> gotoPage DeviceMsg DeviceModel (Device.init key)

    let view model dispatch =
        let homeCrumb active = Breadcrumb.item [Breadcrumb.Item.IsActive active] [a [OnClick (fun _ -> dispatch (Msg.Navigate Pages.Page.Home))] [ str "Home" ]]

        div [] [
            Navbar.navbar [Navbar.Color IsPrimary] [
                Navbar.Item.div [Navbar.Item.Option.IsTab] [
                    Breadcrumb.breadcrumb [Breadcrumb.HasBulletSeparator] [
                        match model.PageModel with
                        | HomeModel _ ->
                            yield homeCrumb true
                        | AddModel _ ->
                            yield homeCrumb false
                        | DeviceModel deviceModel ->
                            yield homeCrumb false
                            yield Breadcrumb.item [Breadcrumb.Item.IsActive true ][ a [ ] [ str deviceModel.Key.DeviceId ] ] ] ] ]

            Container.container [] [
                Content.content [] (viewPage model dispatch) ]

            Footer.footer [] [
                Content.content [] [
                    str "By John Atwood" ] ] ]

    let init _ =
        let model, cmd = Home.init()
        let applicationCommand = Cmd.map HomeMsg cmd
        {PageModel = HomeModel model}, applicationCommand

    let update message model =
        match message, model.PageModel with
        | HomeMsg (Home.Msg.Select station), HomeModel _ ->
            let page = Pages.Page.Device(station.Key)
            navigate page model
        | HomeMsg Home.Msg.Add, HomeModel _ ->
            let page = Pages.Page.AddDevice
            navigate page model
        | HomeMsg cmd, HomeModel m ->
            let homeModel, homeMessage = Home.update cmd m
            {model with PageModel = HomeModel homeModel}, Cmd.map HomeMsg homeMessage
        | Navigate page, _ -> navigate page model
        | DeviceMsg cmd, DeviceModel m ->
            let deviceModel, deviceMessage = Device.update cmd m
            {model with PageModel = DeviceModel deviceModel}, Cmd.map DeviceMsg deviceMessage
        | AddDeviceMsg cmd, AddModel m ->
            let wrappedModel, wrappedMessage = AddDevice.update cmd m
            {model with PageModel = AddModel wrappedModel}, Cmd.map AddDeviceMsg wrappedMessage
        | _ ->
            failwithf "Unexpected message"


    let urlUpdate (result : Pages.Page option) (model: Model) =
        match result with
        | None -> failwith "Page not found"
        | Some (Pages.Page.Device(key)) ->
            let m, cmd = Device.init key
            { model with PageModel = DeviceModel m }, Cmd.map DeviceMsg cmd
        | Some Pages.Page.AddDevice ->
            let m, cmd = AddDevice.init
            { model with PageModel = AddModel m}, Cmd.map AddDeviceMsg cmd
        | Some Pages.Page.Home ->
            let m, cmd = Home.init()
            { model with PageModel = HomeModel m }, Cmd.map HomeMsg cmd

    let pageParser : Parser<Pages.Page option> = Pages.Pages.urlParser

    #if DEBUG
    open Elmish.Debug

    #endif

    // App
    Program.mkProgram init update view
    |> Program.toNavigable pageParser urlUpdate
    #if DEBUG
    |> Program.withConsoleTrace
    #endif
    |> Program.withReactBatched "elmish-app"
    #if DEBUG
    |> Program.withDebugger
    #endif
    |> Program.run