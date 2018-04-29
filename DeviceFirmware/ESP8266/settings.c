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