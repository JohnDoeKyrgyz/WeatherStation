namespace WeatherStations

module AzureStorage =
    open WeatherStations.Shared

    open FSharp.Control.Tasks
    open FSharp.Azure.StorageTypeProvider

    type AzureTableStorage = AzureTypeProvider<connectionStringName = "AzureStorageConnection", configFileName="App.config">

    //TODO: This should not be harcoded. (Definition file somehere)
    let deviceTypes = [
        "Particle"
        "Devices"
        "Hologram"
    ]

    let getWeatherStations() = 
        task {
            return
                [for partitionKey in deviceTypes do
                    let partitionRows = AzureTableStorage.Tables.WeatherStations.GetPartition(partitionKey)
                    for row in partitionRows do 
                        let station = {
                            Name = row.RowKey
                            WundergroundId = string row.WundergroundStationId
                            Location = {Latitude = 0.0m; Longitude = 0.0m }
                            Status = Active
                        }
                        yield row
                ]
        }