namespace WeatherStation
open System.Text.RegularExpressions

module Sensors =

    open WeatherStation.Readings
    
    type ValueType = 
        | Float
        | Int
        | Enum

    type Sensor = {
        Id : byte
        Prefix : char  
        Name : string
        Description : string
        SampleValues : (ReadingValues * ValueType) list
    }

    let ina219 = {
        Id = 1uy
        Prefix = 'p'
        Name = "INA219"
        Description = "Voltage / Current"
        SampleValues = 
            [
                PanelVoltage 0.0m<volts>, ValueType.Float
                ChargeMilliamps 0.0m<milliamps>, ValueType.Float
            ] }

    let internalBattery = {
        Id = 2uy
        Prefix = 'f'
        Name = "Fuel Guage"
        Description = "Onboard Power Sensor"
        SampleValues = 
            [
                BatteryChargeVoltage 0.0m<volts>, ValueType.Float
                BatteryPercentage 0.0m<percent>, ValueType.Float
                BatteryState BatteryState.Disconnected, ValueType.Enum
            ] }

    let anemometer =  {
        Id = 4uy
        Prefix = 'a'
        Name = "LaCrosse_TX23U"
        Description = "Anemometer"
        SampleValues = 
            [
                SpeedMetersPerSecond 0.0m<meters/seconds>, ValueType.Float
                DirectionSixteenths 0<sixteenths>, ValueType.Int
            ] }
        
    let bme280 = {        
        Id = 8uy
        Prefix = 'b'
        Name ="BME280"
        Description = "Temperature / Pressure / Humidity sensor"
        SampleValues = 
            [
                TemperatureCelciusBarometer 0.0m<celcius>, ValueType.Float
                PressurePascal 0.0m<pascal>, ValueType.Float
                HumidityPercentBarometer 0.0m<percent>, ValueType.Float                                
            ] }

    let qmc5883l = {
        Id = 16uy
        Prefix = 'c'
        Name = "QMC5883L"
        Description = "Compass"
        SampleValues = 
            [
                X 0.0m<degrees>, ValueType.Float
                Y 0.0m<degrees>, ValueType.Float
                Z 0.0m<degrees>, ValueType.Float
            ] } 

    let dht22 = {
        Id = 32uy
        Prefix = 'd'
        Name = "DHT22"
        Description = "Temperature / Humidity"
        SampleValues =
            [
                TemperatureCelciusHydrometer 0.0m<celcius>, ValueType.Float
                HumidityPercentHydrometer 0.0m<percent>, ValueType.Float
            ] }    

    let All = [
        internalBattery
        ina219
        anemometer
        bme280
        qmc5883l        
        dht22        
    ]        

    let id sensors =
        sensors |> Seq.fold (fun result sensor -> result ^^^ sensor.Id) 0x00uy

    let sensors id =
        All |> List.filter (fun sensor -> (sensor.Id &&& id) = sensor.Id)

    let readingKey (sampleReading : ReadingValues) = 
        let readingToString = string sampleReading
        readingToString.Substring(0, readingToString.IndexOf(" "))        

    let loadReading sensor (regexMatch : Match) =
        let samples = 
            sensor.SampleValues 
            |> Seq.map fst
            |> Seq.toList
        let rawValues = 
            samples
            |> List.map (readingKey >> (fun key -> regexMatch.Groups.[key].Value))
        [for ((sample, _), value) in rawValues |> Seq.zip sensor.SampleValues -> loadReadingValue sample value]        

    let parseReadingOfSensors (sensors : seq<Sensor>) reading =
        let valuePattern (sampleReading : ReadingValues, valueType) =
            let name = readingKey sampleReading
            let intPattern = sprintf @"(?<%s>-?\d+)" name
            match valueType with
            | Int -> intPattern
            | Enum -> intPattern
            | Float -> sprintf @"(?<%s>-?\d+\.\d+)" name

        let sensorMatch sensor =
            let valuesPattern = sensor.SampleValues |> Seq.map valuePattern |> String.concat ":"
            let pattern = sprintf "%c%s" sensor.Prefix valuesPattern
            [for regexMatch in Regex.Matches(reading, pattern) -> regexMatch]
            
        let matches =
            sensors
            |> Seq.collect sensorMatch

        let sensorReadings = 
            query {
                for regexMatch in matches do
                join sensor in sensors on (regexMatch.Value.[0] = sensor.Prefix)
                select (sensor, regexMatch) }

        let readings = [for (sensor, regexMatch) in sensorReadings do yield! loadReading sensor regexMatch]
        readings

    let parseReading id reading =
        let selectedSensors = sensors id
        parseReadingOfSensors selectedSensors reading