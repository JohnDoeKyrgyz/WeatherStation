#load "../Preamble.fsx"
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

let convertPanelVoltage rawValue = (rawValue / 4095.0m<_>) * 18.0m<_>

let valueParsers (log: TraceWriter) = 
    let read = readRegexGroups log    
    [
        read "BatteryVoltage" (decimal >> LanguagePrimitives.DecimalWithMeasure<volts> >> BatteryChargeVoltage)
        read "PanelVoltage" (decimal >> LanguagePrimitives.DecimalWithMeasure<volts> >> convertPanelVoltage >> PanelVoltage)
        read "DhtTemperature" (decimal >> LanguagePrimitives.DecimalWithMeasure<celcius> >> TemperatureCelciusHydrometer)
        read "DhtHumidity" (decimal >> LanguagePrimitives.DecimalWithMeasure<percent> >> HumidityPercent)
        read "AnemometerWindSpeed" (decimal >> LanguagePrimitives.DecimalWithMeasure<metersPerSecond> >> SpeedMetersPerSecond)
        read "AnemometerDirection" (int >> LanguagePrimitives.Int32WithMeasure<sixteenths> >> DirectionSixteenths)
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