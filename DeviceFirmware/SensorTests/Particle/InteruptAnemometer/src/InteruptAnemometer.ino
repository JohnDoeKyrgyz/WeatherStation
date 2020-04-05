SYSTEM_MODE(MANUAL);

#define DEVICE_POWER D2

uint32_t times[10000];

void setup() {
  Serial.begin(115200);

  pinMode(DEVICE_POWER, OUTPUT);
  digitalWrite(DEVICE_POWER, HIGH);

  Serial.println("Anemometer Test");
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {


  digitalWrite(A4,LOW);
	pinMode(A4,OUTPUT);
	delay(500);
	pinMode(A4,INPUT);
	pulseIn(A4,LOW);

  unsigned long startTime = micros();
  unsigned long currentTime = 0L;
  int readingIndex = 0;
  PinState waitForState = HIGH;
  while((currentTime = micros() - startTime) < 50000){
    times[readingIndex++] = pulseIn(A4,waitForState);
    waitForState = waitForState == HIGH ? LOW : HIGH;
  }

  for(int i = 0; i <= readingIndex; i++)
  {
    Serial.println(times[i]);
  }

  Serial.println();
}