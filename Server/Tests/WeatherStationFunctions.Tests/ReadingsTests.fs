namespace WeatherStation.Tests.Functions
module ReadingsTests =
    open System
    open Expecto    
    open WeatherStation.Functions.Model
    open WeatherStation.Model
    open WeatherStation.Functions.WundergroundForwarder
    
    type WundergroundParameters = {
        StationId : string
        Password : string
        Values : ReadingValues list
    }

    let buildParticleMessage weatherStation (readingTime : DateTime) data =
        sprintf
            """
            {
                "data": "%s",
                "device_id": "%s",
                "event": "Reading",
                "published_at": "%s"
            }
            """
            data
            weatherStation.DeviceId
            (readingTime.ToString())

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
                    message
                
            let wundergroundParameters = !wundergroundParameters
            Expect.isSome wundergroundParameters "No call to wunderground"

            let wundergroundParameters = wundergroundParameters.Value
            Expect.equal wundergroundParameters.StationId weatherStation.WundergroundStationId "Unexpected StationId"
            Expect.equal wundergroundParameters.Password weatherStation.WundergroundPassword "Unexpected Password"
            
            let wundergroundParametersCompare = set wundergroundParameters.Values
            printfn "%A" wundergroundParametersCompare

            let expectedReadingsCompare = set expectedReadings

            Expect.equal wundergroundParametersCompare.Count expectedReadingsCompare.Count "Unexpected number of readings"
            for reading in expectedReadingsCompare do Expect.isTrue (wundergroundParametersCompare.Contains reading) (sprintf "Missing reading %A" reading)
            Expect.equal (set wundergroundParameters.Values) (set expectedReadings) "Unexpected readings"

            let readings = !readingSave
            Expect.isSome readings "No readings saved"

            let reading = readings.Value
            Expect.equal reading.SourceDevice weatherStation.DeviceId "Unexpected DeviceId"
            Expect.isGreaterThanOrEqual reading.ReadingTime readingTime "Unexpected ReadingTime"

            let weatherStationSave = !weatherStationSave
            Expect.isSome weatherStationSave "WeatherStation not saved"
            let weatherStationSave = weatherStationSave.Value
            Expect.equal weatherStationSave {weatherStation with LastReading = Some readingTime} "Unexpected weatherStation state"

            return reading
        }

    let particleDeviceReadingTest log expectedReading weatherStation readingTime data = 
        async {
            let message = buildParticleMessage weatherStation readingTime data
            let toVolts (doubleV : double) = 1.0m<_> * decimal doubleV
            let toCelcius (doubleV : double) = 1.0m<_> * decimal doubleV
            let toPercent (doubleV : double) = 1.0m<_> * decimal doubleV
            let toPascal (doubleV : double) = 1.0m<_> * decimal doubleV
            let toSpeed (doubleV : double) = 1.0m<_> * decimal doubleV
            let toDirection (doubleV : double) = 1<_> * int doubleV
            let! reading = 
                readingTest 
                    log [] readingTime weatherStation message 
                    [
                        ReadingTime readingTime
                        BatteryChargeVoltage (toVolts expectedReading.BatteryChargeVoltage)
                        PanelVoltage (toVolts expectedReading.PanelVoltage)
                        TemperatureCelciusHydrometer (toCelcius expectedReading.TemperatureCelciusHydrometer)
                        TemperatureCelciusBarometer (toCelcius expectedReading.TemperatureCelciusBarometer)
                        HumidityPercentHydrometer (toPercent expectedReading.HumidityPercentHydrometer)
                        HumidityPercentBarometer (toPercent expectedReading.HumidityPercentBarometer)
                        PressurePascal (toPascal expectedReading.PressurePascal)
                        SpeedMetersPerSecond (toSpeed expectedReading.SpeedMetersPerSecond)
                        DirectionSixteenths (toDirection (expectedReading.DirectionDegrees / (360.0 / 16.0)))
                    ]
                
            Expect.equal reading.RefreshIntervalSeconds expectedReading.RefreshIntervalSeconds "Unexpected value"
            Expect.equal reading.DeviceTime expectedReading.DeviceTime "Unexpected value"
            Expect.equal reading.ReadingTime expectedReading.ReadingTime "Unexpected value"
            Expect.equal reading.SupplyVoltage expectedReading.SupplyVoltage "Unexpected value"
            Expect.equal reading.BatteryChargeVoltage expectedReading.BatteryChargeVoltage "Unexpected value"
            Expect.floatClose Accuracy.medium (float reading.PanelVoltage) (float expectedReading.PanelVoltage) "Unexpected value"
            Expect.equal reading.TemperatureCelciusHydrometer expectedReading.TemperatureCelciusHydrometer "Unexpected value"
            Expect.equal reading.TemperatureCelciusBarometer expectedReading.TemperatureCelciusBarometer "Unexpected value"
            Expect.equal reading.HumidityPercentHydrometer expectedReading.HumidityPercentHydrometer "Unexpected value"
            Expect.equal reading.HumidityPercentBarometer expectedReading.HumidityPercentBarometer "Unexpected value"
            Expect.equal reading.PressurePascal expectedReading.PressurePascal "Unexpected value"
            Expect.equal reading.GustMetersPerSecond expectedReading.GustMetersPerSecond "Unexpected value"
            Expect.equal reading.SpeedMetersPerSecond expectedReading.SpeedMetersPerSecond "Unexpected value"
            Expect.equal reading.DirectionDegrees expectedReading.DirectionDegrees "Unexpected value"
            Expect.equal reading.SourceDevice expectedReading.SourceDevice "Unexpected value"
        }
