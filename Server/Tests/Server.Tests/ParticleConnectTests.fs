namespace WeatherStation.Tests.Server
module ParticleConnectTests =
    open Expecto

    [<Tests>]
    let tests =

        testList "Integration" [
            
            testAsync "Connect" {
                let! connection = WeatherStation.ParticleConnect.connect
                Expect.isNotNull connection "No connection available"

                let! devices = connection.GetDevicesAsync() |> Async.AwaitTask
                Expect.isNotNull devices "No devices returned"

                for device in devices do printfn "%s" device.Id
            }
        ]
