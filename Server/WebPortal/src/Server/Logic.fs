namespace WeatherStation
module Logic =

    open System        
    open FSharp.Control.Tasks
    
    open WeatherStation.Shared
    open AzureStorage
    
    let getWeatherStations connectionString activeThreshold = 
        task {
            let! repository = weatherStationRepository connectionString
            let! stations = repository.GetAll()
            return [
                for station in stations do
                    let lastReadingAge = station.LastReading.ToUniversalTime().Subtract(DateTime.Now.ToUniversalTime())
                    let status = if lastReadingAge > activeThreshold then Active else Offline
                    yield {
                        Name = station.DeviceId
                        WundergroundId = station.WundergroundStationId
                        Status = status
                        Location = {
                            Latitude = decimal station.Latitude
                            Longitude = decimal station.Longitude
                        }
                    }]
        }

