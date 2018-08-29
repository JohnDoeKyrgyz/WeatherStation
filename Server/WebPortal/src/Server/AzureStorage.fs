namespace WeatherStations

module AzureStorage =
    open System.Configuration
    open FSharp.Control.Tasks
    open Microsoft.WindowsAzure.Storage
    open WeatherStation

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
            return! repository.GetAll()
        }