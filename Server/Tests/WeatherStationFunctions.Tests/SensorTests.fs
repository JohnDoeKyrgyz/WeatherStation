namespace WeatherStation.Tests.Functions
open NUnit.Framework
open WeatherStation

[<TestFixture>]
module SensorTests =
    open Sensors
    open Readings

    [<Test>]
    let ReadCompassSensor() =
        let sample = "c1.000000:2.000000:3.000000"
        let values = parseReading Sensors.qmc5883l.Id sample
        Assert.That( 
            values
            ,Is.EqualTo([ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>])
            ,"Did not parse compass values")

    [<Test>]
    let ReadAnemometer() =
        let sample = "a10.000000:15"
        let values = parseReading Sensors.anemometer.Id sample
        Assert.That(  
            values 
            ,Is.EqualTo([ReadingValues.SpeedMetersPerSecond 10.000000m<meters/seconds>; ReadingValues.DirectionSixteenths 15<sixteenths>])
            ,"Did not parse anemometer values")

    [<Test>]
    let ReadBME280() =
        let sample = "b1.000000:2.000000:3.000000"
        let values = parseReading Sensors.bme280.Id sample
        Assert.That(  
            values 
            ,Is.EqualTo([ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>])
            ,"Did not parse compass values")
                    
    [<Test>]
    let MultipleReadingsA() =
        let sample = "b1.000000:2.000000:3.000000c1.000000:2.000000:3.000000"
        let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
        let values = parseReading sensors sample
        Assert.That(
            values,
            Is.EqualTo(
                [
                    ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                    ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                ]
            )
            ,"Did not parse combined values")

    [<Test>]
    let MultipleReadingsB() =
        let sample = "c1.000000:2.000000:3.000000b1.000000:2.000000:3.000000"
        let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
        let values = parseReading sensors sample
        Assert.That(  
            values
            ,Is.EqualTo(
                [
                    ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                    ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                ]
            )
            ,"Did not parse combined values")

    [<Test>]
    let MultipleReadingsWithPrefix() =
        let sample = "1c1.000000:2.000000:3.000000b1.000000:2.000000:3.000000"
        let sensors = Sensors.id [Sensors.qmc5883l; Sensors.bme280]
        let values = parseReading sensors sample
        Assert.That(  
            values,
            Is.EqualTo(
                [
                    ReadingValues.TemperatureCelciusBarometer 1.000000m<celcius>; ReadingValues.PressurePascal 2.000000m<pascal>; ReadingValues.HumidityPercentBarometer 3.000000m<percent>;
                    ReadingValues.X 1.000000m<degrees>; ReadingValues.Y 2.000000m<degrees>; ReadingValues.Z 3.000000m<degrees>                        
                ]
            ),
            "Did not parse combined values")

    
    [<Test>]
    let FullReading() =
        let sample = "20f3.908750:69.296875:2p4.656000:-22.200001b22.570000:98883.906250:63.833008c3.940063:0.140991:2.772461"
        let sensors = Sensors.All |> Sensors.id
        let values = parseReading sensors sample
        Assert.That(  
            values,
            Is.EqualTo(
                [
                    ReadingValues.BatteryChargeVoltage 3.908750m<volts>
                    ReadingValues.BatteryPercentage 69.296875m<percent>
                    ReadingValues.BatteryState BatteryState.Charging
                    ReadingValues.PanelVoltage 4.656000m<volts>
                    ReadingValues.ChargeMilliamps -22.200001m<milliamps>                        
                    ReadingValues.TemperatureCelciusBarometer 22.570000m<celcius>
                    ReadingValues.PressurePascal 98883.906250m<pascal>
                    ReadingValues.HumidityPercentBarometer 63.833008m<percent>
                    ReadingValues.X 3.940063m<degrees>
                    ReadingValues.Y 0.140991m<degrees>
                    ReadingValues.Z 2.772461m<degrees>                        
                ]
            )
            ,"Did not parse combined values")