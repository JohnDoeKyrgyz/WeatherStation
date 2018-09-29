#load "Preamble.fsx"
#r @"otp.net\1.2.0\lib\netstandard1.3\Otp.NET.dll"

open FSharp.Data
open System.Net
open System.Text

type Secrets = JsonProvider< @"..\WebPortal\src\Server\Secrets.json" >
let secrets = Secrets.GetSample()

type MfaResponse = JsonProvider< """{"mfa_token":"b1abb537-7a9a-4bee-ba3a-10faef51a093","error":"mfa_required","error_description":"Multi-factor authentication required"}""">
type TokenResponse = JsonProvider< """{"token_type": "bearer","access_token": "7ac571640bc5625b3d67afe01d245fecb7b70be9","expires_in": 7776000,"refresh_token": "77f45105ee6bdd111ea18cdd1cc479d70ebd3ece"}""" >

let getMfaToken =
    async {
        let! response =
            Http.AsyncRequest(
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
            | Choice2Of2 ex ->
                match ex with
                | :? WebException as ex -> 
                    let responseBodyStream = ex.Response.GetResponseStream()
                    MfaResponse.Load responseBodyStream
                | ex -> raise ex
            | _ -> failwith "Unexpected response"
    }

let generateOtp() =
    let accountSecret = Encoding.UTF32.GetBytes(secrets.ParticleAccountSecret)
    let generator = OtpNet.Totp(accountSecret)
    generator.ComputeTotp()
        

let getTokenFromMfaToken mfaToken otpGenerator =
    async {
        let oneTimePassword = defaultArg otpGenerator (generateOtp())
        let! response =
            Http.AsyncRequestStream(
                "https://api.particle.io/oauth/token",  
                httpMethod = "POST",
                body = FormValues [
                    "grant_type", "urn:custom:mfa-otp"
                    "username", secrets.Username
                    "password", secrets.Password
                    "mfa_token", mfaToken
                    "otp", oneTimePassword],
                headers = [
                    HttpRequestHeaders.BasicAuth secrets.Client.Id secrets.Client.Secret
                    HttpRequestHeaders.ContentType HttpContentTypes.FormValues])

        return TokenResponse.Load response.ResponseStream
    }

let getToken otp =
    async {
        let! mfaToken = getMfaToken
        let! token = getTokenFromMfaToken (string mfaToken.MfaToken) otp
        return token
    }

getToken None |> Async.Catch |> Async.RunSynchronously |> printfn "DYNAMIC OTP: %A"

getToken (Some "740686") |> Async.Catch |> Async.RunSynchronously |> printfn "STATIC OTP: %A"

