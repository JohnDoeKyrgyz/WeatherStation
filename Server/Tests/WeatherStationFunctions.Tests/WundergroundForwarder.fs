namespace WeatherStation.Tests.Functions
module WundergroundForwarder =
    open System
    open System.Diagnostics
    open WeatherStation.Functions.WundergroundForwarder
    open Expecto
    open Microsoft.Azure.WebJobs.Host
    open Expecto.Logging

    Environment.SetEnvironmentVariable("WeatherStationStorage", "UseDevelopmentStorage=true")    

    let log = {
        new TraceWriter(TraceLevel.Verbose) with
            override this.Trace event =
                printfn "%A" event }

    type LogMessageCompare =
        | Exact of string
        | Ignore
        | Test of (string -> bool)

    let logExpectations expectedMessages =                     
        let messageIndex = ref 0
        {   
            new TraceWriter(TraceLevel.Verbose) with
                override this.Trace event =
                    printfn "%A" event
                    let index = !messageIndex
                    let level, messageCompare = index |> Array.get expectedMessages
                    Expect.equal level event.Level "Unexpected log level"
                    match messageCompare with
                    | Exact message -> Expect.equal message event.Message "Unexpected message"
                    | Test test -> Expect.isTrue (test event.Message) "Unexpected message"
                    | Ignore -> ()
                    messageIndex := index + 1 }

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

                let log = logExpectations [|
                    TraceLevel.Info, Exact message
                    TraceLevel.Info, Exact "Parsed particle reading for device 1e0037000751363130333334"
                    TraceLevel.Info, Ignore
                    TraceLevel.Info, Exact "Searching for device Particle 1e0037000751363130333334 in registry"
                    TraceLevel.Error, Exact "Device [1e0037000751363130333334] is not provisioned"|]
                
                do!
                    processEventHubMessage message log (fun _ _ _ _ -> async { failwith "Should not have posted to wunderground" }) 
                    |> Async.AwaitTask
            }
        ]
