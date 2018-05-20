#include <ArduinoJson.h>
#include <Arduino.h>

FuelGauge gauge;

struct Reading
{
    float batteryVoltage;
    float panelVoltage;
    float windSpeed;
    float dhtTemperature;
    float bmpTemperature;
    float humidity;
    float pressure;
};

void setup()
{
    Serial.begin(9600);
}

JsonObject &serialize(Reading *reading)
{
    const size_t bufferSize = JSON_OBJECT_SIZE(7);
    DynamicJsonBuffer jsonBuffer(bufferSize);

    JsonObject &root = jsonBuffer.createObject();
    root["batteryVoltage"] = reading->batteryVoltage;
    root["panelVoltage"] = reading->panelVoltage;
    root["windSpeed"] = reading->windSpeed;
    root["dhtTemperature"] = reading->dhtTemperature;
    root["bmpTemperature"] = reading->bmpTemperature;
    root["humidity"] = reading->humidity;
    root["pressure"] = reading->pressure;

    return root;
}

void loop()
{
    Reading reading;
    reading.batteryVoltage = gauge.getVCell();

    reading.panelVoltage = 0.0;
    reading.windSpeed = 0.0;
    reading.dhtTemperature = 0.0;
    reading.bmpTemperature = 0.0;
    reading.humidity = 0.0;
    reading.pressure = 0.0;

    JsonObject& jsonReading = serialize(&reading);
    String publishedReading;
    jsonReading.printTo(publishedReading);

    Particle.publish("Reading", publishedReading.c_str(), 60, PRIVATE);

    delay(5000);
}
