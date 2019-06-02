namespace WeatherStation.Functions

module Model =

    open System
    open WeatherStation.Model

    [<Measure>]
    type volts

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
        | PanelVoltage of decimal<volts>
        | TemperatureCelciusHydrometer of decimal<celcius>
        | TemperatureCelciusBarometer of decimal<celcius>
        | HumidityPercentHydrometer of decimal<percent>
        | HumidityPercentBarometer of decimal<percent>
        | PressurePascal of decimal<pascal>
        | SpeedMetersPerSecond of decimal<meters/seconds>
        | GustMetersPerSecond of decimal<meters/seconds>
        | DirectionSixteenths of int<sixteenths>
        | RefreshInterval of int<seconds>

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
        | PanelVoltage voltage -> {reading with PanelVoltage = toDouble(voltage)}
        | TemperatureCelciusBarometer temp -> {reading with TemperatureCelciusBarometer = toDouble(temp)}
        | TemperatureCelciusHydrometer temp -> {reading with TemperatureCelciusHydrometer = toDouble(temp)}
        | HumidityPercentHydrometer perc -> {reading with HumidityPercentHydrometer = toDouble(perc)}
        | HumidityPercentBarometer perc -> {reading with HumidityPercentBarometer = toDouble(perc)}
        | PressurePascal perc -> {reading with PressurePascal = toDouble(perc)}
        | SpeedMetersPerSecond speed -> {reading with SpeedMetersPerSecond = toDouble(speed)}
        | GustMetersPerSecond speed -> {reading with GustMetersPerSecond = toDouble(speed)}
        | DirectionSixteenths direction -> {reading with DirectionDegrees = sixteenthsToDegrees direction |> double}

    let createReading deviceReading = 
        let readingKey = String.Format("{0:D19}", DateTime.MaxValue.Ticks - DateTime.UtcNow.Ticks)
        let reading = {Reading.Default with RowKey = readingKey}
        let reading = deviceReading.Readings |> List.fold applyReading reading
        let reading =
            if reading.DeviceTime = DateTime.MinValue
            then {reading with DeviceTime = reading.ReadingTime}
            else reading
        {reading with SourceDevice = deviceReading.DeviceId}