/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "Particle.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/Particle/I2CScanner/src/I2CScanner.ino"

void setup();
void loop();
#line 2 "c:/working/WeatherStation/DeviceFirmware/SensorTests/Particle/I2CScanner/src/I2CScanner.ino"
SYSTEM_MODE(MANUAL);

#include <Wire.h>

void setup()
{  
  Serial.begin(115200);
  
  Particle.disconnect();
  Cellular.off();

  //Drive the I2C devices off of D2, so that they can be turned off
  pinMode(D2, OUTPUT);
  digitalWrite(D2, HIGH);

  delay(10000);
  Serial.println("I2C Scanner");

  Wire.begin();
}

void loop()
{
  byte error, address;
  int nDevices;

  Serial.println("Scanning...");

  nDevices = 0;
  for(address = 1; address < 127; address++ ) 
  {
    Serial.print(".");

    Wire.reset();
    Wire.beginTransmission(address);
    error = Wire.endTransmission();

    if (error == 0)
    {
      Serial.print("\nI2C device found at address 0x");
      if (address<16) Serial.print("0");
      Serial.print(address,HEX);
      Serial.println("\n  !");

      nDevices++;
    }
    else if (error==4) 
    {
      Serial.print("\nUnknow error at address 0x");
      if (address<16) Serial.print("0");
      Serial.println(address,HEX);
    }    
  }
  if (nDevices == 0)
    Serial.println("\nNo I2C devices found\n");
  else
    Serial.println("\ndone\n");

  delay(5000);           // wait 5 seconds for next scan
}