#include "settings.h"
#include "iot_configs.h"

SETTINGS_HANDLE getDefaults()
{
    Settings *settings = (Settings*)malloc(sizeof(Settings));

    settings->IotHub.DeviceId = IOT_CONFIG_DEVICE_ID;
    settings->IotHub.ConnectionString = IOT_CONFIG_CONNECTION_STRING;
    settings->Wifi.SSID = IOT_CONFIG_WIFI_SSID;
    settings->Wifi.Password = IOT_CONFIG_WIFI_PASSWORD;
    settings->SleepInterval = 10e6;
    settings->FirmwareVersion = NULL;

    return settings;
}

SETTINGS_HANDLE getSettings()
{
    return getDefaults();
}

SETTINGS_HANDLE deserialize(JsonObject& json)
{
    Settings *settings = getDefaults();

    print(settings);
        
    JsonObject& iotHub = json["IotHub"];
    settings->IotHub.DeviceId = iotHub["DeviceId"];
    settings->IotHub.ConnectionString = iotHub["ConnectionString"];

    JsonObject& wifi = json["Wifi"];
    settings->Wifi.SSID = wifi["SSID"];
    settings->Wifi.Password = wifi["Password"];

    settings->SleepInterval = json["SleepInterval"];
    settings->FirmwareVersion = json["FirmwareVersion"];

    return settings;
}

static const char* nullSafe(const char* string)
{
    return string == NULL ? "<null>" : string;    
}

void print(SETTINGS_HANDLE settings)
{
    printf("SleepInterval: %d\r\n", settings->SleepInterval);
    printf("FirmwareVersion: %s\r\n", nullSafe(settings->FirmwareVersion));
    printf("Wifi.SSID: %s\r\n", nullSafe(settings->Wifi.SSID));
    printf("Wifi.Password: %s\r\n", nullSafe(settings->Wifi.Password));
    printf("IoTHub.DeviceId: %s\r\n", nullSafe(settings->IotHub.DeviceId));
    printf("IoTHub.ConnectionString: %s\r\n", nullSafe(settings->IotHub.ConnectionString));
}

const size_t bufferSize = JSON_OBJECT_SIZE(2) + 2 * JSON_OBJECT_SIZE(2);

SerializeSettingsResult<bufferSize> serialize(SETTINGS_HANDLE settings)
{
    StaticJsonBuffer<bufferSize> jsonBuffer;

    JsonObject& root = jsonBuffer.createObject();
    root["FirmwareVersion"] = settings->FirmwareVersion;
    root["SleepInterval"] = settings->SleepInterval;

    JsonObject& wifi = root.createNestedObject("Wifi");
    wifi["SSID"] = settings->Wifi.SSID;
    wifi["Password"] = settings->Wifi.Password;

    JsonObject& iotHub = root.createNestedObject("IotHub");
    iotHub["IoTHub.ConnectionString"] = settings->IotHub.ConnectionString;
    iotHub["IoTHub.DeviceId"] = settings->IotHub.DeviceId;

    SerializeSettingsResult result;
    result.json = root;
    result.buffer = &jsonBuffer;
    return result;
}