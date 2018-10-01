namespace WeatherStation.Tests.Functions

module Main =

    open Expecto
    open System

    [<EntryPoint>]
    let main argv =
        let result = Tests.runTestsInAssembly defaultConfig argv

        if result = 0 then Console.Beep(440, 500) else Console.Beep(700, 500)
        
        result