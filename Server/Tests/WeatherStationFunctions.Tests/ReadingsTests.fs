namespace WeatherStation.Tests.Functions
module ReadingsTests =
    open System
    open NUnit.Framework    
    open WeatherStation.Readings
    open WeatherStation.Model
    open WeatherStation.Functions.WundergroundForwarder
    
    type WundergroundParameters = {
        StationId : string
        Password : string
        Values : ReadingValues list
    }

    let buildParticleMessage event (weatherStation : WeatherStation) (readingTime : DateTime) data =
        let readingTimeFormat = readingTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
        sprintf
            """
            {
                "data": "%s",
                "device_id": "%s",
                "event": "%s",
                "published_at": "%s"
            }
            """
            data
            weatherStation.DeviceId
            event
            readingTimeFormat

    let readingTest log existingReadings readingTime weatherStation message expectedReadings =
        async {
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
                    (fun _ _ -> async { return existingReadings })
                    (async {return fun key defaultValue -> async {return {Key = key; Value = defaultValue; Group = ""}}})
                    (fun saveStatusMessage -> failwithf "Not expected")
                    message
                
            let wundergroundParameters = !wundergroundParameters
            Assert.That(wundergroundParameters.IsSome, "No call to wunderground")

            let wundergroundParameters = wundergroundParameters.Value
            Assert.That(wundergroundParameters.StationId, Is.EqualTo(weatherStation.WundergroundStationId), "Unexpected StationId")
            Assert.That(wundergroundParameters.Password, Is.EqualTo(weatherStation.WundergroundPassword), "Unexpected Password")
            
            let wundergroundParametersCompare = set wundergroundParameters.Values
            let expectedReadingsCompare = set expectedReadings

            Assert.That(wundergroundParametersCompare.Count, Is.EqualTo(expectedReadingsCompare.Count), "Unexpected number of readings")
            for reading in expectedReadingsCompare do Assert.That( wundergroundParametersCompare.Contains reading, sprintf "Missing reading %A" reading)
            Assert.That( (set wundergroundParameters.Values), Is.EqualTo( (set expectedReadings) ), "Unexpected readings")

            let readings = !readingSave
            Assert.That(readings.IsSome, "No readings saved")

            let reading = readings.Value
            Assert.That( reading.SourceDevice, Is.EqualTo( weatherStation.DeviceId ), "Unexpected DeviceId")
            Assert.That( reading.ReadingTime, Is.GreaterThanOrEqualTo( readingTime ), "Unexpected ReadingTime")

            let weatherStationSave = !weatherStationSave
            Assert.That( weatherStationSave.IsSome, "WeatherStation not saved" )
            let weatherStationSave = weatherStationSave.Value
            Assert.That( weatherStationSave, Is.EqualTo({weatherStation with LastReading = Some readingTime}), "Unexpected weatherStation state")

            return reading
        }

    let particleDeviceReadingTest log expectedReading weatherStation readingTime data = 
        async {
            let message = buildParticleMessage "Reading" weatherStation readingTime data
            let toVolts (doubleV : double) = 1.0m<_> * decimal doubleV
            let toCelcius (doubleV : double) = 1.0m<_> * decimal doubleV
            let toPercent (doubleV : double) = 1.0m<_> * decimal doubleV
            let toPascal (doubleV : double) = 1.0m<_> * decimal doubleV
            let toSpeed (doubleV : double) = 1.0m<_> * decimal doubleV
            let toDirection (doubleV : double) = 1<_> * int doubleV
            let toMilliamps (doubleV : double) = 1.0m<_> * decimal doubleV
            let! reading = 
                readingTest 
                    log [] readingTime weatherStation message 
                    [
                        ReadingTime readingTime
                        BatteryChargeVoltage (toVolts expectedReading.BatteryChargeVoltage)
                        BatteryPercentage (toPercent expectedReading.BatteryPercentage)
                        BatteryState (parseBatteryState expectedReading.BatteryState)
                        ChargeMilliamps (toMilliamps expectedReading.PanelMilliamps)
                        PanelVoltage (toVolts expectedReading.PanelVoltage)
                        TemperatureCelciusHydrometer (toCelcius expectedReading.TemperatureCelciusHydrometer.Value)
                        TemperatureCelciusBarometer (toCelcius expectedReading.TemperatureCelciusBarometer.Value)
                        HumidityPercentHydrometer (toPercent expectedReading.HumidityPercentHydrometer.Value)
                        HumidityPercentBarometer (toPercent expectedReading.HumidityPercentBarometer.Value)
                        PressurePascal (toPascal expectedReading.PressurePascal.Value)
                        SpeedMetersPerSecond (toSpeed expectedReading.SpeedMetersPerSecond.Value)
                        GustMetersPerSecond (toSpeed expectedReading.GustMetersPerSecond.Value)
                        DirectionSixteenths (toDirection (expectedReading.DirectionDegrees.Value / (360.0 / 16.0)))
                    ]
            Assert.That( reading.DeviceTime, Is.EqualTo(expectedReading.DeviceTime), "Unexpected value")
            Assert.That( reading.ReadingTime, Is.EqualTo(expectedReading.ReadingTime), "Unexpected value")
            Assert.That( reading.BatteryChargeVoltage, Is.EqualTo(expectedReading.BatteryChargeVoltage), "Unexpected value")
            Assert.That( (float reading.PanelVoltage), Is.EqualTo(float expectedReading.PanelVoltage).Within(0.01), "Unexpected value")
            Assert.That( reading.TemperatureCelciusHydrometer, Is.EqualTo(expectedReading.TemperatureCelciusHydrometer), "Unexpected value")
            Assert.That( reading.TemperatureCelciusBarometer, Is.EqualTo(expectedReading.TemperatureCelciusBarometer), "Unexpected value")
            Assert.That( reading.HumidityPercentHydrometer, Is.EqualTo(expectedReading.HumidityPercentHydrometer), "Unexpected value")
            Assert.That( reading.HumidityPercentBarometer, Is.EqualTo(expectedReading.HumidityPercentBarometer), "Unexpected value")
            Assert.That( reading.PressurePascal, Is.EqualTo(expectedReading.PressurePascal), "Unexpected value")
            Assert.That( reading.GustMetersPerSecond, Is.EqualTo(expectedReading.GustMetersPerSecond), "Unexpected value")
            Assert.That( reading.SpeedMetersPerSecond, Is.EqualTo(expectedReading.SpeedMetersPerSecond) ,"Unexpected value")
            Assert.That( reading.DirectionDegrees, Is.EqualTo(expectedReading.DirectionDegrees), "Unexpected value")
            Assert.That( reading.SourceDevice, Is.EqualTo(expectedReading.SourceDevice), "Unexpected value")
        }