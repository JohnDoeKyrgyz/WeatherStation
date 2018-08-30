namespace WeatherStation
module SystemSettings =

    open System

    open Cache
    open Repository

    let private settingsCache = new Cache<string, string>()

    let private getOrCreateSetting (settingsRepository : ISystemSettingsRepository) key defaultValue =
        let buildSetting = async {
            let! settingResult =
                settingsRepository.GetSetting(key, defaultValue)
                |> Async.Catch
            return
                match settingResult with
                | Choice1Of2 value -> value.Value
                | Choice2Of2 exn ->
                    printfn "Setting %s: Error %s" key exn.Message
                    defaultValue
        }
        settingsCache.GetOrCreate(key, buildSetting)

    let private objectSetting settingsRepository key defaultValue =
        async {
            let! settingValue = getOrCreateSetting settingsRepository key (string (defaultValue))
            return TimeSpan.Parse settingValue
        }

    let activeThreshold settingsRepository = objectSetting settingsRepository "ActiveThreshold" (TimeSpan.FromHours(1.0))
    let averageReadingsWindow settingsRepository = objectSetting settingsRepository "ReadingAveragingWindow" (TimeSpan.FromMinutes(10.0))