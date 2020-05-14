namespace WeatherStation.Tests.Functions

open NUnit.Framework
open WeatherStation

[<TestFixture>]
module WundergroundForwarderTests =
    open System
    open System.Diagnostics
    open System.Threading.Tasks
    
    open Microsoft.Extensions.Logging    
    open WeatherStation.Model
    open WeatherStation.Functions.WundergroundForwarder
    
    open ReadingsTests
    open Readings

    type DeviceType = WeatherStation.Shared.DeviceType

    let buildLog onMessage = { 
        new ILogger with
            member this.BeginScope(state: 'TState): IDisposable = raise (NotImplementedException())
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
            Assert.That(actualLevel, Is.EqualTo(level), "Unexpected log level") 
            match messageCompare with
            | Exact message -> Assert.That(actualMessage, Is.EqualTo(message), "Unexpected message")
            | Test test -> Assert.That(test actualMessage, "Unexpected message")
            | Ignore -> ()
            messageIndex := index + 1 )

    let processFaultyEventHubMessage log weatherStation readings eventHubMessage =
        let save _ = async { return () }
        processEventHubMessage 
            log 
            (fun _ _ _ _ -> async { failwith "Should not have posted to wunderground" })
            (fun _ _ -> async { return weatherStation} )
            save
            save
            (fun _ _ -> async { return readings })
            (async { return (fun _ _ -> failwith "Did not expect settings retrieval")})
            (fun _ -> failwith "Did not expect save StatusMessage")
            eventHubMessage
        |> Async.StartAsTask
        :> Task

    [<Test>]
    let EmptyMessage() = (processFaultyEventHubMessage log None [] "")
            
    [<Test>]            
    let MissingParticleDevice() =
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
        
        processFaultyEventHubMessage log None [] message

    let readingTime = DateTime(2018,1,1,1,1,0)

    [<Test>]
    let EmptyParticleData() =
        async {
            
            let message = buildParticleMessage "Reading" weatherStation readingTime String.Empty
            let! reading = readingTest log [] readingTime weatherStation message [ReadingTime readingTime]
            Assert.That(reading.SpeedMetersPerSecond.IsNone, "WindSpeed should be blank")
            Assert.That(reading.BatteryChargeVoltage, Is.EqualTo(0.0), "BatteryCharge should be blank")
            Assert.That(reading.DeviceTime, Is.EqualTo(readingTime), "Unexpected DeviceTime")
        }
        |> Async.StartAsTask
        :> Task
    
    [<Test>]
    let BasicReading() =
        let expectedReading = {
            BatteryPercentage = 85.0
            PanelMilliamps = 30.0
            BatteryState = int Shared.BatteryState.Charging
            X = Some 100.0
            Y = Some 101.0
            Z = Some 102.0
            DeviceTime = readingTime
            ReadingTime = readingTime
            BatteryChargeVoltage = 4.0
            PanelVoltage = 16.0
            TemperatureCelciusHydrometer = Some 10.8
            TemperatureCelciusBarometer = Some 1.0
            HumidityPercentHydrometer = Some 86.5
            HumidityPercentBarometer = Some 3.0
            PressurePascal = Some 2.0
            GustMetersPerSecond = Some 10.0
            SpeedMetersPerSecond = Some 10.0
            DirectionDegrees = Some (10.0 * double degreesPerSixteenth)
            SourceDevice = weatherStation.DeviceId
            RowKey = String.Empty
            Message = String.Empty
        }
        
        particleDeviceReadingTest log expectedReading weatherStation readingTime "100:f4.00:85.0:2p16.0:30.0b1.0:2.0:3.0d10.800000:86.500000a10.00:10"        
        |> Async.StartAsTask
        :> Task
        
    [<Test>]
    let NoWundergroundId() =
        let weatherStation = {weatherStation with WundergroundStationId = null}
        let data = "100:4.00:3640|b1.0:2.0:3.0d10.800000:86.500000a10.00:10"
        let message = buildParticleMessage "Reading" weatherStation readingTime data
        let wundergroundParameters = ref None
        let weatherStationSave = ref None
        let readingSave = ref None
        
        async {            
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
                    (fun _ -> failwith "Did not expect save StatusMessage")
                    message
                    
            Assert.That( (!wundergroundParameters).IsNone, "Wunderground should not have been called" )
            Assert.That( (!weatherStationSave).IsSome, "WeatherStation should have been saved" ) 
            Assert.That( (!readingSave).IsSome, "A reading should have been saved")
        }
        |> Async.StartAsTask
        :> Task
        
    [<Test>]        
    let StatusMessage() =
        async {           
            let weatherStation = {weatherStation with WundergroundStationId = null}
            let data = "Brownout"
            let message = buildParticleMessage "Status" weatherStation readingTime data
            let statusMessageSave = ref None
            do!
                processEventHubMessage
                    log
                    (fun _ _ _ _ -> failwith "Not Expected")
                    (fun _ _ -> async{ return Some weatherStation})
                    (fun _ -> failwith "Not Expected")
                    (fun _ -> failwith "Not Expected")
                    (fun _ _  -> failwith "Not Expected")
                    (async {return fun key defaultValue -> async {return {Key = key; Value = defaultValue; Group = ""}}})
                    (fun statusMessage -> async{ statusMessageSave := Some statusMessage })
                    message

            Assert.That((!statusMessageSave).IsSome, "Status Message should have been saved")
            Assert.That((!statusMessageSave).Value.StatusMessage, Is.EqualTo(data), (sprintf "Message should be %s" data))
            Assert.That(weatherStation.DeviceId, Is.EqualTo((!statusMessageSave).Value.DeviceId), "DeviceId")
            Assert.That(weatherStation.DeviceType, Is.EqualTo((!statusMessageSave).Value.DeviceType), "DeviceType")
        }
        |> Async.StartAsTask
        :> Task
            

            
    [<Test>]
    let RotationTests() =
        async {
            for rotation in [0.0 .. 15.0] do
            for windDirection in [0.0 .. 15.0] do
                let rotationDegrees = rotation * float degreesPerSixteenth
                let expectedWindDirection = (windDirection - rotation)
                let expectedWindDirection = if expectedWindDirection < 0.0 then 16.0 + expectedWindDirection else expectedWindDirection
                
                let expectedReading = {
                    BatteryPercentage = 85.0
                    PanelMilliamps = 30.0
                    BatteryState = int Shared.BatteryState.Charging
                    X = Some 100.0
                    Y = Some 101.0
                    Z = Some 102.0
                    DeviceTime = readingTime
                    ReadingTime = readingTime
                    BatteryChargeVoltage = 4.0
                    PanelVoltage = 16.0
                    TemperatureCelciusHydrometer = Some 10.8
                    TemperatureCelciusBarometer = Some 1.0
                    HumidityPercentHydrometer = Some 86.5
                    HumidityPercentBarometer = Some 3.0
                    PressurePascal = Some 2.0
                    GustMetersPerSecond = Some 10.0
                    SpeedMetersPerSecond = Some 10.0
                    DirectionDegrees = Some (expectedWindDirection * float degreesPerSixteenth)
                    SourceDevice = weatherStation.DeviceId
                    RowKey = String.Empty
                    Message = String.Empty
                }
                
                let data = sprintf "100f4.00:85.0:2p16.0:30.0b1.0:2.0:3.0d10.800000:86.500000a10.0:%d" (int windDirection)
                let weatherStation = {weatherStation with DirectionOffsetDegrees = Some (int rotationDegrees)}
                do! particleDeviceReadingTest quietLog expectedReading weatherStation readingTime data

                Console.ForegroundColor <- ConsoleColor.Blue
                printfn "WindDirection - Rotation %f, Direction %f" rotationDegrees (degreesPerSixteenth * windDirection)
                printfn "Rotation %f, ReportedDirection %f, ActualDirection %f" rotationDegrees (windDirection * 22.5) (expectedWindDirection * 22.5)
                Console.ResetColor()    
        }
        |> Async.StartAsTask
        :> Task
        
    open DataSetup        

    [<Test>]
    [<Category("Integration")>]
    let InsertSampleRecord() =
        async {
            let! weatherStationRepository = AzureStorage.weatherStationRepository connectionString

            let weatherStation = {weatherStation with CreatedOn = weatherStation.CreatedOn.ToUniversalTime()}
            do! weatherStationRepository.Save weatherStation

            let! weatherStationReloaded = weatherStationRepository.Get DeviceType.Particle weatherStation.DeviceId
            Assert.That(weatherStationReloaded.IsSome,"No WeatherStation found")
            Assert.That(weatherStationReloaded.Value, Is.EqualTo(weatherStation), "WeatherStations are not equal") 
        }
        |> Async.StartAsTask
        :> Task
            
    [<Test>]
    [<Category("Integration")>]
    let StatusMessageIntegrationTest() =
        DataSetup.initialize()
        async {
            do! loadWeatherStations [weatherStation]                
            let status = "Brownout"
            let messageTime = DateTime.Now
            let message = buildParticleMessage "Status" weatherStation messageTime status

            let wundergroundParameters = ref None
            do!                    
                processEventHubMessageWithAzureStorage (fun stationId password values _ -> async {
                    wundergroundParameters := Some {StationId = stationId; Password = password; Values = values |> Seq.toList}
                }) log message
            
            let wundergroundParameters = !wundergroundParameters
            Assert.That(wundergroundParameters.IsNone, "No call to wunderground")

            let! statusMessageRepository = AzureStorage.statusMessageRepository connectionString
            let! statusMessages = statusMessageRepository.GetAll()

            Assert.That(statusMessages.Length, Is.EqualTo(1), "There should only be one status message")

            let statusMessage = statusMessages.[0]
            Assert.That(statusMessage.StatusMessage, Is.EqualTo(status), (sprintf "Message should be %s" status))
            Assert.That((string statusMessage.CreatedOn), Is.EqualTo(string messageTime), "Message time should be the CreatedOn time")

            do! clearStatusMessages
            do! clearWeatherStations
        }
        |> Async.StartAsTask
        :> Task
            
    [<Test>]
    [<Category("Integration")>]
    let ReadingForBasicDevice() =
        DataSetup.initialize()
        async {        
            do! loadWeatherStations [weatherStation]
            do! clearReadings

            let message = buildParticleMessage "Reading" weatherStation readingTime "100f4.006250:85.0:2p16.98:100.0b1.0:2.0:3.0d10.800000:86.500000a1.700000:15"

            let expectedReadings = [
                BatteryChargeVoltage 4.006250M<volts>            
                BatteryPercentage 85.0M<percent>
                BatteryState Shared.BatteryState.Charging
                PanelVoltage 16.98M<volts>
                ChargeMilliamps 100.0m<milliamps>
                SpeedMetersPerSecond 1.700000M<meters/seconds>
                DirectionSixteenths 15<sixteenths>
                TemperatureCelciusBarometer 1.0M<celcius>
                PressurePascal 2.0m<pascal>
                HumidityPercentBarometer 3.0m<percent>                    
                TemperatureCelciusHydrometer 10.800000M<celcius>
                HumidityPercentHydrometer 86.500000M<percent>                    
                ReadingTime readingTime
                GustMetersPerSecond 1.700000M<meters/seconds>]
            
            let wundergroundParameters = ref None
            do!                    
                processEventHubMessageWithAzureStorage (fun stationId password values _ -> async {
                    wundergroundParameters := Some {StationId = stationId; Password = password; Values = values |> Seq.toList}
                }) log message
            
            let wundergroundParameters = !wundergroundParameters
            Assert.That(wundergroundParameters.IsSome, "No call to wunderground")

            let wundergroundParameters = wundergroundParameters.Value
            Assert.That( wundergroundParameters.StationId, Is.EqualTo(weatherStation.WundergroundStationId), "Unexpected StationId")
            Assert.That( wundergroundParameters.Password, Is.EqualTo(weatherStation.WundergroundPassword), "Unexpected Password")
            Assert.That( wundergroundParameters.Values, Is.EqualTo(expectedReadings), "Unexpected readings")

            let! readingsRepository = AzureStorage.readingsRepository connectionString
            let! readings = readingsRepository.GetAll()

            match readings with
            | [reading] ->
                Assert.That(reading.SourceDevice, Is.EqualTo(weatherStation.DeviceId), "Unexpected DeviceId")
                Assert.That(reading.ReadingTime, Is.GreaterThanOrEqualTo(readingTime), "Unexpected ReadingTime")
                Assert.That(reading.SpeedMetersPerSecond, Is.EqualTo(Some 1.70), "Unexpected SpeedMetersPerSecond")
            | _ -> failwith "Unexpected readings"

            do! clearWeatherStations
            do! clearReadings
        }
        |> Async.StartAsTask
        :> Task