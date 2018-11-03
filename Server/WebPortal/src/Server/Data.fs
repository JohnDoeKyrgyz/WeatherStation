namespace WeatherStation
module Data =
    open WeatherStation.Model
    open WeatherStation.Shared
    open AzureStorage

    let allStations connectionString = async {
        let! repository = weatherStationRepository connectionString
        return! repository.GetAll()
    }
    let weatherStationDetails connectionString readingsCount key = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            let! readingsRepository = readingsRepository connectionString
            let! readings = readingsRepository.GetRecentReadings key.DeviceId readingsCount
            return Some (station, readings)
        | None -> return None
    }

    let weatherStationSettings connectionString key = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station -> return Some station.Settings
        | None -> return None
    }

    let updateWeatherStationSettings connectionString key settings = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            do! weatherStationRepository.Save {station with Settings = settings}
        | None -> () }