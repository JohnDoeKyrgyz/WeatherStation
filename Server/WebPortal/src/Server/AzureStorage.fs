namespace WeatherStation

module AzureStorage =
    open System
    open System.Configuration
    open FSharp.Control.Tasks

    open Microsoft.WindowsAzure.Storage    
    open Microsoft.WindowsAzure.Storage.Table

    open WeatherStation.Shared
    open WeatherStation.Cache

    let connection =
        let connectionString = ConfigurationManager.ConnectionStrings.["AzureStorageConnection"].ConnectionString
        let storageAccount = CloudStorageAccount.Parse connectionString
        storageAccount.CreateCloudTableClient()

    let deviceTypes =
        let cases =
            typedefof<Model.DeviceType>
            |> FSharp.Reflection.FSharpType.GetUnionCases
        [for case in cases -> case.Name]

    let private repositoryCache = new Cache<string, obj>()

    let private getOrCreateRepository<'TRepository> key (builder : CloudTableClient -> Async<'TRepository>) =
        let cacheValueBuilder =
            async {
                let! repository = builder connection
                return box repository }
        async {
            let! repository = repositoryCache.GetOrCreate(key, cacheValueBuilder)
            return unbox<'TRepository> repository
        }

    let weatherStationRepository = getOrCreateRepository "WeatherStations" Repository.createWeatherStationsRepository        
    let settingsRepository = getOrCreateRepository "SystemSettings" Repository.createSystemSettingRepository
    
    let getWeatherStations activeThreshold = 
        task {
            let! repository = weatherStationRepository
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