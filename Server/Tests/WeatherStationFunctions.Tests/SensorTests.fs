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

            testCase "Full Reading" (fun () ->
                let sample = "20f3.908750:69.296875p4.656000:-22.200001b22.570000:98883.906250:63.833008c3.940063:0.140991:2.772461"
                let sensors = Sensors.All |> Sensors.id
                let values = parseReading sensors sample
                Expect.equal 
                    values 
                    [
                        ReadingValues.BatteryChargeVoltage 3.908750m<volts>
                        ReadingValues.BatteryPercentage 69.296875m<percent>
                        ReadingValues.PanelVoltage 4.656000m<volts>
                        ReadingValues.ChargeMilliamps -22.200001m<milliamps>                        
                        ReadingValues.TemperatureCelciusBarometer 22.570000m<celcius>
                        ReadingValues.PressurePascal 98883.906250m<pascal>
                        ReadingValues.HumidityPercentBarometer 63.833008m<percent>
                        ReadingValues.X 3.940063m<degrees>
                        ReadingValues.Y 0.140991m<degrees>
                        ReadingValues.Z 2.772461m<degrees>                        
                    ]
                    "Did not parse combined values"
            )
        ]