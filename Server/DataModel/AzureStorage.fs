namespace WeatherStation

module AzureStorage =

    open Microsoft.WindowsAzure.Storage    
    open Microsoft.WindowsAzure.Storage.Table

    open WeatherStation.Cache

    let createConnection connectionString =
        let storageAccount = CloudStorageAccount.Parse connectionString
        storageAccount.CreateCloudTableClient()

    let deviceTypes =
        let cases =
            typedefof<Model.DeviceType>
            |> FSharp.Reflection.FSharpType.GetUnionCases
        [for case in cases -> case.Name]

    let private repositoryCache = new Cache<string, obj>()

    let private getOrCreateRepository<'TRepository> key (builder : CloudTableClient -> Async<'TRepository>) connectionString =
        let cacheValueBuilder =
            async {
                let connection = createConnection connectionString
                let! repository = builder connection
                return box repository }
        async {
            let! repository = repositoryCache.GetOrCreate(key, cacheValueBuilder)
            return unbox<'TRepository> repository
        }

    let weatherStationRepository connectionString = getOrCreateRepository "WeatherStations" Repository.createWeatherStationsRepository connectionString
    let settingsRepository connectionString = getOrCreateRepository "SystemSettings" Repository.createSystemSettingRepository connectionString
    let readingsRepository connectionString = getOrCreateRepository "Readings" Repository.createReadingRepository connectionString