
#include <Arduino.h>
#include <time.h>

#ifdef ARDUINO_ARCH_ESP8266  
#include <ESP8266WiFi.h>
#endif

#include <WiFiClientSecure.h>
#include <WiFiUdp.h>

#define FIRMWARE_VERSION "1.0"

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
    settingsJson.printTo(Serial);
    Serial.println();

    SETTINGS_HANDLE newSettings = deserialize(settingsJson);

    if(newSettings->SleepInterval != settings->SleepInterval)
    {
        settings->SleepInterval = newSettings->SleepInterval;
        printf("Updating SleepInterval to %d\r\n", newSettings->SleepInterval);
    }
    free(newSettings);
}

void initSerial() {
    // Start serial and initialize stdout
    Serial.begin(115200);
    Serial.setDebugOutput(true);
}

void initWifi(const char* ssid, const char* pass) {
    // Attempt to connect to Wifi network:
    Serial.print("\r\n\r\nAttempting to connect to SSID: ");
    Serial.println(ssid);
    
    // Connect to WPA/WPA2 network. Change this line if using open or WEP network:
    WiFi.begin(ssid, pass);
    while (WiFi.status() != WL_CONNECTED) {
      delay(500);
      Serial.print(".");
    }
    
    Serial.println("\r\nConnected to wifi");
}

// Times before 2010 (1970 + 40 years) are invalid
#define MIN_EPOCH 40 * 365 * 24 * 3600
void initTime() {  
   time_t epochTime;

   configTime(0, 0, "pool.ntp.org", "time.nist.gov");

   while (true) {
       epochTime = time(NULL);

       if (epochTime < MIN_EPOCH) {
           Serial.println("Fetching NTP epoch time failed! Waiting 2 seconds to retry.");
           delay(2000);
       } else {
           Serial.print("Fetched NTP epoch time is: ");
           Serial.println(epochTime);
           break;
       }
   }
}

void setup()
{
    initSerial();
    
    settings = getSettings();
    settings->FirmwareVersion = FIRMWARE_VERSION;
    
    printf("\r\n\r\nDeviceId %s\r\n", settings->IotHub.DeviceId);
    printf("Firmware Version %s\r\n", FIRMWARE_VERSION);
    printf("Initializing WIFI %s\r\n", settings->Wifi.SSID);

    initWifi(settings->Wifi.SSID, settings->Wifi.Password);
    
    initTime();
    
    azureIot = initializeAzureIot(settings->IotHub.ConnectionString);
    if (azureIot == NULL)
    {
        printf("Could not initialize AzureIot");
    }
    else
    {
        anemometer = initializeAnemometer(azureIot, settings->IotHub.DeviceId);
    }

    dht.begin();
}

void shutdown()
{
    destroyAnemometer(anemometer);
    destroyAzureIoT(azureIot);
}

bool traceOn = false;

bool syncDeviceTwin()
{
    DynamicJsonBuffer jsonBuffer;
    JsonObject& json = serialize(jsonBuffer, settings);
    return beginDeviceTwinSync(azureIot, json, onSettingsUpdate) == IOTHUB_CLIENT_OK;
}

void loop()
{
    if(!syncDeviceTwin())
    {
        printf("Cannot sync device twin.\r\n");
        //TODO: Blink LED in Error.
    }
    if(IoTHubClient_LL_SetOption(azureIot, OPTION_LOG_TRACE, &traceOn) != IOTHUB_CLIENT_OK)
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

        printf("Sending update: Windspeed = %d, DhtTemperature = %f, DhtHumidity = %f\r\n", anemometer->WindSpeed, anemometer->DhtTemperature, anemometer->DhtHumidity);
        sendUpdate(azureIot, anemometer);

        while(!deviceTwinUpdateComplete() || getSendState() == SENDING)
        {
            printf("Free heap size: %u\r\n", ESP.getFreeHeap());   
            doWork(azureIot);
        }        

        float seconds = (float)settings->SleepInterval * 1e-6;
        printf("Sleep for %f seconds...\r\n", seconds);

        shutdown();
        
        ESP.deepSleep(settings->SleepInterval);
    }    
}