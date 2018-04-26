// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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


#include "DHT.h"
#define DHTPIN 2
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

static char ssid[] = IOT_CONFIG_WIFI_SSID;
static char pass[] = IOT_CONFIG_WIFI_PASSWORD;

IOTHUB_CLIENT_LL_HANDLE azureIot;
Anemometer *anemometer;

void onSettingsChanged(void* argument)
{
    // Note: The argument is NOT a pointer to desired_maxSpeed, but instead a pointer to the MODEL 
    //       that contains desired_maxSpeed as one of its arguments.  In this case, it
    //       is CarSettings*.

    Settings* car = argument;
    printf("received a new desired_maxSpeed = %" PRIu8 "\n", car->interval);
}

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

void loop()
{
    srand((unsigned int)time(NULL));
    int avgWindSpeed = 10;
    float minTemperature = 20.0;
    float minHumidity = 60.0;

    anemometer->WindSpeed = avgWindSpeed + (rand() % 4 + 2);
    anemometer->DhtTemperature = dht.readTemperature();;
    anemometer->DhtHumidity = dht.readHumidity();

    sendUpdate(azureIot, anemometer);

    doWork(azureIot);

    delay(10000);
}
