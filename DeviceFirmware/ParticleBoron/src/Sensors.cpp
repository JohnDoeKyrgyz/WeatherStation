#include <Sensor.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>
#include <Adafruit_INA219.h>



//Compass compassSensor;
#include <math.h>

class BatteryPower : Sensor {
    private:
        FuelGauge fuelGuage;
    public:
        int getReadingSize() {
            return 12;
        }
        bool getReading(char* reading){
            float batteryVoltage = fuelGuage.getVCell();
            float batteryPercentage = fuelGuage.getSoC();
            reading += sprintf(reading, "p%9.6f:%3.2f", batteryVoltage, batteryPercentage);
            return true;
        }
        bool begin(){
            return true;
        }
};

class PanelPower : Sensor {
    private:
        Adafruit_INA219 powerMonitor;
    public:
        int getReadingSize() {
            return 20;
        }
        bool getReading(char* reading){  
            float panelVoltage = powerMonitor.getBusVoltage_V();
            float panelCurrent = powerMonitor.getCurrent_mA();
            bool read = panelVoltage < 16;
            if(read){
                reading += sprintf(reading, "p%9.6f:%9.6f", panelVoltage, panelCurrent);
            }
            return read;
        }
        bool begin(){
            powerMonitor.begin();
            powerMonitor.setCalibration_16V_400mA();
            return true;
        }
};

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