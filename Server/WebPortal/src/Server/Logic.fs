namespace WeatherStation

module Logic =

    open System
    
    open FSharp.Control.Tasks
    open FSharp.Data

    open WeatherStation.Model
    open WeatherStation.Shared
    open WeatherStation    

    let getWeatherStations activeThreshold allStations = 
        async {
            let! stations = allStations
            return [
                for (station : WeatherStation) in stations do
                    let stationLastReadingTime = (defaultArg station.LastReading DateTime.MinValue)
                    let lastReadingAge = DateTime.Now.Subtract(stationLastReadingTime)
                    let status = if lastReadingAge < activeThreshold then Active else Offline
                    yield {
                        Name = station.DeviceId.Trim()
                        WundergroundId = station.WundergroundStationId.Trim()
                        DeviceId = station.DeviceId.Trim()
                        Status = status
                        Location = {
                            Latitude = decimal station.Latitude
                            Longitude = decimal station.Longitude
                        }
                    }] }

    let getWeatherStationDetails data = async {
        match! data with
        | Some (station : WeatherStation, readings : Model.Reading list ) ->
            return
                Some {
                    Name = station.WundergroundStationId
                    WundergroundId = station.WundergroundStationId
                    DeviceId = station.DeviceId
                    Location = {Latitude = 0.0m; Longitude = 0.0m}
                    LastReading = None
                    Readings = [
                        for reading in readings -> {
                            DeviceTime = reading.DeviceTime
                            ReadingTime = reading.ReadingTime
                            SupplyVoltage = reading.SupplyVoltage
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
                        }]
                }
        | None -> return None
    }

    [<Literal>]
    let particleSettingsJson = __SOURCE_DIRECTORY__ + "/../../../../DeviceFirmware/ParticleElectron/src/Settings.json"
    type ParticleSettings = JsonProvider< particleSettingsJson >

    let updateSettings deviceId (settings : ParticleSettings.Root) =
        task {
            let! particleCloud = ParticleConnect.connect |> Async.StartAsTask
            let! device = particleCloud.GetDeviceAsync deviceId
            let serializedSettings = settings.JsonValue.ToString()
            let! result = device.RunFunctionAsync("Settings", serializedSettings)
            return result
        }

