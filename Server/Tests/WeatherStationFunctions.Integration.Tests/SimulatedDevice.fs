open System
open System.IO
open System.Text
open FSharp.Data

open Microsoft.Azure.Devices.Client

open WeatherStation
open WeatherStation.Model
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

let devices = Sensors.All

let random = Random()
let generateRandomReading values =

    let version = 1
    let batteryVoltage = random.NextDouble() * 4.2
    let batteryPercentage = random.NextDouble() * 100.0

    let builder = StringBuilder()

    let generate {Name = _; Type = valueType} =
        match valueType with
        | ValueType.Float -> 
            let value = random.NextDouble()
            builder.Append(value)
        | ValueType.Int -> 
            let value = random.Next()
        builder.Append(',') |> ignore

    values |> Seq.iter generate
    valuesWriter.Flush() |> ignore

    use outputBuffer = new StringWriter()
    valuesBuffer.Seek(0L, SeekOrigin.Begin) |> ignore
    SimpleBase.Base85.Z85.Encode(valuesBuffer, outputBuffer)

    printfn "%d" builder.Length

    outputBuffer.ToString()  

let createSampleMessage() =
    let date = DateTimeOffset.UtcNow.ToString()
    let randomReading = generateRandomReading [for device in devices do yield! device.Values]

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
        Sensors = Sensors.id devices |> int}
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