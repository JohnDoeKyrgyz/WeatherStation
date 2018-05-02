#ifndef SETTINGS_H
#define SETTINGS_H

    #include <ArduinoJson.h>

    typedef struct WifiSettingsTag {
        const char* SSID;
        const char* Password;
    } WifiSettings;

    typedef struct IotHubSettingsTag {
        const char* DeviceId;
        const char* ConnectionString;
    } IotHubSettings;

    typedef struct SettingsTag {
        long SleepInterval;
        IotHubSettings IotHub;
        WifiSettings Wifi;
        const char* FirmwareVersion;
    } Settings;

    typedef struct SettingsTag* SETTINGS_HANDLE;

    SETTINGS_HANDLE getDefaults();
    SETTINGS_HANDLE getSettings();

    JsonObject& serialize(SETTINGS_HANDLE settings);
    SETTINGS_HANDLE deserialize(JsonObject& json);
    void print(SETTINGS_HANDLE settings);
    
#endif