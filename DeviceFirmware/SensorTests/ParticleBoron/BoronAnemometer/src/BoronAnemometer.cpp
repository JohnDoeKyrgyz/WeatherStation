#include "application.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleBoron/BoronAnemometer/src/BoronAnemometer.ino"
void setup();
void loop();
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleBoron/BoronAnemometer/src/BoronAnemometer.ino"
SYSTEM_MODE(SEMI_AUTOMATIC);


#include <LaCrosse_TX23.h>

#define ANEMOMETER A4

LaCrosse_TX23 laCrosseTX23(ANEMOMETER);

void setup() {
  Serial.begin(115200);
  Serial.println("Anemometer test");  
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {
  float windSpeed;
  int windDirection;
  bool result = laCrosseTX23.read(windSpeed, windDirection);

  Serial.printlnf("%d, %f, %d", result, windSpeed, windDirection);
}