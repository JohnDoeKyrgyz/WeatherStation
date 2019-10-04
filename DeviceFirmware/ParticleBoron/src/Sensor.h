#ifndef SENSOR_h
#define SENSOR_h

class Sensor
{
public:
    const char *Name;
    const int ReadingSize;
    Sensor(int readingSize, const char *name) : ReadingSize(readingSize)
    {
        Name = name;
    }
    virtual bool getReading(char *reading);
    virtual bool begin();
};

#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_INA219.h>
#include <Compass.h>
#include <math.h>

class CompassSensor : public Sensor
{
private:
    Compass compassSensor;

public:
    CompassSensor() : Sensor(12, "compass") {}
    bool getReading(char *buffer)
    {
        CompassReading reading = compassSensor.getReading();
        buffer += sprintf(buffer, "c%3.0f:%3.0f:%3.0f", reading.x, reading.y, reading.z);
        return true;
    }
    bool begin()
    {
        return compassSensor.begin();
    }
};

class BatteryPower : public Sensor
{
private:
    FuelGauge fuelGuage;

public:
    BatteryPower() : Sensor(14, "battery") {}
    bool getReading(char *reading)
    {
        float batteryVoltage = fuelGuage.getVCell();
        float batteryPercentage = fuelGuage.getSoC();
        reading += sprintf(reading, "p%9.6f:%3.2f", batteryVoltage, batteryPercentage);
        return true;
    }
    bool begin()
    {
        return true;
    }
};

class PanelPower : public Sensor
{
private:
    Adafruit_INA219 powerMonitor;
    bool _read;
    float _voltage;
    float _current;

public:
    PanelPower() : Sensor(21, "panel") {}
    bool getReading(char *reading)
    {
        _voltage = powerMonitor.getBusVoltage_V();
        _current = powerMonitor.getCurrent_mA();
        _read = _voltage < 16;
        if (_read)
        {
            reading += sprintf(reading, "p%9.6f:%9.6f", _voltage, _current);
        }
        return _read;
    }
    bool begin()
    {
        powerMonitor.begin();
        powerMonitor.setCalibration_16V_400mA();
        return true;
    }
    bool read() { return _read; }
    float voltage() { return _voltage; }
    float current() { return _current; }
};

class Anemometer : public Sensor
{
private:
    LaCrosse_TX23 laCrosseTX23;
    int maxTries;

public:
    Anemometer(const int pin, const int maxTries) : Sensor(13, "anemometer"), laCrosseTX23(pin)
    {
        this->maxTries = maxTries;
    }
    int getReadingSize()
    {
        return 13;
    }
    bool getReading(char *reading)
    {
        bool read = false;
        int tries = 0;
        float speed = 0.0;
        int direction = 0;
        while (!(read = laCrosseTX23.read(speed, direction)) && tries++ < maxTries)
            ;
        if (read)
        {
            reading += sprintf(reading, "a%9.6f:%2d", speed, direction);
        }
        return read;
    }
    bool begin()
    {
        float speed;
        int direction;
        return laCrosseTX23.read(speed, direction);
    }
};

class Barometer : public Sensor
{
private:
    Adafruit_BME280 bme280;

public:
    Barometer() : Sensor(24, "barometer") {}
    bool getReading(char *reading)
    {
        float bmeTemperature = bme280.readTemperature();
        float pressure = bme280.readPressure();
        float bmeHumidity = bme280.readHumidity();
        bool result = !isnan(bmeTemperature) && !isnan(pressure) && pressure > 0.0 && !isnan(bmeHumidity);
        reading += sprintf(reading, "b%9.6f:%9.6f:%9.6f", bmeTemperature, pressure, bmeHumidity);
        return result;
    }
    bool begin()
    {
        return bme280.begin(0x76);
    }
};

#endif
