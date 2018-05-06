#include <Arduino.h>

#include "FS.h"
#include <time.h>

#ifdef ARDUINO_ARCH_ESP32
#include "SPIFFS.h"
#endif

#ifdef ARDUINO_ARCH_ESP8266
#include <ESP8266WiFi.h>
#endif

#include <WiFiClientSecure.h>
#include <WiFiUdp.h>

#include <AzureIoTHub.h>
#if defined(IOT_CONFIG_MQTT)
#include <AzureIoTProtocol_MQTT.h>
#elif defined(IOT_CONFIG_HTTP)
#include <AzureIoTProtocol_HTTP.h>
#endif

#include "azure_iot.h"
#include "device_twin.h"
#include "settings.h"

#include "DHT.h"
#define DHTPIN 2
#define DHTTYPE DHT22
DHT dht(DHTPIN, DHTTYPE);

SETTINGS_HANDLE settings;
IOTHUB_CLIENT_LL_HANDLE azureIot;
Anemometer *anemometer;

void onSettingsUpdate(JsonObject &settingsJson)
{
    Serial.println(F("Desired..."));
    settingsJson.printTo(Serial);
    Serial.println();

    SETTINGS_HANDLE newSettings = deserialize(settingsJson);
    if (!updateSettings(settings, newSettings))
    {
        Serial.println(F("Could not save settings"));
    }
    free(newSettings);
}

void initWifi(const char *ssid, const char *pass)
{
    // Attempt to connect to Wifi network:
    Serial.print(F("\r\n\r\nAttempting to connect to SSID: "));
    Serial.println(ssid);

    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    WiFi.begin(ssid, pass);
    while (WiFi.status() != WL_CONNECTED)
    {
        delay(500);
        Serial.print(F("."));
    }

    Serial.println(F("\r\nConnected to wifi"));
}

// Times before 2010 (1970 + 40 years) are invalid
#define MIN_EPOCH 40 * 365 * 24 * 3600
void initTime()
{
    time_t epochTime;

    configTime(0, 0, "pool.ntp.org", "time.nist.gov");

    while (true)
    {
        epochTime = time(NULL);

        if (epochTime < MIN_EPOCH)
        {
            Serial.println(F("Fetching NTP epoch time failed! Waiting 2 seconds to retry."));
            delay(2000);
        }
        else
        {
            Serial.print(F("Fetched NTP epoch time is: "));
            Serial.println(epochTime);
            break;
        }
    }
}

void setup()
{
    Serial.begin(115200);

    if (SPIFFS.begin())
    {
        settings = getSettings();

        Serial.println();
        Serial.println();
        Serial.print("Firmware Version ");
        Serial.println(FIRMWARE_VERSION);
        Serial.print("DeviceId");
        Serial.println(settings->IotHub.DeviceId);

        Serial.print("Initializing WIFI ");
        Serial.println(settings->Wifi.SSID);

        initWifi(settings->Wifi.SSID, settings->Wifi.Password);

        initTime();

        azureIot = initializeAzureIot(settings->IotHub.ConnectionString);
        if (azureIot == NULL)
        {
            Serial.println(F("Could not initialize AzureIot"));
        }
        else
        {
            anemometer = initializeAnemometer(azureIot, settings->IotHub.DeviceId);
        }

        dht.begin();
    }
    else
    {
        Serial.println(F("Failed to init..."));
    }
}

void shutdown()
{
    destroyAnemometer(anemometer);
    destroyAzureIoT(azureIot);
}

bool syncDeviceTwin()
{
    DynamicJsonBuffer jsonBuffer;
    JsonObject &json = serialize(jsonBuffer, settings);

    Serial.println(F("Reporting..."));
    json.printTo(Serial);
    Serial.println();

    return beginDeviceTwinSync(azureIot, json, onSettingsUpdate) == IOTHUB_CLIENT_OK;
}

void loop()
{
    srand((unsigned int)time(NULL));
    int avgWindSpeed = 10;
    float minTemperature = 20.0;
    float minHumidity = 60.0;

    anemometer->WindSpeed = avgWindSpeed + (rand() % 4 + 2);
    anemometer->DhtTemperature = dht.readTemperature();
    anemometer->DhtHumidity = dht.readHumidity();

    Serial.print("Sending update: Windspeed = ");
    Serial.print(anemometer->WindSpeed);
    Serial.print(", DhtTemperature = ");
    Serial.print(anemometer->DhtTemperature);
    Serial.print(", DhtHumidity = ");
    Serial.println(anemometer->DhtHumidity);

    sendUpdate(azureIot, anemometer);

    if (!syncDeviceTwin())
    {
        Serial.println(F("Cannot sync device twin"));
    }

    while (!deviceTwinUpdateComplete() || getSendState() == SENDING)
    {
        //printf("Free heap size: %u\r\n", ESP.getFreeHeap());
        doWork(azureIot);
    }

    long seconds = settings->SleepInterval * 1e-6;
    Serial.print("Sleep for ");
    Serial.print(seconds);
    Serial.println(" seconds...");

    shutdown();

    ESP.deepSleep(settings->SleepInterval);
}