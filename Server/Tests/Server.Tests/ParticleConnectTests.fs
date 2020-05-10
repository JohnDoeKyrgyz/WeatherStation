namespace WeatherStation.Tests.Server
open NUnit.Framework
[<TestFixture>]
module ParticleConnectTests =
    
    open System.Threading.Tasks
    
    [<Test>]
    [<Category("Integration")>]
    let Connect() =
        async {
            let! connection = WeatherStation.ParticleConnect.connect
            match connection with
            | Error _ ->
                Assert.Fail "Connection failed"    
            | Ok connection ->
                Assert.That(connection, Is.Not.Null, "No connection available")

                let! devices = connection.GetDevicesAsync() |> Async.AwaitTask
                Assert.That(devices, Is.Not.Null, "No devices returned")

                for device in devices do printfn "%s" device.Id
        }
        |> Async.StartAsTask
        :> Task
