#ifndef Sensor_h
#define Sensor_h

#include "Arduino.h"
#include "ArduinoJson.h"

typedef struct DataReading{
  float temperatureCelciusBarometer;
  float temperatureCelciusHydrometer;
  float pressurePascal;
  float humidityPercent;
  float speedMetersPerSecond;
  uint16_t directionSixteenths;
  uint16_t chargeVoltage;
  uint16_t batteryVoltage;
  uint16_t supplyVoltage;
} DataReading;

class Sensor
{
	public:
		virtual bool init() = 0;
        virtual bool read(DataReading&) = 0;
        virtual void json(JsonObject&, DataReading&) = 0;
        virtual void print(Print&, DataReading&) = 0;
        virtual String Name() = 0;
};

#include "DHT.h"
#include "LaCrosse_TX23.h"
#include "Adafruit_BMP280.h"

class DHTSensorAdapter : public Sensor {
    public:
        DHTSensorAdapter(DHT*);
        bool init();
        bool read(DataReading&);
        void json(JsonObject&, DataReading&);
        virtual void print(Print&, DataReading&);
        String Name();
    private:
        DHT& dht;
};

class BMP280Sensor : public Sensor {
    public:
        BMP280Sensor(int chipSelectPin, Adafruit_BMP280*);
        bool init();
        bool read(DataReading&);
        void json(JsonObject&, DataReading&);
        virtual void print(Print&, DataReading&);
        String Name();
    private:
        int chipSelectPin;
        Adafruit_BMP280& barometer;
};

class Anemometer : public Sensor {
    public:
        Anemometer(LaCrosse_TX23*);
        bool init();
        bool read(DataReading&);
        void json(JsonObject&, DataReading&);
        virtual void print(Print&, DataReading&);
        String Name();
    private:
        String name;
        LaCrosse_TX23& anemometer;
};

class VoltageSensor : public Sensor {
    public:
        VoltageSensor(int supplyPin, int chargePin);
        bool init();
        bool read(DataReading&);
        void json(JsonObject&, DataReading&);
        virtual void print(Print&, DataReading&);
        String Name();
    private:
        int supplyPin;
        int chargePin;
};
#endif