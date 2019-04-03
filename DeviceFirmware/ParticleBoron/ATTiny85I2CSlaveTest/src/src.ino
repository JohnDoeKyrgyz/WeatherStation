#include <SoftwareSerial.h>

#define RX 3   // *** D3, Pin 2
#define TX 4   // *** D4, Pin 3
SoftwareSerial Serial(RX, TX);

#include "DebugMacros.h"
#include "Parameters.h"

#define I2C_SLAVE_ADDRESS 8
#include "TinyWireS.h"

#define LED_BUILTIN 1

void onReceiveEvent(uint8_t length)
{
    DEBPMSG("Received Message");
    DEBPVAR(length)
    
    for(int i = 0; i < length; i++)
    {
        Serial.print(i);
        Serial.print(" ");
        Serial.println(TinyWireS.receive());
    }

    blink();
}

// the setup function runs once when you press reset or power the board
void setup()
{
    TinyWireS.begin(I2C_SLAVE_ADDRESS);
    TinyWireS.onReceive(onReceiveEvent);

    // initialize digital pin LED_BUILTIN as an output.
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, LOW);

    DEBINIT // to be able to use debug output later on
    DEBPSTATUS  // print debug status

    blink();

    DEBPMSG("ATTiny I2C Slave Test");
}

void blink()
{
    digitalWrite(LED_BUILTIN, HIGH); // turn the LED on (HIGH is the voltage level)
    delay(500);
    digitalWrite(LED_BUILTIN, LOW); // turn the LED on (HIGH is the voltage level)
}

int loopCounter = 0;
#define LOOP_COUNT 1000
#define DOTS 80

// the loop function runs over and over again forever
void loop()
{   
    TinyWireS_stop_check();
    if(loopCounter++ == LOOP_COUNT)
    {
        Serial.print(".");
        if(loopCounter >= LOOP_COUNT * DOTS )
        {
            loopCounter = 0;
            Serial.println();
        }        
    }
}