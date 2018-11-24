namespace WeatherStation.Tests.Server
module LogicTests =
    open System
    open Expecto

    open WeatherStation.Model
    open WeatherStation.Shared
    open WeatherStation.Logic

    let weatherStation = {
        DeviceType = "Particle"
        DeviceId = "1234"
        WundergroundStationId = "KSTAT"
        WundergroundPassword = "pass"
        DirectionOffsetDegrees = None
        Latitude = 0.0
        Longitude = 0.0
        LastReading = None
        Settings = null
    }   

    [<Tests>]
    let tests =
        
        testList "Logic Tests" [
            testAsync "Get WeatherStations" {
                let activeStation = {weatherStation with LastReading = Some (DateTime.Now); WundergroundStationId = "0"}
                let inactiveStation = {weatherStation with LastReading = Some (DateTime.Now.AddDays(-3.0)); WundergroundStationId = "1" }
                let stations = async { return [activeStation; inactiveStation]}
                let activeThreshold = TimeSpan.FromHours(1.0)
                let! stations = getWeatherStations activeThreshold stations

                Expect.equal stations.Length 2 "Should be two stations"
                Expect.equal stations.[0].Status Status.Active "First station should be active"
                Expect.equal stations.[0].WundergroundId (Some "0") "Unexpected station"

                Expect.equal stations.[1].Status Status.Offline "Second station should not be active"
                Expect.equal stations.[1].WundergroundId (Some "1") "Unexpected station"
            }
            testAsync "Get WeatherStations Trimming" {
                let activeStation = {
                    weatherStation with 
                        DeviceId = weatherStation.DeviceId + Environment.NewLine
                        WundergroundStationId = weatherStation.WundergroundStationId + Environment.NewLine}
                let stations = async { return [activeStation]}
                let activeThreshold = TimeSpan.FromHours(1.0)
                let! stations = getWeatherStations activeThreshold stations

                Expect.equal stations.Length 1 "Should be two stations"
                Expect.equal stations.[0].WundergroundId (Some weatherStation.WundergroundStationId) "Unexpected station"
                Expect.equal stations.[0].Key.DeviceId weatherStation.DeviceId "Unexpected device id"
            }
            testAsync "Optional WundergroundId" {
                let activeStation = {
                    weatherStation with 
                        DeviceId = weatherStation.DeviceId + Environment.NewLine
                        WundergroundStationId = null}
                let stations = async { return [activeStation]}
                let activeThreshold = TimeSpan.FromHours(1.0)
                let! stations = getWeatherStations activeThreshold stations

                Expect.equal stations.Length 1 "Should be two stations"
                Expect.isNone stations.[0].WundergroundId "Unexpected station"
                Expect.equal stations.[0].Key.DeviceId weatherStation.DeviceId "Unexpected device id"}
            ]