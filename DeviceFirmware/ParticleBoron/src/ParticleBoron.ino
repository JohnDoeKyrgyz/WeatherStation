
#define RBG_NOTIFICATIONS_OFF
#define FIRMWARE_VERSION "2.0"

#define ANEMOMETER_TRIES 5
#define POWER_MONITOR_TRIES 3
#define SEND_TRIES 30
#define WATCHDOG_TIMEOUT 120000 //milliseconds

#define PERIPHERAL_POWER D2
#define LED D7
#define BUZZER D6
#define ANEMOMETER A4

#define CHARGE_CURRENT_LOW_THRESHOLD 1.0
#define CHARGE_CURRENT_HIGH_THRESHOLD 400.0

SYSTEM_THREAD(ENABLED);
SYSTEM_MODE(MANUAL);

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
bool firstLoop = true;

CompassSensor compassSensor;
BatteryPower battery;
PanelPower panel;
Anemometer anemometer(ANEMOMETER, ANEMOMETER_TRIES);
Barometer barometer;
Sensor *sensors[] = {&panel, &barometer, &anemometer, &battery, &compassSensor};

void waitForConnection()
{
  if (!Particle.connected)
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

void watchDogTimeout()
{
  publishStatusMessage("WATCHDOG_TIMEOUT");
  Serial.flush();
  System.reset();
}

void deepSleep(unsigned long seconds)
{
  digitalWrite(LED, LOW);

  //Disable the RGB LED
  RGB.control(true);
  RGB.color(0, 0, 0);

  Serial.flush();
  fuelGuage.sleep();

  if (seconds > 360)
  {
    System.sleep({}, RISING, seconds);
  }
  else
  {
    System.sleep({}, RISING, SLEEP_NETWORK_STANDBY, seconds);
  }

  //return to the beginning of the LOOP function
  loop();
}

void onSettingsUpdate(const char *event, const char *data)
{
  digitalWrite(LED, HIGH);
  settings = deserialize(data);
  saveSettings(settings);

  Serial.print("SETTINGS UPDATE: ");
  Serial.println(data);
  digitalWrite(LED, LOW);

  char *buffer = statusBuffer;
  sprintf(buffer, "SETTINGS %d", settings.version);
  publishStatusMessage(buffer);
}

void setup()
{
  //Allow the PMIC to charge the battery from a solar panel
  pmic.begin();
  pmic.setInputVoltageLimit(5080);         //  for 6V Solar Panels
  pmic.setInputCurrentLimit(2000);         // 2000 mA, higher than req'd
  pmic.setChargeVoltage(4208);             //  Set Li-Po charge termination voltage to 4.21V,  Monitor the Enclosure Temps
  pmic.setChargeCurrent(0, 0, 1, 1, 1, 0); // 1408 mA [0+0+512mA+256mA+128mA+0] + 512 Offset
  pmic.enableDPDM();

  Particle.subscribe("Settings", onSettingsUpdate, MY_DEVICES);

  Serial.begin(115200);
  Serial.printlnf("WeatherStation %s", FIRMWARE_VERSION);

  //Load saved settings;
  Serial.print("Loaded settings...");
  settings = loadSettings();
  Serial.println("!");

  //Turn off the peripherals to start
  pinMode(PERIPHERAL_POWER, OUTPUT);
  digitalWrite(PERIPHERAL_POWER, LOW);

  //Configure the buzzer for output
  pinMode(BUZZER, OUTPUT);
  digitalWrite(BUZZER, LOW);
}

void checkBrownout()
{
  Serial.print("Checking brownout...");
  fuelGuage.begin();
  float systemSoC = fuelGuage.getSoC();
  bool brownout = settings.brownout && systemSoC < settings.brownoutPercentage;
  Serial.println("!");
  Serial.flush();

  if (brownout)
  {
    Serial.printlnf("Brownout threshold %f exceeded by system battery percentage %f", settings.brownoutPercentage, systemSoC);

    if (Particle.connected())
    {
      Particle.process();

      char *buffer = statusBuffer;
      sprintf(buffer, "BROWNOUT %f:%d", systemSoC, settings.brownoutMinutes);
      publishStatusMessage(buffer);
    }

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
  bool result = true;
  for (auto sensor : sensors)
    result &= sensor->begin();
  return result;
}

bool selfTest()
{
  Serial.println("Self Test...");

  bool result = true;
  char *buffer = messageBuffer;
  for (auto sensor : sensors)
  {
    bool success = sensor->getReading(buffer);
    if (!success)
    {
      Serial.printlnf("Could not read %s", sensor->Name);
    }
    result &= success;
  }

  if (result)
  {
    Serial.println("Self test SUCCESS\n");

    //one long buzz
    pinMode(BUZZER, OUTPUT);
    digitalWrite(BUZZER, HIGH);
    delay(500);
    digitalWrite(BUZZER, LOW);
    pinMode(BUZZER, INPUT);
  }
  else
  {
    Serial.println("Self test FAIL\n");

    //three short beeps
    for (int i = 0; i < 3; i++)
    {
      pinMode(BUZZER, OUTPUT);
      digitalWrite(BUZZER, HIGH);
      delay(250);
      digitalWrite(BUZZER, LOW);
      delay(250);
      pinMode(BUZZER, INPUT);
    }
  }

  return result;
}

void loop()
{
  duration = millis();
  ApplicationWatchdog watchDog = ApplicationWatchdog(WATCHDOG_TIMEOUT, watchDogTimeout);

  checkBrownout();

  connect();

  //signal LED if in Diagnostic Mode
  if (settings.diagnositicCycles > 0)
  {
    pinMode(LED, OUTPUT);
    digitalWrite(LED, HIGH);
    settings.diagnositicCycles = settings.diagnositicCycles - 1;
    saveSettings(settings);
  }

  digitalWrite(PERIPHERAL_POWER, HIGH);

  if (initializeSensors())
  {
    if (firstLoop)
    {
      if (selfTest())
      {
        publishStatusMessage("START");
      }
      else
      {
        onError("SELF TEST FAIL");
      }
      firstLoop = false;
    }

    char *buffer = messageBuffer;
    int sensorCount = sizeof(sensors) / sizeof(sensors[0]);
    bool results[sensorCount];
    for (int i = 0; i < sensorCount; i++)
    {
      Sensor *sensor = sensors[i];
      results[i] = sensor->getReading(buffer);
    }

    for (int i = 0; i < sensorCount; i++)
    {
      if (!results[i])
      {
        sprintf(statusBuffer, "ERROR: %s", sensors[i]->Name);
        onError(statusBuffer);
      }
    }
  }

  digitalWrite(PERIPHERAL_POWER, LOW);

  //send serialized reading to the cloud
  waitForConnection();

  int tries = SEND_TRIES;
  bool sentReading = false;
  do
  {
    watchDog.checkin();
    Serial.print("Sending reading... ");
    sentReading = Particle.publish("Reading", messageBuffer, 60, PRIVATE, WITH_ACK);
    Serial.print(".");
  } while (!sentReading && --tries > 0);

  Serial.printlnf(" %s!", sentReading ? "+" : "-");

  Serial.printlnf("DIAGNOSTIC COUNT %d", settings.diagnositicCycles);
  digitalWrite(LED, LOW);

  //Publish a message if the panel starts or stops charging the battery
  if (panel.read())
  {
    bool charging =
        panel.current() >= CHARGE_CURRENT_LOW_THRESHOLD && panel.current() <= CHARGE_CURRENT_HIGH_THRESHOLD;
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
  Serial.printlnf("DURATION %d", millis() - duration);

  Particle.process();
  Serial.flush();

  sleepAction();
}