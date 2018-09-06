namespace WeatherStation.Tests.Functions

module Main =

    open Expecto

    [<EntryPoint>]
    let main argv =
        let result = Tests.runTestsInAssembly defaultConfig argv
        result