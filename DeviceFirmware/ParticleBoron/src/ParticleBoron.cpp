/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"

void publishStatusMessage(const char* message);
void onError(const char *message);
void watchDogTimeout();
void deepSleep(unsigned long seconds);
void onSettingsUpdate(const char *event, const char *data);
void setup();
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
#define RBG_NOTIFICATIONS_OFF
#define FIRMWARE_VERSION "1.0"

#define ANEMOMETER_TRIES 5
#define SEND_TRIES 3
#define WATCHDOG_TIMEOUT 120000 //milliseconds

#define LED D7
#define ANEMOMETER A4

SYSTEM_THREAD(ENABLED);
SYSTEM_MODE(SEMI_AUTOMATIC);

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
Compass compassSensor;

ApplicationWatchdog watchDog(WATCHDOG_TIMEOUT, watchDogTimeout);

Settings settings;
unsigned long duration;
bool brownout;

struct Reading
{
  unsigned int version;
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
float systemSoC;

void publishStatusMessage(const char* message){  
  Serial.println(message);
  waitUntil(Particle.connected);
  Particle.publish("Status", message, 60, PRIVATE, WITH_ACK);
  Particle.process();
}

void onError(const char *message)
{
  RGB.color(255, 255, 0);
  publishStatusMessage(message);
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

void watchDogTimeout()
{
  publishStatusMessage("WATCHDOG_TIMEOUT");

  Serial.flush();
  System.reset();
}

void deepSleep(unsigned long seconds)
{
  Serial.printlnf("Deep Sleep for %d seconds. Brownout = %d, SoC = %f, settings.diagnositicCycles = %d", seconds, brownout, systemSoC, settings.diagnositicCycles);

  Serial.flush();
  fuelGuage.sleep();
  watchDog.dispose();

  if(seconds > 360)
  {
    System.sleep({}, RISING, seconds);
  }
  else
  {
    System.sleep({}, RISING, SLEEP_NETWORK_STANDBY, seconds);
  }

  Serial.println("Wakeup"); 

  //Disable the RGB LED
  RGB.control(true);
  RGB.color(0, 0, 0);  
}

void onSettingsUpdate(const char *event, const char *data)
{
  digitalWrite(LED, HIGH);
  settings = deserialize(data);
  saveSettings(settings);

  Serial.print("SETTINGS UPDATE: ");
  Serial.println(data);
  digitalWrite(LED, LOW);

  char *buffer = messageBuffer;
  sprintf(buffer, "SETTINGS %d", settings.version);
  publishStatusMessage(buffer);
}

char *serialize(Reading *reading)
{
  char *buffer = messageBuffer;
  buffer += 
    sprintf(
      buffer, 
      "%df%f:%fp%f:%f", 
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
  Serial.begin(115200);

  duration = millis();
  Serial.printlnf("WeatherStation %s", FIRMWARE_VERSION);

  //don't send reset info. This will just take up all our bandwith since we are using a deep sleep
  System.disable(SYSTEM_FLAG_PUBLISH_RESET_INFO);

  //connect to the cloud once we have taken all our measurements
  Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);
  
  //begin connecting to the cloud
  Serial.println("Connecting...");
  Cellular.on();
  Cellular.connect();
  Particle.connect();
  Particle.process();

  //Load saved settings;
  Serial.print("Loaded settings...");
  settings = loadSettings();
  Serial.println("!");

  Serial.print("Checking brownout...");
  fuelGuage.begin();
  systemSoC = fuelGuage.getSoC();
  brownout = settings.brownout && systemSoC < settings.brownoutPercentage;
  Serial.println("!");

  if(!brownout)
  {
    Serial.print("Initializing sensors...");    
    powerMonitor.begin();
    bme280.begin(0x76);
    compassSensor.begin();
    Serial.println("!");
    
    if (settings.diagnositicCycles > 0)
    {
      pinMode(LED, OUTPUT);
      digitalWrite(LED, HIGH);
      settings.diagnositicCycles = settings.diagnositicCycles - 1;

      saveSettings(settings);
    }

    //take an initial wind reading
    if (!(initialReading.anemometerRead = readAnemometer(&initialReading)))
    {
      onError("ERROR: Could not get initial wind reading");
    }
  }  
}

void loop()
{
  watchDog.checkin();
  Particle.process();

  if (brownout)
  {
    Serial.printlnf("Brownout threshold %f exceeded by system battery percentage %f", settings.brownoutPercentage, systemSoC);
    Particle.process();

    char *buffer = messageBuffer;
    sprintf(buffer, "BROWNOUT %f:%d", systemSoC, settings.brownoutMinutes);
    
    publishStatusMessage(buffer);

    delay(4000);
    Particle.process();

    deepSleep(settings.brownoutMinutes * 60);
  }
  else
  {
    Reading reading;
    reading.version = settings.version;

    Serial.println("Reading...");
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

    Serial.print("Waiting for connection...");
    watchDog.checkin();
    waitUntil(Particle.connected);
    Serial.println("!");

    Particle.process();

    int tries = SEND_TRIES;
    bool sentReading = false;
    do
    {
      watchDog.checkin();    
      Serial.print("Sending reading... ");
      sentReading = Particle.publish("Reading", publishedReading, 60, PRIVATE, WITH_ACK);
      Serial.print(".");
    }
    while(!sentReading && --tries > 0);

    Serial.printlnf(" %s!", sentReading ? "+" : "-");    

    //Allow particle to process before going into deep sleep
    Particle.process();

    Serial.printlnf("DIAGNOSTIC COUNT %d", settings.diagnositicCycles);
    digitalWrite(LED, LOW);

    void (*sleepAction)();
    const char *sleepMessage;
    if (settings.useDeepSleep)
    {
      sleepMessage = "DEEP";
      sleepAction = []() {
        deepSleep(settings.sleepTime);
      };
    }
    else
    {
      sleepMessage = "LIGHT";
      sleepAction = []() {
        delay(settings.sleepTime);
        setup();
      };
    }

    Serial.printlnf("%s SLEEP %d", sleepMessage, settings.sleepTime);
    Serial.printlnf("DURATION %d", millis() - duration);

    Particle.process();

    Serial.flush(); 
    sleepAction();
  }
}
