#I __SOURCE_DIRECTORY__
#load "../Preamble.fsx"
#load "../Database.fsx"
#load "Hologram.fsx"
#load "Particle.fsx"
#load "WundergroundPost.fsx"
#r "../packages/FSharp.Data/lib/net40/FSharp.Data.dll"

open System.Linq

open Microsoft.Azure.WebJobs.Host

open Database
open Model
open WundergroundPost

let tryParse parser content =
    try
        Choice1Of2 (parser content)
    with ex -> Choice2Of2 ex

let parsers = 
    [Particle, Particle.parseValues; Hologram, Hologram.parseValues]

let rec innerMostException (ex : exn) =
    if ex.InnerException <> null then innerMostException ex.InnerException else ex

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<WeatherStation>, storedReading : byref<Reading>, log: TraceWriter) =
    
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
            | (deviceType, values) :: _ ->

                log.Info(sprintf "%A" values)
                let reading = Model.createReading values

                let partitionKey = string deviceType
                let deviceSerialNumber = reading.SourceDevice
                log.Info(sprintf "Searching for device %s %s in registry" partitionKey deviceSerialNumber)

                let weatherStations = 
                    weatherStationsTable
                        .Where( fun station -> station.PartitionKey = partitionKey && station.RowKey = deviceSerialNumber )
                        .ToArray()

                if weatherStations.Length <> 1 then failwithf "WeatherStation %s %s is incorrectly provisioned" partitionKey deviceSerialNumber
                let weatherStation = weatherStations.[0]

                let valuesSeq = values |> Seq.ofList
                let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log

                log.Info(sprintf "%A" wundergroundResponse)

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
    | Some reading -> storedReading <- reading
    | None -> ()