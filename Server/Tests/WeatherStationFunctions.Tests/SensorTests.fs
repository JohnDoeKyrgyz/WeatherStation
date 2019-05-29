namespace WeatherStation.Tests.Functions
open WeatherStation
module SensorTests =
    open Expecto
    open Sensors
    open Readings

    [<Tests>]
    let sensorTests = 
        testList "Basic Readings" [
            testCase "Read Compass Sensor" (fun () ->
                let sample = sprintf "c1.000000:2.000000:3.000000"
                let values = parseReading Sensors.qmc5883l.Id sample
                Expect.equal 
                    values 
                    [ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>]
                    "Did not parse compass values")

            testCase "Read BME280" (fun () ->
                let sample = sprintf "%c%f:%f:%f" Sensors.bme280.Prefix 1.0 2.0 3.0
                let values = parseReading Sensors.bme280.Id sample
                Expect.equal 
                    values 
                    [ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.HumidityPercentBarometer 2.000000m<percent>; ReadingValues.PressurePascal 3.000000m<pascal>]
                    "Did not parse compass values")                
        ]