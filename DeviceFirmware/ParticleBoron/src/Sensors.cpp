#include <Sensor.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_INA219.h>


//Adafruit_INA219 powerMonitor;
//FuelGauge fuelGuage;
//Compass compassSensor;
//PMIC pmic;
#include <math.h>

class Anemometer : Sensor {
    private:
        LaCrosse_TX23 laCrosseTX23;
        int maxTries;
    public:
        Anemometer(const int pin, const int maxTries) : laCrosseTX23(pin) {
            this->maxTries = maxTries;
        }
        int getReadingSize() {
            return 12;
        }
        bool getReading(char* reading){
            bool read = false;
            int tries = 0;
            float speed = 0.0;
            int direction = 0;
            while (!(read = laCrosseTX23.read(speed, direction)) && tries++ < maxTries);
            if(read){
                reading += sprintf(reading, "a%9.6f:%d", speed, direction);
            }
            return read;
        }
        bool begin(){
            float speed;
            int direction;
            return laCrosseTX23.read(speed, direction);
        }
};

class Barometer : Sensor {
    private:
        Adafruit_BME280 bme280;
    public:
        int getReadingSize() {
            return 22;
        }
        bool getReading(char* reading){
            float bmeTemperature = bme280.readTemperature();
            float pressure = bme280.readPressure();
            float bmeHumidity = bme280.readHumidity();
            bool result = !isnan(bmeTemperature) && !isnan(pressure) && pressure > 0.0 && !isnan(bmeHumidity);
            reading += sprintf(reading, "b%9.6f:%9.6f:%9.6f", bmeTemperature, pressure, bmeHumidity);
            return result;
        }
        bool begin(){
            return bme280.begin(0x76);
        }
};