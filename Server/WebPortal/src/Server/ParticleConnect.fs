namespace WeatherStation
module ParticleConnect =

    open System
    open System.IO
    open System.Net
    open System.Diagnostics

    open FSharp.Data
    
    open OtpNet
    open Particle.SDK    

    type Secrets = JsonProvider< @"Secrets.json" >

    type MfaResponse = JsonProvider< """{"mfa_token":"b1abb537-7a9a-4bee-ba3a-10faef51a093","error":"mfa_required","error_description":"Multi-factor authentication required"}""">
    type TokenResponse = JsonProvider< """{"token_type": "bearer","access_token": "7ac571640bc5625b3d67afe01d245fecb7b70be9","expires_in": 7776000,"refresh_token": "77f45105ee6bdd111ea18cdd1cc479d70ebd3ece"}""">
    type Token = JsonProvider< """{"token_type": "bearer","access_token": "7ac571640bc5625b3d67afe01d245fecb7b70be9","expires_in": 7776000,"refresh_token": "77f45105ee6bdd111ea18cdd1cc479d70ebd3ece", "expires": "2018-10-01T21:12:20.492Z"}""">

    type GetTokenResult = 
        | Token of TokenResponse.Root
        | RequireOneTimePassword of MfaResponse.Root

    let getToken (secrets : Secrets.Root) =
        async {
            let! response =
                Http.AsyncRequestStream(
                    "https://api.particle.io/oauth/token",  
                    httpMethod = "POST",
                    body = FormValues [
                        "grant_type", "password"
                        "username", secrets.Username
                        "password", secrets.Password],
                    headers = [
                        HttpRequestHeaders.BasicAuth secrets.Client.Id secrets.Client.Secret
                        HttpRequestHeaders.ContentType HttpContentTypes.FormValues])
                |> Async.Catch            

            return
                match response with
                | Choice1Of2 response ->
                    let tokenResponse = TokenResponse.Load response.ResponseStream
                    Token tokenResponse
                | Choice2Of2 ex ->
                    match ex with
                    | :? WebException as ex -> 
                        let responseBodyStream = ex.Response.GetResponseStream()
                        let mfaResponse = MfaResponse.Load responseBodyStream
                        RequireOneTimePassword mfaResponse
                    | ex -> raise ex
        }

    let getTokenFromMfaToken (secrets : Secrets.Root) mfaToken otp =
        async {
            let! response =
                Http.AsyncRequestStream(
                    "https://api.particle.io/oauth/token",  
                    httpMethod = "POST",
                    body = FormValues [
                        "grant_type", "urn:custom:mfa-otp"
                        "username", secrets.Username
                        "password", secrets.Password
                        "mfa_token", mfaToken
                        "otp", otp],
                    headers = [
                        HttpRequestHeaders.BasicAuth secrets.Client.Id secrets.Client.Secret
                        HttpRequestHeaders.ContentType HttpContentTypes.FormValues])

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

    let getTokenWithDefaultMfa =        
        let secrets = Secrets.GetSample()
        getTokenWithMfa secrets (generateOtp secrets.ParticleAccountSecret)

    let private savedTokenFile = "ParticleToken.json"

    let savedToken =
        if File.Exists savedTokenFile
        then Some (TokenResponse.AsyncLoad savedTokenFile)
        else None

    let saveToken (token : Token.Root) =
        async {
            use output = File.OpenWrite savedTokenFile
            use outputWriter = new StreamWriter(output)
            token.JsonValue.WriteTo(outputWriter, JsonSaveOptions.None)
            do! outputWriter.FlushAsync() |> Async.AwaitTask
        }

    let expiration (token : TokenResponse.Root) = DateTime.Now.AddSeconds(float token.ExpiresIn)
    let applyExpiration (token : TokenResponse.Root) = new Token.Root(token.TokenType, token.AccessToken, token.ExpiresIn, token.RefreshToken, expiration token)

    let private tokenCache secrets otpGenerator =

        let getNewToken =
            async {
                let! getTokenResponse = getTokenWithMfa secrets (otpGenerator())
                let token = applyExpiration getTokenResponse
                do! saveToken token
                Debug.WriteLine(sprintf "New token generated. Expires at %A" token.Expires)                
                return token
            }

        let rec cache token (mailBox : MailboxProcessor<AsyncReplyChannel<string>>) =
            async {
                let! responseChannel = mailBox.Receive()
                let! token =
                    match token with
                    | Some (token : Token.Root) ->                        
                        if token.Expires > DateTime.Now
                        then async { return token }
                        else
                            Debug.WriteLine(sprintf "Token expired on %A" expiration)
                            getNewToken
                    | None -> getNewToken                        

                responseChannel.Reply token.AccessToken

                do! cache (Some token) mailBox}

        let mailBox = MailboxProcessor.Start (cache None)

        fun () -> mailBox.PostAndAsyncReply id

    let connect =
        let secrets = Secrets.GetSample()
        let token = tokenCache secrets (fun () -> generateOtp secrets.ParticleAccountSecret)
        async {
            let! token = getTokenWithDefaultMfa
            return new ParticleCloud(token.AccessToken)
        }