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
        float batteryPercentage = fuelGuage.getNormalizedSoC();
        int batteryState = System.batteryState();        
        reading += sprintf(reading, "f%f:%f:%d", batteryVoltage, batteryPercentage, batteryState);
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
class LaCrosseAnemometer : public Sensor
{
private:
    LaCrosse_TX23 laCrosseTX23;
    int _maxTries;
    int _samples;

public:
    LaCrosseAnemometer(const int pin, const int samples, const int maxTries) : Sensor("anemometer"), laCrosseTX23(pin)
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

//Code inspired by:
// https://github.com/sparkfun/Wimp_Weather_Station/blob/master/Wimp_Weather_Station.ino
class SparkFunAnemometer : public Sensor
{
private:
    int _pinWindSpeed;
    int _pinWindDirection;
    volatile static int anemometerTicks;
    volatile static long lastWindTime;
    static void onAnemometerTick() 
    {
        //Ignore switch - bounce glitches less than 10 ms
        long time = millis();
        if(time - lastWindTime > 10)
        {
            lastWindTime = time;
            anemometerTicks++;
        }  
    }

public:
    SparkFunAnemometer(const int pinWindSpeed, const int pinWindDirection) : Sensor("anemometer")
    {
        _pinWindSpeed = pinWindSpeed;
        _pinWindDirection = pinWindDirection;    
    }
    bool getReading(char*& reading)
    {   
        //Get the wind direction
        byte numberOfReadings = 8;
	    unsigned int adc = 0;

	    for(int x = 0 ; x < numberOfReadings ; x++) adc += analogRead(_pinWindDirection);
	    adc /= numberOfReadings;

        int windDirection = -1;
        if (adc < 380) windDirection = 113;
        else if (adc < 393) windDirection = 68;
        else if (adc < 414) windDirection = 90;
        else if (adc < 456) windDirection = 158;
        else if (adc < 508) windDirection = 135;
        else if (adc < 551) windDirection = 203;
        else if (adc < 615) windDirection = 180;
        else if (adc < 680) windDirection = 23;
        else if (adc < 746) windDirection = 45;
        else if (adc < 801) windDirection = 248;
        else if (adc < 833) windDirection = 225;
        else if (adc < 878) windDirection = 338;
        else if (adc < 913) windDirection = 0;
        else if (adc < 940) windDirection = 293;
        else if (adc < 967) windDirection = 315;
        else if (adc < 990) windDirection = 270;

        float deltaTime = millis() - lastWindTime;
	    deltaTime /= 1000.0; //Covert to seconds

	    float windSpeed = (float)anemometerTicks / deltaTime; //3 / 0.750s = 4

	    anemometerTicks = 0; //Reset and start watching for new wind
	    lastWindTime = millis();
	    windSpeed *= 0.66698368; //convert to meters per second
        
        bool read = windDirection != -1 && lastWindTime > 0;
        if(read)
        {
            reading += sprintf(reading, "a%f:%d", windSpeed, windDirection);
        }
        
        return read;
    }
    bool begin()
    {
        pinMode(_pinWindSpeed, INPUT_PULLUP);
        pinMode(_pinWindDirection, INPUT);

        anemometerTicks = 0;
        lastWindTime = 0;
        attachInterrupt(_pinWindSpeed, onAnemometerTick, FALLING);
        return true;
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
