namespace WeatherStation
open Newtonsoft.Json
module Data =
    open WeatherStation.Model
    open WeatherStation.Shared
    open AzureStorage

    let allStations connectionString = async {
        let! repository = weatherStationRepository connectionString
        return! repository.GetAll()
    }

    let weatherStationDetails connectionString readingsCount (key : StationKey) = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            let! readingsRepository = readingsRepository connectionString
            let! readings = readingsRepository.GetRecentReadings key.DeviceId readingsCount
            return Some (station, readings)
        | None -> return None
    }

    let weatherStationDetailsPage connectionString key fromDate readingsCount  = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            let! readingsRepository = readingsRepository connectionString
            let! readings = readingsRepository.GetPage key.DeviceId fromDate readingsCount
            return Some (station, readings)
        | None -> return None
    }

    let weatherStationSettings connectionString (key : StationKey) = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station -> 
            return
                if station.Settings = null 
                then None 
                else
                    let settings = JsonConvert.DeserializeObject<StationSettings>(station.Settings)
                    Some settings
        | None -> return None
    }

    let updateWeatherStationSettings connectionString (key : StationKey) (settings : StationSettings option) = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            let serializedSettings = 
                match settings with 
                | Some settings -> JsonConvert.SerializeObject(settings)
                | None -> null
            do! weatherStationRepository.Save {station with Settings = serializedSettings}
        | None -> () }