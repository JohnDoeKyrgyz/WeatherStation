namespace WeatherStation
module Data =
    open AzureStorage

    let allStations connectionString = async {
        let! repository = weatherStationRepository connectionString
        return! repository.GetAll()
    }

    let weatherStationDetails connectionString readingsCount deviceType id = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get deviceType id with
        | Some station ->
            let! readingsRepository = readingsRepository connectionString
            let! readings = readingsRepository.GetRecentReadings id readingsCount
            return Some (station, readings)
        | None -> return None
    }