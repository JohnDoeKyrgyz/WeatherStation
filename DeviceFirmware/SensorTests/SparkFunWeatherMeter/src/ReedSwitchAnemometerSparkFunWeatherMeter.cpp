/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "Particle.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/SparkFunWeatherMeter/src/ReedSwitchAnemometerSparkFunWeatherMeter.ino"
void onAnemometerTick();
void setup();
void loop();
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/SparkFunWeatherMeter/src/ReedSwitchAnemometerSparkFunWeatherMeter.ino"
#define ANEMOMETER D1
#define POWER D2

SYSTEM_MODE(MANUAL);

volatile int anemometerTicks = 0;
volatile long lastWindTime = 0L;

void onAnemometerTick() 
{
  //Ignore switch - bounc glitches less than 10 ms
  long time = millis();
  if(time - lastWindTime > 10)
  {
    lastWindTime = time;
    anemometerTicks++;
  }  
}

void setup() 
{
  Serial.begin(115200);

  pinMode(POWER, OUTPUT);
  digitalWrite(POWER, LOW);

  pinMode(ANEMOMETER, INPUT_PULLUP);
  attachInterrupt(ANEMOMETER, onAnemometerTick, FALLING);
}

void loop() 
{
  delay(1000);
  noInterrupts();

  float windSpeed = 1.428 * anemometerTicks;

  Serial.print(anemometerTicks);
  Serial.print(" ");
  Serial.println(windSpeed);

  anemometerTicks = 0;

  interrupts();
}