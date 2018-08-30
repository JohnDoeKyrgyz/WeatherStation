namespace WeatherStation.Functions

module ProcessReadings =
    
    open Model
    open WeatherStation.Model

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

    let fixReadings recentReadings (weatherStation : WeatherStation) values =

        let readingTime = readingTime values
        let recentWindSpeeds = [for reading in recentReadings -> reading.SpeedMetersPerSecond]

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
            | offset when offset.IsSome && offset.Value > 0 ->
                [for reading in values ->
                    match reading with
                    | DirectionSixteenths direction ->                    
                        let offset = float offset.Value * 1.0<degrees>
                        let degrees = degreesPerSixteenth * (float direction) * 1.0<sixteenths>
                        let correctDegrees = offset + degrees % 360.0<degrees>
                        let trueDegrees = correctDegrees / degreesPerSixteenth
                        DirectionSixteenths ((int trueDegrees) * 1<sixteenths>)
                    | value -> value]
            | _-> values

        transformedReadings @ additionalReadings                