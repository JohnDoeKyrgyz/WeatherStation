#I __SOURCE_DIRECTORY__
#r @"packages\Newtonsoft.Json\lib\net45\Newtonsoft.Json.dll"

#if INTERACTIVE
#I @"C:\Users\jatwood\AppData\Roaming\npm\node_modules\azure-functions-core-tools\bin"
#endif

#r "Microsoft.Azure.Webjobs.Host"
#r "System.Net.Http"
#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"

open System.Linq
open Microsoft.WindowsAzure.Storage.Table
open Microsoft.Azure.WebJobs.Host

let rec innerMostException (ex : exn) =
    if ex.InnerException <> null then innerMostException ex.InnerException else ex

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<TableEntity>, readingsTable: IQueryable<TableEntity>, storedReading : byref<TableEntity>, log: TraceWriter) =
    
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