namespace WeatherStation
module Sensors =

    open WeatherStation.Model
    
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
        LoadValues : Map<string, obj> -> ReadingValues list
    }

    let anemometer =  {
        Id = 1uy
        Name = "LaCrosse_TX23U"
        Description = "Anemometer"
        Values = [
            {Name = "WindSpeed"; Type = ValueType.Float}
            {Name = "WindDirection"; Type = ValueType.Int}
        ] }
        
    let bme280 = {        
        Id = 2uy
        Name ="BME280"
        Description = "Temperature / Pressure / Humidity sensor"
        Values = [
            {Name = "Temperature"; Type = ValueType.Float}
            {Name = "Pressure"; Type = ValueType.Float}
            {Name = "Humidity"; Type = ValueType.Float}
        ]}

    let qmc5883l = {
        Id = 8uy
        Name = "QMC5883L"
        Description = "Compass"
        Values = [
            {Name = "X"; Type = ValueType.Float}
            {Name = "Y"; Type = ValueType.Float}
            {Name = "Z"; Type = ValueType.Float}
        ]}
        
    let ina219 = {
        Id = 16uy
        Name = "INA219"
        Description = "Voltage / Current"
        Values = [
            {Name = "Volts"; Type = ValueType.Float}
            {Name = "Milliamps"; Type = ValueType.Float}
        ]}

    let dht22 = {
        Id = 32uy
        Name = "DHT22"
        Description = "Temperature / Pessure"
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
