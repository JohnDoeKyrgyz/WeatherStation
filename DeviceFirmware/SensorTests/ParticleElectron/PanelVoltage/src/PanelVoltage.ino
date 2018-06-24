
#define PANEL_VOLTAGE A0
#define SENSOR_POWER C0

void setup()
{
    Serial.begin(115200);
    pinMode(SENSOR_POWER, OUTPUT);    
}
void loop()
{
    digitalWrite(SENSOR_POWER, HIGH);

    delay(1000);

    int voltage = analogRead(PANEL_VOLTAGE);
    Serial.print("Panel Voltage ");
    Serial.println(voltage);
    digitalWrite(SENSOR_POWER, LOW);

    delay(2000);
}