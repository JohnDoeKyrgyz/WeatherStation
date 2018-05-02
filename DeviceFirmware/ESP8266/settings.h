#ifndef SETTINGS_H
#define SETTINGS_H

#ifdef __cplusplus
extern "C" {
#endif
    #include <Arduino.h>
    #include "sdk\parson.h"

    typedef struct WifiSettingsTag {
        const char* SSID;
        const char* Password;
    } WifiSettings;

    typedef struct IotHubSettingsTag {
        const char* DeviceId;
        const char* ConnectionString;
    } IotHubSettings;

    typedef struct SettingsTag {
        uint64_t SleepInterval;
        IotHubSettings IotHub;
        WifiSettings Wifi;
        const char* FirmwareVersion;
    } Settings;

    typedef struct SettingsTag* SETTINGS_HANDLE;

    SETTINGS_HANDLE getDefaults();
    SETTINGS_HANDLE getSettings();

    JSON_Value* serialize(SETTINGS_HANDLE settings);
    SETTINGS_HANDLE deserialize(JSON_Value *json);
    void print(SETTINGS_HANDLE settings);

#ifdef __cplusplus
}
#endif
#endif