namespace WeatherStation

module Model =

    open System    
    open FSharp.Azure.Storage.Table
    open WeatherStation.Shared

    type SystemSetting = {
        [<PartitionKey>]
        Group : string
        [<RowKey>]
        Key : string
        Value : string
    }

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

    type StatusMessage = {
        [<PartitionKey>]
        DeviceType : string
        [<RowKey>]
        DeviceId : string
        StatusMessage : string
        CreatedOn : DateTime
    }

    type Reading = {
        DeviceTime : DateTime
        ReadingTime : DateTime
        BatteryChargeVoltage : double
        BatteryPercentage : double
        BatteryState : int
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
        Message : string
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
            BatteryState = 0
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
            Message = null
            SourceDevice = null
            RowKey = null
        }