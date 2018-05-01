#include "settings.h"
#include "iot_configs.h"

SETTINGS_HANDLE getDefaults()
{
    Settings *settings = malloc(sizeof(Settings));

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

SETTINGS_HANDLE deserialize(JSON_Value *json)
{
    Settings *settings = getDefaults();

    print(settings);
        
    JSON_Object *settingsJson = json_value_get_object(json);

    settings->IotHub.DeviceId = json_object_dotget_string(settingsJson, "IotHub.DeviceId");
    printf("READ: 1\r\n");
    print(settings);

    settings->IotHub.ConnectionString = json_object_dotget_string(settingsJson, "IoTHub.ConnectionString");
    printf("READ: 2\r\n");
    print(settings);

    settings->Wifi.SSID = json_object_dotget_string(settingsJson, "Wifi.SSID");
    printf("READ: 3\r\n");
    print(settings);

    settings->Wifi.Password = json_object_dotget_string(settingsJson, "Wifi.Password");
    printf("READ: 4\r\n");
    print(settings);

    settings->SleepInterval = json_object_get_number(settingsJson, "SleepInterval");
    printf("READ: 5\r\n");
    print(settings);

    settings->FirmwareVersion = json_object_get_string(settingsJson, "FirmwareVersion");
    printf("READ: 6\r\n");
    print(settings);

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