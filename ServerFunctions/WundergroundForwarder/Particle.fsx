#load @"..\Preamble.fsx"
#load "Model.fsx"

open System.Text.RegularExpressions

open FSharp.Data
open Model

[<Literal>]
let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

let readingParser = new Regex(@"(?<SettingsCounter>\d+):(?<BatteryVoltage>\d+\.\d+):(?<PanelVoltage>\d+)\|(d(?<DhtTemperature>\d+\.\d+):(?<DhtHumidity>\d+.\d+))?(a(?<AnemometerWindSpeed>\d+\.\d+):(?<AnemometerDirection>\d+))?")

let readRegexGroups key builder (regexGroups : GroupCollection) =
    let value = regexGroups.[key : string]
    if value <> null then Some (builder value.Value) else None

let valueParsers = [
    readRegexGroups "BatteryVoltage" (int >> BatteryChargeVoltage)
    readRegexGroups "PanelVoltage" (int >> PanelVoltage)
    readRegexGroups "DhtTemperature" (double >> TemperatureCelciusHydrometer)
    readRegexGroups "DhtHumidity" (double >> HumidityPercent)
    readRegexGroups "AnemometerWindSpeed" (double >> SpeedMetersPerSecond)
    readRegexGroups "AnemometerDirection" (int >> DirectionSixteenths)]

let parseValues content =
    let particleReading = ParticlePayload.Parse content
    let sensorValues = [
        for regexMatch in readingParser.Matches(particleReading.Data) do                
            for valueParser in valueParsers do
                let parseResult = valueParser regexMatch.Groups
                yield parseResult ]
    sensorValues @ [ReadingTime (particleReading.PublishedAt.ToUniversalTime())]