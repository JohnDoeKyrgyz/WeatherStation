#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"

void onError(const char *message);
bool checkBrownout();
void deviceSetup();
void watchDogTimeout();
void deepSleep(unsigned int milliseconds);
void onSettingsUpdate(const char* event, const char* data);
void setup();
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
#define FIRMWARE_VERSION "1.0"

#define LED D7
#define SENSOR_POWER D2
#define DHT_IN D6
#define ANEMOMETER D1
#define WAKEUP_BUDDY_ADDRESS 8

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
#include <ArduinoJson.h>

DHT dht(DHT_IN, DHTTYPE);
Adafruit_BME280 bme280;
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);

FuelGauge fuelGuage;
PMIC pmic;
Compass compassSensor;

ApplicationWatchdog watchDog(60000, watchDogTimeout);

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
char messageBuffer[255];

void onError(const char *message)
{
  RGB.color(255, 255, 0);
  Serial.println(message);
}

bool timeout(int timeout, std::function<bool()> opperation)
{
  unsigned long endTime = millis() + timeout;
  bool result = false;
  while (!(result = opperation()) && millis() < endTime)
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
  bool result = timeout(ANEMOMETER_TIMEOUT, [reading]() {
    return laCrosseTX23.read(reading->windSpeed, reading->windDirection);
  });
  Serial.println();
  return result;
}

bool readDht(Reading *reading)
{
  Serial.print("DHT ");
  bool result = timeout(DHT_INIT_TIMEOUT, [reading]() {
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

  Serial.begin(115200);

  //Turn off the status LED to save power
  RGB.control(true);
  RGB.color(0, 0, 0);

  delay(10000);
  Serial.println("WeatherStation: deviceSetup()");

  //Load saved settings;
  settings = loadSettings();
  Serial.printlnf("Loaded Settings %d", settings.version);

  if (!(brownout = checkBrownout()))
  {
    if (diagnosticMode)
    {
      pinMode(LED, OUTPUT);
      digitalWrite(LED, HIGH);
      settings.diagnositicCycles--;

      saveSettings(settings);
    }

    //turn on the sensors
    pinMode(SENSOR_POWER, OUTPUT);
    digitalWrite(SENSOR_POWER, HIGH);

    Wire.begin();
    dht.begin();
    bme280.begin(0x76);

    //take an initial wind reading
    if (!(initialReading.anemometerRead = readAnemometer(&initialReading)))
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

void deepSleep(unsigned int milliseconds)
{
  Serial.printlnf("Deep Sleep for %d milliseconds", milliseconds);
  Wire.beginTransmission(WAKEUP_BUDDY_ADDRESS);
  Wire.write(milliseconds);
  Wire.write(milliseconds >> 8);
  Wire.endTransmission();

  fuelGuage.sleep();
  System.sleep(SLEEP_MODE_DEEP);
}

void onSettingsUpdate(const char* event, const char* data)
{
    digitalWrite(LED, HIGH);
    settings = deserialize(data);
    saveSettings(settings);

    Serial.print("SETTINGS UPDATE: ");
    Serial.println(data);

    Particle.publish("Settings", data, 60, PRIVATE);
    Particle.process();

    digitalWrite(LED, LOW);
}

void setup()
{
  Serial.begin(115200);

  watchDog.checkin();

  Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
  Particle.connect();

  if (brownout)
  {
    float voltage = fuelGuage.getVCell();
    Serial.print("BROWNOUT ");
    Serial.println(voltage);

    char *buffer = messageBuffer;
    sprintf(buffer, "%f:%d", voltage, settings.brownoutMinutes);
    Particle.publish("Brownout", buffer, 60, PRIVATE);
    Particle.process();

    deepSleep(settings.brownoutMinutes * 60000);
  }
}

void loop()
{
  watchDog.checkin();

  void (*sleepAction)();
  const char *sleepMessage;
  if (settings.useDeepSleep)
  {
    sleepMessage = "DEEP";
    sleepAction = []() {
      deepSleep(settings.sleepTime * 1000);
    };
  }
  else
  {
    sleepMessage = "LIGHT";
    sleepAction = []() {
      delay(settings.sleepTime * 1000);
      deviceSetup();
    };
  }

  Serial.print(sleepMessage);
  Serial.print(" SLEEP ");
  Serial.println(settings.sleepTime);

  Serial.print("DURATION ");
  Serial.println(millis() - duration);

  sleepAction();
}
