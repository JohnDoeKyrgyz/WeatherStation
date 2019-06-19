namespace WeatherStation

module Model =

    open System    
    open FSharp.Azure.Storage.Table

    type SystemSetting = {
        [<PartitionKey>]
        Group : string
        [<RowKey>]
        Key : string
        Value : string
    }

    type DeviceType =
        | Particle
        | Test

    let parseDeviceType value =
        match value with 
        | "Particle" -> Particle
        | "Test" -> Test
        | _ -> failwithf "%s is not a valid DeviceType" value

    type WeatherStation = {
        [<PartitionKey>]
        DeviceType : string
        [<RowKey>]
        DeviceId : string
        CreatedOn : DateTime
        WundergroundStationId : string
        WundergroundPassword : string
        DirectionOffsetDegrees : int option
        Latitude : double
        Longitude : double
        LastReading : DateTime option
        Settings : string
        Sensors : int
    }

    type Reading = {
        DeviceTime : DateTime
        ReadingTime : DateTime
        BatteryChargeVoltage : double
        BatteryPercentage : double
        PanelMilliamps : double
        PanelVoltage : double
        TemperatureCelciusHydrometer : double option
        TemperatureCelciusBarometer : double option
        HumidityPercentHydrometer : double option
        HumidityPercentBarometer : double option
        PressurePascal : double option
        GustMetersPerSecond : double option
        SpeedMetersPerSecond : double option
        DirectionDegrees : double option
        X : double option
        Y : double option
        Z : double option
        [<PartitionKey>]
        SourceDevice : string
        [<RowKey>]
        RowKey : string
    }
    with
        static member Default = {            
            DeviceTime = DateTime.MinValue
            ReadingTime = DateTime.MinValue
            BatteryPercentage = 0.0
            BatteryChargeVoltage = 0.0
            PanelVoltage = 0.0
            PanelMilliamps = 0.0
            TemperatureCelciusHydrometer = None
            TemperatureCelciusBarometer = None
            HumidityPercentHydrometer = None
            HumidityPercentBarometer = None
            PressurePascal = None
            GustMetersPerSecond = None
            SpeedMetersPerSecond = None
            DirectionDegrees = None
            X = None
            Y = None
            Z = None
            SourceDevice = null
            RowKey = null
        }