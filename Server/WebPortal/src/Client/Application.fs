namespace WeatherStation.Client

module Application =
    
    open Elmish
    open Elmish.React
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

    // VIEW
    open Fable.Helpers.React
    
    /// Constructs the view for a page given the model and dispatcher.
    let viewPage model dispatch =
        match model.PageModel with
        | HomeModel m -> Home.view dispatch m
        | DeviceModel m -> Device.view dispatch m

    /// Constructs the view for the application given the model.
    let view model dispatch =
        div [] [
            //Menu.view (Logout >> dispatch) model.User
            hr []
            //div [ centerStyle "column" ] (viewPage model dispatch)
            viewPage model dispatch
        ]

    let init() =
        let model, cmd = Home.init()
        let applicationCommand = Cmd.map HomeMsg cmd
        {PageModel = PageModel.HomeModel model}, applicationCommand

    let update message model =
        match message, model.PageModel with
        | HomeMsg cmd, PageModel.HomeModel m ->
            let homeModel, homeMessage = Home.update cmd m
            {model with PageModel = PageModel.HomeModel homeModel}, Cmd.map HomeMsg homeMessage
        | DeviceMsg cmd, PageModel.DeviceModel m ->
            let deviceModel, deviceMessage = Device.update cmd m
            {model with PageModel = PageModel.DeviceModel deviceModel}, Cmd.map DeviceMsg deviceMessage
        | _ -> failwithf "Unexpected message"


    #if DEBUG
    open Elmish.Debug
    open Elmish.HMR

    #endif

    Program.mkProgram init update view
    #if DEBUG
    |> Program.withConsoleTrace
    |> Program.withHMR
    #endif
    |> Program.withReact "elmish-app"
    #if DEBUG
    |> Program.withDebugger
    #endif
    |> Program.run