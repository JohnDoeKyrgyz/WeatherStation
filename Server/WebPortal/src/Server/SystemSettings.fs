namespace WeatherStation
module SystemSettings =

    open System

    open AzureStorage
    open Cache

    let private settingsCache = new Cache<string, string>()

    let private getOrCreateSetting key defaultValue = settingsCache.GetOrCreate(key, async { return defaultValue })

    let activeThreshold =
        async {
            let! settingValue = getOrCreateSetting "ActiveThreshold" (string (TimeSpan.FromHours(1.0)))
            return TimeSpan.Parse settingValue
        }