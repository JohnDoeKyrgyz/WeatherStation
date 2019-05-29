namespace WeatherStation.Tests.Functions
module WundergroundForwarderTests =
    open Expecto
    open Expecto.Flip
    open System
    open System.Diagnostics  
    open Microsoft.Extensions.Logging    
    open WeatherStation
    open WeatherStation.Model
    open WeatherStation.Functions.Model    
    open WeatherStation.Tests.Functions.DataSetup
    open WeatherStation.Functions.WundergroundForwarder    
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
        CreatedOn = DateTime.Now
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
        Settings = null
        Sensors = 0xFFFF
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
            Expect.equal "Unexpected log level" level actualLevel 
            match messageCompare with
            | Exact message -> Expect.equal "Unexpected message" message actualMessage
            | Test test -> Expect.isTrue "Unexpected message" (test actualMessage)
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
                    LogLevel.Information, Exact "Particle 1e0037000751363130333334 not found. Searching for device Test 1e0037000751363130333334 in registry"
                    LogLevel.Error, Exact "Device [1e0037000751363130333334] is not provisioned"|]
                
                do!
                    processEventHubMessage log None [] message
            }
        ]

    let readingTime = DateTime(2018,1,1,1,1,0)

    [<Tests>]
    let readingTests =
        testList "Reading Static Unit Tests" [
            testAsync "Empty particle data" {
                let message = buildParticleMessage weatherStation readingTime String.Empty
                let! reading = readingTest log [] readingTime weatherStation message [ReadingTime readingTime]
                Expect.equal "WindSpeed should be blank" reading.SpeedMetersPerSecond 0.0
                Expect.equal "BatteryCharge should be blank" reading.BatteryChargeVoltage 0.0
                Expect.equal "Unexpected DeviceTime" reading.DeviceTime readingTime
            }            
            testAsync "Basic reading" {
                let expectedReading = {
                    BatteryPercentage = 85.0
                    PanelMilliamps = 30.0
                    X = 100.0
                    Y = 101.0
                    Z = 102.0
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
            }
            testAsync "No WundergroundId" {
                let weatherStation = {weatherStation with WundergroundStationId = null}
                let data = "100:4.00:3640|b1.0:2.0:3.0d10.800000:86.500000a10.00:10"
                let message = buildParticleMessage weatherStation readingTime data
                let wundergroundParameters = ref None
                let weatherStationSave = ref None
                let readingSave = ref None
                do!
                    processEventHubMessage
                        log
                        (fun stationId password values traceWriter -> 
                            async { wundergroundParameters := Some {StationId = stationId; Password = password; Values = values |> Seq.toList} })
                        (fun _ _ -> async{ return Some weatherStation})
                        (fun saveWeatherStation -> async {weatherStationSave := Some saveWeatherStation})
                        (fun saveReading -> async {readingSave := Some saveReading})
                        (fun _ _ -> async { return [] })
                        (async {return fun key defaultValue -> async {return {Key = key; Value = defaultValue; Group = ""}}})
                        message

                Expect.isNone "Wunderground should not have been called" !wundergroundParameters
                Expect.isSome "WeatherStation should have been saved" !weatherStationSave
                Expect.isSome "A reading should have been saved" !readingSave
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
                            BatteryPercentage = 85.0
                            PanelMilliamps = 30.0
                            X = 100.0
                            Y = 101.0
                            Z = 102.0
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

                let weatherStation = {weatherStation with CreatedOn = weatherStation.CreatedOn.ToUniversalTime()}
                do! weatherStationRepository.Save weatherStation

                let! weatherStationReloaded = weatherStationRepository.Get Particle weatherStation.DeviceId
                Expect.isSome "No WeatherStation found" weatherStationReloaded
                Expect.equal "WeatherStations are not equal" weatherStation weatherStationReloaded.Value
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
                Expect.isSome "No call to wunderground" wundergroundParameters

                let wundergroundParameters = wundergroundParameters.Value
                Expect.equal "Unexpected StationId" wundergroundParameters.StationId weatherStation.WundergroundStationId
                Expect.equal "Unexpected Password" wundergroundParameters.Password weatherStation.WundergroundPassword
                Expect.equal "Unexpected readings" wundergroundParameters.Values expectedReadings

                let! readingsRepository = AzureStorage.readingsRepository connectionString
                let! readings = readingsRepository.GetAll()

                match readings with
                | [reading] ->
                    Expect.equal "Unexpected DeviceId" reading.SourceDevice weatherStation.DeviceId
                    Expect.isGreaterThanOrEqual "Unexpected ReadingTime" (reading.ReadingTime, readingTime)
                    Expect.equal "Unexpected SpeedMetersPerSecond" reading.SpeedMetersPerSecond 1.70 
                | _ -> failwith "Unexpected readings"

                do! clearWeatherStations
                do! clearReadings
            }]