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

let parsers = 
    [Particle, Particle.parseValues; Hologram, Hologram.parseValues]

let tryParse parser content =
    try
        Some (parser content)
    with _ -> None    

let Run(eventHubMessage: string, weatherStationsTable: IQueryable<WeatherStation>, storedReading : byref<Reading>, log: TraceWriter) =

    async {
        log.Info(eventHubMessage)

        let parseAttempts = [
            for (key, parser) in parsers do
                let parseResult = tryParse parser eventHubMessage
                if parseResult.IsSome then
                    yield key, parseResult.Value ]

        match parseAttempts with
        | (deviceType, values) :: _ ->

            let reading = Model.createReading values

            let partitionKey = string deviceType
            let deviceSerialNumber = reading.SourceDevice

            let weatherStation = 
                weatherStationsTable
                    .Where( fun station -> station.PartitionKey = partitionKey && station.RowKey = deviceSerialNumber )
                    .ToArray()
                    .Single()

            let valuesSeq = values |> Seq.ofList
            let! wundergroundResponse = postToWunderground weatherStation.WundergroundStationId weatherStation.WundergroundPassword valuesSeq log

            log.Info(sprintf "%A" wundergroundResponse)

            storedReading <- reading
        | _ -> 
            log.Info("No values parsed") }            