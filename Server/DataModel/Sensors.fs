namespace WeatherStation
open System.Text.RegularExpressions

module Sensors =

    open System
    open WeatherStation.Readings
    
    type ValueType = 
        | Float
        | Int

    type Sensor = {
        Id : byte
        Prefix : char  
        Name : string
        Description : string
        SampleValues : Map<ReadingValues, ValueType>
    }

    let anemometer =  {
        Id = 1uy
        Prefix = 'a'
        Name = "LaCrosse_TX23U"
        Description = "Anemometer"
        SampleValues = 
            [
                SpeedMetersPerSecond 0.0m<meters/seconds>, ValueType.Float
                DirectionSixteenths 0<sixteenths>, ValueType.Int
            ]
            |> Map.ofList }
        
    let bme280 = {        
        Id = 2uy
        Prefix = 'b'
        Name ="BME280"
        Description = "Temperature / Pressure / Humidity sensor"
        SampleValues = 
            [
                TemperatureCelciusBarometer 0.0m<celcius>, ValueType.Float
                PressurePascal 0.0m<pascal>, ValueType.Float
                HumidityPercentBarometer 0.0m<percent>, ValueType.Float
            ]
            |> Map.ofList }

    let qmc5883l = {
        Id = 8uy
        Prefix = 'c'
        Name = "QMC5883L"
        Description = "Compass"
        SampleValues = 
            [
                X 0.0m<degrees>, ValueType.Float
                Y 0.0m<degrees>, ValueType.Float
                Z 0.0m<degrees>, ValueType.Float
            ]
            |> Map.ofList }
        
    let ina219 = {
        Id = 16uy
        Prefix = 'p'
        Name = "INA219"
        Description = "Voltage / Current"
        SampleValues = 
            [
                SpeedMetersPerSecond 0.0m<meters/seconds>, ValueType.Float
                DirectionSixteenths 0<sixteenths>, ValueType.Float
            ]
            |> Map.ofList}

    let dht22 = {
        Id = 32uy
        Prefix = 'd'
        Name = "DHT22"
        Description = "Temperature / Pessure"
        SampleValues =
            [
                SpeedMetersPerSecond 0.0m<meters/seconds>, ValueType.Float
                DirectionSixteenths 0<sixteenths>, ValueType.Float
            ]
            |> Map.ofList}

    let All = [
        anemometer
        bme280
        qmc5883l
        ina219
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
            |> Map.toSeq
            |> Seq.map fst
            |> Seq.toList
        let rawValues = 
            samples
            |> List.map (readingKey >> (fun key -> regexMatch.Groups.[key].Value))
        [for (sample, value) in rawValues |> Seq.zip sensor.SampleValues -> loadReadingValue sample.Key value]
        

    let parseReadingOfSensors (sensors : seq<Sensor>) reading =
        let valuePattern (sampleReading : ReadingValues, valueType) =
            let name = readingKey sampleReading
            match valueType with
            | Int -> sprintf @"(?<%s>\d+)" name
            | Float -> sprintf @"(?<%s>\d+\.\d+)" name

        let sensorMatch sensor =
            let valuesPattern = sensor.SampleValues |> Map.toSeq |> Seq.map valuePattern |> String.concat ":"
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

        [for (sensor, regexMatch) in sensorReadings -> loadReading sensor regexMatch]

    let parseReading id reading =
        let selectedSensors = sensors id
        parseReadingOfSensors selectedSensors reading
        |> List.concat