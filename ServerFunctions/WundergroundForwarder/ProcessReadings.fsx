
#load "../Preamble.fsx"
#load "../Database.fsx"
#load "Model.fsx"

open System
open System.Linq

open Model
open Database

let averageMinutes = 10.0

let rec readingTime values =
    match values with
    | ReadingTime v :: _ -> Some v
    | _ :: vs -> readingTime vs
    | [] -> None

let rec direction values =    
    match values with
    | ReadingTime v :: _ -> Some v
    | _ :: vs -> readingTime vs
    | [] -> None

let fixReadings (history : IQueryable<Reading>) (weatherStation : WeatherStation) values =

    let readingTime = readingTime values

    let recentReadings =
        match readingTime with
        | Some readingTime ->
            let tenMinutesAgo = readingTime.Subtract(TimeSpan.FromMinutes(averageMinutes))
            query {
                for reading in history do
                where (reading.ReadingTime > tenMinutesAgo && reading.SpeedMetersPerSecond.HasValue)
                select reading }
            |> Seq.toList
        | None -> []

    let recentWindSpeeds = [
        for reading in recentReadings do
            if reading.SpeedMetersPerSecond.HasValue then yield reading.SpeedMetersPerSecond.Value]

    let additionalReadings = [
        if recentWindSpeeds.Length > 0 then
            let gust = 
                recentWindSpeeds
                |> Seq.max
                |> decimal
                |> LanguagePrimitives.DecimalWithMeasure
            yield GustMetersPerSecond gust

        match readingTime, recentReadings with
        | Some readingTime, mostRecentWindReading :: _ ->
            let secondsSinceLastRun = int (readingTime.Subtract(mostRecentWindReading.ReadingTime).TotalSeconds) * 1<seconds>
            yield RefreshInterval secondsSinceLastRun
        | _ -> () ]

    let transformedReadings =
        match weatherStation.DirectionOffsetDegrees with
        | offset when offset.HasValue && offset.Value > 0 ->
            [for reading in values ->
                match reading with
                | DirectionSixteenths direction ->                    
                    let offset = offset.Value * 1<degrees>
                    let degrees = direction * 16<degrees/sixteenths>
                    DirectionSixteenths (((offset + degrees) % 360<degrees>) / 16<degrees/sixteenths>)
                | value -> value]
        | _-> values

    transformedReadings @ additionalReadings                