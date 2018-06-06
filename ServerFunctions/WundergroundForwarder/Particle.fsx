#load @"..\Preamble.fsx"
#load "Model.fsx"

open System
open System.Text.RegularExpressions

open Microsoft.Azure.WebJobs.Host

open FSharp.Data
open Model

[<Literal>]
let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

let readingParser = new Regex(@"(?<SettingsCounter>\d+):(?<BatteryVoltage>\d+\.\d+):(?<PanelVoltage>\d+)\|(d(?<DhtTemperature>\d+\.\d+):(?<DhtHumidity>\d+.\d+))?(a(?<AnemometerWindSpeed>\d+\.\d+):(?<AnemometerDirection>\d+))?")

let readRegexGroups (log: TraceWriter) key builder (regexGroups : GroupCollection) =
    let value = regexGroups.[key : string]
    if value <> null && not (String.IsNullOrWhiteSpace(value.Value)) then 
        log.Info(sprintf "RegexValue %s %s" key value.Value)
        Some (builder value.Value) else None

let convertPanelVoltage rawValue = (rawValue / 4095.0) * 18.0

let valueParsers (log: TraceWriter) = 
    let read = readRegexGroups log    
    [
        read "BatteryVoltage" (double >> BatteryChargeVoltage)
        read "PanelVoltage" (double >> convertPanelVoltage >> PanelVoltage)
        read "DhtTemperature" (double >> TemperatureCelciusHydrometer)
        read "DhtHumidity" (double >> HumidityPercent)
        read "AnemometerWindSpeed" (double >> SpeedMetersPerSecond)
        read "AnemometerDirection" (int >> DirectionSixteenths)
    ]

let parseValues (log: TraceWriter) content =
    let particleReading = ParticlePayload.Parse content
    log.Info(sprintf "Parsed particle reading for device %s" particleReading.DeviceId)
    let sensorValues = [
        for regexMatch in readingParser.Matches(particleReading.Data) do                
            for valueParser in valueParsers log do
                let parseResult = valueParser regexMatch.Groups
                if parseResult.IsSome then
                    yield parseResult.Value ]
    sensorValues @ [ReadingTime (particleReading.PublishedAt.ToUniversalTime()); DeviceId particleReading.DeviceId]