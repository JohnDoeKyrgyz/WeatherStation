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

unsigned int x = 0;

void loop()
{
  Wire.beginTransmission(8);
  Wire.write(x); //sends the lower order byte
  Wire.write(x >> 8); //sends the higher order byte
  Wire.endTransmission();    // stop transmitting

  x += 200;

  blink();
  delay(500);

  Serial.println(".");
}