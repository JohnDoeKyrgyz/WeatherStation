namespace WeatherStation.Tests.Functions
module WundergroundForwarder =
    open System
    open System.Diagnostics
    open WeatherStation.Functions.WundergroundForwarder
    open Expecto
    open Microsoft.Azure.WebJobs.Host

    Environment.SetEnvironmentVariable("WeatherStationStorage", "UseDevelopmentStorage=true")    

    let log = {
        new TraceWriter(TraceLevel.Verbose) with
            override this.Trace event =
                printfn "%A" event }

    [<Tests>]
    let tests =
        testList "Error Handling" [
            testAsync "Empty message" {
                do! processEventHubMessage "" log (fun _ _ _ _ -> async { failwith "Should not have posted to wunderground" }) |> Async.AwaitTask
            }
            
            testAsync "Missing particle device" {
                let message =
                    """
                    {
                        "data": "100:4.006250:3864|d10.800000:86.500000a1.700000:15",
                        "device_id": "1e0037000751363130333334",
                        "event": "Reading",
                        "published_at": "2018-06-04T23:35:04.892Z"
                    }
                    """
                
                let! result =
                    processEventHubMessage message log (fun _ _ _ _ -> async { failwith "Should not have posted to wunderground" }) 
                    |> Async.AwaitTask
                    |> Async.Catch

                match result with
                | Choice1Of2 _ -> failwith "Did not expect the process to succeed"
                | Choice2Of2 exn -> 
                    match exn with
                    | :? AggregateException as aggregateException ->
                            let innerMostException = aggregateException.InnerException
                            Expect.equal typedefof<InvalidOperationException> (innerMostException.GetType()) "Unexpected exception type"
                    | exn -> failwithf "Unexpected exception type [%s]" (exn.GetType().Name)
            }
        ]
