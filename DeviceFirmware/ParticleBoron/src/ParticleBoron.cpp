#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"

void onError(const char* message);
bool checkBrownout();
void deviceSetup();
void watchDogTimeout();
void setup();
void printSystemInfo();
void deepSleep(unsigned int milliseconds);
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
#define FIRMWARE_VERSION "1.0"

#define LED D7
#define SENSOR_POWER A0
#define DHT_IN D6
#define ANEMOMETER D1

#define DHTTYPE DHT22
#define DHT_INIT_TIMEOUT 1000
#define ANEMOMETER_TIMEOUT 1000

SYSTEM_MODE(SEMI_AUTOMATIC);

#include <Wire.h>
#include "Compass.h"
#include "settings.h"

#include <Adafruit_DHT.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>

DHT dht(DHT_IN, DHTTYPE);
Adafruit_BME280 bme280;
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);

FuelGauge fuelGuage;
PMIC pmic;
Compass compassSensor;

ApplicationWatchdog wd(20000, watchDogTimeout);

Settings settings;
#define diagnosticMode settings.diagnositicCycles
unsigned long duration;
bool brownout;

struct Reading
{
    int version;
    float batteryVoltage;
    int panelVoltage;
    bool anemometerRead;
    float windSpeed;
    int windDirection;
    bool dhtRead;
    float dhtTemperature;
    float dhtHumidity;
    bool bmeRead;
    float bmeTemperature;
    float pressure;
    float bmeHumidity;
};

Reading initialReading;

void onError(const char* message)
{
    RGB.color(255, 255, 0);
    Serial.println(message);
}

bool timeout(int timeout, std::function<bool()> opperation)
{
    unsigned long endTime = millis() + timeout;
    bool result = false;
    while(!(result = opperation()) && millis() < endTime)
    {
        Serial.print(".");
        delay(10);
    }
    return result;
}

bool checkBrownout()
{
    return settings.brownout && fuelGuage.getVCell() < settings.brownoutVoltage;
}

bool readAnemometer(Reading *reading)
{
    Serial.print("ANEMOMETER ");
    bool result = timeout(ANEMOMETER_TIMEOUT, [reading]()
    {
        return laCrosseTX23.read(reading->windSpeed, reading->windDirection);
    });
    Serial.println();
    return result;
}

bool readDht(Reading *reading)
{
    Serial.print("DHT ");
    bool result = timeout(DHT_INIT_TIMEOUT, [reading]()
    {
        reading->dhtTemperature = dht.getTempCelcius();
        reading->dhtHumidity = dht.getHumidity();
        return !isnan(reading->dhtTemperature) && !isnan(reading->dhtHumidity);
    });
    Serial.println();
    return result;
}

bool readBme280(Reading *reading)
{
    reading->bmeTemperature = bme280.readTemperature();
    reading->pressure = bme280.readPressure();
    reading->bmeHumidity = bme280.readHumidity();
    return !isnan(reading->bmeTemperature) && !isnan(reading->pressure) && reading->pressure > 0 && !isnan(reading->bmeHumidity);
}

STARTUP(deviceSetup());
void deviceSetup()
{
    duration = millis();

    //Turn off the status LED to save power
    RGB.control(true);
    RGB.color(0, 0, 0);

    //Load saved settings;
    settings = *loadSettings();

    if(!(brownout = checkBrownout()))
    {
        if(diagnosticMode)
        {
            pinMode(LED, OUTPUT);
            digitalWrite(LED, HIGH);
            settings.diagnositicCycles--;

            saveSettings(&settings);
        }

        //turn on the sensors    
        pinMode(SENSOR_POWER, OUTPUT);    
        digitalWrite(SENSOR_POWER, HIGH);

        dht.begin();
        bme280.begin(0x76);

        //take an initial wind reading
        if(!(initialReading.anemometerRead = readAnemometer(&initialReading)))
        {
            onError("ERROR: Could not get initial wind reading");
        }
    }
}

void watchDogTimeout()
{
  Serial.println("Watchdog timeout");
  System.reset();
}

void setup()
{
  pinMode(LED, OUTPUT);

  Serial.begin(9600);
  Wire.begin();

  delay(10000);

  Serial.println("Weather Station");
}

void printSystemInfo()
{
  String myID = System.deviceID();
  Serial.printlnf("Device ID: %s", myID.c_str());
  Serial.printlnf("System version: %s", System.version().c_str());

  byte inputVoltageLimit = pmic.getInputVoltageLimit();
  Serial.printlnf("Input Voltage Limit: %d", inputVoltageLimit);

  byte inputCurrentLimit = pmic.getInputCurrentLimit();
  Serial.printlnf("Input Current Limit: %d", inputCurrentLimit);

  uint16_t minimumSystemVoltage = pmic.getMinimumSystemVoltage();
  Serial.printlnf("Minimum System Voltage: %d", minimumSystemVoltage);

  byte chargeCurrent = pmic.getChargeCurrent();
  Serial.printlnf("Charge Current: %d", chargeCurrent);

  uint16_t chargeVoltage = pmic.getChargeVoltageValue();
  Serial.printlnf("Charge Voltage: %d", chargeVoltage);

  Serial.print("Charge: ");
  Serial.println(fuelGuage.getSoC());

  Serial.print("Voltage: ");
  Serial.println(fuelGuage.getVCell());

  Serial.println("Compass initializing");
  compassSensor = Compass();
  Serial.println(compassSensor.begin());
  Serial.println("Compass initialed");

  CompassReading reading = compassSensor.getReading();
  Serial.printlnf("X: %d, Y: %d, Z: %d", reading.x, reading.y, reading.z);
}

#define WAKEUP_BUDDY_ADDRESS 8

void deepSleep(unsigned int milliseconds)
{
  Serial.printlnf("Deep Sleep for %d milliseconds", milliseconds);
  Wire.beginTransmission(WAKEUP_BUDDY_ADDRESS);
  Wire.write(milliseconds);
  Wire.write(milliseconds >> 8);
  uint8_t status = Wire.endTransmission();
  Serial.printlnf("Transmission status %d", status);

  fuelGuage.sleep();
  System.sleep(SLEEP_MODE_DEEP);
}

void loop()
{
  digitalWrite(LED_BUILTIN, HIGH);
  delay(1000);
  digitalWrite(LED_BUILTIN, LOW);

  // And repeat!
  printSystemInfo();

  wd.checkin();

  deepSleep(30000);
}
