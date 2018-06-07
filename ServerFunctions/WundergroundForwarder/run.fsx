#I __SOURCE_DIRECTORY__

#if INTERACTIVE
#I @"C:\Users\jatwood\AppData\Roaming\npm\node_modules\azure-functions-core-tools\bin"
#I @"..\packages\FSharp.Data\lib\net45\"
#I @"..\packages\Newtonsoft.Json\lib\net45\"
#endif

#r "Microsoft.Azure.Webjobs.Host"
#r "System.Net.Http"
#r "Microsoft.Azure.WebJobs"
#r "Microsoft.WindowsAzure.Storage"

#r "Newtonsoft.Json"
#r "FSharp.Data"

open System.Linq
open Microsoft.WindowsAzure.Storage.Table
open Microsoft.Azure.WebJobs.Host

open FSharp.Data

[<Literal>]
let ParticleSample = __SOURCE_DIRECTORY__ + @"\ParticleStatusUpdate.json"
type ParticlePayload = JsonProvider<ParticleSample, SampleIsList = true>

let rec innerMostException (ex : exn) =
    if ex.InnerException <> null then innerMostException ex.InnerException else ex

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<TableEntity>, readingsTable: IQueryable<TableEntity>, storedReading : byref<TableEntity>, log: TraceWriter) =
    
    let reading =
        async {
            log.Info(eventHubMessage)

            let payload = ParticlePayload.Parse eventHubMessage

            log.Info(sprintf "DeviceId %s" payload.DeviceId)

            return None }
        |> Async.RunSynchronously

    match reading with
    | Some reading -> 
        log.Info(sprintf "Saving Reading %A" reading)
        storedReading <- reading
    | None ->
        log.Info("Nothing to save")