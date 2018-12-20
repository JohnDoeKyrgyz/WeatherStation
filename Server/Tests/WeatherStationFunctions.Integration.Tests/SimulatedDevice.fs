open System
open System.Text
open FSharp.Data
open Microsoft.Azure.Devices.Client
open WeatherStation
open WeatherStation.Model
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

let sampleMessage =
    let date = DateTimeOffset.UtcNow.ToString()
    sprintf
        """
        {
            "data": "100:4.006250:3864|d10.800000:86.500000a1.700000:15",
            "device_id": "TestDevice",
            "event": "Reading",
            "published_at": "%s"
        }
        """
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
        Settings = null}
}

let runTest (client : DeviceClient) (repository : IWeatherStationsRepository) = async {

    let! initialReadingTime = getLastReadingTime repository
    
    if initialReadingTime.IsNone then do! createTestWeatherStation repository

    do! sendDeviceToCloudMessages client sampleMessage
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