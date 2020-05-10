open System
open System.Text
open FSharp.Data

open Microsoft.Azure.Devices.Client

open WeatherStation
open WeatherStation.Model
open WeatherStation.Shared
open WeatherStation.Sensors
open WeatherStation.Repository

type Secrets = JsonProvider< "Secrets.Template.json" >

// Gets the connection string for the test device
let secrets = async {
    let! secrets = Secrets.AsyncLoad "Secrets.json"
    return secrets
}

let connect (secrets : Secrets.Root) =
    let connectionString = secrets.DeviceConnectionString
    DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt)

let sensors = [
    bme280
    ina219
    internalBattery
    qmc5883l]

let random = Random()
let generateRandomSensorReading (sensor : Sensor) =
    [for (_, valueType) in sensor.SampleValues ->
        match valueType with
        | Float -> 
            let value = random.NextDouble()
            string value
        | Int -> 
            let value = random.Next()
            string value
        | Enum ->
            let value = random.Next()
            string value]
    |> String.concat ":"
    |> sprintf "%c%s" sensor.Prefix

let generateRandomReading sensors =

    let version = 1
    let batteryVoltage = random.NextDouble() * 4.2
    let batteryPercentage = random.NextDouble() * 100.0
    let panelVoltage = random.NextDouble() * 6.0
    let panelCurrent = random.NextDouble() * 100.0

    let readings = sensors |> List.map generateRandomSensorReading |> String.concat ""
    sprintf "%df%f:%fp%f:%f%s" version batteryVoltage batteryPercentage panelVoltage panelCurrent readings

let createSampleMessage() =
    let date = DateTimeOffset.UtcNow.ToString()
    let randomReading = generateRandomReading sensors

    printfn "%d" randomReading.Length

    sprintf
        """
        {
            "data": "%s",
            "device_id": "TestDevice",
            "event": "Reading",
            "published_at": "%s"
        }
        """
        randomReading
        date

let deviceId = "TestDevice"
let deviceType = DeviceType.Test

let sendDeviceToCloudMessages (client : DeviceClient) messageString = async {
    printfn "%s" messageString        
    let message = new Message(Encoding.ASCII.GetBytes(messageString : string));
    do! client.SendEventAsync(message) |> Async.AwaitTask
}

let getLastReadingTime (repository : IWeatherStationsRepository) = async {
    match! repository.Get deviceType deviceId with
    | Some device -> return device.LastReading
    | None -> return None
}

let createTestWeatherStation (repository : IWeatherStationsRepository) = async {
    do! repository.Save {
        DeviceType = string deviceType
        DeviceId = deviceId
        WundergroundStationId = null
        WundergroundPassword = null
        DirectionOffsetDegrees = None
        CreatedOn = DateTime.Now
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
        Settings = null
        Sensors = Sensors.id sensors |> int}
}

let runTest (client : DeviceClient) (repository : IWeatherStationsRepository) = async {

    let! initialReadingTime = getLastReadingTime repository
    
    if initialReadingTime.IsNone then do! createTestWeatherStation repository

    do! sendDeviceToCloudMessages client (createSampleMessage())
    do! Async.Sleep 5000
    let! subsequentReadingTime = getLastReadingTime repository
    
    return initialReadingTime, subsequentReadingTime
}
    

[<EntryPoint>]
let main argv =

    async {
        let! secrets = Secrets.AsyncLoad "Secrets.json"
        let deviceClient = connect secrets

        let! deviceRepository = AzureStorage.weatherStationRepository secrets.StorageConnectionString

        while true do
            let! (initialTime, subsequentTime) = runTest deviceClient deviceRepository
            printfn "%b %A" (subsequentTime > initialTime) (initialTime, subsequentTime)
    }
    |> Async.RunSynchronously
    |> ignore

    0