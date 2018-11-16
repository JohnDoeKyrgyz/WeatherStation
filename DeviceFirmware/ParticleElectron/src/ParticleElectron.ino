#include <ArduinoJson.h>
#include <Arduino.h>
#include "settings.h"

#include <Adafruit_DHT.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>

#include <stdio.h>
#include <stdarg.h>

#define FIRMWARE_VERSION "1.1"

SYSTEM_MODE(SEMI_AUTOMATIC);

struct Reading
{
    int version;
    float batteryVoltage;
    int panelVoltage;
    bool anemometerRead;
    float windSpeed;
    int windDirection;
    bool dhtRead;
    float dhtTemperature;
    float dhtHumidity;
    bool bmeRead;
    float bmeTemperature;
    float pressure;
    float bmeHumidity;
};

/* Connections */
#define ANEMOMETER C1
#define PANEL_VOLTAGE A0
#define LED D7 //Builtin LED

#define DHT_IN D6

//This pin is switched high to drive a transistor that powers sensors
#define SENSOR_POWER C0

/* Sensors */
#define DHTTYPE DHT22 // DHT 22  (AM2302), AM2321
#define DHT_INIT 1000
#define ANEMOMETER_TIME 1000

#define BROWNOUT 3.7

DHT dht(DHT_IN, DHTTYPE);

Adafruit_BME280 bme280;
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);
FuelGauge gauge;

Settings settings;
#define diagnosticMode settings.diagnositicCycles
unsigned long duration;

Reading initialReading;

char messageBuffer[255];

char* serializeToJson(JsonObject& json)
{
    int length = json.measureLength() + 1;
    char* jsonString = (char*)malloc(length);
    jsonString[length] = NULL;
    json.printTo(jsonString, length);
    return jsonString;
}

bool timeout(int timeout, std::function<bool()> opperation)
{
    unsigned long endTime = millis() + timeout;
    bool result = false;
    while(!(result = opperation()) && millis() < endTime)
    {
        Serial.print(".");
        delay(10);
    }
    return result;
}

bool readAnemometer(Reading *reading)
{
    Serial.print("ANEMOMETER ");
    bool result = timeout(ANEMOMETER_TIME, [reading]()
    {
        return laCrosseTX23.read(reading->windSpeed, reading->windDirection);
    });
    Serial.println();
    return result;
}

bool readVoltage(Reading *reading)
{
    reading->batteryVoltage = gauge.getVCell();
    int panelVoltage = analogRead(PANEL_VOLTAGE);
    reading->panelVoltage = panelVoltage;
    return true;
}

bool readDht(Reading *reading)
{
    Serial.print("DHT ");
    bool result = timeout(DHT_INIT, [reading]()
    {
        reading->dhtTemperature = dht.getTempCelcius();
        reading->dhtHumidity = dht.getHumidity();
        return !isnan(reading->dhtTemperature) && !isnan(reading->dhtHumidity);
    });
    Serial.println();
    return result;
}

bool readBme280(Reading *reading)
{
    reading->bmeTemperature = bme280.readTemperature();
    reading->pressure = bme280.readPressure();
    reading->bmeHumidity = bme280.readHumidity();
    return !isnan(reading->bmeTemperature) && !isnan(reading->pressure) && reading->pressure > 0 && !isnan(reading->bmeHumidity);
}

void onError(char* message)
{
    RGB.color(255, 255, 0);
    Serial.println(message);
}

void deviceSetup()
{
    duration = millis();

    //Turn off the status LED to save power
    RGB.control(true);
    RGB.color(0, 0, 0);

    //Load saved settings;
    settings = *loadSettings();

    if(diagnosticMode)
    {
        pinMode(LED, OUTPUT);
        digitalWrite(LED, HIGH);
        settings.diagnositicCycles--;

        saveSettings(&settings);
    }

    //turn on the sensors    
    pinMode(SENSOR_POWER, OUTPUT);    
    digitalWrite(SENSOR_POWER, HIGH);

    dht.begin();
    bme280.begin(0x76);

    //take an initial wind reading
    if(!(initialReading.anemometerRead = readAnemometer(&initialReading)))
    {
        onError("ERROR: Could not get initial wind reading");
    }
}
STARTUP(deviceSetup());

void onSettingsUpdate(const char* event, const char* data)
{
    digitalWrite(LED, HIGH);
    settings = *deserialize(data);
    saveSettings(&settings);

    JsonObject& settingsJson = serialize(&settings);
    char* settingsEcho = serializeToJson(settingsJson);

    Serial.print("SETTINGS UPDATE: ");
    Serial.println(settingsEcho);

    Particle.publish("Settings", settingsEcho, 60, PRIVATE);
    Particle.process();

    digitalWrite(LED, LOW);
}

void checkBrownout()
{
    float voltage;
    if(settings.brownout)
    {
        if (voltage = gauge.getVCell()) < BROWNOUT)
        {
            Serial.print("BROWNOUT ");
            Serial.println(voltage);
            System.sleep(SLEEP_MODE_SOFTPOWEROFF, settings.brownoutMinutes * 60);

            Particle.connect();
            char* buffer = messageBuffer;
            sprintf(buffer, "%f:%d", voltage, settings.brownoutMinutes);
            Particle.publish("Brownout", buffer, 60, PRIVATE);
            Particle.process();
        }
        else 
        {
            Serial.println("NO BROWNOUT");
        }
    }
}

void setup()
{
    Serial.begin(115200);
    
    checkBrownout();

    JsonObject& settingsJson = serialize(&settings);
    Serial.print("SETTINGS: ");
    settingsJson.printTo(Serial);

    Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
    Particle.connect();
}


char* serialize(Reading *reading)
{
    char* buffer = messageBuffer;
    buffer += sprintf(buffer, "%d:%f:%d|", reading->version, reading->batteryVoltage, reading->panelVoltage);
    if(reading->bmeRead)
    {
        buffer += sprintf(buffer, "b%f:%f:%f", reading->bmeTemperature, reading->pressure, reading->bmeHumidity);
    }
    if(reading->dhtRead)
    {
        buffer += sprintf(buffer, "d%f:%f", reading->dhtTemperature, reading->dhtHumidity);
    }
    if(reading->anemometerRead)
    {
        buffer += sprintf(buffer, "a%f:%d", reading->windSpeed, reading->windDirection);
    }
    return messageBuffer;
}

void loop()
{
    Reading reading;

    //read data
    reading.version = settings.version;
    readVoltage(&reading);

    if(!(reading.bmeRead = readBme280(&reading)))
    {
        onError("ERROR: BME280 temp/pressure sensor");
    }
    if(!(reading.dhtRead = readDht(&reading)))
    {
        onError("ERROR: DHT22 temp/humidity sensor");
    }
    if(!(reading.anemometerRead = readAnemometer(&reading)))
    {
        onError("ERROR: Could not read anemometer");
    }

    digitalWrite(SENSOR_POWER, LOW);

    //take the greater of the initial wind reading or the most recent wind reading
    if(!reading.anemometerRead || (reading.anemometerRead && initialReading.anemometerRead && initialReading.windSpeed > reading.windSpeed))
    {
        reading.windSpeed = initialReading.windSpeed;
        reading.windDirection = initialReading.windDirection;
        Serial.print("Using faster wind speed ");
        Serial.print(initialReading.windSpeed);
        Serial.print(", ");
        Serial.println(reading.windSpeed);        
    }

    //send serialized reading to the cloud
    char* publishedReading = serialize(&reading);
    Serial.println(publishedReading);
    Particle.publish("Reading", publishedReading, 60, PRIVATE);

    //Allow particle to process before going into deep sleep
    Particle.process();

    if(diagnosticMode)
    {
        Serial.print("DIAGNOSTIC COUNT ");
        Serial.println(diagnosticMode);
        digitalWrite(LED, LOW);
    }

    void (*sleepAction)();
    const char* sleepMessage;
    if(settings.useDeepSleep)
    {
        sleepMessage = "DEEP";
        sleepAction = []()
        {
            System.sleep(SLEEP_MODE_SOFTPOWEROFF, settings.sleepTime);
        };
    }
    else
    {
        sleepMessage = "LIGHT";
        sleepAction = []()
        {
            delay(settings.sleepTime * 1000);
            deviceSetup();
        };
    }

    Serial.print(sleepMessage);
    Serial.print(" SLEEP ");
    Serial.println(settings.sleepTime);

    Serial.print("DURATION ");
    duration = millis() - duration;
    Serial.println(duration);
    sleepAction();
}
