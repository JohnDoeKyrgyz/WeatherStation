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
                let sample = "c1.000000:2.000000:3.000000"
                let values = parseReading Sensors.qmc5883l.Id sample
                Expect.equal 
                    values 
                    [ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>]
                    "Did not parse compass values")

            testCase "Read Anemometer" (fun () ->
                let sample = "a10.000000:15"
                let values = parseReading Sensors.anemometer.Id sample
                Expect.equal 
                    values 
                    [ReadingValues.SpeedMetersPerSecond 10.000000m<meters/seconds>; ReadingValues.DirectionSixteenths 15<sixteenths>]
                    "Did not parse anemometer values")

            testCase "Read BME280" (fun () ->
                let sample = "b1.000000:2.000000:3.000000"
                let values = parseReading Sensors.bme280.Id sample
                Expect.equal 
                    values 
                    [ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>]
                    "Did not parse compass values")
                    
            testCase "Multiple Readings A" (fun () ->
                let sample = "b1.000000:2.000000:3.000000c1.000000:2.000000:3.000000"
                let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
                let values = parseReading sensors sample
                Expect.equal 
                    values 
                    [
                        ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                        ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                    ]
                    "Did not parse combined values"
            )

            testCase "Multiple Readings B" (fun () ->
                let sample = "c1.000000:2.000000:3.000000b1.000000:2.000000:3.000000"
                let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
                let values = parseReading sensors sample
                Expect.equal 
                    values 
                    [
                        ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                        ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                    ]
                    "Did not parse combined values"
            )

            testCase "Multiple Readings With Prefix" (fun () ->
                let sample = "1c1.000000:2.000000:3.000000b1.000000:2.000000:3.000000"
                let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
                let values = parseReading sensors sample
                Expect.equal 
                    values 
                    [
                        ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                        ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                    ]
                    "Did not parse combined values"
            )
        ]