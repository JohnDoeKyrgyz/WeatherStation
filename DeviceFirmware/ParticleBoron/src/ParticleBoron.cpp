#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"

void watchDogTimeout();
void setup();
void printSystemInfo();
void deepSleep(unsigned int milliseconds);
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
SYSTEM_MODE(SEMI_AUTOMATIC);

#include <Wire.h>
#include "Compass.h"

FuelGauge fuel;
PMIC pmic;
Compass compassSensor;

ApplicationWatchdog wd(20000, watchDogTimeout);

void watchDogTimeout()
{
  Serial.println("Watchdog timeout");
  System.reset();
}

void setup()
{
  pinMode(LED_BUILTIN, OUTPUT);

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
  Serial.println(fuel.getSoC());

  Serial.print("Voltage: ");
  Serial.println(fuel.getVCell());

  /*
  Serial.println("Compass initializing");
  compassSensor = Compass();
  Serial.println(compassSensor.begin());
  Serial.println("Compass initialed");

  CompassReading reading = compassSensor.getReading();
  Serial.printlnf("X: %d, Y: %d, Z: %d", reading.x, reading.y, reading.z);
  
  */
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

  fuel.sleep();
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
