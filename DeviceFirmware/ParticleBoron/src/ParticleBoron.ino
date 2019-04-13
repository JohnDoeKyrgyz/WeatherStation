
#define FIRMWARE_VERSION "1.0"

#define LED D7
#define SENSOR_POWER A4
#define DHT_IN D2
#define ANEMOMETER D1
#define WAKEUP_BUDDY_ADDRESS 8

#define ANEMOMETER_TIMEOUT 1000

SYSTEM_MODE(SEMI_AUTOMATIC);

#include <Wire.h>
#include "Compass.h"
#include "settings.h"

#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>
#include <ArduinoJson.h>

Adafruit_BME280 bme280;
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);

FuelGauge fuelGuage;
PMIC pmic;
Compass compassSensor;

ApplicationWatchdog watchDog(60000, watchDogTimeout);

Settings settings;
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

bool readVoltage(Reading *reading)
{
  reading->batteryVoltage = fuelGuage.getVCell();
  //int panelVoltage = analogRead(PANEL_VOLTAGE);
  //reading->panelVoltage = panelVoltage;
  reading->panelVoltage = 0;
  return true;
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

  //Load saved settings;
  settings = loadSettings();

  if (!(brownout = checkBrownout()))
  {
    if (settings.diagnositicCycles > 0)
    {
      pinMode(LED, OUTPUT);
      digitalWrite(LED, HIGH);
      settings.diagnositicCycles--;

      saveSettings(settings);
    }

    //turn on the sensors
    pinMode(SENSOR_POWER, OUTPUT);
    digitalWrite(SENSOR_POWER, HIGH);

    //the bme280 will activate the Wire library as well.
    bme280.begin(0x76);

    //Enable the Wire library if it wasn't already enabled by another sensor
    if (!Wire.isEnabled())
    {
      Wire.begin();
      Serial.println("Activated Wire");
    }

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
  delay(500);
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

void onSettingsUpdate(const char *event, const char *data)
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
  watchDog.checkin();

  //Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
  //Particle.connect();

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
  
  Serial.println("CONNECTED");
}

char* serialize(Reading *reading)
{
    char* buffer = messageBuffer;
    buffer += sprintf(buffer, "%d:%f:%d|", reading->version, reading->batteryVoltage, reading->panelVoltage);
    if(reading->bmeRead)
    {
        buffer += sprintf(buffer, "b%f:%f:%f", reading->bmeTemperature, reading->pressure, reading->bmeHumidity);
    }
    if(reading->anemometerRead)
    {
        buffer += sprintf(buffer, "a%f:%d", reading->windSpeed, reading->windDirection);
    }
    return messageBuffer;
}

void loop()
{
  Serial.println("LOOP");

  watchDog.checkin();

  //read data
  Reading reading;
  reading.version = settings.version;
  readVoltage(&reading);

  if (!(reading.bmeRead = readBme280(&reading)))
  {
    onError("ERROR: BME280 temp/pressure sensor");
  }
  if (!(reading.anemometerRead = readAnemometer(&reading)))
  {
    onError("ERROR: Could not read anemometer");
  }

  digitalWrite(SENSOR_POWER, LOW);

  //take the greater of the initial wind reading or the most recent wind reading
  if (!reading.anemometerRead || (reading.anemometerRead && initialReading.anemometerRead && initialReading.windSpeed > reading.windSpeed))
  {
    reading.windSpeed = initialReading.windSpeed;
    reading.windDirection = initialReading.windDirection;
    Serial.print("Using faster wind speed ");
    Serial.print(initialReading.windSpeed);
    Serial.print(", ");
    Serial.println(reading.windSpeed);
  }

  //send serialized reading to the cloud
  char *publishedReading = serialize(&reading);
  Serial.println(publishedReading);
  Particle.publish("Reading", publishedReading, 60, PRIVATE);

  //Allow particle to process before going into deep sleep
  Particle.process();

  if (settings.diagnositicCycles)
  {
    Serial.print("DIAGNOSTIC COUNT ");
    Serial.println(settings.diagnositicCycles);
    digitalWrite(LED, LOW);
  }

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
