#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
#include "Compass.h"

void setup();
void printSystemInfo();
void loop();
#line 3 "c:/working/WeatherStation/DeviceFirmware/ParticleBoron/src/ParticleBoron.ino"
int led1 = D0; // Instead of writing D0 over and over again, we'll write led1
// You'll need to wire an LED to this one to see it blink.

int led2 = D7; // Instead of writing D7 over and over again, we'll write led2

FuelGauge fuel;
PMIC pmic;
Compass *compass;

void setup()
{

  // We are going to tell our device that D0 and D7 (which we named led1 and led2 respectively) are going to be output
  // (That means that we will be sending voltage to them, rather than monitoring voltage that comes from them)

  // It's important you do this here, inside the setup() function rather than outside it or in the loop function.

  pinMode(led1, OUTPUT);
  pinMode(led2, OUTPUT);

  Serial.begin(9600);

  Serial.println("Compass initializing");
  compass = new Compass(1234);
  Serial.println("Compass initialed");
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

  CompassReading reading = compass->getReading();
  Serial.printlnf("X: %d, Y: %d, Z: %d", reading.x, reading.y, reading.z);
}

// Next we have the loop function, the other essential part of a microcontroller program.
// This routine gets repeated over and over, as quickly as possible and as many times as possible, after the setup function is called.
// Note: Code that blocks for too long (like more than 5 seconds), can make weird things happen (like dropping the network connection).  The built-in delay function shown below safely interleaves required background activity, so arbitrarily long delays can safely be done if you need them.

void loop()
{
  // To blink the LED, first we'll turn it on...
  digitalWrite(led1, HIGH);
  digitalWrite(led2, HIGH);

  // We'll leave it on for 1 second...
  delay(1000);

  // Then we'll turn it off...
  digitalWrite(led1, LOW);
  digitalWrite(led2, LOW);

  // And repeat!
  printSystemInfo();

  delay(2000);
}
