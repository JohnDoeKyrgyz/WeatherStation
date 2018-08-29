namespace WeatherStation

module AzureStorage =
    open System
    open System.Configuration
    open FSharp.Control.Tasks
    open Microsoft.WindowsAzure.Storage
    open WeatherStation
    open WeatherStation.Shared

    let connection =
        let connectionString = ConfigurationManager.ConnectionStrings.["AzureStorageConnection"].ConnectionString
        let storageAccount = CloudStorageAccount.Parse connectionString
        storageAccount.CreateCloudTableClient()

    let deviceTypes =
        let cases =
            typedefof<Model.DeviceType>
            |> FSharp.Reflection.FSharpType.GetUnionCases
        [for case in cases -> case.Name]

    let weatherStationRepository = Repository.createWeatherStationsRepository connection
    let settingsRepository = Repository.createSystemSettingRepository connection

    let getSystemSetting key =
        task {
        }

    let getWeatherStations activeThreshold = 
        task {
            let! repository = Repository.createWeatherStationsRepository connection
            let! stations = repository.GetAll()
            return [
                for station in stations do
                    let lastReadingAge = station.LastReading.ToUniversalTime().Subtract(DateTime.Now.ToUniversalTime())
                    let status = if lastReadingAge > activeThreshold then Active else Offline
                    yield {
                        Name = station.WundergroundStationId
                        WundergroundId = station.WundergroundStationId
                        Status = status
                        Location = {
                            Latitude = decimal station.Latitude
                            Longitude = decimal station.Longitude
                        }
                    }]
        }