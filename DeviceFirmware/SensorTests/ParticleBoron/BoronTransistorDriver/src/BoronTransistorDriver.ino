SYSTEM_MODE(SEMI_AUTOMATIC);

#define SENSOR_POWER A4

void setup() {
  pinMode(SENSOR_POWER, OUTPUT);
  pinMode(D7, OUTPUT);
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {
  digitalWrite(SENSOR_POWER, HIGH);
  digitalWrite(D7, LOW);
  delay(1000);
  digitalWrite(SENSOR_POWER, LOW);
  digitalWrite(D7, HIGH);
  delay(1000);
}