namespace WeatherStation.Client

module Application =
    open System
    open Elmish
    open Elmish.React    
    open Elmish.Browser.Navigation

    open Fable.Helpers.React
    open Fable.Helpers.React.Props

    open WeatherStation.Client.Pages
    
    /// The composed model for the different possible page states of the application
    type PageModel =
        | HomeModel of Home.Model
        | DeviceModel of Device.Model

    /// The composed model for the application, which is a single page state plus login information
    type Model = {
        PageModel : PageModel }

    /// The composed set of messages that update the state of the application
    type Msg =
        | HomeMsg of Home.Msg
        | DeviceMsg of Device.Msg
        | Navigate of Pages.Page

    // VIEW
    open Fulma
    
    /// Constructs the view for a page given the model and dispatcher.
    let viewPage model dispatch =
        match model.PageModel with
        | HomeModel m -> Home.view (HomeMsg >> dispatch) m
        | DeviceModel m -> Device.view (DeviceMsg >> dispatch) m

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
        | Pages.Page.Device key -> gotoPage DeviceMsg DeviceModel (Device.init key)

    let view model dispatch =
        let today = DateTime.Now
        
        let homeCrumb active = Breadcrumb.item [Breadcrumb.Item.IsActive active] [a [OnClick (fun _ -> dispatch (Msg.Navigate Pages.Page.Home))] [ str "Home" ]]

        div [] [            
            Navbar.navbar [ Navbar.Color IsPrimary ] [
                Navbar.Item.div [Navbar.Item.Option.IsTab] [
                    Breadcrumb.breadcrumb [Breadcrumb.HasBulletSeparator] [
                        match model.PageModel with
                        | PageModel.HomeModel _ ->
                            yield homeCrumb true
                        | PageModel.DeviceModel deviceModel ->
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
        {PageModel = PageModel.HomeModel model}, applicationCommand

    let update message model =
        match message, model.PageModel with
        | HomeMsg (Home.Msg.Select station), PageModel.HomeModel _ ->
            let page = Pages.Page.Device(station.Key)
            navigate page model
        | HomeMsg cmd, PageModel.HomeModel m ->
            let homeModel, homeMessage = Home.update cmd m
            {model with PageModel = PageModel.HomeModel homeModel}, Cmd.map HomeMsg homeMessage
        | Navigate page, _ -> navigate page model
        | DeviceMsg cmd, PageModel.DeviceModel m ->
            let deviceModel, deviceMessage = Device.update cmd m
            {model with PageModel = PageModel.DeviceModel deviceModel}, Cmd.map DeviceMsg deviceMessage
        | _ -> failwithf "Unexpected message"


    let urlUpdate (result : Pages.Page option) (model: Model) =
        match result with
        | None -> failwith "Page not found"
        | Some (Pages.Page.Device(key)) ->
            let m, cmd = Device.init key
            { model with PageModel = DeviceModel m }, Cmd.map DeviceMsg cmd
        | Some Pages.Page.Home ->
            let m, cmd = Home.init()
            { model with PageModel = HomeModel m }, Cmd.map HomeMsg cmd

    let pageParser : Parser<Pages.Page option> = Pages.Pages.urlParser

    #if DEBUG
    open Elmish.Debug
    open Elmish.HMR

    #endif

    // App
    Program.mkProgram init update view
    |> Program.toNavigable pageParser urlUpdate
    #if DEBUG
    |> Program.withConsoleTrace
    |> Program.withHMR
    #endif
    |> Program.withReact "elmish-app"
    #if DEBUG
    |> Program.withDebugger
    #endif
    |> Program.run