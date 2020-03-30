SYSTEM_MODE(MANUAL);
SYSTEM_THREAD(ENABLED);

FuelGauge fuelGuage;

void setup()  { 
  Serial.begin(115200);
  pinMode(D7, OUTPUT);

  fuelGuage.begin();

  Serial.println("Start");
} 
void loop()   
{
  digitalWrite(D7, HIGH);
  delay(500);
  digitalWrite(D7, LOW);

  Serial.println(fuelGuage.getSoC());

  Serial.println("Timed Sleep");
  fuelGuage.sleep();
  System.sleep( {}, RISING, 15);         // Timed Sleep, 30 seconds
  Serial.println("Timed Wakeup");

  Serial.println(fuelGuage.getSoC());
  
  /*
  digitalWrite(D7, HIGH);
  delay(500);
  digitalWrite(D7, LOW);

  Cellular.on(); 
  delay(5000); 
  Cellular.off(); 
  delay(5000); 
  
  Serial.println("Deep Sleep");
  System.sleep(SLEEP_MODE_DEEP);     // Deep Sleep
  Serial.println("Deep Wakeup");
  */
 }