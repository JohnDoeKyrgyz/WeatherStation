#ifndef SENSOR_h
#define SENSOR_h

class Sensor
{
public:
    const char *Name;
    Sensor(const char *name)
    {
        Name = name;
    }
    virtual bool getReading(char*& reading);
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
    CompassSensor() : Sensor("compass") {}
    bool getReading(char*& buffer)
    {
        CompassReading reading = compassSensor.getReading();
        buffer += sprintf(buffer, "c%f:%f:%f", reading.x, reading.y, reading.z);
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
    BatteryPower() : Sensor("battery") {}
    bool getReading(char*& reading)
    {
        float batteryVoltage = fuelGuage.getVCell();
        float batteryPercentage = fuelGuage.getSoC();
        reading += sprintf(reading, "f%f:%f", batteryVoltage, batteryPercentage);
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
    PanelPower() : Sensor("panel") {}
    bool getReading(char*& reading)
    {
        _voltage = powerMonitor.getBusVoltage_V();
        _current = powerMonitor.getCurrent_mA();
        _read = _voltage < 16;
        if (_read)
        {
            reading += sprintf(reading, "p%f:%f", _voltage, _current);
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

#include <assert.h>
class Anemometer : public Sensor
{
private:
    LaCrosse_TX23 laCrosseTX23;
    int _maxTries;
    int _samples;

public:
    Anemometer(const int pin, const int samples, const int maxTries) : Sensor("anemometer"), laCrosseTX23(pin)
    {
        assert(maxTries > samples);

        _maxTries = maxTries;
        _samples = samples;
    }
    bool getReading(char*& reading)
    {
        bool read = false;
        int tries = 0;
        float maxSpeed = 0.0, speed = 0.0;
        int direction;
        int sampleCount = 0;
        for(int i = 0; tries++ < _maxTries && i < _samples; i++)
        {
            if(laCrosseTX23.read(speed, direction))
            {
                read = true;
                sampleCount++;
                if(speed > maxSpeed) maxSpeed = speed;
            }
        }
        if (read)
        {
            reading += sprintf(reading, "a%f:%d", maxSpeed, direction);
        }
        
        return read;
    }
    bool begin()
    {
        float speed;
        int direction;
        bool read = false;
        for(int tries = 0; !read && tries < _maxTries && (read = laCrosseTX23.read(speed, direction)); tries++);
        return read;
    }
};

class Barometer : public Sensor
{
private:
    Adafruit_BME280 bme280;

public:
    Barometer() : Sensor("barometer") {}
    bool getReading(char*& reading)
    {
        float bmeTemperature = bme280.readTemperature();
        float pressure = bme280.readPressure();
        float bmeHumidity = bme280.readHumidity();
        bool result = !isnan(bmeTemperature) && !isnan(pressure) && pressure > 0.0 && !isnan(bmeHumidity);
        reading += sprintf(reading, "b%f:%f:%f", bmeTemperature, pressure, bmeHumidity);
        return result;
    }
    bool begin()
    {
        return bme280.begin(0x76);
    }
};

#endif
