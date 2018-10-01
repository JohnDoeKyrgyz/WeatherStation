namespace WeatherStation.Tests.Server
module ParticleConnectTests =
    open System
    open Expecto
    open WeatherStation

    [<Tests>]
    let getTokenTests =

        testList "Integration" [
            //testCaseAsync "Connect" (async {return ()})
            
            testAsync "Connect" {
                let! connection = WeatherStation.ParticleConnect.connect
                Expect.isNotNull connection "No connection available"

                let! devices = connection.GetDevicesAsync() |> Async.AwaitTask
                Expect.isNotNull devices "No devices returned"

                for device in devices do printfn "%s" device.Id
            }
        ]
