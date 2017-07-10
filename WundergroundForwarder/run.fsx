#if INTERACTIVE
#I @"..\node_modules\azure-functions-core-tools\bin"

#r "Microsoft.Azure.Webjobs.Host.dll"
open Microsoft.Azure.WebJobs.Host
open System

#r "System.Net.Http.dll"
#r "System.Net.Http.Formatting.dll"
#r "System.Web.Http.dll"
#r "Newtonsoft.Json.dll"

#endif

#r "System.Net.Http"
#r "Newtonsoft.Json"

open System.Net
open System.Net.Http
open Newtonsoft.Json

type Named = {
    name: string
}

let Run(req: HttpRequestMessage, log: TraceWriter) =
    async {
        
        let! content = req.Content.ReadAsStringAsync() |> Async.AwaitTask
        log.Info(sprintf "%A" content)
        
        // Set name to query string
        let name =
            req.GetQueryNameValuePairs()
            |> Seq.tryFind (fun q -> q.Key = "name")

        match name with
        | Some x ->
            return req.CreateResponse(HttpStatusCode.OK, "Hello " + x.Value);
        | None ->
            let! data = req.Content.ReadAsStringAsync() |> Async.AwaitTask

            if not (String.IsNullOrEmpty(data)) then
                let named = JsonConvert.DeserializeObject<Named>(data)
                return req.CreateResponse(HttpStatusCode.OK, "Hello " + named.name);
            else
                return req.CreateResponse(HttpStatusCode.BadRequest, "Specify a Name value");
    } |> Async.RunSynchronously
