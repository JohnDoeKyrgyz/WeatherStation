namespace WeatherStation.Functions

module WundergroundPost =

    open Model
    open Microsoft.Azure.WebJobs.Host
    open FSharp.Data

    let queryParameter value =
        match value with
        | ReadingTime time -> Some( "dateutc", time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") )
        | SpeedMetersPerSecond speed -> Some( "windspeedmph", string (speed * 2.23694m<_>) )
        | GustMetersPerSecond gust -> Some( "windgustmph", string (gust * 2.23694m<_>) )
        | DirectionSixteenths direction -> Some( "winddir", string (float direction * (360.0 / 16.0)))
        | HumidityPercentBarometer humidity -> Some( "humidity", string (humidity))
        | RefreshInterval intervalSeconds -> Some( "rtfreq", string intervalSeconds )
        | TemperatureCelciusBarometer tempC -> Some( "tempf", string (((9.0m<_> / 5.0m<_>) * tempC) + 32.0m<_>))
        | _ -> None

    let queryParameters values = [
        for value in values do
            let queryParameter = queryParameter value
            if queryParameter.IsSome then yield queryParameter.Value]

    let postToWunderground wundergroundId wundergroundPassword values (log : TraceWriter) =
        async {
        
            //http://wiki.wunderground.com/index.php/PWS_-_Upload_Protocol
            let url = "https://weatherstation.wunderground.com/weatherstation/updateweatherstation.php"
            let queryParameters = 
                [ 
                    "ID", wundergroundId
                    "PASSWORD", wundergroundPassword
                ] @ queryParameters values

            log.Info( sprintf "Wunderground Request: %A" queryParameters )
    
            let! wundergroundResponse = Http.AsyncRequest( url, queryParameters )
            log.Info( sprintf "Wunderground Response: %A" wundergroundResponse )

            return wundergroundResponse
        }