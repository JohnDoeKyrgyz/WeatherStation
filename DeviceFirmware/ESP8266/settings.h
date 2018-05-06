#ifndef SETTINGS_H
#define SETTINGS_H

    #include <Arduino.h>
    #include <ArduinoJson.h>
    #include "FS.h"

    #ifdef ARDUINO_ARCH_ESP32
        #include "SPIFFS.h"
    #endif

    #include "iot_configs.h"

    #define SETTINGS_FILE "/settings.json"
    #define FILE_READ "r"
    #define FILE_WRITE "w"

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
    SETTINGS_HANDLE deserialize(JsonObject& json);
    bool updateSettings(SETTINGS_HANDLE currentSettings, SETTINGS_HANDLE newSettings);
    void print(SETTINGS_HANDLE settings);

    JsonObject& serialize(JsonBuffer& jsonBuffer, SETTINGS_HANDLE settings);
    
#endif