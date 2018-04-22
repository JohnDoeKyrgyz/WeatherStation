/**
 * Battery Voltage Monitor
 * https://arduinodiy.wordpress.com/2016/12/25/monitoring-lipo-battery-voltage-with-wemos-d1-minibattery-shield-and-thingspeak/
 * */

void setup()
{
    Serial.begin(115200);
    printf("Initializing...\r\n");

    pinMode(A0, INPUT);
    pinMode(LED_BUILTIN, OUTPUT);
}

void loop()
{
    digitalWrite(LED_BUILTIN, HIGH);
	int raw = analogRead(A0);
    float volt = raw / 1023.0;
    volt = volt * 3.3;

    printf("Raw = %d, Volt = %f\r\n", raw, volt);

    digitalWrite(LED_BUILTIN, LOW);
    
    delay(200);
}
