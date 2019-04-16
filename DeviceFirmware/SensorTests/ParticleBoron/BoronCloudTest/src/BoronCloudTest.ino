int i = 0;
void onSettingsChanged(const char *event, const char *data)
{
  i++;
  Serial.print(i);
  Serial.print(event);
  Serial.print(", data: ");
  if (data)
    Serial.println(data);
  else
    Serial.println("NULL");
}

void setup() {
  Serial.begin(115200);

  Particle.subscribe("SETTINGS", onSettingsChanged, ALL_DEVICES);
  Particle.connect();
  Serial.printlnf("SETUP COMPLETE");  
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {
  Particle.publish("ORIENTATION_CHANGED", "1.0,2.0,3.0", PRIVATE);
  Serial.println(".");  
  delay(5000);
}