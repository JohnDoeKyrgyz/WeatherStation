namespace WeatherStation

module AzureStorage =
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
    

    let getWeatherStations() = 
        task {
            let repository = Repository.createWeatherStationsRepository connection
            let! stations = repository.GetAll()
            return [
                for station in stations do
                    yield {
                        Name = station.WundergroundStationId
                        WundergroundId = station.WundergroundStationId
                        Status = Active
                        Location = {
                            Latitude = 0.0m
                            Longitude = 0.0m
                        }
                    }]
        }