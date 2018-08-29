namespace WeatherStation
module SystemSettings =

    open System

    open Cache

    let private settingsCache = new Cache<string, string>()

    let private getOrCreateSetting key defaultValue =
        let buildSetting = async {
            let! settingsRepository = AzureStorage.settingsRepository
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

    let activeThreshold =
        async {
            let! settingValue = getOrCreateSetting "ActiveThreshold" (string (TimeSpan.FromHours(1.0)))
            return TimeSpan.Parse settingValue
        }