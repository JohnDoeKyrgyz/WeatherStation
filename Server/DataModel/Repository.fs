namespace WeatherStation

module Repository =
    open System.Linq
    open WeatherStation.Model
    open FSharp.Azure.Storage.Table
    open Microsoft.WindowsAzure.Storage.Table

    type IRepository<'TEntity> =
        abstract member GetAll : unit -> Async<'TEntity list>

    type ISystemSettingsRepository =
        inherit IRepository<SystemSetting>
        abstract member GetSetting : key:string -> Async<SystemSetting>
        abstract member GetSetting : key:string * defaultValue:string -> Async<SystemSetting>

    let createTableIfNecessary (connection : CloudTableClient) tableName =
        let tableReference = connection.GetTableReference(tableName)
        tableReference.CreateIfNotExistsAsync()

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
        interface IRepository<'TEntity> with
            member this.GetAll() = runQuery Query.all<'TEntity>

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
            member this.GetSetting(key, defaultValue) = 
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
                                do!
                                    InsertOrReplace setting
                                    |> inTableAsync connection tableName
                                    |> Async.Ignore
                                return setting
                            }
                        | _ -> failwithf "Expected only one setting for key [%s]" key
                    return setting}

    let createRepository tableName constructor connection =
        async {
            do! createTableIfNecessary connection tableName |> Async.AwaitTask |> Async.Ignore
            let repository : 'TRepository = constructor(connection, tableName)
            return repository
        }

    let createWeatherStationsRepository connection = 
        async {
            let! repository = createRepository "WeatherStations" AzureStorageRepository<WeatherStation> connection
            return repository :> IRepository<WeatherStation> }
    let createReadingRepository connection = 
        async {
            let! repository = createRepository "Readings" AzureStorageRepository<Reading> connection
            return repository :> IRepository<Reading>}
    let createSystemSettingRepository connection = 
        async {
            let! repository = createRepository "Settings" SystemSettingsRepository connection
            return repository :> ISystemSettingsRepository }