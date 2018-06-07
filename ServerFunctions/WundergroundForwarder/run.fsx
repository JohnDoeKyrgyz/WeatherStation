#I __SOURCE_DIRECTORY__
#r @"packages\Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"

#if INTERACTIVE
#r @"packages\FSharp.Data\lib\net45\FSharp.Data.dll"
#I @"C:\Users\jatwood\AppData\Roaming\npm\node_modules\azure-functions-core-tools\bin"
#r "Microsoft.Azure.Webjobs.Host.dll"
#r "System.Net.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "Microsoft.WindowsAzure.Storage.dll"
#else
#r "FSharp.Data"
#endif

#r "System.Net.Http"
#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"

#load "../Database.fsx"
open Database


open System
open System.Linq

open Microsoft.Azure.WebJobs.Host

let rec innerMostException (ex : exn) =
    if ex.InnerException <> null then innerMostException ex.InnerException else ex

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<WeatherStation>, readingsTable: IQueryable<Reading>, storedReading : byref<Reading>, log: TraceWriter) =
    
    let reading =
        async {
            log.Info(eventHubMessage)
            return None }
        |> Async.RunSynchronously

    match reading with
    | Some reading -> 
        log.Info(sprintf "Saving Reading %A" reading)
        storedReading <- reading
    | None ->
        log.Info("Nothing to save")