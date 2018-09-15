namespace WeatherStation.Tests.Functions
module ReadingsTests =
    open System
    open Expecto    
    open WeatherStation.Functions.Model
    open WeatherStation.Model
    open WeatherStation.Functions.WundergroundForwarder
    
    type WundergroundParameters = {
        StationId : string
        Password : string
        Values : ReadingValues list
    }

    let buildParticleMessage weatherStation (readingTime : DateTime) data =
        sprintf
            """
            {
                "data": "%s",
                "device_id": "%s",
                "event": "Reading",
                "published_at": "%s"
            }
            """
            data
            weatherStation.DeviceId
            (readingTime.ToString())

    let readingTest log existingReadings readingTime weatherStation message expectedReadings =
        async {
            let wundergroundParameters = ref None
            let weatherStationSave = ref None
            let readingSave = ref None
            do!
                processEventHubMessage
                    log
                    (fun stationId password values traceWriter -> 
                        async { wundergroundParameters := Some {StationId = stationId; Password = password; Values = values |> Seq.toList} })
                    (fun _ _ -> async{ return Some weatherStation})
                    (fun saveWeatherStation -> async {weatherStationSave := Some saveWeatherStation})
                    (fun saveReading -> async {readingSave := Some saveReading})
                    (fun _ _ -> async { return existingReadings })
                    (async {return fun key defaultValue -> async {return {Key = key; Value = defaultValue; Group = ""}}})
                    message
                
            let wundergroundParameters = !wundergroundParameters
            Expect.isSome wundergroundParameters "No call to wunderground"

            let wundergroundParameters = wundergroundParameters.Value
            Expect.equal wundergroundParameters.StationId weatherStation.WundergroundStationId "Unexpected StationId"
            Expect.equal wundergroundParameters.Password weatherStation.WundergroundPassword "Unexpected Password"                        
            Expect.equal wundergroundParameters.Values expectedReadings "Unexpected readings"

            let readings = !readingSave
            Expect.isSome readings "No readings saved"

            let reading = readings.Value
            Expect.equal reading.SourceDevice weatherStation.DeviceId "Unexpected DeviceId"
            Expect.isGreaterThanOrEqual reading.ReadingTime readingTime "Unexpected ReadingTime"

            let weatherStationSave = !weatherStationSave
            Expect.isSome weatherStationSave "WeatherStation not saved"
            let weatherStationSave = weatherStationSave.Value
            Expect.equal weatherStationSave {weatherStation with LastReading = Some readingTime} "Unexpected weatherStation state"

            return reading
        }
