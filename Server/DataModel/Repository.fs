namespace WeatherStation

module Repository =
    open System
    open System.Linq

    open Microsoft.Azure.Cosmos.Table    
    open FSharp.Azure.Storage.Table

    open WeatherStation.Model

    type IRepository<'TEntity> =
        abstract member GetAll : unit -> Async<'TEntity list>
        abstract member Save : 'TEntity -> Async<unit>

    type ISystemSettingsRepository =
        inherit IRepository<SystemSetting>        
        abstract member GetSettingWithDefault : key:string -> defaultValue:string -> Async<SystemSetting>
        abstract member GetSetting : key:string -> Async<SystemSetting>

    type IWeatherStationsRepository =
        inherit IRepository<WeatherStation>
        abstract member Get : deviceType:DeviceType -> deviceId:string -> Async<WeatherStation option>

    type IReadingsRepository =
        inherit IRepository<Reading>
        abstract member GetHistory : deviceId:string -> cutOff:DateTime -> Async<Reading list>
        abstract member GetPage : deviceId:string -> from:DateTime -> too:DateTime -> Async<Reading list>
        abstract member GetRecentReadings : deviceId:string -> cutOff:DateTime -> Async<Reading list>

    type IStatusMessagesRepository =
        inherit IRepository<StatusMessage>
        abstract member GetDeviceStatuses : deviceId:string -> fromDate:DateTime -> tooDate:DateTime -> Async<StatusMessage list>

    let createTableIfNecessary (connection : CloudTableClient) tableName =
        let tableReference = connection.GetTableReference(tableName)
        async {
            let! tableCreated = tableReference.CreateIfNotExistsAsync() |> Async.AwaitTask
            if(tableCreated) 
            then printfn "Created Table [%s]" tableName 
            else printfn "Table [%s] already exists" tableName
        }

    let runQuery connection tableName query =
        async {
            let! data =
                query
                |> fromTableAsync connection tableName
            let result = [for entity, _ in data -> entity]
            return result
        }

    type AzureStorageRepository<'TEntity>(connection, tableName) =
        let runQuery = runQuery connection tableName
        member internal this.Save entity =
            async {
                do!
                    InsertOrReplace entity
                    |> inTableAsync connection tableName
                    |> Async.Ignore }
        interface IRepository<'TEntity> with
            member this.GetAll() = runQuery Query.all<'TEntity>
            member this.Save entity = this.Save entity

    type SystemSettingsRepository(connection, tableName) =
        inherit AzureStorageRepository<SystemSetting>(connection, tableName)
        interface ISystemSettingsRepository with
            member this.GetSetting key = 
                async {
                    let! results =
                        Query.all<SystemSetting>
                        |> Query.where <@ fun setting _ -> setting.Key = key @>
                        |> runQuery connection tableName
                    return results.Single()}
            member this.GetSettingWithDefault key defaultValue = 
                async {
                    let! settings =
                        Query.all<SystemSetting>
                        |> Query.where <@ fun setting _ -> setting.Key = key @>
                        |> runQuery connection tableName
                    let! setting =
                        match settings with
                        | [setting] -> async { return setting }
                        | [] ->
                            async {
                                let setting = {Group = "Default"; Key = key; Value = defaultValue}
                                printfn "Adding setting %s %s" key defaultValue
                                let! settingInsertResult =
                                    InsertOrReplace setting
                                    |> inTableAsync connection tableName
                                    |> Async.Catch
                                printfn "Added setting %s %s %A" key defaultValue settingInsertResult
                                return setting
                            }
                        | _ -> failwithf "Expected only one setting for key [%s]" key
                    return setting}

    type WeatherStationsRepository(connection, tableName) =
        inherit AzureStorageRepository<WeatherStation>(connection, tableName)
        interface IWeatherStationsRepository with
            member this.Get deviceType deviceId =
                async {
                    let! weatherStations =
                        Query.all<WeatherStation>
                        |> Query.where <@ fun station key -> key.PartitionKey = string deviceType && key.RowKey = deviceId @>
                        |> Query.take 1
                        |> runQuery connection tableName
                    let result = weatherStations.SingleOrDefault()
                    return if isNull (box result) then None else Some result
                }

    type ReadingsRepository(connection, tableName) =
        inherit AzureStorageRepository<Reading>(connection, tableName)        

        interface IReadingsRepository with
            member this.GetPage(deviceId: string) (from: DateTime) (too: DateTime): Async<Reading list> = 
                async {
                    let! readings =
                        Query.all<Reading>
                        |> Query.where <@ fun reading key -> key.PartitionKey = deviceId && from <= reading.ReadingTime && reading.ReadingTime <= too @>                        
                        |> runQuery connection tableName
                    return readings
                }
            member this.GetHistory deviceId cutOff =
                async {
                    let! readings =
                        Query.all<Reading>
                        |> Query.where <@ fun reading key -> key.PartitionKey = deviceId && reading.ReadingTime > cutOff @>
                        |> runQuery connection tableName
                    return readings
                }
            member this.GetRecentReadings deviceId (cutOff: DateTime) =
                async {
                    let! readings =
                        Query.all<Reading>
                        |> Query.where <@ fun reading key -> key.PartitionKey = deviceId && reading.ReadingTime > cutOff @>
                        |> runQuery connection tableName
                    return readings
                }
            override this.Save(reading) =
                let updatedReading = {reading with ReadingTime = reading.ReadingTime}
                base.Save(updatedReading)

    type StatusMessageRepository(connection, tableName) =
        inherit AzureStorageRepository<StatusMessage>(connection, tableName)
        interface IStatusMessagesRepository with
            member this.GetDeviceStatuses deviceId (fromDate: DateTime) (tooDate: DateTime) =                           
                async {
                    let! statuses =
                        Query.all<StatusMessage>
                        |> Query.where <@ fun statusMessage key -> key.PartitionKey = deviceId && fromDate <= statusMessage.CreatedOn && statusMessage.CreatedOn <= tooDate @>
                        |> runQuery connection tableName
                    return statuses
                }
    let createRepository tableName constructor connection =
        async {
            do! createTableIfNecessary connection tableName
            let repository : 'TRepository = constructor(connection, tableName)
            return repository
        }

    let createWeatherStationsRepository connection = 
        async {
            let! repository = createRepository "WeatherStations" WeatherStationsRepository connection
            return repository :> IWeatherStationsRepository }
    let createReadingRepository connection = 
        async {
            let! repository = createRepository "Readings" ReadingsRepository connection
            return repository :> IReadingsRepository}
    let createStatusMessageRepository connection = 
        async {
            let! repository = createRepository "StatusMessage" StatusMessageRepository connection
            return repository :> IStatusMessagesRepository}            
    let createSystemSettingRepository connection = 
        async {
            let! repository = createRepository "Settings" SystemSettingsRepository connection
            return repository :> ISystemSettingsRepository }