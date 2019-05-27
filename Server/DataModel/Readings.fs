namespace WeatherStation

module Readings =

    open System
    open Model

    [<Measure>]
    type volts

    [<Measure>]
    type milliamps

    [<Measure>]
    type celcius

    [<Measure>]
    type percent

    [<Measure>]
    type pascal

    [<Measure>]
    type meters

    [<Measure>]
    type sixteenths

    [<Measure>]
    type degrees

    [<Measure>]
    type seconds

    let degreesPerSixteenth = 22.5<degrees/sixteenths>

    let sixteenthsToDegrees (value : int<sixteenths>) = 
        let directionDegrees = ((float value) * 1.0<sixteenths>)
        directionDegrees * degreesPerSixteenth

    type ReadingValues =
        | ReadingTime of DateTime
        | DeviceTime of DateTime
        | SupplyVoltage of decimal<volts>
        | BatteryChargeVoltage of decimal<volts>
        | BatteryPercentage of decimal<milliamps>
        //INA219
        | PanelVoltage of decimal<volts>
        | ChargeMilliamps of decimal<milliamps>
        //BME280
        | TemperatureCelciusBarometer of decimal<celcius>
        | HumidityPercentBarometer of decimal<percent>
        | PressurePascal of decimal<pascal>
        //DHT22
        | TemperatureCelciusHydrometer of decimal<celcius>        
        | HumidityPercentHydrometer of decimal<percent>
        //anemometer
        | SpeedMetersPerSecond of decimal<meters/seconds>
        | GustMetersPerSecond of decimal<meters/seconds>
        | DirectionSixteenths of int<sixteenths>
        //compass
        | X of decimal<degrees>
        | Y of decimal<degrees>
        | Z of decimal<degrees>

    let loadReadingValue sampleReadingValue value =
        match sampleReadingValue with
        | ReadingTime _ -> value |> DateTime.Parse |> ReadingTime
        | DeviceTime _ -> value |> DateTime.Parse |> DeviceTime
        | SupplyVoltage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> SupplyVoltage
        | BatteryChargeVoltage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> BatteryChargeVoltage
        | BatteryPercentage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> BatteryPercentage
        //INA219
        | PanelVoltage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> PanelVoltage
        | ChargeMilliamps  _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> ChargeMilliamps
        //BME280
        | TemperatureCelciusBarometer _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> TemperatureCelciusBarometer
        | HumidityPercentBarometer _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> HumidityPercentBarometer
        | PressurePascal _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> PressurePascal
        //DHT22
        | TemperatureCelciusHydrometer _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> TemperatureCelciusHydrometer
        | HumidityPercentHydrometer _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> HumidityPercentHydrometer
        //anemometer
        | SpeedMetersPerSecond _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> SpeedMetersPerSecond
        | GustMetersPerSecond _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> GustMetersPerSecond
        | DirectionSixteenths _ -> value |> int |> LanguagePrimitives.Int32WithMeasure |> DirectionSixteenths
        //compass
        | X  _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> X
        | Y  _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> Y
        | Z  _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> Z

    type DeviceReadings = {
        DeviceId : string
        Readings : ReadingValues list
    }    

    let applyReading (reading : Reading) value =

        let toDouble (v : decimal<'T>) = 
            let cleanV = (v / LanguagePrimitives.DecimalWithMeasure<'T> 1.0m)
            double cleanV

        match value with
        | ReadingTime time -> {reading with ReadingTime = time}
        | DeviceTime time -> {reading with DeviceTime = time}
        | SupplyVoltage voltage -> {reading with SupplyVoltage = toDouble(voltage)}
        | BatteryChargeVoltage voltage -> {reading with BatteryChargeVoltage = toDouble(voltage)}
        | ChargeMilliamps milliamps -> {reading with PanelMilliamps = toDouble(milliamps)}
        | BatteryPercentage percentage -> {reading with BatteryPercentage = toDouble(percentage)}
        | PanelVoltage voltage -> {reading with PanelVoltage = toDouble(voltage)}
        | TemperatureCelciusBarometer temp -> {reading with TemperatureCelciusBarometer = toDouble(temp)}
        | TemperatureCelciusHydrometer temp -> {reading with TemperatureCelciusHydrometer = toDouble(temp)}
        | HumidityPercentHydrometer perc -> {reading with HumidityPercentHydrometer = toDouble(perc)}
        | HumidityPercentBarometer perc -> {reading with HumidityPercentBarometer = toDouble(perc)}
        | PressurePascal perc -> {reading with PressurePascal = toDouble(perc)}
        | SpeedMetersPerSecond speed -> {reading with SpeedMetersPerSecond = toDouble(speed)}
        | GustMetersPerSecond speed -> {reading with GustMetersPerSecond = toDouble(speed)}
        | DirectionSixteenths direction -> {reading with DirectionDegrees = sixteenthsToDegrees direction |> double}
        | X angle -> {reading with X = toDouble(angle)}
        | Y angle -> {reading with Y = toDouble(angle)}
        | Z angle -> {reading with Z = toDouble(angle)}

    let createReading deviceReading = 
        let readingKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks)
        let reading = {Reading.Default with RowKey = readingKey}
        let reading = deviceReading.Readings |> List.fold applyReading reading
        let reading =
            if reading.DeviceTime = DateTime.MinValue
            then {reading with DeviceTime = reading.ReadingTime}
            else reading
        {reading with SourceDevice = deviceReading.DeviceId}