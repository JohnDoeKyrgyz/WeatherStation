namespace WeatherStation.Tests.Server

open NUnit.Framework

[<TestFixture>]
module LogicTests =
    open System
    open System.Threading.Tasks

    open WeatherStation.Model
    open WeatherStation.Shared
    open WeatherStation.Logic

    let weatherStation = {
        DeviceType = "Particle"
        DeviceId = "1234"
        WundergroundStationId = "KSTAT"
        WundergroundPassword = "pass"
        DirectionOffsetDegrees = None
        CreatedOn = DateTime.Now
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
        Settings = null
        Sensors = 0xFFFF
    }   

    [<Test>]
    let GetWeatherStations() =
        async {
            let activeStation = {weatherStation with LastReading = Some (DateTime.Now); WundergroundStationId = "0"}
            let inactiveStation = {weatherStation with LastReading = Some (DateTime.Now.AddDays(-3.0)); WundergroundStationId = "1" }
            let stations = async { return [activeStation; inactiveStation]}
            let activeThreshold = TimeSpan.FromHours(1.0)
            let! stations = getWeatherStations activeThreshold stations

            Assert.That(stations.Length, Is.EqualTo(2), "Should be two stations")
            Assert.That(stations.[0].Status, Is.EqualTo(Status.Active), "First station should be active")
            Assert.That(stations.[0].WundergroundId, Is.EqualTo(Some "0"), "Unexpected station")

            Assert.That(stations.[1].Status, Is.EqualTo(Status.Offline), "Second station should not be active")
            Assert.That(stations.[1].WundergroundId, Is.EqualTo(Some "1"), "Unexpected station")
        }
        |> Async.StartAsTask
        :> Task            
            
    [<Test>]
    let GetWeatherStationsTrimming() =
        async {
            let activeStation = {
                weatherStation with 
                    DeviceId = weatherStation.DeviceId + Environment.NewLine
                    WundergroundStationId = weatherStation.WundergroundStationId + Environment.NewLine}
            let stations = async { return [activeStation]}
            let activeThreshold = TimeSpan.FromHours(1.0)
            let! stations = getWeatherStations activeThreshold stations

            Assert.That(stations.Length, Is.EqualTo(1), "Should be two stations")
            Assert.That(stations.[0].WundergroundId, Is.EqualTo(Some weatherStation.WundergroundStationId), "Unexpected station")
            Assert.That(stations.[0].Key.DeviceId, Is.EqualTo(weatherStation.DeviceId), "Unexpected device id")
        }
        |> Async.StartAsTask
        :> Task
            
    [<Test>]
    let OptionalWundergroundId() =
        async {            
            let activeStation = {
                weatherStation with 
                    DeviceId = weatherStation.DeviceId + Environment.NewLine
                    WundergroundStationId = null}
            let stations = async { return [activeStation]}
            let activeThreshold = TimeSpan.FromHours(1.0)
            let! stations = getWeatherStations activeThreshold stations

            Assert.That(stations.Length, Is.EqualTo(1), "Should be two stations")
            Assert.That(stations.[0].WundergroundId.IsNone, "Unexpected station")
            Assert.That(stations.[0].Key.DeviceId, Is.EqualTo(weatherStation.DeviceId), "Unexpected device id")
        }        
        |> Async.StartAsTask
        :> Task