namespace WeatherStation.Functions
open WeatherStation

module Particle =

    open Microsoft.Extensions.Logging

    open FSharp.Data
    open WeatherStation.Readings

    [<Literal>]
    let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
    type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

    type ParticleEvent =
        | StatusMessage of Model.StatusMessage
        | Reading of Readings.DeviceReadings

    let parseParticleEvent (log: ILogger) content =
        let particleReading = ParticlePayload.Parse content

        match particleReading.Event with
        | "Reading" ->
            log.LogInformation(sprintf "Parsed particle reading for device %s" particleReading.DeviceId)
            let sensorValues = 
                WeatherStation.Sensors.parseReading 0xFFuy particleReading.Data
                @ [ReadingTime (particleReading.PublishedAt.UtcDateTime)]

            Reading { Readings = sensorValues; DeviceId = particleReading.DeviceId; Message = particleReading.Data}
        | "Status" ->
            StatusMessage {
                DeviceId = particleReading.DeviceId
                StatusMessage = particleReading.Data
                DeviceType = string Shared.DeviceType.Particle
                CreatedOn = particleReading.PublishedAt.UtcDateTime}
        | event ->
            failwithf "Unsupported Event %s" event            