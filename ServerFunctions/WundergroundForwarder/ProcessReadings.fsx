
#load "../Preamble.fsx"
#load "../Database.fsx"
#load "Model.fsx"

open System
open System.Linq

open Model
open Database

let averageWindowMinutes = 10.0

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
            let windowStartTime = readingTime.Subtract(TimeSpan.FromMinutes(averageWindowMinutes))
            query {
                for reading in history do
                where (reading.ReadingTime > windowStartTime)
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
                    let offset = float offset.Value * 1.0<degrees>
                    let degrees = float direction * 16.0<degrees>
                    let correctDegrees = offset + degrees % 360.0<degrees>
                    let trueDegrees = correctDegrees / 22.5<degrees/sixteenths>
                    DirectionSixteenths ((int trueDegrees) * 1<sixteenths>)
                | value -> value]
        | _-> values

    transformedReadings @ additionalReadings                