#include "settings.h"
#include "iot_configs.h"

SETTINGS_HANDLE getDefaults()
{
    IotHubSettings iotSettings;
    iotSettings.DeviceId = IOT_CONFIG_DEVICE_ID;
    iotSettings.ConnectionString = IOT_CONFIG_CONNECTION_STRING;

    WifiSettings wifiSettings;
    wifiSettings.SSID = IOT_CONFIG_WIFI_SSID;
    wifiSettings.Password = IOT_CONFIG_WIFI_PASSWORD; 

    Settings *settings = malloc(sizeof(Settings));
    settings->SleepInterval = 10e6;
    settings->IotHub = iotSettings;
    settings->Wifi = wifiSettings;

    return settings;
}

SETTINGS_HANDLE getSettings()
{
    return getDefaults();
}

SETTINGS_HANDLE deserialize(JSON_Value *json)
{
    JSON_Object *settingsJson = json_value_get_object(json);

    IotHubSettings iotSettings;
    iotSettings.DeviceId = json_object_dotget_string(settingsJson, "IotHub.DeviceId");
    iotSettings.ConnectionString = json_object_dotget_string(settingsJson, "IoTHub.ConnectionString");

    WifiSettings wifiSettings;
    wifiSettings.SSID = json_object_dotget_string(settingsJson, "Wifi.SSID");
    wifiSettings.Password = json_object_dotget_string(settingsJson, "Wifi.Password"); 

    Settings *settings = malloc(sizeof(Settings));
    settings->SleepInterval = json_object_get_number(settingsJson, "SleepInterval");;
    settings->FirmwareVersion = json_object_get_string(settingsJson, "FirmwareVersion");;
    settings->IotHub = iotSettings;
    settings->Wifi = wifiSettings;

    return settings;
}

JSON_Value* serialize(SETTINGS_HANDLE settings)
{
    JSON_Value *root = json_value_init_object();
    JSON_Object *rootObject = json_value_get_object(root);
    json_object_set_string(rootObject, "FirmwareVersion", settings->FirmwareVersion);
    json_object_set_number(rootObject, "SleepInterval", settings->SleepInterval);
    json_object_dotset_string(rootObject, "Wifi.SSID", settings->Wifi.SSID);
    json_object_dotset_string(rootObject, "Wifi.Password", settings->Wifi.Password);
    json_object_dotset_string(rootObject, "IoTHub.ConnectionString", settings->IotHub.ConnectionString);
    json_object_dotset_string(rootObject, "IoTHub.DeviceId", settings->IotHub.DeviceId);
    return root;
}