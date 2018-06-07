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
        let readOptionalDouble = readOptional Convert.ToDouble
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
            Some (RefreshInterval(readInt 0))
            readOptionalDouble 1 TemperatureCelciusHydrometer
            readOptionalDouble 2 HumidityPercent
            readOptionalDouble 3 TemperatureCelciusBarometer
            readOptionalDouble 4 PressurePascal
            readOptionalDouble 5 SupplyVoltage
            readOptionalDouble 6 BatteryChargeVoltage
            readOptionalDouble 7 PanelVoltage
            readOptionalDouble 8 SpeedMetersPerSecond
            readOptionalInt 9 DirectionSixteenths
            Some (ReadingTime time)
        ]

        [for value in possibleValues do
            match value with
            | Some value -> yield value
            | None -> ()]

    let payload = HologramPayload.Parse content
    log.Info(sprintf "Parsed Hologram reading for device %d" payload.SourceDevice)
    parsePayload payload.Body @ [DeviceId (string payload.SourceDevice)]    