namespace WeatherStation
module SystemSettings =

    open System

    open Cache
    open Model

    let private settingsCache = new Cache<string, string>()

    let private getOrCreateSetting settingsGetter key defaultValue =
        let buildSetting = async {
            let! settingResult =
                settingsGetter key defaultValue
                |> Async.Catch
            return
                match settingResult with
                | Choice1Of2 (value : SystemSetting) -> value.Value
                | Choice2Of2 exn ->
                    printfn "Setting %s: Error %s" key exn.Message
                    defaultValue
        }
        settingsCache.GetOrCreate(key, buildSetting)

    let private objectSetting settingsGetter parser key defaultValue = 
        let serializedDefaultValue = defaultValue.ToString()
        async {
            let! setting = getOrCreateSetting settingsGetter key serializedDefaultValue
            return parser setting }

    let readingsCount settingsGetter = objectSetting settingsGetter int "ReadingsCount" 100
    let activeThreshold settingsGetter = objectSetting settingsGetter TimeSpan.Parse "ActiveThreshold" (TimeSpan.FromHours(1.0))    
    let averageReadingsWindow settingsGetter = objectSetting settingsGetter TimeSpan.Parse "ReadingAveragingWindow" (TimeSpan.FromMinutes(10.0))