#include "Sensors.h"
#include "DHT.h"
#include "LaCrosse_TX23.h"
#include "Adafruit_BMP280.h"
#include "ArduinoJson.h"

/*
* DHT Sensor
*/
DHTSensorAdapter::DHTSensorAdapter(DHT* thermometer):
    dht(*thermometer){
}

String DHTSensorAdapter::Name() {
    return "DHT22 Temp/Humidity";
}

bool DHTSensorAdapter::init(){
    dht.begin();
    return true;
}

bool DHTSensorAdapter::read(DataReading& reading){
    float temp, humidity;
    bool result = true;
    temp = dht.readHumidity();
    humidity = dht.readTemperature();
    result = !isnan(temp) && !isnan(humidity);

    if(result){
        reading.temperatureCelciusHydrometer = temp;
        reading.humidityPercent = humidity;
    }
    return result;
}

void DHTSensorAdapter::json(JsonObject& json, DataReading& reading){
    json["temperatureCelciusHydrometer"] = reading.temperatureCelciusHydrometer;
    json["humidityPercent"] = reading.humidityPercent;
}

void DHTSensorAdapter::print(Print& print, DataReading& reading){
    print.print(reading.temperatureCelciusHydrometer);
    print.print(":");
    print.print(reading.humidityPercent);
    print.print(":");
}

/*
* BMP280 Sensor
*/
BMP280Sensor::BMP280Sensor(int chipSelectPin, Adafruit_BMP280* barometer):
    chipSelectPin(chipSelectPin),
    barometer(*barometer){
}

String BMP280Sensor::Name() {
    return "BMP280 Pressure/Temperature";
}

bool BMP280Sensor::init(){
    return barometer.begin();
}

bool BMP280Sensor::read(DataReading& reading){
    float temp, pressure;
    bool result = true;
    digitalWrite(chipSelectPin, HIGH);
    temp = barometer.readTemperature();
    pressure = barometer.readPressure();
    digitalWrite(chipSelectPin, LOW);
    result = !isnan(temp) && !isnan(pressure);
    if(result){
        reading.temperatureCelciusBarometer = temp;
        reading.pressurePascal = pressure;
    }
    return result;
}

void BMP280Sensor::json(JsonObject& json, DataReading& reading){
    json["temperatureCelciusBarometer"] = reading.temperatureCelciusBarometer;
    json["pressurePascal"] = reading.pressurePascal;
}

void BMP280Sensor::print(Print& print, DataReading& reading){
    print.print(reading.temperatureCelciusBarometer);
    print.print(":");
    print.print(reading.pressurePascal);
    print.print(":");
}

/*
* Anemometer Sensor
*/
Anemometer::Anemometer(LaCrosse_TX23* anemometer):
    anemometer(*anemometer){
}

String Anemometer::Name() {
    return "LaCrosse_TX23 Anemometer";
}

bool Anemometer::init(){
    return true;
}

bool Anemometer::read(DataReading& reading){
    float speed;
	int direction;
    bool result = anemometer.read(speed, direction);
    if(result){
        reading.speedMetersPerSecond = speed;
        reading.directionSixteenths = direction;
    }
    return result;
}

void Anemometer::json(JsonObject& json, DataReading& reading){
    json["speedMetersPerSecond"] = reading.speedMetersPerSecond;
    json["directionSixteenths"] = reading.directionSixteenths;
}

void Anemometer::print(Print& print, DataReading& reading){
    print.print(reading.speedMetersPerSecond);
    print.print(":");
    print.print(reading.directionSixteenths);
    print.print(":");
}

/*
* Voltage Sensor
*/
VoltageSensor::VoltageSensor(int supplyPin, int chargePin):
    supplyPin(supplyPin),
    chargePin(chargePin){
}

String VoltageSensor::Name() {
    return "Builtin Voltages";
}

bool VoltageSensor::init(){
    return true;
}

bool VoltageSensor::read(DataReading& reading){
    reading.supplyVoltage = analogRead(supplyPin);
    reading.chargeVoltage = analogRead(chargePin);
    reading.batteryVoltage = Charger.batteryMillivolts();
}

void VoltageSensor::json(JsonObject& json, DataReading& reading){
    json["supplyVoltage"] = reading.supplyVoltage;
    json["batteryVoltage"] = reading.batteryVoltage;
    json["chargeVoltage"] = reading.chargeVoltage;
}

void VoltageSensor::print(Print& print, DataReading& reading){
    print.print(reading.supplyVoltage);
    print.print(":");
    print.print(reading.batteryVoltage);
    print.print(":");
    print.print(reading.chargeVoltage);
    print.print(":");
}