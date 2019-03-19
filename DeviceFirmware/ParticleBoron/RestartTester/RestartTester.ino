#include <Wire.h>

#define SIGNAL_PIN 2

volatile unsigned long triggeredTime = 0;
void onResetTriggered()
{
    triggeredTime = millis();
}

void setup()
{
    Wire.begin(); // join i2c bus (address optional for master)

    pinMode(SIGNAL_PIN, INPUT);
    uint8_t interrupt = digitalPinToInterrupt(SIGNAL_PIN);
    attachInterrupt(interrupt, onResetTriggered, RISING);
    
    Serial.begin(9600);
    Serial.println("Restart Tester");
}


void loop()
{
    Serial.println("Enter sleepTime");
    while(!Serial.available())
    {
        delay(500);
    }
    int sleepTimeRequest = Serial.parseInt();
    Serial.print("Sleeping for ");
    Serial.print(sleepTimeRequest);
    Serial.println(" milliseconds");
    
    Wire.beginTransmission(0x4);
    Wire.write(sleepTimeRequest);
    Wire.endTransmission();

    unsigned long sleepStartTime = millis();

    while(triggeredTime == 0)
    {
        Serial.print(".");
        delay(1000);        
    }
    Serial.println();
    
    unsigned long sleepTime = triggeredTime - sleepStartTime;
    Serial.print("Slept for ");
    Serial.print(sleepTime);
    Serial.println(" milliseconds\n");    
}
