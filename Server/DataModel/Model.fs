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
        SupplyVoltage : double
        BatteryChargeVoltage : double
        BatteryPercentage : double
        PanelMilliamps : double
        PanelVoltage : double
        TemperatureCelciusHydrometer : double
        TemperatureCelciusBarometer : double
        HumidityPercentHydrometer : double
        HumidityPercentBarometer : double
        PressurePascal : double
        GustMetersPerSecond : double
        SpeedMetersPerSecond : double
        DirectionDegrees : double
        X : double
        Y : double
        Z : double
        RefreshInterval : double
        [<PartitionKey>]
        SourceDevice : string
        [<RowKey>]
        RowKey : string
    }
    with
        static member Default = {            
            DeviceTime = DateTime.MinValue
            ReadingTime = DateTime.MinValue
            SupplyVoltage = 0.0
            BatteryPercentage = 0.0
            BatteryChargeVoltage = 0.0
            PanelVoltage = 0.0
            PanelMilliamps = 0.0
            TemperatureCelciusHydrometer = 0.0
            TemperatureCelciusBarometer = 0.0
            HumidityPercentHydrometer = 0.0
            HumidityPercentBarometer = 0.0
            PressurePascal = 0.0
            GustMetersPerSecond = 0.0
            SpeedMetersPerSecond = 0.0
            DirectionDegrees = 0.0
            X = 0.0
            Y = 0.0
            Z = 0.0
            RefreshInterval = 0.0
            SourceDevice = null
            RowKey = null
        }