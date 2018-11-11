open System.Text
open FSharp.Data
open Microsoft.Azure.Devices.Client
open WeatherStation
open WeatherStation.Model

type Secrets = JsonProvider< "Secrets.Template.json" >

// Gets the connection string for the test device
let secrets = async {
    let! secrets = Secrets.AsyncLoad "Secrets.json"
    return secrets
}

let connect (secrets : Secrets.Root) = async {    
    let connectionString = secrets.DeviceConnectionString
    return DeviceClient.CreateFromConnectionString(connectionString, TransportType.Mqtt)
}

let sampleMessage =
    """
    {
        "data": "100:4.006250:3864|d10.800000:86.500000a1.700000:15",
        "device_id": "TestDevice",
        "event": "Reading",
        "published_at": "2018-06-04T23:35:04.892Z"
    }
    """

let sendDeviceToCloudMessages (client : DeviceClient) messageString = async {
    let message = new Message(Encoding.ASCII.GetBytes(messageString : string));
    do! client.SendEventAsync(message) |> Async.AwaitTask
}

let getLastReadingTime (secrets : Secrets.Root) = async {
    let! deviceRepository = AzureStorage.weatherStationRepository secrets.StorageConnectionString
    match! deviceRepository.Get DeviceType.Test "TestDevice" with
    | Some device -> return Some device.LastReading
    | None -> return None
}

let runTest settings
    

[<EntryPoint>]
let main argv =

    sendDeviceToCloudMessagesAsync
    |> Async.RunSynchronously
    |> ignore

    0
