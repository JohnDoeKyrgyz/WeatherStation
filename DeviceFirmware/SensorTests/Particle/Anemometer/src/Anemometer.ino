SYSTEM_MODE(MANUAL);

#include <LaCrosse_TX23.h>

#define ANEMOMETER A4
#define DEVICE_POWER D2

LaCrosse_TX23 laCrosseTX23(ANEMOMETER);

void setup() {
  Serial.begin(115200);
  Serial.println("Anemometer test");

  pinMode(DEVICE_POWER, OUTPUT);
  digitalWrite(DEVICE_POWER, HIGH);
}

// loop() runs over and over again, as quickly as it can execute.
void loop() {
  float windSpeed;
  int windDirection;

  bool result;
  SINGLE_THREADED_BLOCK() {
    result = laCrosseTX23.read(windSpeed, windDirection);
  }

  Serial.printlnf("%d, %f, %d", result, windSpeed, windDirection);
}