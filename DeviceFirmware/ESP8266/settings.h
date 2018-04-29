#ifndef SETTINGS_H
#define SETTINGS_H


#ifdef __cplusplus
extern "C" {
#endif
    #include <Arduino.h>
    #include "sdk\parson.h"

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
        char* FirmwareVersion;
    } Settings;

    typedef struct SettingsTag* SETTINGS_HANDLE;

    SETTINGS_HANDLE getDefaults();
    SETTINGS_HANDLE getSettings();

    JSON_Value* serialize(SETTINGS_HANDLE settings);
    SETTINGS_HANDLE deserialize(JSON_Value *json);

#ifdef __cplusplus
}
#endif
#endif