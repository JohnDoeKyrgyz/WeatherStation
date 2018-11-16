namespace WeatherStation.Client
module Client =
    open System
    open Fable.Helpers.React
    open Fable.PowerPack
    open Fable.PowerPack.Fetch
    open Fable.Helpers.React.Props
    open Fable.Recharts
    open Fable.Recharts.Props

    open Fulma
    open Thoth.Json
    open Fable.Import.React
    open Fulma.FontAwesome
    
    type Loadable<'T> =
        | NotLoading
        | Loading
        | Loaded of Result<'T, exn>

    let inline fetchAs<'T> url parameters =
        promise {
            let! response = fetch url parameters
            let! text = response.text()
            return Decode.Auto.unsafeFromString<'T> text
        }

    let button txt onClick =
        Button.button [ Button.IsFullWidth; Button.Color IsPrimary; Button.OnClick onClick ] [
            str txt ]

    let tableRow values = tr [] [for value in values -> td [] [str value]]

    let table headers data formatter =
        Table.table [Table.IsBordered; Table.IsFullWidth; Table.IsStriped] [
            thead [] [
                tr [] [for header in headers -> th [] [str header]]]
            tbody [] [
                for row in data -> tableRow (formatter row)]]

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

    let spinner message = div [Class "loading"] [str message]

    ///Displays feedback to the user for loadable content
    let loader loadable onLoaded =
        match loadable with
        | Loaded (Error error) -> [str (string error)]
        | Loading -> [spinner "Loading..."]
        | Loaded (Ok data) -> [onLoaded data]
        | NotLoading -> []

    module P = Fable.Helpers.React.Props
    
    type Point = {x: string; y: float}
    type Voltage = {time: string; battery: float; panel: float}

    let readingsChart (data : seq<Point>) =    
        lineChart [ Chart.Height 300.0; Chart.Width 900.0; Chart.Data (data |> Array.ofSeq) ] [
            xaxis [Cartesian.DataKey "x"; Cartesian.Label "x"] []
            yaxis [] []
            tooltip [][]
            cartesianGrid [][]
            line [Cartesian.DataKey "y"; P.Fill "#88c188"] [] ]

    let margin t r b l =
        Chart.Margin { top = t; bottom = b; right = r; left = l }            

    let voltageChart (data : seq<Voltage>) =
        responsiveContainer [Chart.Height 300.0] [
            lineChart [ margin 0. 50. 120. 0.; Chart.Data (data |> Array.ofSeq) ] [            
                xaxis [Cartesian.DataKey "time"; Cartesian.Custom("angle", 67.5); Cartesian.Custom("textAnchor", "start")] []
                yaxis [] []
                tooltip [][]
                cartesianGrid [][]
                legend [Legend.VerticalAlign "top"] []
                line [Cartesian.DataKey "battery"; P.Fill "red"; Cartesian.Stroke "red"] []
                line [Cartesian.DataKey "panel"; P.Fill "orange"; Cartesian.Stroke "orange"] [] ]]

    let formControl label control additionalControls =
        Field.div [Field.Option.IsHorizontal] [
            div [P.Class "field-label"] [
                Label.label [] [str label]]
            div [P.Class "field-body"][
                Field.div [][
                    yield Control.div [] [control]
                    yield! additionalControls]]]

    let simpleFormControl label control = formControl label control []

    let numberInput (value : int option) onChange =
        Input.number [
            if value.IsSome then
                yield Input.Option.Value (string value)
            yield Input.OnChange (fun event -> onChange (int event.Value))]
            
    let checkBoxInput (value : bool option) onChange =
        Checkbox.checkbox [] [Checkbox.input [Props [
            if value.IsSome then yield Props.Checked value.Value;
            yield OnChange (fun event -> onChange (event.Value = "on"))]]]            