namespace WeatherStation.Client

module Client =
    open System
    
    open Fable.React
    open Fable.React.Props
    open Fable.Recharts
    open Fable.Recharts.Props

    open Thoth.Json
    open Fable.FontAwesome
    open Fulma

    open Fetch

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

    let button txt onClick icon =
        Button.button [ Button.IsFullWidth; Button.Color IsPrimary; Button.OnClick onClick ] [
            span [][str txt]
            span [Class "icon"][
                Icon.icon [] [Fa.i [icon] []]]]

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
        Icon : Fa.IconOption option
    }

    let tabs dispatch tabs activeTab options =
        div [] [
            yield
                Tabs.tabs options [
                    for tabDef in tabs do
                        let isActive = (activeTab = tabDef.Key)
                        yield Tabs.tab [Tabs.Tab.IsActive isActive] [
                            a [OnClick (fun _ -> dispatch tabDef.Key)] [
                                if tabDef.Icon.IsSome then yield Icon.icon [] [Fa.i [tabDef.Icon.Value] []]
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

    module P = Fable.React.Props

    let margin t r b l =
        Chart.Margin { top = t; bottom = b; right = r; left = l }

    let readingsChart data lineInfos =
        responsiveContainer [Chart.Height 300.0] [
            lineChart [margin 0. 50. 120. 0.; Chart.Data (data |> Array.ofSeq)] [
                yield xaxis [Cartesian.DataKey "time"; Cartesian.Custom("angle", 67.5); Cartesian.Custom("textAnchor", "start")] []
                yield yaxis [][]
                yield tooltip [][]
                yield cartesianGrid [][]
                yield legend [Legend.VerticalAlign "top"] []
                for (key, color) in lineInfos do
                    yield line [Cartesian.DataKey key; P.Fill color; Cartesian.Stroke color][]]]

    let formControl label control additionalControls =
        Field.div [Field.Option.IsHorizontal] [
            div [P.Class "field-label"] [
                Label.label [] [str label]]
            div [P.Class "field-body"][
                Field.div [][
                    yield Control.div [] [control]
                    yield! additionalControls]]]

    let simpleFormControl label control = formControl label control []

    let numberInput<'T> converter value onChange =
        Input.number [
            match value with
            | Some (value : 'T) ->
                yield Input.Option.Value (value.ToString())
            | None -> ()
            yield Input.OnChange (fun event ->
                let stringValue = event.Value
                let actualValue : 'T = converter stringValue
                onChange actualValue)]

    let intInput value onChange = numberInput int value onChange
    let decimalInput value onChange = numberInput decimal value onChange

    let checkBoxInput (value : bool option) onChange =
        Checkbox.checkbox [] [Checkbox.input [Props [
            if value.IsSome then yield Props.Checked value.Value;
            yield OnChange (fun event -> onChange  event.Checked)]]]