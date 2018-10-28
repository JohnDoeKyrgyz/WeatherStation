namespace WeatherStation.Client
module Client =
    open System
    open Fable.Helpers.React
    open Fable.PowerPack
    open Fable.PowerPack.Fetch
    open Fable.Helpers.React.Props

    open Fulma
    open Thoth.Json
    open Fable.Import.React
    open Fulma.FontAwesome
    
    type Loadable<'T> =
        | Loading
        | Loaded of Result<'T, exn>

    let inline fetchAs url parameters =
        promise {
            let! response = fetch url parameters
            let! text = response.text()
            return Decode.Auto.unsafeFromString text
        }

    let button txt onClick =
        Button.button [ Button.IsFullWidth; Button.Color IsPrimary; Button.OnClick onClick ] [
            str txt ]

    let tableRow values = tr [] [for value in values -> td [] [str value]]

    let table headers data formatter =
        Table.table [Table.IsBordered; Table.IsFullWidth; Table.IsStriped] [
            thead [] [for header in headers -> th [] [str header]]
            tbody [] [for row in data -> tableRow (formatter row)]]

    let date (date : DateTime) = sprintf "%s %s" (date.ToString("MM/dd hh:mm:ss")) (if date.Hour >= 12 then "PM" else "AM")
    let number value = sprintf "%.2f" value

    type TabDef<'T> = {
        Content : seq<ReactElement>
        Key : 'T
        Name : string
        Icon : Fa.I.FontAwesomeIcons option
    }

    let tabs dispatch tabs activeTab options =
        div [] [
            yield
                Tabs.tabs options [
                    for tabDef in tabs do
                        let isActive = (activeTab = tabDef.Key)
                        yield Tabs.tab [Tabs.Tab.IsActive isActive] [
                            a [OnClick (fun _ -> dispatch tabDef.Key)] [
                                if tabDef.Icon.IsSome then yield Icon.faIcon [] [Fa.icon tabDef.Icon.Value]
                                yield span [] [str tabDef.Name]]]]
            for tabDef in tabs do
                let isActive = (activeTab = tabDef.Key)
                yield div [Hidden (not isActive)] tabDef.Content]

    ///Displays feedback to the user for loadable content
    let loader loadable onLoaded =
        match loadable with
        | Loaded (Error error) -> str (string error)
        | Loading -> str "Loading..."
        | Loaded (Ok data) -> onLoaded data
        