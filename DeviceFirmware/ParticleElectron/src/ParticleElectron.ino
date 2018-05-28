#include <ArduinoJson.h>
#include <Arduino.h>
#include "settings.h"

#include <Adafruit_DHT.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BMP280.h>

#define FIRMWARE_VERSION "1.0"

SYSTEM_MODE(SEMI_AUTOMATIC);

struct Reading
{
    int version;
    float batteryVoltage;
    float panelVoltage;
    bool anemometerRead;
    float windSpeed;
    int windDirection;
    bool dhtRead;
    float dhtTemperature;
    float humidity;
    bool bmpRead;
    float bmpTemperature;
    float pressure;
};

/* Connections */
#define BAROMETER_CHIP_SELECT 1
#define ANEMOMETER 1
#define PANEL_VOLTAGE 1
#define LED D7 //Builtin LED

#define DHT_IN D6
#define DHT_POWER C0

/* Sensors */
#define DHTTYPE DHT22 // DHT 22  (AM2302), AM2321
#define DHT_INIT 1000
DHT dht(DHT_IN, DHTTYPE);

Adafruit_BMP280 bmp280(BAROMETER_CHIP_SELECT);
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);
FuelGauge gauge;

Settings settings;
#define diagnosticMode settings.diagnositicCycles
unsigned long duration;

char* serializeToJson(JsonObject& json)
{
    int length = json.measureLength() + 1;
    char* jsonString = (char*)malloc(length);
    jsonString[length] = NULL;
    json.printTo(jsonString, length);
    return jsonString;
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

    pinMode(PANEL_VOLTAGE, INPUT);
    pinMode(DHT_POWER, OUTPUT);

    digitalWrite(DHT_POWER, HIGH);
    dht.begin();
    bmp280.begin();
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

void setup()
{
    Serial.begin(115200);

    JsonObject& settingsJson = serialize(&settings);
    Serial.print("SETTINGS: ");
    settingsJson.printTo(Serial);

    Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
    Particle.connect();
}

String serialize(Reading *reading)
{
    String result = String::format("%d:%f8.2:%f8.2|", reading->version, reading->batteryVoltage, reading->panelVoltage);
    String serializedReading;
    if(reading->bmpRead)
    {
        serializedReading = String::format("b%f8.2:%f8.2", reading->bmpTemperature, reading->pressure);
        result.concat(serializedReading);
    }
    if(reading->dhtRead)
    {
        serializedReading = String::format("%sd%f8.2:%f8.2", reading->dhtTemperature, reading->humidity);
        result.concat(serializedReading);
    }
    if(reading->anemometerRead)
    {
        serializedReading = String::format("%sa%f8.2:%d", reading->windSpeed, reading->windDirection);
        result.concat(serializedReading);
    }
    return result;
}

void readAnemometer(Reading *reading)
{
    if(!(reading->anemometerRead = laCrosseTX23.read(reading->windSpeed, reading->windDirection)))
    {
        Serial.println("ERROR: Could not read anemometer");
    }
}

void readVoltage(Reading *reading)
{
    reading->batteryVoltage = gauge.getVCell();
    reading->panelVoltage = 1023.0 / (float)analogRead(PANEL_VOLTAGE);
}

void readDht(Reading *reading)
{
    reading->dhtTemperature = dht.getTempCelcius();
    reading->humidity = dht.getHumidity();
    digitalWrite(DHT_POWER, LOW);
    if(!(reading->dhtRead = !isnan(reading->dhtTemperature) && !isnan(reading->humidity)))
    {
        Serial.println("ERROR: Could not read DHT temp/humidity sensor");
    }
    //Serial.println(String::format("%d %f %f", reading->dhtRead, reading->dhtTemperature, reading->humidity));
}

void readBmp280(Reading *reading)
{
    digitalWrite(BAROMETER_CHIP_SELECT, HIGH);
    reading->bmpTemperature = bmp280.readTemperature();
    reading->pressure = bmp280.readPressure();
    digitalWrite(BAROMETER_CHIP_SELECT, LOW);

    if(!(reading->bmpRead = !isnan(reading->bmpTemperature) && !isnan(reading->pressure) && reading->pressure > 0))
    {
        Serial.println("ERROR: BMP280 temp/pressure sensor");
    }
}

void loop()
{
    Reading reading;

    //read data
    reading.version = settings.version;
    readAnemometer(&reading);
    readVoltage(&reading);
    readBmp280(&reading);

    unsigned long currentRuntime = millis() - duration;
    if(currentRuntime < DHT_INIT)
    {
        delay(DHT_INIT - currentRuntime);
    }
    readDht(&reading);

    //send serialized reading to the cloud    
    String publishedReading = serialize(&reading);    
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
            delay(settings.sleepTime);
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
