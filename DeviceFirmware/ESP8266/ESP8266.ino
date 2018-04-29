// Please use an Arduino IDE 1.6.8 or greater

// You must set the device id, device key, IoT Hub name and IotHub suffix in
// iot_configs.h
#include "iot_configs.h"

#include <AzureIoTHub.h>
#if defined(IOT_CONFIG_MQTT)
#include <AzureIoTProtocol_MQTT.h>
#elif defined(IOT_CONFIG_HTTP)
#include <AzureIoTProtocol_HTTP.h>
#endif

#include "sample.h"
#include "esp8266/sample_init.h"

#include "device_twin.h"

#include "DHT.h"
#define DHTPIN 2
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

static char ssid[] = IOT_CONFIG_WIFI_SSID;
static char pass[] = IOT_CONFIG_WIFI_PASSWORD;

IOTHUB_CLIENT_LL_HANDLE azureIot;
Anemometer *anemometer;

void setup()
{
    sample_init(ssid, pass);

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

        //ESP.deepSleep(10e6);
    }    
}