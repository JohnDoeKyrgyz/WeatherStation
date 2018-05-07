#include "settings.h"
SETTINGS_HANDLE getDefaults()
{
    auto settings = (SETTINGS_HANDLE)malloc(sizeof(Settings));

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
    SETTINGS_HANDLE result = NULL;    
    File configFile = SPIFFS.open(SETTINGS_FILE, FILE_READ);
    if (configFile)
    {
        DynamicJsonBuffer buffer;
        JsonObject& json = buffer.parseObject(configFile);

        Serial.println("Stored settings...");
        json.printTo(Serial);
        Serial.println();
        
        result = deserialize(json);
    }   
    if(result == NULL || result->Wifi.SSID == NULL || result->Wifi.Password == NULL)
    {
        SETTINGS_HANDLE defaultSettings = getDefaults();
        bool saveDefaults = updateSettings(result, defaultSettings);

        Serial.print("Default settings [");
        Serial.print(saveDefaults);
        Serial.println("]...");
        
        free(result);
        result = defaultSettings;        
    }    

    result->FirmwareVersion = FIRMWARE_VERSION;
    return result;
}

SETTINGS_HANDLE deserialize(JsonObject& json)
{
    SETTINGS_HANDLE settings = getDefaults();
        
    JsonObject& iotHub = json["IoTHub"];
    settings->IotHub.DeviceId = iotHub["DeviceId"];
    settings->IotHub.ConnectionString = iotHub["ConnectionString"];

    JsonObject& wifi = json["Wifi"];
    settings->Wifi.SSID = wifi["SSID"];
    settings->Wifi.Password = wifi["Password"];

    settings->SleepInterval = json["SleepInterval"];
    settings->FirmwareVersion = json["FirmwareVersion"];

    return settings;
}

const char* coalesce(const char* a, const char* b)
{
    return a == NULL ? b : a;
}

bool updateSettings(SETTINGS_HANDLE currentSettings, SETTINGS_HANDLE newSettings)
{    
    bool result = true;
    bool different =
        currentSettings->SleepInterval != newSettings->SleepInterval
        || !strcmp(currentSettings->FirmwareVersion, newSettings->FirmwareVersion)
        || !strcmp(currentSettings->IotHub.DeviceId, newSettings->IotHub.DeviceId)
        || !strcmp(currentSettings->IotHub.ConnectionString, newSettings->IotHub.ConnectionString)
        || !strcmp(currentSettings->Wifi.SSID, newSettings->Wifi.SSID)
        || !strcmp(currentSettings->Wifi.Password, newSettings->Wifi.Password);

    if(different)
    {
        currentSettings->SleepInterval = newSettings->SleepInterval;
        currentSettings->FirmwareVersion = coalesce(newSettings->FirmwareVersion, currentSettings->FirmwareVersion);
        currentSettings->IotHub.DeviceId = coalesce(newSettings->IotHub.DeviceId, currentSettings->IotHub.DeviceId);
        currentSettings->IotHub.ConnectionString = coalesce(newSettings->IotHub.ConnectionString, currentSettings->IotHub.ConnectionString);
        currentSettings->Wifi.SSID = coalesce(newSettings->Wifi.SSID, currentSettings->Wifi.SSID);
        currentSettings->Wifi.Password = coalesce(newSettings->Wifi.Password, currentSettings->Wifi.Password);

        DynamicJsonBuffer jsonBuffer;
        JsonObject& json = serialize(jsonBuffer, newSettings);
        File configFile = SPIFFS.open(SETTINGS_FILE, FILE_WRITE);        
        bool result = configFile;
        if(result)
        {
            json.printTo(configFile);

            Serial.println("Updated config to...");
            json.printTo(Serial);
            Serial.println();            
        }
    }
    return result;
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

JsonObject& serialize(JsonBuffer& jsonBuffer, SETTINGS_HANDLE settings)
{
    JsonObject& root = jsonBuffer.createObject();
    root["FirmwareVersion"] = settings->FirmwareVersion;
    root["SleepInterval"] = settings->SleepInterval;

    JsonObject& wifi = root.createNestedObject("Wifi");
    wifi["SSID"] = settings->Wifi.SSID;
    wifi["Password"] = settings->Wifi.Password;

    JsonObject& iotHub = root.createNestedObject("IoTHub");
    iotHub["ConnectionString"] = settings->IotHub.ConnectionString;
    iotHub["DeviceId"] = settings->IotHub.DeviceId;

    return root;
}
