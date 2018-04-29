#define FIRMWARE_VERSION "1.0"

#include <AzureIoTHub.h>
#if defined(IOT_CONFIG_MQTT)
#include <AzureIoTProtocol_MQTT.h>
#elif defined(IOT_CONFIG_HTTP)
#include <AzureIoTProtocol_HTTP.h>
#endif

#include "sample.h"
#include "esp8266/sample_init.h"

#include "device_twin.h"
#include "settings.h"

SETTINGS_HANDLE settings;

#include "DHT.h"
#define DHTPIN 2
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

IOTHUB_CLIENT_LL_HANDLE azureIot;
Anemometer *anemometer;

void setup()
{
    Serial.begin(115200);
    delay(10);
    
    settings = getSettings();
    
    printf("\r\n\r\nDeviceId %s\r\n", settings->IotHub.DeviceId);
    printf("Firmware Version %s\r\n", FIRMWARE_VERSION);
    printf("Initializing WIFI %s\r\n", settings->Wifi.SSID);

    sample_init(settings->Wifi.SSID, settings->Wifi.Password);

    azureIot = initializeAzureIot();
    if (azureIot == NULL)
    {
        printf("Could not initialize AzureIot");
    }
    else
    {
        anemometer = initializeAnemometer(azureIot);
    }

    dht.begin();
}

void shutdown()
{
    destroyAnemometer(anemometer);
    destroyAzureIoT(azureIot);
}

bool traceOn = false;

void loop()
{
    if(beginDeviceTwinSync(azureIot) != IOTHUB_CLIENT_OK)
    {
        printf("Cannot sync device twin");
        //TODO: Blink LED in Error.
    }
    else if(IoTHubClient_LL_SetOption(azureIot, OPTION_LOG_TRACE, &traceOn) != IOTHUB_CLIENT_OK)
    {
        printf("Could not initialize logging.\r\n");
    }
    else 
    {
        srand((unsigned int)time(NULL));
        int avgWindSpeed = 10;
        float minTemperature = 20.0;
        float minHumidity = 60.0;

        anemometer->WindSpeed = avgWindSpeed + (rand() % 4 + 2);
        anemometer->DhtTemperature = dht.readTemperature();;
        anemometer->DhtHumidity = dht.readHumidity();

        printf("Sending update: Windspeed = %d, DhtTemperature = %d, DhtHumidity = %d\r\n", anemometer->WindSpeed, anemometer->DhtTemperature, anemometer->DhtHumidity);
        sendUpdate(azureIot, anemometer);

        while(!deviceTwinUpdateComplete())
        {
            doWork(azureIot);
        }        
        printf("Sleep...\r\n");

        shutdown();
        
        ESP.deepSleep(settings->SleepInterval);
    }    
}