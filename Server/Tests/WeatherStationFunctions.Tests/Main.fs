namespace WeatherStation.Tests.Functions

module Main =

    open Expecto

    [<EntryPoint>]
    let main argv =
        DataSetup.initialize()
        let result = Tests.runTestsInAssembly defaultConfig argv
        result