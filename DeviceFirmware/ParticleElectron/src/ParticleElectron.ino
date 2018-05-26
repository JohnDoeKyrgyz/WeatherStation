#include <ArduinoJson.h>
#include <Arduino.h>
#include "settings.h"

#include <Adafruit_DHT.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BMP280.h>

#define FIRMWARE_VERSION "1.0"

struct Reading
{
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
#define DHTPIN 1
#define ANEMOMETER 1
#define PANEL_VOLTAGE 1
#define LED D7 //Builtin LED

/* Sensors */
#define DHTTYPE DHT22 // DHT 22  (AM2302), AM2321
DHT dht(DHTPIN, DHTTYPE);
Adafruit_BMP280 bmp280(BAROMETER_CHIP_SELECT);
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);
FuelGauge gauge;

Settings settings;
#define diagnosticMode settings.diagnositicCycles

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

    dht.begin();
    bmp280.begin();
}
STARTUP(deviceSetup());

void setup()
{
    Serial.begin(115200);

    JsonObject& settingsJson = serialize(&settings);
    char* settingsJsonString = serializeToJson(settingsJson);
    Serial.print("SETTINGS: ");
    Serial.println(settingsJsonString);
    
    Particle.variable("settings", settingsJsonString);
}

JsonObject &serialize(Reading *reading)
{
    const size_t bufferSize = JSON_OBJECT_SIZE(8);
    DynamicJsonBuffer jsonBuffer(bufferSize);

    JsonObject &root = jsonBuffer.createObject();
    root["batt_v"] = reading->batteryVoltage;
    root["panel_v"] = reading->panelVoltage;

    if(reading->bmpRead)
    {
        root["temp_bmp_c"] = reading->bmpTemperature;
        root["press_pas"] = reading->pressure;
    }
    if(reading->dhtRead)
    {
        root["temp_dht_c"] = reading->dhtTemperature;
        root["humidity_perc"] = reading->humidity;
    }
    if(reading->anemometerRead)
    {
        root["wspeed_mps"] = reading->windSpeed;
        root["wdir_16ths"] = reading->windDirection;
    }

    return root;
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
    if(!(reading->dhtRead = !isnan(reading->dhtTemperature) && !isnan(reading->humidity)))
    {
        Serial.println("ERROR: Could not read DHT temp/humidity sensor");
    }
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
    readAnemometer(&reading);
    readVoltage(&reading);
    readDht(&reading);
    readBmp280(&reading);

    //serialize reading to json string
    JsonObject &jsonReading = serialize(&reading);
    jsonReading.prettyPrintTo(Serial);
    Serial.println();
    char* publishedReading = serializeToJson(jsonReading);

    //send serialized reading to the cloud
    Serial.println(publishedReading);
    Particle.publish("Reading", publishedReading, 60, PRIVATE);

    //Allow particle to process before going into deep sleep
    Particle.process();
    
    Serial.print("DIAGNOSTIC MODE ");
    Serial.println(diagnosticMode);
    if(diagnosticMode)
    {
        digitalWrite(LED, LOW);
    }

    if(settings.useDeepSleep)
    {
        Serial.print("DEEP SLEEP ");
        Serial.print(settings.sleepTime);
        Serial.println();
        System.sleep(SLEEP_MODE_SOFTPOWEROFF, settings.sleepTime);
    }
    else
    {
        Serial.print("LIGHT SLEEP ");
        Serial.print(settings.sleepTime);
        Serial.println();
        delay(settings.sleepTime);
    }
}
