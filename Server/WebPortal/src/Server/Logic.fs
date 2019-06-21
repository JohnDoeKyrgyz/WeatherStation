namespace WeatherStation

module Logic =

    open System

    open FSharp.Control.Tasks
    open FSharp.Data

    open Particle.SDK

    open WeatherStation.Model
    open WeatherStation.Shared
    open WeatherStation

    let toOption value = if isNull value then None else Some value

    let getWeatherStations activeThreshold allStations =
        async {
            let! stations = allStations
            return [
                for (station : WeatherStation) in stations do
                    let stationLastReadingTime = (defaultArg station.LastReading DateTime.MinValue)
                    let lastReadingAge = DateTime.Now.Subtract(stationLastReadingTime)
                    let status = if lastReadingAge < activeThreshold then Active else Offline
                    yield {
                        Key = {DeviceId = station.DeviceId.Trim(); DeviceType = station.DeviceType.Trim()}
                        Name = station.DeviceId.Trim()
                        WundergroundId = station.WundergroundStationId |> toOption |> Option.map (fun v -> v.Trim())
                        Status = status
                        Location = {
                            Latitude = decimal station.Latitude
                            Longitude = decimal station.Longitude
                        }
                    }] }

    let createReading (reading : Model.Reading) = {
        DeviceTime = reading.DeviceTime
        ReadingTime = reading.ReadingTime
        PanelMilliamps = reading.PanelMilliamps
        BatteryChargeVoltage = reading.BatteryChargeVoltage
        PanelVoltage = reading.PanelVoltage
        TemperatureCelciusHydrometer = reading.TemperatureCelciusHydrometer
        TemperatureCelciusBarometer = reading.TemperatureCelciusBarometer
        HumidityPercentHydrometer = reading.HumidityPercentHydrometer
        HumidityPercentBarometer = reading.HumidityPercentBarometer
        PressurePascal = reading.PressurePascal
        GustMetersPerSecond = reading.GustMetersPerSecond
        SpeedMetersPerSecond = reading.SpeedMetersPerSecond
        DirectionDegrees = reading.DirectionDegrees
    }

    let getWeatherStationDetails pageSizeHours data = async {
        match! data with
        | Some (station : WeatherStation, readings : Model.Reading list ) ->
            let readings = readings |> List.sortByDescending (fun reading -> reading.ReadingTime)
            return
                Some {
                    Key = {DeviceId = station.DeviceId; DeviceType = station.DeviceType}
                    Name = station.DeviceId
                    CreatedOn = station.CreatedOn
                    WundergroundId = toOption station.WundergroundStationId
                    Location = {Latitude = 0.0m; Longitude = 0.0m}
                    LastReading = station.LastReading
                    Readings = readings |> List.map createReading
                    PageSizeHours = pageSizeHours
                }
        | None -> return None
    }

    [<Literal>]
    let ParticleSettingsJson = __SOURCE_DIRECTORY__ + "/../../../../DeviceFirmware/ParticleElectron/src/Settings.json"
    type ParticleSettings = JsonProvider< ParticleSettingsJson >

    let updateParticleDeviceSettings (key : StationKey) (settings : StationSettings) =
        if parseDeviceType key.DeviceType <> Particle then failwithf "DeviceType %s is not supported" key.DeviceType
        task {
            let! particleCloud = ParticleConnect.connect |> Async.StartAsTask
            match particleCloud with
            | Ok particleCloud ->
                let particleSettings =
                    ParticleSettings.Root(
                        settings.Version,
                        settings.BrownoutPercentage,
                        settings.Brownout,
                        settings.BrownoutMinutes,
                        settings.SleepTime,
                        settings.DiagnosticCycles,
                        settings.UseDeepSleep)
                let serializedSettings = particleSettings.JsonValue.ToString()
                try
                    let! result = particleCloud.PublishEventAsync("Settings", serializedSettings)
                    return Ok result
                with
                | :? ParticleRequestBadRequestException as ex ->
                    return Error ex.Message
            | Error error -> return Error error.Message
        }



