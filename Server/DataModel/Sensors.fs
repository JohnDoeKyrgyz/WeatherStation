namespace WeatherStation
module Sensors =

    open WeatherStation.Readings
    
    type ValueType = 
        | Float
        | Int

    type Value = {
        Name : string
        Type : ValueType
    }        

    type Sensor = {
        Id : byte        
        Name : string
        Description : string
        Values : Value list
        DefaultReadingValues : ReadingValues list
    }

    let anemometer =  {
        Id = 1uy
        Name = "LaCrosse_TX23U"
        Description = "Anemometer"
        DefaultReadingValues = [SpeedMetersPerSecond 0.0m<meters/seconds>; DirectionSixteenths 0<sixteenths>]
        Values = [
            {Name = "WindSpeed"; Type = ValueType.Float}
            {Name = "WindDirection"; Type = ValueType.Int}
        ] }
        
    let bme280 = {        
        Id = 2uy
        Name ="BME280"
        Description = "Temperature / Pressure / Humidity sensor"
        DefaultReadingValues = [TemperatureCelciusBarometer 0.0m<celcius>; PressurePascal 0.0m<pascal>; HumidityPercentBarometer 0.0m<percent>]
        Values = [
            {Name = "Temperature"; Type = ValueType.Float}
            {Name = "Pressure"; Type = ValueType.Float}
            {Name = "Humidity"; Type = ValueType.Float}
        ]}

    let qmc5883l = {
        Id = 8uy
        Name = "QMC5883L"
        Description = "Compass"
        DefaultReadingValues = [X 0.0m<degrees>; Y 0.0m<degrees>; Z 0.0m<degrees>]
        Values = [
            {Name = "X"; Type = ValueType.Float}
            {Name = "Y"; Type = ValueType.Float}
            {Name = "Z"; Type = ValueType.Float}
        ]}
        
    let ina219 = {
        Id = 16uy
        Name = "INA219"
        Description = "Voltage / Current"
        DefaultReadingValues = [SpeedMetersPerSecond 0.0m<meters/seconds>; DirectionSixteenths 0<sixteenths>]
        Values = [
            {Name = "Volts"; Type = ValueType.Float}
            {Name = "Milliamps"; Type = ValueType.Float}
        ]}

    let dht22 = {
        Id = 32uy
        Name = "DHT22"
        Description = "Temperature / Pessure"
        DefaultReadingValues = [SpeedMetersPerSecond 0.0m<meters/seconds>; DirectionSixteenths 0<sixteenths>]
        Values = [
            {Name = "Temperature"; Type = ValueType.Float}
            {Name = "Humidity"; Type = ValueType.Float}
        ]}

    let All = [
        anemometer
        bme280
        qmc5883l
        ina219
        dht22
    ]        

    let id sensors =
        sensors |> Seq.fold (fun result sensor -> result &&& sensor.Id) 0x00uy

    let sensors id =
        All |> List.filter (fun sensor -> sensor.Id &&& id = 0x01uy)

    let sensorRegex sensors =
        let valuePattern value =
            match value.Type with
            | Int -> sprintf @"(?<%s>\d+)" value.Name
            | Float -> sprintf @"(?<%s>\d+\.\d+)" value.Name
            
        let valuesPattern =
            sensors
            |> Seq.map valuePattern
            |> String.concat ":"
        let pattern = sprintf "%O%s" valuesPattern
        Regex()        

    let parseReading id reading =
        let selectedSensors = sensors id
