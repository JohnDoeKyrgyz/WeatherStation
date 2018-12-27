#r "paket: groupref build //"
#load ".fake/build.fsx/intellisense.fsx"

#if !FAKE
#r "netstandard"
#r "Facades/netstandard" // https://github.com/ionide/ionide-vscode-fsharp/issues/839#issuecomment-396296095
#endif

open System
open System.Net

open Fake.Core
open Fake.DotNet
open Fake.IO
open Fake.IO.Globbing.Operators

open FSharp.Data

let functionsPath = Path.getFullName "ServerFunctions"
let functionsSolutionName = "ServerFunctions.sln"
let webPath = Path.getFullName "WebPortal"
let serverTestsPath = Path.getFullName @"Tests\Server.Tests"
let webServerPath = Path.combine webPath @"src\Server"
let webClientPath = Path.combine webPath @"src\Client"
let webDeployDir = Path.combine webPath "deploy"

[<Literal>]
let BuildSettingsFile = __SOURCE_DIRECTORY__ + @"\build.settings.json"
type DeploymentSettings = JsonProvider< BuildSettingsFile >
let deploymentSettings = DeploymentSettings.Load (Path.combine __SOURCE_DIRECTORY__ "local.build.settings.json")

let platformTool tool winTool =
    let tool = if Environment.isUnix then tool else winTool
    match ProcessUtils.tryFindFileOnPath tool with
    | Some t -> t
    | _ ->
        let errorMsg =
            tool + " was not found in path. " +
            "Please install it and make sure it's available from your path. " +
            "See https://safe-stack.github.io/docs/quickstart/#install-pre-requisites for more info"
        failwith errorMsg

let nodeTool = platformTool "node" "node.exe"
let yarnTool = platformTool "yarn" "yarn.cmd"

let runTool cmd args workingDir =
    let result =
        Process.execSimple (fun info ->
            { info with
                FileName = cmd
                WorkingDirectory = workingDir
                Arguments = args })
            TimeSpan.MaxValue
    if result <> 0 then failwithf "'%s %s' failed" cmd args

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    let result =
        //https://github.com/dotnet/corefx/issues/10361
        Process.execSimple (fun info ->
            { info with
                FileName = url
                UseShellExecute = true })
            TimeSpan.MaxValue
    if result <> 0 then failwithf "opening browser failed"

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [webDeployDir]
    runDotNet "clean" "."
)

Target.create "InstallClient" (fun _ ->
    printfn "%s" webPath
    printfn "%s" webClientPath
    printfn "Node version:"
    runTool nodeTool "--version" webPath
    printfn "Yarn version:"
    runTool yarnTool "--version" webPath
    runTool yarnTool "install --frozen-lockfile" webPath
    runDotNet "restore" webClientPath
)

Target.create "RestoreServer" (fun _ ->
    runDotNet "restore" webServerPath
)

Target.create "Bundle" (fun _ ->
    runDotNet (sprintf "publish \"%s\" -c release -o \"%s\"" webServerPath webDeployDir) webPath
    Shell.copyDir (Path.combine webDeployDir "public") (Path.combine webClientPath "public") FileFilter.allFiles
)

Target.create "Build" (fun _ ->
    runDotNet "build" webServerPath
    runDotNet "fable webpack-cli -- --config src/Client/webpack.config.js -p" webClientPath
)

Target.create "BuildFunctions" (fun _ ->
    runDotNet (sprintf "build %s" functionsSolutionName) functionsPath
)

let deploy zipFile deployDir appName appPassword =
    IO.File.Delete zipFile
    Zip.zip deployDir zipFile !!(deployDir + @"\**\**")

    let destinationUri = sprintf "https://%s.scm.azurewebsites.net/api/zipdeploy" appName
    let client = new WebClient(Credentials = NetworkCredential("$" + (appName : string), (appPassword : string)))
    Trace.tracefn "Uploading %s to %s" zipFile destinationUri
    client.UploadData(destinationUri, IO.File.ReadAllBytes zipFile) |> ignore

Target.create "WebAppDeploy" (fun _ ->
    deploy "WebAppDeploy.zip" webDeployDir deploymentSettings.WebApp.Name deploymentSettings.WebApp.Password
)

Target.create "FunctionsDeploy" (fun _ ->
    let deployDir = Path.combine functionsPath @"bin\Debug\netstandard2.0"
    printfn "DeployDir %s" deployDir
    deploy "FunctionsDeploy.zip" deployDir deploymentSettings.FunctionApp.Name deploymentSettings.FunctionApp.Password
)
    

Target.create "Run" (fun _ ->
    let server = async { runDotNet "watch run" webServerPath }    
    let serverTests = async { runDotNet "watch run" serverTestsPath }
    let client = async {
        runDotNet "fable webpack-dev-server -- --config src/Client/webpack.config.js" webClientPath
    }
    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    [ serverTests; server; client; browser ]
    |> Async.Parallel
    |> Async.RunSynchronously
    |> ignore
)


open Fake.Core.TargetOperators

"Clean"
    ==> "InstallClient"
    ==> "Build"
    ==> "Bundle"
    ==> "WebAppDeploy"

"Clean"
    ==> "BuildFunctions"
    ==> "FunctionsDeploy"

"Clean"
    ==> "InstallClient"
    ==> "RestoreServer"
    ==> "Run"

Target.runOrDefault "Build"