SYSTEM_MODE(SEMI_AUTOMATIC);

#include <Wire.h>

void setup()
{
  Wire.begin();

  Serial.begin(9600);
  Serial.println("\nI2C Scanner");
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