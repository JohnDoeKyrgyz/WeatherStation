namespace WeatherStation.Tests.Functions
module WundergroundForwarder =
    open System
    open System.Diagnostics    
    open Microsoft.Azure.WebJobs.Host
    open Expecto    
    open WeatherStation.Functions.Model
    open WeatherStation.Model
    open WeatherStation.Tests.Functions.DataSetup
    open WeatherStation.Functions.WundergroundForwarder
    open WeatherStation
    

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
    let errorHandlingTests =
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

    let weatherStation = {
        DeviceType = string DeviceType.Particle
        DeviceId = "1e0037000751363130333334"
        WundergroundStationId = "K1234"
        WundergroundPassword = "fuzzybunny"
        DirectionOffsetDegrees = None
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
    }

    [<Tests>]
    let validTests = 
        testList "Basic Device" [
            testAsync "Insert sample record" {
                let! weatherStationRepository = AzureStorage.weatherStationRepository connectionString
                do! weatherStationRepository.Save weatherStation

                let! weatherStationReloaded = weatherStationRepository.Get Particle weatherStation.DeviceId
                Expect.isSome weatherStationReloaded "No WeatherStation found"
                Expect.equal weatherStation weatherStationReloaded.Value "WeatherStations are not equal"
            }
            testAsync "Reading for basic device" {
                do! loadWeatherStations [weatherStation]
                do! clearReadings

                let message =
                    """
                    {
                        "data": "100:4.006250:3864|d10.800000:86.500000a1.700000:15",
                        "device_id": "1e0037000751363130333334",
                        "event": "Reading",
                        "published_at": "2018-06-04T23:35:04.892Z"
                    }
                    """
                let expectedReadings = [SpeedMetersPerSecond(1.70M<meters/seconds>)] :> seq<ReadingValues>
                let testStartTime = DateTime.Now.ToUniversalTime()

                do!
                    processEventHubMessage message log (fun stationId password values traceWriter -> async { 
                        Expect.equal stationId weatherStation.WundergroundStationId "Unexpected StationId"
                        Expect.equal password weatherStation.WundergroundPassword "Unexpected Password"                        
                        Expect.equal values expectedReadings "Unexpected readings"
                        Expect.isNotNull traceWriter "Missing traceWriter" }) 
                    |> Async.AwaitTask

                let! readingsRepository = AzureStorage.readingsRepository connectionString
                let! readings = readingsRepository.GetAll()

                match readings with
                | [reading] ->
                    Expect.equal reading.SourceDevice weatherStation.DeviceId "Unexpected DeviceId"
                    Expect.isGreaterThan reading.ReadingTime testStartTime "Unexpected ReadingTime"
                    Expect.equal reading.SpeedMetersPerSecond 1.70 "Unexpected SpeedMetersPerSecond"
                | _ -> failwith "Unexpected readings"

                do! clearReadings
            }]
