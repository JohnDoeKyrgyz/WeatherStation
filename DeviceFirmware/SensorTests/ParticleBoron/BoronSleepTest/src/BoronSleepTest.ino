SYSTEM_MODE(MANUAL);
SYSTEM_THREAD(ENABLED);
void setup()  { 
  Serial.begin(115200);
  pinMode(D7, OUTPUT);
  Serial.println("Start");
} 
void loop()   
{
  /*
  digitalWrite(D7, HIGH);
  delay(500);
  digitalWrite(D7, LOW);

  Serial.println("Timed Sleep");
  System.sleep( {}, {}, 30);         // Timed Sleep, 30 seconds
  Serial.println("Timed Wakeup");
  */

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
 }