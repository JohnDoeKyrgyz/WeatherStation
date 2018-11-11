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
        | Hologram
        | Test

    let parseDeviceType value =
        match value with 
        | "Particle" -> Particle
        | "Hologram" -> Hologram
        | "Test" -> Test
        | _ -> failwithf "%s is not a valid DeviceType" value

    type WeatherStation = {
        [<PartitionKey>]
        DeviceType : string
        [<RowKey>]
        DeviceId : string
        WundergroundStationId : string
        WundergroundPassword : string
        DirectionOffsetDegrees : int option
        Latitude : double
        Longitude : double
        LastReading : DateTime option
        Settings : string
    }

    type Reading = {
        RefreshIntervalSeconds : int
        DeviceTime : DateTime
        ReadingTime : DateTime
        SupplyVoltage : double
        BatteryChargeVoltage : double
        PanelVoltage : double
        TemperatureCelciusHydrometer : double
        TemperatureCelciusBarometer : double
        HumidityPercentHydrometer : double
        HumidityPercentBarometer : double
        PressurePascal : double
        GustMetersPerSecond : double
        SpeedMetersPerSecond : double
        DirectionDegrees : double
        [<PartitionKey>]
        SourceDevice : string
        [<RowKey>]
        RowKey : string
    }
    with
        static member Default = {
            RefreshIntervalSeconds = 0
            DeviceTime = DateTime.MinValue
            ReadingTime = DateTime.MinValue
            SupplyVoltage = 0.0
            BatteryChargeVoltage = 0.0
            PanelVoltage = 0.0
            TemperatureCelciusHydrometer = 0.0
            TemperatureCelciusBarometer = 0.0
            HumidityPercentHydrometer = 0.0
            HumidityPercentBarometer = 0.0
            PressurePascal = 0.0
            GustMetersPerSecond = 0.0
            SpeedMetersPerSecond = 0.0
            DirectionDegrees = 0.0
            SourceDevice = null
            RowKey = null
        }