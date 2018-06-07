
#I __SOURCE_DIRECTORY__
#r @"..\packages\FSharp.Data\lib\net45\FSharp.Data.dll"

#load "../Preamble.fsx"
#load "../Database.fsx"
#load "Hologram.fsx"
#load "Particle.fsx"
#load "WundergroundPost.fsx"

open System
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

                //Find the last ten minutes of readings
                let lastTenMinutesOfReadings =
                    let tenMinutesAgo = reading.ReadingTime.Subtract(TimeSpan.FromMinutes(10.0))
                    query {
                        for reading in readingsTable do
                        where (reading.ReadingTime > tenMinutesAgo && reading.SpeedMetersPerSecond.HasValue)
                        select reading }
                    |> Seq.toList

                let additionalReadings = 
                    match lastTenMinutesOfReadings with
                    | mostRecentWindReading :: _ -> 
                        [
                            let gust = 
                                lastTenMinutesOfReadings 
                                |> Seq.map (fun reading -> reading.SpeedMetersPerSecond.Value) 
                                |> Seq.max 
                            yield GustMetersPerSecond gust
                            
                            let secondsSinceLastRun = int (reading.ReadingTime.Subtract(mostRecentWindReading.ReadingTime).TotalSeconds)
                            yield RefreshInterval secondsSinceLastRun

                            if values |> Seq.exists (fun value -> match value with | SpeedMetersPerSecond _ -> true | _ -> false) |> not then
                                yield SpeedMetersPerSecond mostRecentWindReading.SpeedMetersPerSecond.Value
                        ]
                    | _ -> []
                log.Info(sprintf "Extrapolated readings %A" additionalReadings)

                let values = values @ additionalReadings                                                                    

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
    | Some reading -> 
        log.Info(sprintf "Saving Reading %A" reading)
        storedReading <- reading
    | None ->
        log.Info("Nothing to save")