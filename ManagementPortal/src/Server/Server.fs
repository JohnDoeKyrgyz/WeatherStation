open System.IO
open System.Net

open Suave
open Suave.Files
open Suave.Successful
open Suave.Filters
open Suave.Operators

open Shared

open Fable.Remoting.Server
open Fable.Remoting.Suave
open Microsoft.WindowsAzure.Storage

let publicPath = Azure.tryGetEnv "public_path" |> Option.defaultValue "../Client/public" |> Path.GetFullPath
let port = Azure.tryGetEnv "HTTP_PLATFORM_PORT" |> Option.map System.UInt16.Parse |> Option.defaultValue 8085us
let storageAccount = Azure.tryGetEnv "STORAGE_CONNECTIONSTRING" |> Option.defaultValue "UseDevelopmentStorage=true" |> CloudStorageAccount.Parse

let config =
    { defaultConfig with
          homeFolder = Some publicPath
          bindings = [ HttpBinding.create HTTP (IPAddress.Parse "0.0.0.0") port ] }

let getInitCounter() : Async<Counter> = async { return 42 }
let counterApi = {
    initialCounter = getInitCounter
}

let webApi =
    Remoting.createApi()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue counterApi
    |> Remoting.buildWebPart

let webApp =
    choose [
        webApi
        path "/" >=> browseFileHome "index.html"
        browseHome
        RequestErrors.NOT_FOUND "Not found!"
    ] |> Azure.AI.withAppInsights Azure.AI.buildApiOperationName

Azure.AppServices.addTraceListeners()
Azure.tryGetEnv "APPINSIGHTS_INSTRUMENTATIONKEY"
|> Option.iter(fun appInsightsKey ->
    Azure.AI.configure { AppInsightsKey = appInsightsKey; DeveloperMode = false; TrackDependencies = true })

startWebServer config webApp