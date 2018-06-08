#load "Model.fsx"

open System

open FSharp.Data
open Microsoft.Azure.WebJobs.Host
open Model

[<Literal>]
let HologramSample = __SOURCE_DIRECTORY__ + @"/HologramStatusUpdate.json"
type HologramPayload = JsonProvider<HologramSample, SampleIsList = true>

let parseValues (log: TraceWriter) content =

    let parsePayload (data : string) =

        let data = data.Split([|":"|], StringSplitOptions.None)

        let readOptional reader i builder =
            let value = data.[i]
            if String.IsNullOrWhiteSpace( value ) |> not then Some (builder (reader value)) else None
        let readOptionalDecimal = readOptional (fun v -> (Convert.ToDecimal v))
        let readInt i = int (Convert.ToInt32(data.[i]) )
        let readOptionalInt = readOptional Convert.ToInt32

        let year = readInt 10
        let month = readInt 11
        let day = readInt 12
        let hour = readInt 13
        let minute = readInt 14
        let second = readInt 15

        let time = DateTime(int year, int month, int day, int hour, int minute, int second)

        let possibleValues = [
            Some (RefreshInterval(readInt 0 * 1<seconds>))
            readOptionalDecimal 1 (LanguagePrimitives.DecimalWithMeasure >> TemperatureCelciusHydrometer)
            readOptionalDecimal 2 (LanguagePrimitives.DecimalWithMeasure >> HumidityPercent)
            readOptionalDecimal 3 (LanguagePrimitives.DecimalWithMeasure >> TemperatureCelciusBarometer)
            readOptionalDecimal 4 (LanguagePrimitives.DecimalWithMeasure >> PressurePascal)
            readOptionalDecimal 5 (LanguagePrimitives.DecimalWithMeasure >> SupplyVoltage)
            readOptionalDecimal 6 (LanguagePrimitives.DecimalWithMeasure >> BatteryChargeVoltage)
            readOptionalDecimal 7 (LanguagePrimitives.DecimalWithMeasure >> PanelVoltage)
            readOptionalDecimal 8 (LanguagePrimitives.DecimalWithMeasure >> SpeedMetersPerSecond)
            readOptionalInt 9 (LanguagePrimitives.Int32WithMeasure >> DirectionSixteenths)
            Some (ReadingTime time)]

        [for value in possibleValues do
            match value with
            | Some value -> yield value
            | None -> ()]

    let payload = HologramPayload.Parse content
    log.Info(sprintf "Parsed Hologram reading for device %d" payload.SourceDevice)
    {Readings = parsePayload payload.Body; DeviceId = (string payload.SourceDevice)}