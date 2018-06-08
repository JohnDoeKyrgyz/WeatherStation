#load "../Preamble.fsx"
#load "../Database.fsx"
#load "Hologram.fsx"
#load "Particle.fsx"
#load "WundergroundPost.fsx"
#load "ProcessReadings.fsx"

open System
open System.Linq

open Microsoft.Azure.WebJobs.Host

open Database
open Model
open ProcessReadings
open WundergroundPost

let tryParse parser content =
    try
        Choice1Of2 (parser content)
    with ex -> Choice2Of2 ex

let parsers = 
    [Particle, Particle.parseValues; Hologram, Hologram.parseValues]

let rec innerMostException (ex : exn) =
    if ex.InnerException <> null then innerMostException ex.InnerException else ex

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<WeatherStation>, readingsTable: IQueryable<Reading>, storedReading : byref<Reading>, log: TraceWriter) =
    
    let reading =
        async {
            log.Info(eventHubMessage)

            let parseAttempts = [
                for (key, parser) in parsers do
                    yield key, (tryParse (parser log) eventHubMessage)]

            let successfulAttempts = [
                for (key, attempt) in parseAttempts do
                    match attempt with
                    | Choice1Of2 result -> yield key, result
                    | _ -> ()]

            match successfulAttempts with
            | (deviceType, deviceReading) :: _ ->

                log.Info(sprintf "%A" deviceReading)

                let partitionKey = string deviceType
                log.Info(sprintf "Searching for device %s %s in registry" partitionKey deviceReading.DeviceId)

                let weatherStations = 
                    weatherStationsTable
                        .Where( fun station -> station.PartitionKey = partitionKey && station.RowKey = deviceReading.DeviceId )
                        .ToArray()

                if weatherStations.Length <> 1 then failwithf "WeatherStation %s %s is incorrectly provisioned" partitionKey deviceReading.DeviceId
                let weatherStation = weatherStations.[0]
                
                let values = fixReadings readingsTable weatherStation deviceReading.Readings
                log.Info(sprintf "Fixed Values %A" values)

                let valuesSeq = values |> Seq.ofList
                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log

                log.Info(sprintf "%A" wundergroundResponse)
                
                let reading = Model.createReading deviceReading
                return Some reading
            | _ -> 
                log.Info("No values parsed")
                for (key, attempt) in parseAttempts do
                    let message =
                        match attempt with
                        | Choice2Of2 ex -> 
                            string (innerMostException ex)
                        | _ -> "no exception"

                    log.Info(string key)
                    log.Error(message)
                return None }
        |> Async.RunSynchronously

    match reading with
    | Some reading -> 
        log.Info(sprintf "Saving Reading %A" reading)
        storedReading <- reading
    | None ->
        log.Info("Nothing to save")