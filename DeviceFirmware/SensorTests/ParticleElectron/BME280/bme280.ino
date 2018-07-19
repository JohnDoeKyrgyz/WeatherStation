/***************************************************************************
  This is a library for the BMP280 humidity, temperature & pressure sensor

  Designed specifically to work with the Adafruit BMEP280 Breakout 
  ----> http://www.adafruit.com/products/2651

  These sensors use I2C or SPI to communicate, 2 or 4 pins are required 
  to interface.

  Adafruit invests time and resources providing this open source code,
  please support Adafruit andopen-source hardware by purchasing products
  from Adafruit!

  Written by Limor Fried & Kevin Townsend for Adafruit Industries.  
  BSD license, all text above must be included in any redistribution
 ***************************************************************************/

#include <Wire.h>
#include <SPI.h>
#include <Adafruit_Sensor.h>
#include <Adafruit_BME280.h>
#include <Wire.h>

#define BMP_CS A2

Adafruit_BME280 bmpI2C;                 // I2C
Adafruit_BME280 bmpHardwareSpi(BMP_CS); // hardware SPI
//Adafruit_BME280 bmpSoftwareSpi(BMP_CS, BMP_MOSI, BMP_MISO, BMP_SCK);
Adafruit_BME280 *sensor;
String connectionType;

void setup()
{
  Serial.begin(9600);
  Serial.println(F("BME280 test"));

  Wire.begin();

  bool connected = false;
  while (!connected)
  {
    connectionType = "I2C";
    connected = bmpI2C.begin(0x76);
    sensor = &bmpI2C;
    if (!connected)
    {
      Serial.println("Could not connect with I2C");
      delay(1000);

      connectionType = "Hardware SPI";
      connected = bmpHardwareSpi.begin();
      sensor = &bmpHardwareSpi;
      Serial.println("Could not connect with Hardware SPI");
      delay(1000);
/*
      connected = bmpSoftwareSpi.begin();
      sensor = &bmpSoftwareSpi;
      Serial.println("Could not connect with Software SPI");
*/
      i2cScan();
    }
    delay(5000);
  }
}

void i2cScan()
{
  byte error, address;
  int nDevices;

  Serial.println("Scanning...");

  nDevices = 0;
  for (address = 1; address < 127; address++)
  {
    // The i2c_scanner uses the return value of
    // the Write.endTransmisstion to see if
    // a device did acknowledge to the address.
    Wire.beginTransmission(address);
    error = Wire.endTransmission();

    if (error == 0)
    {
      Serial.print("I2C device found at address 0x");
      if (address < 16)
        Serial.print("0");
      Serial.print(address, HEX);
      Serial.println("  !");

      nDevices++;
    }
    else if (error == 4)
    {
      Serial.print("Unknown error at address 0x");
      if (address < 16)
        Serial.print("0");
      Serial.println(address, HEX);
    }
  }
  if (nDevices == 0)
    Serial.println("No I2C devices found\n");
  else
    Serial.println("done\n");
}

void loop()
{
  Serial.println(connectionType);
  Serial.print("Temperature = ");
  Serial.print(sensor->readTemperature());
  Serial.println(" *C");

  Serial.print("Pressure = ");
  Serial.print(sensor->readPressure());
  Serial.println(" Pa");

  Serial.print("Approx altitude = ");
  Serial.print(sensor->readAltitude(1013.25)); // this should be adjusted to your local forcase
  Serial.println(" m");

  Serial.print("Humidity = ");
  Serial.print(sensor->readHumidity()); // this should be adjusted to your local forcase
  Serial.println(" %");

  Serial.println();
  delay(2000);
}