/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "Particle.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/Particle/src/Particle.ino"

void waitForConnection();
void publishStatusMessage(const char *message);
void onError(const char *message);
void initializePowerSettings();
void deepSleep(unsigned long seconds);
void onSettingsUpdate(const char *event, const char *data);
void beep(int duration);
void checkBrownout();
void connect();
bool initializeSensors();
bool readSensors();
bool selfTest();
void setup();
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/Particle/src/Particle.ino"
#define RBG_NOTIFICATIONS_OFF
#define FIRMWARE_VERSION "4.0"

#define ANEMOMETER_TRIES 10
#define POWER_MONITOR_TRIES 3
#define SEND_TRIES 30
#define MINIMUM_RUNTIME 5000 //milliseconds

#define PERIPHERAL_POWER D2
#define LED D7
#define BUZZER D6
#define ANEMOMETER A4

SYSTEM_THREAD(ENABLED);
SYSTEM_MODE(MANUAL);

#if PLATFORM_ID == 10 //ELECTRON
STARTUP(System.enableFeature(FEATURE_RETAINED_MEMORY));
#endif

#include <Wire.h>
#include "settings.h"
#include "Sensor.h"

PMIC pmic;
FuelGauge fuelGuage;

Settings settings;
unsigned long duration;

char messageBuffer[255];
char statusBuffer[255];
bool panelOn = true;
retained bool setupComplete;

CompassSensor compassSensor;
BatteryPower battery;
PanelPower panel;
Anemometer anemometer(ANEMOMETER, 2, ANEMOMETER_TRIES);
Barometer barometer;
//Sensor *sensors[] = {&panel, &barometer, &anemometer, &battery, &compassSensor};
Sensor *sensors[] = {&battery, &panel, &anemometer, &barometer};
const int sensorCount = sizeof(sensors) / sizeof(sensors[0]);
bool sensorReadResults[sensorCount];

void waitForConnection()
{
  if (!Particle.connected())
  {
    Serial.print("Waiting for connection...");
    waitUntil(Particle.connected);
    Serial.println("!");
  }
  Particle.process();
}

void publishStatusMessage(const char *message)
{
  Serial.print(message);
  Serial.print(" ");
  waitForConnection();
  int tries = 0;
  bool result;
  while (!(result = Particle.publish("Status", message, 60, PRIVATE, WITH_ACK)) && ++tries < SEND_TRIES)
  {
    Serial.print(".");
    delay(3000);
    Particle.process();
  }
  Serial.println();
  if (!result)
  {
    Serial.println("ERROR: Could not publish status message");
  }
}

void onError(const char *message)
{
  RGB.color(255, 255, 0);
  publishStatusMessage(message);
}

void initializePowerSettings()
{
  //Allow the PMIC to charge the battery from a solar panel
  pmic.begin();
  pmic.setInputVoltageLimit(5080);         //  for 6V Solar Panels
  pmic.setInputCurrentLimit(2000);         // 2000 mA, higher than req'd
  pmic.setChargeVoltage(4208);             //  Set Li-Po charge termination voltage to 4.21V,  Monitor the Enclosure Temps
  pmic.setChargeCurrent(0, 0, 1, 1, 1, 0); // 1408 mA [0+0+512mA+256mA+128mA+0] + 512 Offset
  pmic.enableDPDM();  
}

void deepSleep(unsigned long seconds)
{
  digitalWrite(LED, LOW);

  //Disable the RGB LED
  RGB.control(true);
  RGB.color(0, 0, 0);

  Serial.flush();
  fuelGuage.sleep();

  #if PLATFORM_ID == 13 //BORON
  if (seconds > 360)
  {
    System.sleep({}, RISING, seconds);
  }
  else
  {
    System.sleep({}, RISING, SLEEP_NETWORK_STANDBY, seconds);
  }
  #elif PLATFORM_ID == 10 //ELECTRON
  if(seconds > 900)
  {
    System.sleep(SLEEP_MODE_DEEP, seconds);
  }
  else
  {
    System.sleep(SLEEP_MODE_DEEP, seconds, SLEEP_NETWORK_STANDBY);
  }
  #endif

  initializePowerSettings();

  //return to the beginning of the LOOP function
  fuelGuage.wakeup();
  loop();
}

void onSettingsUpdate(const char *event, const char *data)
{
  digitalWrite(LED, HIGH);
  settings = deserialize(data);
  saveSettings(settings);

  Serial.print("SETTINGS UPDATE: ");
  Serial.println(data);

  beep(500);
  delay(500);
  beep(500);

  digitalWrite(LED, LOW);

  char *buffer = statusBuffer;
  sprintf(buffer, "SETTINGS %d", settings.version);
  publishStatusMessage(buffer);
}

void beep(int duration)
{
  digitalWrite(BUZZER, HIGH);
  delay(200);
  digitalWrite(BUZZER, LOW);
}

void checkBrownout()
{
  Serial.print("Checking brownout...");  
  float systemSoC = fuelGuage.getNormalizedSoC();
  bool brownout = settings.brownout && systemSoC < settings.brownoutPercentage;
  Serial.println("!");
  Serial.flush();

  if (brownout)
  {
    Serial.printlnf("Brownout threshold %f exceeded by system battery percentage %f", settings.brownoutPercentage, systemSoC);

    //long beep
    beep(3000);

    //signal the LED red and white
    RGB.control(true);
    for (int i = 0; i < 4; i++)
    {
      RGB.color(255, 0, 0); //red
      delay(100);
      RGB.color(255, 255, 255); //white
      delay(100);
    }
    RGB.control(false);

    deepSleep(settings.brownoutMinutes * 60);
  }
}

void connect()
{
  //begin connecting to the cloud
  Serial.println("Connecting...");
  Cellular.on();
  Cellular.connect();  
  Particle.connect();
  Particle.process();
}

bool initializeSensors()
{
  Wire.reset();
  
  bool result = true;
  for (auto sensor : sensors) 
  {
    result &= sensor->begin();
  }
  return result;
}

bool readSensors()
{
  char *buffer = messageBuffer;
  buffer += sprintf(buffer, "d%d", settings.version);

  int readSensors = 0;
  for (int i = 0; i < sensorCount; i++)
  {
    Sensor *sensor = sensors[i];
    bool read = sensor->getReading(buffer);
    sensorReadResults[i] = read;
    if(read)
    {
      readSensors++;
    }
  }
  return readSensors == sensorCount;
}

bool selfTest()
{
  Serial.println("Self Test...");
  bool result = readSensors();

  if (result)
  {
    beep(500);
  }
  else
  {
    //three short beeps
    for (int i = 0; i < 3; i++)
    {
      beep(250);
      delay(250);
    }
  }
  return result;
}

void setup()
{
  fuelGuage.begin();
  initializePowerSettings();
  
  Serial.begin(115200);

  //Turn off the peripherals to start
  pinMode(PERIPHERAL_POWER, OUTPUT);
  digitalWrite(PERIPHERAL_POWER, LOW);

  //if the user pressed reset, redo the setup
  int resetReason = System.resetReason();
  if(resetReason == RESET_REASON_PIN_RESET || resetReason == RESET_REASON_UPDATE){
    setupComplete = false;
  }

  Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);

  //short beep to indicate startup
  pinMode(BUZZER, OUTPUT);
  if(!setupComplete){
    beep(200);
    delay(5000);
  }  

  Serial.printlnf("WeatherStation %s", FIRMWARE_VERSION);

  //Load saved settings;
  Serial.print("Loaded settings...");
  settings = loadSettings();
  Serial.println("!");

  if(!setupComplete){
    beep(150);
    delay(150);
    beep(150);
  }  
}

void loop()
{
  duration = millis();
  
  checkBrownout();

  //signal LED if in Diagnostic Mode
  if (settings.diagnositicCycles > 0)
  {
    pinMode(LED, OUTPUT);
    digitalWrite(LED, HIGH);
    settings.diagnositicCycles = settings.diagnositicCycles - 1;
    saveSettings(settings);
  }

  //begin connecting to the cloud
  connect();

  digitalWrite(PERIPHERAL_POWER, HIGH);
  bool initialized = initializeSensors();

  bool selfTestSuccess = false;
  selfTestSuccess = setupComplete || (initialized && selfTest());

  if (initialized)
  {
    char *buffer = messageBuffer;
    buffer += sprintf(buffer, "d%d", settings.version);

    for (int i = 0; i < sensorCount; i++)
    {
      Sensor *sensor = sensors[i];
      sensorReadResults[i] = sensor->getReading(buffer);
    }    
  }

  digitalWrite(PERIPHERAL_POWER, LOW);

  //send serialized reading to the cloud
  waitForConnection();

  int successfulyReadSensors = 0;
  for (int i = 0; i < sensorCount; i++)
  {
    if (!sensorReadResults[i])
    {
      sprintf(statusBuffer, "ERROR: %s", sensors[i]->Name);
      onError(statusBuffer);
    }
    else
    {
      ++successfulyReadSensors;
    }
  }

  if(!setupComplete)
  {
    setupComplete = true;
    if (selfTestSuccess)
    {
      publishStatusMessage("START");
    }
    else
    {
      onError("SELF TEST FAIL");
    }
  }
  
  Serial.println(messageBuffer);

  if(initialized && successfulyReadSensors > 0)
  {
    int tries = SEND_TRIES;
    bool sentReading = false;
    do
    {
      Serial.print("Sending reading... ");
      sentReading = Particle.publish("Reading", messageBuffer, 60, PRIVATE, WITH_ACK);
      Serial.print(".");
    } while (!sentReading && --tries > 0);

    Serial.printlnf(" %s!", sentReading ? "+" : "-");
  }  

  Serial.printlnf("DIAGNOSTIC COUNT %d", settings.diagnositicCycles);
  digitalWrite(LED, LOW);

  //Publish a message if the panel starts or stops charging the battery
  if (panel.read())
  {
    bool charging = panel.voltage() >= fuelGuage.getVCell();
    if (charging && !panelOn)
    {
      publishStatusMessage("PANEL ON");
    }
    if (!charging)
    {
      publishStatusMessage("PANEL OFF");
      if (settings.panelOffMinutes > 0)
      {
        deepSleep(settings.panelOffMinutes * 60);
      }
    }
    panelOn = charging;
  }

  //sleep till the next reading
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
  Serial.printlnf("DURATION %d\n\n", millis() - duration);
  Serial.flush();

  Particle.process();

  //make sure that the device has run at least the MINIMUM_RUNTIME so that messages get to the server.
  unsigned long runTime = millis() - duration;
  if(runTime < MINIMUM_RUNTIME)
  {
    delay(MINIMUM_RUNTIME - runTime);
  }
  
  sleepAction();
}