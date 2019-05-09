namespace WeatherStation.Functions

module Particle =

    open System
    open System.Text.RegularExpressions

    open Microsoft.Azure.WebJobs.Host    
    open Microsoft.Extensions.Logging

    open FSharp.Data
    open Model

    [<Literal>]
    let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
    type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

    let readingParser = Regex(@"(?<SettingsCounter>\d+):(?<BatteryVoltage>\d+\.\d+):(?<PanelVoltage>\d+)\|(b(?<BmeTemperature>\d+\.\d+):(?<BmePressure>\d+.\d+):(?<BmeHumidity>\d+.\d+))(d(?<DhtTemperature>\d+\.\d+):(?<DhtHumidity>\d+.\d+))?(a(?<AnemometerWindSpeed>\d+\.\d+):(?<AnemometerDirection>\d+))?")

    let readRegexGroups (log: ILogger) key builder (regexGroups : GroupCollection) =
        let value = regexGroups.[key : string]
        if (not (isNull value)) && not (String.IsNullOrWhiteSpace(value.Value)) then 
            log.LogInformation(sprintf "RegexValue %s %s" key value.Value)
            Some (builder value.Value) else None

    let convertPanelVoltage rawValue = (rawValue / 4095.0m<_>) * 18.0m<_>

    let valueParsers (log: ILogger) = 
        let read = readRegexGroups log    
        [
            read "BatteryVoltage" (decimal >> LanguagePrimitives.DecimalWithMeasure<volts> >> BatteryChargeVoltage)
            read "PanelVoltage" (decimal >> LanguagePrimitives.DecimalWithMeasure<volts> >> convertPanelVoltage >> PanelVoltage)
            read "BmeTemperature" (decimal >> LanguagePrimitives.DecimalWithMeasure<celcius> >> TemperatureCelciusBarometer)
            read "BmePressure" (decimal >> LanguagePrimitives.DecimalWithMeasure<pascal> >> PressurePascal)
            read "BmeHumidity" (decimal >> LanguagePrimitives.DecimalWithMeasure<percent> >> HumidityPercentBarometer)
            read "DhtTemperature" (decimal >> LanguagePrimitives.DecimalWithMeasure<celcius> >> TemperatureCelciusHydrometer)
            read "DhtHumidity" (decimal >> LanguagePrimitives.DecimalWithMeasure<percent> >> HumidityPercentHydrometer)
            read "AnemometerWindSpeed" (decimal >> LanguagePrimitives.DecimalWithMeasure<meters/seconds> >> SpeedMetersPerSecond)
            read "AnemometerDirection" (int >> LanguagePrimitives.Int32WithMeasure<sixteenths> >> DirectionSixteenths)
        ]

    let parseValues (log: ILogger) content =
        let particleReading = ParticlePayload.Parse content
        log.LogInformation(sprintf "Parsed particle reading for device %s" particleReading.DeviceId)
        let matches = readingParser.Matches(particleReading.Data)
        let sensorValues = [
            for regexMatch in matches do                
                for valueParser in valueParsers log do
                    let parseResult = valueParser regexMatch.Groups
                    if parseResult.IsSome then
                        yield parseResult.Value ]
        { Readings = sensorValues @ [ReadingTime (particleReading.PublishedAt.UtcDateTime)]; DeviceId = particleReading.DeviceId}