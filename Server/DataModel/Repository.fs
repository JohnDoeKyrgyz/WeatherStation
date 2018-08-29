namespace WeatherStation

module Repository =

    type IRepository<'TEntity> =
        abstract member GetAll : unit -> Async<'TEntity list>
        abstract member Find : unit -> Async<'TEntity list>

    type AzureStorageRepository<'TEntity>

