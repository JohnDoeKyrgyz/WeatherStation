namespace WeatherStation.Functions

module WundergroundPost =

    open FSharp.Data
    open Microsoft.Extensions.Logging
    open WeatherStation.Readings
    
    let queryParameter value =
        match value with
        | ReadingTime time -> Some( "dateutc", time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss") )
        | SpeedMetersPerSecond speed -> Some( "windspeedmph", string (speed * 2.23694m<_>) )
        | GustMetersPerSecond gust -> Some( "windgustmph", string (gust * 2.23694m<_>) )
        | DirectionSixteenths direction -> Some( "winddir", string (sixteenthsToDegrees direction))
        | HumidityPercentBarometer humidity -> Some( "humidity", string (humidity))
        | RefreshInterval intervalSeconds -> Some( "rtfreq", string intervalSeconds )
        | TemperatureCelciusBarometer tempC -> Some( "tempf", string (((9.0m<_> / 5.0m<_>) * tempC) + 32.0m<_>))
        | _ -> None

    let queryParameters values = [
        for value in values do
            let queryParameter = queryParameter value
            if queryParameter.IsSome then yield queryParameter.Value]

    let postToWunderground wundergroundId wundergroundPassword values (log : ILogger) =
        async {
        
            //http://wiki.wunderground.com/index.php/PWS_-_Upload_Protocol
            let url = "https://weatherstation.wunderground.com/weatherstation/updateweatherstation.php"
            let queryParameters = 
                [ 
                    "ID", wundergroundId
                    "PASSWORD", wundergroundPassword
                ] @ queryParameters values

            log.LogInformation( sprintf "Wunderground Request: %A" queryParameters )
    
            let! wundergroundResponse = Http.AsyncRequest( url, queryParameters )
            log.LogInformation( sprintf "Wunderground Response: %A" wundergroundResponse )

            return wundergroundResponse
        }