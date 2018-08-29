﻿namespace WeatherStation

module Repository =
    open WeatherStation.Model
    open FSharp.Azure.Storage.Table

    type IRepository<'TEntity> =
        abstract member GetAll : unit -> Async<'TEntity list>

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

    let createWeatherStationsRepository connection = new AzureStorageRepository<WeatherStation>(connection, "WeatherStations")
    let createReadingRepository connection = new AzureStorageRepository<Reading>(connection, "Readings")