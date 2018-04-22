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
    volt = volt * 4.2;

    printf("Raw = %d, Volt = %f\r\n", raw, volt);

    delay(500);
    digitalWrite(LED_BUILTIN, LOW);
    
    delay(1000);
}
