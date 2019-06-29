/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleBoron/BoronINA219/src/BoronBME280.ino"
void setup(void);
void loop(void);
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleBoron/BoronINA219/src/BoronBME280.ino"
SYSTEM_MODE(MANUAL);

#include "adafruit-ina219.h"

Adafruit_INA219 ina219;

void setup(void) 
{    
  Serial.begin(115200);
  delay(10000);

  Serial.println("Hello!");
  
  // Initialize the INA219.P
  // By default the initialization will use the largest range (32V, 2A).  However
  // you can call a setCalibration function to change this range (see comments).
  Serial.println("begin");
  ina219.begin();
  // To use a slightly lower 32V, 1A range (higher precision on amps):
  //ina219.setCalibration_32V_1A();
  // Or to use a lower 16V, 400mA range (higher precision on volts and amps):
  //ina219.setCalibration_16V_400mA();

  Serial.println("Measuring voltage and current with INA219 ...");
}

void loop(void) 
{
  float shuntvoltage = 0;
  float busvoltage = 0;
  float current_mA = 0;
  float loadvoltage = 0;

  shuntvoltage = ina219.getShuntVoltage_mV();
  busvoltage = ina219.getBusVoltage_V();
  current_mA = ina219.getCurrent_mA();
  loadvoltage = busvoltage + (shuntvoltage / 1000);
  
  Serial.print("Bus Voltage:   "); Serial.print(busvoltage); Serial.println(" V");
  Serial.print("Shunt Voltage: "); Serial.print(shuntvoltage); Serial.println(" mV");
  Serial.print("Load Voltage:  "); Serial.print(loadvoltage); Serial.println(" V");
  Serial.print("Current:       "); Serial.print(current_mA); Serial.println(" mA");
  Serial.println("");

  delay(2000);
}
