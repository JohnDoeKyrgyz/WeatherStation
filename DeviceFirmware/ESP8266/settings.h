#ifndef SETTINGS_H
#define SETTINGS_H


#ifdef __cplusplus
extern "C" {
#endif
    #include <Arduino.h>

    typedef struct WifiSettingsTag {
        char* SSID;
        char* Password;
    } WifiSettings;

    typedef struct IotHubSettingsTag {
        char* DeviceId;
        char* ConnectionString;
    } IotHubSettings;

    typedef struct SettingsTag {
        uint64_t SleepInterval;
        IotHubSettings IotHub;
        WifiSettings Wifi;
    } Settings;

    typedef struct SettingsTag* SETTINGS_HANDLE;

    SETTINGS_HANDLE getDefaults();
    SETTINGS_HANDLE getSettings();

#ifdef __cplusplus
}
#endif
#endif