namespace WeatherStation.Tests.Functions
module WundergroundForwarderTests =
    open System
    open System.Diagnostics  
    open Microsoft.Extensions.Logging
    open Expecto    
    open WeatherStation.Functions.Model
    open WeatherStation.Model
    open WeatherStation.Tests.Functions.DataSetup
    open WeatherStation.Functions.WundergroundForwarder
    open WeatherStation
    open ReadingsTests

    let buildLog onMessage = { 
        new ILogger with
            member this.BeginScope(state: 'TState): IDisposable = raise (System.NotImplementedException())
            member this.IsEnabled(logLevel: LogLevel): bool = true
            member this.Log<'TState>(logLevel: LogLevel, eventId: EventId, state: 'TState, exToLog: exn, formatter: Func<'TState,exn,string>): unit = 
                let message = formatter.Invoke(state, exToLog)
                onMessage logLevel message}

    let quietLog = buildLog (fun level message -> Debug.WriteLine(level, message))

    let log = buildLog (fun level message -> printfn "%A" (level, message))

    let weatherStation = {
        DeviceType = string DeviceType.Particle
        DeviceId = "1e0037000751363130333334"
        WundergroundStationId = "K1234"
        WundergroundPassword = "fuzzybunny"
        DirectionOffsetDegrees = None
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
        Settings = null
    }

    type LogMessageCompare =
        | Exact of string
        | Ignore
        | Test of (string -> bool)
    let logExpectations expectedMessages =                     
        let messageIndex = ref 0
        buildLog (fun actualLevel actualMessage->
            printfn "%A" (actualLevel, actualMessage)
            let index = !messageIndex
            let level, messageCompare = index |> Array.get expectedMessages
            Expect.equal level actualLevel "Unexpected log level"
            match messageCompare with
            | Exact message -> Expect.equal message actualMessage "Unexpected message"
            | Test test -> Expect.isTrue (test actualMessage) "Unexpected message"
            | Ignore -> ()
            messageIndex := index + 1 )

    [<Tests>]
    let errorHandlingTests =

        let processEventHubMessage log weatherStation readings =
            let save _ = async { return () }
            processEventHubMessage 
                log 
                (fun _ _ _ _ -> async { failwith "Should not have posted to wunderground" })
                (fun _ _ -> async { return weatherStation} )
                save
                save
                (fun _ _ -> async { return readings })
                (async { return (fun _ _ -> failwith "Did not expect settings retrieval")})

        testList "Error Handling" [
            testCaseAsync "Empty message" (processEventHubMessage log None [] "")
            
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
                    LogLevel.Information, Exact message
                    LogLevel.Information, Exact "Parsed particle reading for device 1e0037000751363130333334"
                    LogLevel.Information, Ignore
                    LogLevel.Information, Exact "Searching for device Particle 1e0037000751363130333334 in registry"
                    LogLevel.Error, Exact "Device [1e0037000751363130333334] is not provisioned"|]
                
                do!
                    processEventHubMessage log None [] message
            }
        ]

    let readingTime = new DateTime(2018,1,1,1,1,0)

    [<Tests>]
    let readingTests =
        testList "Reading Static Unit Tests" [
            testAsync "Empty particle data" {
                let message = buildParticleMessage weatherStation readingTime String.Empty
                let! reading = readingTest log [] readingTime weatherStation message [ReadingTime readingTime]
                Expect.equal reading.SpeedMetersPerSecond 0.0 "WindSpeed should be blank"
                Expect.equal reading.BatteryChargeVoltage 0.0 "BatteryCharge should be blank"
                Expect.equal reading.DeviceTime readingTime "Unexpected DeviceTime"
            }            
            testAsync "Basic reading" {
                let expectedReading = {
                    RefreshIntervalSeconds = 0
                    DeviceTime = readingTime
                    ReadingTime = readingTime
                    SupplyVoltage = 0.0
                    BatteryChargeVoltage = 4.0
                    PanelVoltage = 16.0
                    TemperatureCelciusHydrometer = 10.8
                    TemperatureCelciusBarometer = 1.0
                    HumidityPercentHydrometer = 86.5
                    HumidityPercentBarometer = 3.0
                    PressurePascal = 2.0
                    GustMetersPerSecond = 10.0
                    SpeedMetersPerSecond = 10.0
                    DirectionDegrees = 10.0 * double degreesPerSixteenth
                    SourceDevice = weatherStation.DeviceId
                    RowKey = String.Empty
                }
                do! particleDeviceReadingTest log expectedReading weatherStation readingTime "100:4.00:3640|b1.0:2.0:3.0d10.800000:86.500000a10.00:10"
            }]
            
    [<Tests>]
    let rotationTests =
        testList "Rotation Tests" [            
            for rotation in [0.0 .. 15.0] do
                for windDirection in [0.0 .. 15.0] do
                    let rotationDegrees = rotation * float degreesPerSixteenth
                    let testName = sprintf "WindDirection - Rotation %f, Direction %f" rotationDegrees (degreesPerSixteenth * windDirection)
                    let expectedWindDirection = (windDirection - rotation)
                    let expectedWindDirection = if expectedWindDirection < 0.0 then 16.0 + expectedWindDirection else expectedWindDirection
                    yield testAsync testName {
                        let expectedReading = {
                            RefreshIntervalSeconds = 0
                            DeviceTime = readingTime
                            ReadingTime = readingTime
                            SupplyVoltage = 0.0
                            BatteryChargeVoltage = 4.0
                            PanelVoltage = 16.0
                            TemperatureCelciusHydrometer = 10.8
                            TemperatureCelciusBarometer = 1.0
                            HumidityPercentHydrometer = 86.5
                            HumidityPercentBarometer = 3.0
                            PressurePascal = 2.0
                            GustMetersPerSecond = 10.0
                            SpeedMetersPerSecond = 10.0
                            DirectionDegrees = expectedWindDirection * float degreesPerSixteenth
                            SourceDevice = weatherStation.DeviceId
                            RowKey = String.Empty
                        }
                        let data = sprintf "100:4.00:3640|b1.0:2.0:3.0d10.800000:86.500000a10.0:%f" windDirection
                        let weatherStation = {weatherStation with DirectionOffsetDegrees = Some (int rotationDegrees)}
                        do! particleDeviceReadingTest quietLog expectedReading weatherStation readingTime data

                        Console.ForegroundColor <- ConsoleColor.Blue
                        printfn "Rotation %f, ReportedDirection %f, ActualDirection %f" rotationDegrees (windDirection * 22.5) (expectedWindDirection * 22.5)
                        Console.ResetColor()
                }]

    [<Tests>]
    let validTests = 
        testList "Regression Tests" [
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

                let message = buildParticleMessage weatherStation readingTime "100:4.006250:3864|b1.0:2.0:3.0d10.800000:86.500000a1.700000:15"

                let expectedReadings = [
                    BatteryChargeVoltage 4.006250M<volts>
                    PanelVoltage 16.984615384615384615384615385M<volts>
                    TemperatureCelciusBarometer 1.0M<celcius>
                    PressurePascal 2.0m<pascal>
                    HumidityPercentBarometer 3.0m<percent>
                    TemperatureCelciusHydrometer 10.800000M<celcius>
                    HumidityPercentHydrometer 86.500000M<percent>
                    SpeedMetersPerSecond 1.700000M<meters/seconds>
                    DirectionSixteenths 15<sixteenths>
                    ReadingTime readingTime
                    GustMetersPerSecond 1.700000M<meters/seconds>]
                
                let wundergroundParameters = ref None
                do!
                    
                    processEventHubMessageWithAzureStorage (fun stationId password values _ -> async {
                        wundergroundParameters := Some {StationId = stationId; Password = password; Values = values |> Seq.toList}
                    }) log message
                
                let wundergroundParameters = !wundergroundParameters
                Expect.isSome wundergroundParameters "No call to wunderground"

                let wundergroundParameters = wundergroundParameters.Value
                Expect.equal wundergroundParameters.StationId weatherStation.WundergroundStationId "Unexpected StationId"
                Expect.equal wundergroundParameters.Password weatherStation.WundergroundPassword "Unexpected Password"                        
                Expect.equal wundergroundParameters.Values expectedReadings "Unexpected readings"

                let! readingsRepository = AzureStorage.readingsRepository connectionString
                let! readings = readingsRepository.GetAll()

                match readings with
                | [reading] ->
                    Expect.equal reading.SourceDevice weatherStation.DeviceId "Unexpected DeviceId"
                    Expect.isGreaterThanOrEqual reading.ReadingTime readingTime "Unexpected ReadingTime"
                    Expect.equal reading.SpeedMetersPerSecond 1.70 "Unexpected SpeedMetersPerSecond"
                | _ -> failwith "Unexpected readings"

                do! clearWeatherStations
                do! clearReadings
            }]
