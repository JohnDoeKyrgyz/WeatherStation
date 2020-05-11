#r "paket: 
nuget Fake.Core.Target
nuget Fake.IO.Zip
nuget Fake.DotNet.Cli
nuget Fake.Core.Process
nuget FSharp.Data
"
#load ".fake/build.fsx/intellisense.fsx"
#if !FAKE
  #r "Facades/netstandard"
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
let webPath = Path.getFullName "./WebPortal"
let serverTestsPath = Path.getFullName @"Tests/Server.Tests"
let webServerPath = Path.combine webPath @"src/Server"
let webClientPath = Path.combine webPath @"src/Client"
let webDeployDir = Path.combine webPath "deploy"

[<Literal>]
let BuildSettingsFile = __SOURCE_DIRECTORY__ + @"\build.settings.json"
type DeploymentSettings = JsonProvider< BuildSettingsFile >
let deploymentSettings = lazy DeploymentSettings.Load (Path.combine __SOURCE_DIRECTORY__ "local.build.settings.json")

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
let funcTool = platformTool "func" "func.cmd"

let runTool cmd args workingDir =
    let arguments = args |> String.split ' ' |> Arguments.OfArgs
    Command.RawCommand (cmd, arguments)
    |> CreateProcess.fromCommand
    |> CreateProcess.withWorkingDirectory workingDir
    |> CreateProcess.ensureExitCode
    |> Proc.run
    |> ignore

let runDotNet cmd workingDir =
    let result =
        DotNet.exec (DotNet.Options.withWorkingDirectory workingDir) cmd ""
    if result.ExitCode <> 0 then failwithf "'dotnet %s' failed in %s" cmd workingDir

let openBrowser url =
    //https://github.com/dotnet/corefx/issues/10361
    Command.ShellCommand url
    |> CreateProcess.fromCommand
    |> CreateProcess.ensureExitCodeWithMessage "opening browser failed"
    |> Proc.run
    |> ignore    

Target.create "Clean" (fun _ ->
    Shell.cleanDirs [webDeployDir]
    runDotNet "clean" "."
)

Target.create "InstallClient" (fun _ ->
    printfn "Node version:"
    runTool nodeTool "--version" webPath
    printfn "Yarn version:"
    runTool yarnTool "--version" webPath
    runTool yarnTool "install --frozen-lockfile" webPath
    runDotNet "restore" webClientPath
)

Target.create "Bundle" (fun _ ->
    runDotNet (sprintf "publish \"%s\" -c release -o \"%s\"" webServerPath webDeployDir) webPath
    Shell.copyDir (Path.combine webDeployDir "public") (Path.combine webClientPath "public") FileFilter.allFiles
)

Target.create "Build" (fun _ ->
    runDotNet "build" webServerPath
    runTool yarnTool "webpack-cli -p" webClientPath
)

Target.create "BuildFunctions" (fun _ ->
    let runFunctionsProjectDotnetCommand command = runDotNet (sprintf "%s %s" command functionsSolutionName) functionsPath
    runFunctionsProjectDotnetCommand "clean"
    runFunctionsProjectDotnetCommand "restore"
    runFunctionsProjectDotnetCommand "build"
)

let deploy zipFile deployDir appName appPassword =
    IO.File.Delete zipFile
    Zip.zip deployDir zipFile !!(deployDir + @"\**\**")

    let destinationUri = sprintf "https://%s.scm.azurewebsites.net/api/zipdeploy" appName
    let client = new WebClient(Credentials = NetworkCredential("$" + (appName : string), (appPassword : string)))
    Trace.tracefn "Uploading %s to %s" zipFile destinationUri
    client.UploadData(destinationUri, IO.File.ReadAllBytes zipFile) |> ignore

Target.create "WebAppDeploy" (fun _ ->
    deploy "WebAppDeploy.zip" webDeployDir deploymentSettings.Value.WebApp.Name deploymentSettings.Value.WebApp.Password
)

Target.create "FunctionsDeploy" (fun _ ->
    let deployDir = Path.combine functionsPath @"bin\Debug\netcoreapp3.1"
    runTool funcTool "azure functionapp publish WeatherStationFunctions" deployDir
)    

Target.create "Run" (fun _ ->
    let server = async { runDotNet "watch run" webServerPath }    
    let serverTests = async { runDotNet "watch run" serverTestsPath }
    let client = async {
        runTool yarnTool "webpack-dev-server" webClientPath
    }

    let browser = async {
        do! Async.Sleep 5000
        openBrowser "http://localhost:8080"
    }

    let vsCodeSession = Environment.hasEnvironVar "vsCodeSession"
    let safeClientOnly = Environment.hasEnvironVar "safeClientOnly"

    let tasks =
        [ 
            yield serverTests
            if not safeClientOnly then yield server
            yield client
            if not vsCodeSession then yield browser 
        ]

    tasks
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
    ==> "Run"

Target.runOrDefault "Build"