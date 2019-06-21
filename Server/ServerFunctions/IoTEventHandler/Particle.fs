namespace WeatherStation.Functions

module Particle =

    open Microsoft.Extensions.Logging

    open FSharp.Data
    open WeatherStation.Readings

    [<Literal>]
    let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
    type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

    let parseValues (log: ILogger) content =
        let particleReading = ParticlePayload.Parse content
        log.LogInformation(sprintf "Parsed particle reading for device %s" particleReading.DeviceId)
        let sensorValues = 
            WeatherStation.Sensors.parseReading 0xFFuy particleReading.Data
            @ [ReadingTime (particleReading.PublishedAt.UtcDateTime)]

        { Readings = sensorValues; DeviceId = particleReading.DeviceId; Message = particleReading.Data}