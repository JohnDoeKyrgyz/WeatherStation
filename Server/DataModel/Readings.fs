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
        
    type BatteryState =        
        | Unknown = 0
        | NotCharging = 1
        | Charging = 2
        | Charged = 3
        | Discharging = 4
        | Faulted = 5
        | Disconnected = 6
        
    let parseBatteryState value =
        match value with
        | 0 -> BatteryState.Unknown
        | 1 -> BatteryState.NotCharging
        | 2 -> BatteryState.Charging
        | 3 -> BatteryState.Charged
        | 4 -> BatteryState.Discharging
        | 5 -> BatteryState.Faulted
        | 6 -> BatteryState.Disconnected
        | _ -> invalidArg "value" (sprintf "Unrecognized BatteryState value %d" value)

    type ReadingValues =
        | ReadingTime of DateTime
        | DeviceTime of DateTime
        //FuelGauge
        | BatteryChargeVoltage of decimal<volts>
        | BatteryPercentage of decimal<percent>
        | BatteryState of BatteryState
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
        //Derived
        | RefreshInterval of int<seconds>

    let loadReadingValue sampleReadingValue value =
        match sampleReadingValue with
        | ReadingTime _ -> value |> DateTime.Parse |> ReadingTime
        | DeviceTime _ -> value |> DateTime.Parse |> DeviceTime
        //FuelGuage
        | BatteryChargeVoltage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> BatteryChargeVoltage
        | BatteryPercentage _ -> value |> decimal |> LanguagePrimitives.DecimalWithMeasure |> BatteryPercentage
        | BatteryState _ -> value |> int |> parseBatteryState |> BatteryState
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
        //Derived
        | RefreshInterval _ -> value |> int |> LanguagePrimitives.Int32WithMeasure |> RefreshInterval

    type DeviceReadings = {
        DeviceId : string
        Readings : ReadingValues list
        Message : string
    }    

    let applyReading (reading : Reading) value =

        let toDouble (v : decimal<'T>) = 
            let cleanV = (v / LanguagePrimitives.DecimalWithMeasure<'T> 1.0m)
            double cleanV

        match value with
        | BatteryChargeVoltage voltage -> {reading with BatteryChargeVoltage = toDouble(voltage)}
        | BatteryPercentage percentage -> {reading with BatteryPercentage = toDouble(percentage)}
        | BatteryState value -> {reading with BatteryState = int value}
        | ChargeMilliamps milliamps -> {reading with PanelMilliamps = toDouble(milliamps)}
        | DeviceTime time -> {reading with DeviceTime = time}
        | DirectionSixteenths direction -> {reading with DirectionDegrees = sixteenthsToDegrees direction |> double |> Some}
        | GustMetersPerSecond speed -> {reading with GustMetersPerSecond = toDouble(speed) |> Some}
        | HumidityPercentBarometer perc -> {reading with HumidityPercentBarometer = toDouble(perc) |> Some}
        | HumidityPercentHydrometer perc -> {reading with HumidityPercentHydrometer = toDouble(perc) |> Some}
        | PanelVoltage voltage -> {reading with PanelVoltage = toDouble(voltage)}
        | PressurePascal perc -> {reading with PressurePascal = toDouble(perc) |> Some}
        | ReadingTime time -> {reading with ReadingTime = time}
        | SpeedMetersPerSecond speed -> {reading with SpeedMetersPerSecond = toDouble(speed) |> Some}
        | TemperatureCelciusBarometer temp -> {reading with TemperatureCelciusBarometer = toDouble(temp) |> Some}
        | TemperatureCelciusHydrometer temp -> {reading with TemperatureCelciusHydrometer = toDouble(temp) |> Some}
        | X angle -> {reading with X = toDouble(angle) |> Some}
        | Y angle -> {reading with Y = toDouble(angle) |> Some}
        | Z angle -> {reading with Z = toDouble(angle) |> Some}
        | RefreshInterval _ -> reading

    let createReading deviceReading message = 
        let readingKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks)
        let reading = {Reading.Default with RowKey = readingKey; Message = message}
        let reading = deviceReading.Readings |> List.fold applyReading reading
        let reading =
            if reading.DeviceTime = DateTime.MinValue
            then {reading with DeviceTime = reading.ReadingTime}
            else reading
        {reading with SourceDevice = deviceReading.DeviceId}