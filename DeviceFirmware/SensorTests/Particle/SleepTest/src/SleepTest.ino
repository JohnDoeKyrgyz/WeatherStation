//SYSTEM_MODE(MANUAL);
//SYSTEM_THREAD(ENABLED);

FuelGauge fuel;
void setup()
{
  Serial.begin(115200);
  Serial.println("Setup!");
}

void loop()
{
  Serial.println("Loop!");

  char buffer[100];
  snprintf(buffer, sizeof buffer, "Voltage: %f, SoC %f, NormalizedSoc %f", fuel.getVCell(), fuel.getSoC(), fuel.getNormalizedSoC());
  Serial.println(buffer);

  //Particle.publish("READING", buffer, PRIVATE);

  Serial.println("Light Sleep");

  //Light Sleep
  #if PLATFORM_ID == 10 //ELECTRON
  System.sleep(SLEEP_MODE_SOFTPOWEROFF, 5, SLEEP_NETWORK_STANDBY);

  /*
  auto sleepDuration = 5s;
  SystemSleepConfiguration config;
  config
    .mode(SystemSleepMode::STOP)
    .gpio(WKP, RISING)
    .duration(sleepDuration);    
  System.sleep(config);
  */
  #elif PLATFORM_ID == 13 //BORON
  System.sleep({}, RISING, 5);
  #endif

  Serial.println("Waking up!");
  Serial.println("Deep Sleep");

  //Deep Sleep
  #if PLATFORM_ID == 10 //ELECTRON  
  /*
  config
    .mode(SystemSleepMode::HIBERNATE)
    .gpio(WKP, RISING)
    .duration(sleepDuration);    
  System.sleep(config);
  */
  System.sleep(SLEEP_MODE_SOFTPOWEROFF,5);
  #elif PLATFORM_ID == 13 //BORON
  System.sleep({}, RISING, SLEEP_NETWORK_STANDBY, 5);
  #endif
}