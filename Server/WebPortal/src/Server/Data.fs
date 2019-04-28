namespace WeatherStation
open Newtonsoft.Json
module Data =
    open System

    open WeatherStation.Model
    open WeatherStation.Shared

    open AzureStorage

    let allStations connectionString = async {
        let! repository = weatherStationRepository connectionString
        return! repository.GetAll()
    }

    let weatherStationDetails connectionString pageSize (key : StationKey) = async {
        let! weatherStationRepository = weatherStationRepository connectionString
        match! weatherStationRepository.Get (parseDeviceType key.DeviceType) key.DeviceId with
        | Some station ->
            let! readingsRepository = readingsRepository connectionString
            let lastReadingDate = defaultArg station.LastReading DateTime.Now
            let cutOffDate = lastReadingDate - pageSize
            let! readings = readingsRepository.GetRecentReadings key.DeviceId cutOffDate
            return Some (station, readings)
        | None -> return None
    }

    let readings connectionString key fromDate tooDate  = async {
        if fromDate >= tooDate then failwithf "fromDate %A should be less than tooDate %A" fromDate tooDate
        let! readingsRepository = readingsRepository connectionString
        let! readings = readingsRepository.GetPage key.DeviceId fromDate tooDate
        return readings
    }

    let weatherStationSettings connectionString key = async {
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
            let updatedSettings, serializedSettings = 
                match settings with 
                | Some settings ->
                    //increment the Version property
                    let settingsVersion =
                        if station.Settings <> null then
                            let existingSettings = JsonConvert.DeserializeObject<StationSettings>(station.Settings)
                            existingSettings.Version + 1
                        else 1
                    let settings = {settings with Version = settingsVersion}
                    Some settings, JsonConvert.SerializeObject(settings)
                | None -> None, null
            do! weatherStationRepository.Save {station with Settings = serializedSettings}
            return updatedSettings
        | None -> return None }