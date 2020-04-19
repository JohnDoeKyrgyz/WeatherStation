namespace WeatherStation
open Particle.SDK
module ParticleConnect =

    open System
    open System.IO
    open System.Net
    open System.Diagnostics

    open FSharp.Data

    open OtpNet
    open Particle.SDK

    open WeatherStation.Cache

    [<Literal>]
    let SecretsFile = __SOURCE_DIRECTORY__ + @"\Secrets.json"
    type Secrets = JsonProvider< SecretsFile >

    type MfaResponse = JsonProvider< """{"mfa_token":"b1abb537-7a9a-4bee-ba3a-10faef51a093","error":"mfa_required","error_description":"Multi-factor authentication required"}""">
    type TokenResponse = JsonProvider< """{"token_type": "bearer","access_token": "7ac571640bc5625b3d67afe01d245fecb7b70be9","expires_in": 7776000,"refresh_token": "77f45105ee6bdd111ea18cdd1cc479d70ebd3ece"}""">
    type Token = JsonProvider< """{"token_type": "bearer","access_token": "7ac571640bc5625b3d67afe01d245fecb7b70be9","expires_in": 7776000,"refresh_token": "77f45105ee6bdd111ea18cdd1cc479d70ebd3ece", "expires": "2018-10-01T21:12:20.492Z"}""">

    type GetTokenResult =
        | Token of TokenResponse.Root
        | RequireOneTimePassword of MfaResponse.Root

    let private particleTokenRequest (secrets : Secrets.Root) formValues =
        Http.AsyncRequestStream(
            "https://api.particle.io/oauth/token",
            httpMethod = "POST",
            body = FormValues formValues,
            headers = [
                HttpRequestHeaders.BasicAuth secrets.Client.Id secrets.Client.Secret
                HttpRequestHeaders.ContentType HttpContentTypes.FormValues])


    let getToken (secrets : Secrets.Root) =
        async {
            try
                let! response =
                    particleTokenRequest
                        secrets [
                        "grant_type", "password"
                        "username", secrets.Username
                        "password", secrets.Password]
                let tokenResponse = TokenResponse.Load response.ResponseStream
                return Token tokenResponse
            with
            | :? WebException as ex ->
                let responseBodyStream = ex.Response.GetResponseStream()
                let mfaResponse = MfaResponse.Load responseBodyStream
                return RequireOneTimePassword mfaResponse
        }

    let getTokenFromMfaToken (secrets : Secrets.Root) mfaToken otp =
        async {
            let! response =
                particleTokenRequest
                    secrets [
                    "grant_type", "urn:custom:mfa-otp"
                    "username", secrets.Username
                    "password", secrets.Password
                    "mfa_token", mfaToken
                    "otp", otp]
            return TokenResponse.Load response.ResponseStream
        }

    let generateOtp (secret : string) =
        let sanitizedSecret = secret.Replace(" ", "")
        let accountSecret = Base32Encoding.ToBytes sanitizedSecret
        let generator = OtpNet.Totp(accountSecret)
        generator.ComputeTotp()

    let getTokenWithMfa secrets otp =
        async {
            let! getTokenResponse = getToken secrets
            return!
                match getTokenResponse with
                | Token token -> async { return token }
                | RequireOneTimePassword mfaResponse -> getTokenFromMfaToken secrets (string mfaResponse.MfaToken) otp }

    let refreshToken (secrets : Secrets.Root) otpGenerator (token : Token.Root) =
        async {
            try
                let! response =
                    particleTokenRequest
                        secrets [
                        "grant_type", "refresh_token"
                        "refresh_token", string token.RefreshToken]
                let result = TokenResponse.Load response.ResponseStream
                return result
            with error ->
                printfn "Could not refresh token: %s" error.Message
                return! getTokenWithMfa secrets (otpGenerator()) }

    let getTokenWithDefaultMfa =
        let secrets = Secrets.GetSample()
        getTokenWithMfa secrets (generateOtp secrets.ParticleAccountSecret)

    let private savedTokenFile = Path.Combine(Environment.CurrentDirectory, "ParticleToken.json")

    let savedToken = async {
        return!
            if File.Exists savedTokenFile
            then
                async {
                    let! token = Token.AsyncLoad savedTokenFile
                    return Some token
                }

            else async { return None }
        }

    let saveToken (token : Token.Root) =
        async {
            use output = File.OpenWrite savedTokenFile
            use outputWriter = new StreamWriter(output)
            token.JsonValue.WriteTo(outputWriter, JsonSaveOptions.None)
            do! outputWriter.FlushAsync() |> Async.AwaitTask
            Debug.WriteLine(sprintf "Saved %s" savedTokenFile)
        }

    let expiration (token : TokenResponse.Root) = DateTimeOffset(DateTime.UtcNow.AddSeconds(float token.ExpiresIn))
    let applyExpiration (token : TokenResponse.Root) = Token.Root(token.TokenType, token.AccessToken, token.ExpiresIn, token.RefreshToken, expiration token)

    let tokenCache secrets otpGenerator =

        let getNewToken =
            async {
                let! getTokenResponse = getTokenWithMfa secrets (otpGenerator())
                let token = applyExpiration getTokenResponse
                do! saveToken token
                printfn "New token generated. Expires at %A" token.Expires
                return token
            }

        let refreshToken token =
            async {
                let! token = refreshToken secrets otpGenerator token
                return applyExpiration token }

        buildTimedCache savedToken (fun token -> token.Expires.DateTime) refreshToken getNewToken

    let connect =
        let secrets = Secrets.GetSample()
        let getToken = tokenCache secrets (fun () -> generateOtp secrets.ParticleAccountSecret)
        async {
            let! token = getToken DateTime.Now
            return token |> Result.map (fun token -> ParticleCloud(token.AccessToken))
        }