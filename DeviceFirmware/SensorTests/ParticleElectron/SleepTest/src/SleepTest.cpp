/******************************************************/
//       THIS IS A GENERATED FILE - DO NOT EDIT       //
/******************************************************/

#include "Particle.h"
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleElectron/SleepTest/src/SleepTest.ino"
void setup();
void loop();
#line 1 "c:/working/WeatherStation/DeviceFirmware/SensorTests/ParticleElectron/SleepTest/src/SleepTest.ino"
FuelGauge fuel;
void setup()
{
  Serial.begin(115200);
}

void loop()
{
  char buffer[100];
  snprintf(buffer, sizeof buffer, "Voltage: %f, SoC %f, NormalizedSoc %f", fuel.getVCell(), fuel.getSoC(), fuel.getNormalizedSoC());
  Serial.println(buffer);

  Particle.publish("READING", buffer, PRIVATE);

  //deep sleep
  #if PLATFORM_ID == 10 //ELECTRON  
  SystemSleepConfiguration config;
  auto sleepDuration = 10s;
  config
    .mode(SystemSleepMode::HIBERNATE)
    .gpio(WKP, RISING)
    .duration(sleepDuration);    
  System.sleep(config);
  #elif PLATFORM_ID == 13 //BORON
  System.sleep({}, RISING, SLEEP_NETWORK_STANDBY, 10);
  #endif
}