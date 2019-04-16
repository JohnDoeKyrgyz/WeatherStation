
#define FIRMWARE_VERSION "1.0"

#define ANEMOMETER_TRIES 3
#define WATCHDOG_TIMEOUT 60000

#define LED D7
#define ANEMOMETER A4
#define WAKEUP_BUDDY_ADDRESS 8

SYSTEM_THREAD(ENABLED);
SYSTEM_MODE(AUTOMATIC);

#include <Wire.h>
#include "Compass.h"
#include "settings.h"

#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <LaCrosse_TX23.h>
#include <ArduinoJson.h>
#include <Adafruit_INA219.h>

Adafruit_BME280 bme280;
LaCrosse_TX23 laCrosseTX23(ANEMOMETER);
Adafruit_INA219 powerMonitor;
FuelGauge fuelGuage;
PMIC pmic;
Compass compassSensor;

ApplicationWatchdog watchDog(WATCHDOG_TIMEOUT, watchDogTimeout);

Settings settings;
unsigned long duration;
bool brownout;

struct Reading
{
  int version;
  float batteryVoltage;
  float batteryPercentage;
  float panelVoltage;
  float panelCurrent;
  bool anemometerRead;
  float windSpeed;
  int windDirection;
  bool bmeRead;
  float bmeTemperature;
  float pressure;
  float bmeHumidity;
  bool compassRead;
  CompassReading compassReading;
};

Reading initialReading;
char messageBuffer[255];

void onError(const char *message)
{
  RGB.color(255, 255, 0);
  Serial.println(message);
}

bool checkBrownout()
{
  return settings.brownout && fuelGuage.getVCell() < settings.brownoutVoltage;
}

bool readAnemometer(Reading *reading)
{
  bool read = false;
  int tries = 0;
  while (!(read = laCrosseTX23.read(reading->windSpeed, reading->windDirection)) && tries++ < ANEMOMETER_TRIES)
  {
    Serial.print(".");
  }
  if (tries > 0)
  {
    Serial.println();
  }
  return read;
}

bool readVoltage(Reading *reading)
{
  reading->batteryVoltage = fuelGuage.getVCell();
  reading->batteryPercentage = fuelGuage.getSoC();
  reading->panelVoltage = powerMonitor.getBusVoltage_V();
  reading->panelCurrent = powerMonitor.getCurrent_mA();
  return true;
}

bool readBme280(Reading *reading)
{
  reading->bmeTemperature = bme280.readTemperature();
  reading->pressure = bme280.readPressure();
  reading->bmeHumidity = bme280.readHumidity();
  return !isnan(reading->bmeTemperature) && !isnan(reading->pressure) && reading->pressure > 0 && !isnan(reading->bmeHumidity);
}

bool readCompass(Reading *reading)
{
  reading->compassReading = compassSensor.getReading();
  return true;
}

STARTUP(deviceSetup());
void deviceSetup()
{
  //Turn off the status LED to save power
  RGB.control(true);
  RGB.color(0, 0, 0);

  Serial.begin(115200);
  //delay(10000); //This is handy when you want to debug from the start

  duration = millis();
  Serial.printlnf("WeatherStation %s", FIRMWARE_VERSION);

  //Load saved settings;
  settings = loadSettings();

  if (!(brownout = checkBrownout()))
  {
    //Enable the Wire library if it wasn't already enabled by another sensor
    if (!Wire.isEnabled())
    {
      Wire.begin();
    }

    //the bme280 will activate the Wire library as well.
    Wire.reset();
    powerMonitor.begin();

    Wire.reset();
    bme280.begin(0x76);

    Wire.reset();
    compassSensor.begin();
    
    if (settings.diagnositicCycles > 0)
    {
      pinMode(LED, OUTPUT);
      digitalWrite(LED, HIGH);
      settings.diagnositicCycles--;

      saveSettings(settings);
    }

    //take an initial wind reading
    if (!(initialReading.anemometerRead = readAnemometer(&initialReading)))
    {
      onError("ERROR: Could not get initial wind reading");
    }
  }

  watchDog.checkin();
}

void watchDogTimeout()
{
  Serial.println("Watchdog timeout");
  delay(100); //Allow the Serial buffer to fully flush
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

char *serialize(Reading *reading)
{
  char *buffer = messageBuffer;
  buffer += 
    sprintf(
      buffer, 
      "%d:%f:%f:%f:%f|", 
      reading->version, 
      reading->batteryVoltage, 
      reading->batteryPercentage, 
      reading->panelVoltage, 
      reading->panelCurrent);
  if (reading->bmeRead)
  {
    buffer += sprintf(buffer, "b%f:%f:%f", reading->bmeTemperature, reading->pressure, reading->bmeHumidity);
  }
  if (reading->anemometerRead)
  {
    buffer += sprintf(buffer, "a%f:%d", reading->windSpeed, reading->windDirection);
  }
  if (reading->compassRead)
  {
    buffer += sprintf(buffer, "c%f:%f:%f", reading->compassReading.x, reading->compassReading.y, reading->compassReading.z);
  }
  return messageBuffer;
}

void setup()
{
  watchDog.checkin();

  //connect to the cloud once we have taken all our measurements
  Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
}

void loop()
{
  watchDog.checkin();

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
  else
  {
    Reading reading;
    reading.version = settings.version;

    readVoltage(&reading);

    if (!(reading.bmeRead = readBme280(&reading)))
    {
      onError("ERROR: BME280 temp/pressure sensor");
    }
    if (!(reading.compassRead = readCompass(&reading)))
    {
      onError("ERROR: Could not read compass");
    }
    if (!(reading.anemometerRead = readAnemometer(&reading)))
    {
      onError("ERROR: Could not read anemometer");
    }

    Particle.process();

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

    //watchDog.checkin();
    waitUntil(Particle.connected);
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
}
