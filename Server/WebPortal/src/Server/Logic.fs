namespace WeatherStation
module Logic =

    open System
    
    open FSharp.Control.Tasks
    open FSharp.Data

    open WeatherStation.Model
    open WeatherStation.Shared
    open WeatherStation

    open AzureStorage

    let allStations connectionString = async {
        let! repository = weatherStationRepository connectionString
        return! repository.GetAll()
    }        

    let getWeatherStations activeThreshold allStations = 
        async {
            let! stations = allStations
            return [
                for (station : WeatherStation) in stations do
                    let stationLastReadingTime = (defaultArg station.LastReading DateTime.MinValue)
                    let lastReadingAge = DateTime.Now.Subtract(stationLastReadingTime)
                    let status = if lastReadingAge > activeThreshold then Active else Offline
                    yield {
                        Name = station.DeviceId
                        WundergroundId = station.WundergroundStationId
                        Status = status
                        Location = {
                            Latitude = decimal station.Latitude
                            Longitude = decimal station.Longitude
                        }
                    }] }

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

