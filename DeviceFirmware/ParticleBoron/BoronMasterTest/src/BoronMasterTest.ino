#include <Wire.h>

SYSTEM_MODE(SEMI_AUTOMATIC);

void blink()
{
  digitalWrite(D7, HIGH);
  delay(500);
  digitalWrite(D7, LOW);
}

void setup()
{
  Serial.begin(9600);
  Wire.begin();

  pinMode(D7, OUTPUT);

  Serial.println("Boron Master Test");
}

byte x = 0;

void loop()
{
  Wire.beginTransmission(8); // transmit to device #8
  Wire.write("x is ");       // sends five bytes
  Wire.write(x);             // sends one byte
  Wire.endTransmission();    // stop transmitting

  x++;

  blink();
  delay(500);

  Serial.println(".");
}