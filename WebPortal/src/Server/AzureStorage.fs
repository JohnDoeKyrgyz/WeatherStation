namespace AzureStorage

open FSharp.Azure.StorageTypeProvider

type AzureTableStorage = AzureTypeProvider<connectionStringName = "AzureStorageConnection", configFileName="App.config">

