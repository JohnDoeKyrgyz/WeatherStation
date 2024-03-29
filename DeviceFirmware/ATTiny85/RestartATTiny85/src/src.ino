#include <avr/sleep.h>
#include <math.h>
#include <SoftwareSerial.h>

#define RX 3   // *** D3, Pin 2
#define TX 4   // *** D4, Pin 3
SoftwareSerial Serial(RX, TX);

#include "DebugMacros.h"
#include "Parameters.h"

#define I2C_SLAVE_ADDRESS 8
#include "TinyWireS.h"

#define MAX_LISTEN_TIME 360000 /* 6 minutes */

// Utility macros
#define adc_disable() (ADCSRA &= ~(1 << ADEN)) // disable ADC (before power-off)

#define LED_BUILTIN 1

//table of the time increments in milliseconds that the ATtiny85 watchdog can sleep
unsigned long prescales[] = {16, 32, 64, 128, 250, 500, 1000, 2000, 4000, 8000};

unsigned long requestedSleepTime = 0;
void onReceiveEvent(uint8_t length)
{
    DEBPMSG("Received Message");
    DEBPVAR(length)

    if (requestedSleepTime == 0 && length == 4)
    {        
        //get the last two bytes in the receive buffer, and concatenate them into an int
        uint8_t a = TinyWireS.receive();
        uint8_t b = TinyWireS.receive();
        uint8_t c = TinyWireS.receive();
        uint8_t d = TinyWireS.receive();
        requestedSleepTime = a | (b << 8) | (c << 16) | (d << 24);
        requestedSleepTime = requestedSleepTime * 1000;
    }
    DEBPVAR(requestedSleepTime)
}

// the setup function runs once when you press reset or power the board
void setup()
{
    adc_disable();
    set_sleep_mode(SLEEP_MODE_PWR_DOWN);
    
    TinyWireS.begin(I2C_SLAVE_ADDRESS);
    TinyWireS.onReceive(onReceiveEvent);

    // initialize digital pin LED_BUILTIN as an output.
    pinMode(LED_BUILTIN, OUTPUT);
    digitalWrite(LED_BUILTIN, HIGH);

    Serial.begin(9600);

    DEBPSTATUS  // print debug status
    
    Serial.println("Restart Timer");
}

void setup_watchdog(int timerPrescaler)
{
    byte bb = timerPrescaler & 7;
    if (timerPrescaler > 7) bb |= (1 << 5); //Set the special 5th bit if necessary

    cli(); //disable interrupts

    //This order of commands is important and cannot be combined
    MCUSR &= ~(1 << WDRF);             //Clear the watch dog reset
    WDTCR |= (1 << WDCE) | (1 << WDE); //Set WD_change enable, set WD enable
    WDTCR = bb;                        //Set new watchdog timeout value
    WDTCR |= _BV(WDIE);                //Set the interrupt enable, this will keep unit from resetting after each int

    sei(); //re-enable interrupts
}

volatile unsigned int timerCounter;
unsigned int timerPrescaler;

//This runs each time the watch dog wakes us up from sleep
ISR(WDT_vect)
{
    timerCounter--;
    DEBPVAR(timerCounter)
}

void sleep(unsigned long milliseconds)
{
    timerPrescaler = 9;
    while(prescales[timerPrescaler] > milliseconds && timerPrescaler > 0) timerPrescaler--;
    unsigned long prescale = prescales[timerPrescaler];
    
    timerCounter = milliseconds / prescale;
    unsigned long remainder = milliseconds - (timerCounter * prescale);

    DEBPMSG("SLEEP CALCULATION");
    DEBPVAR(milliseconds)
    DEBPVAR(prescale)
    DEBPVAR(timerCounter)
    DEBPVAR(remainder)

    setup_watchdog(timerPrescaler);

    sleep_enable();
    do
    {
        sleep_cpu();
    } 
    while(timerCounter > 0);
    
    //turn off the watchdog so that it doesn't keep triggering
    WDTCR = 0x00;
    sleep_disable();
    DEBPMSG("\nDone sleeping");        
    
    if(remainder > prescales[0]) sleep(remainder);
}

unsigned long beginListenTime;

// the loop function runs over and over again forever
void loop()
{
    beginListenTime = millis();

    DEBPMSG("LISTENING");
    while(requestedSleepTime == 0 && (millis() - beginListenTime) < MAX_LISTEN_TIME)
    {
        TinyWireS_stop_check();
        yield();
    }

    bool sleepRequested = requestedSleepTime > 0;
    
    if(sleepRequested)
    {
        DEBPMSG("SLEEP REQUESTED");
        DEBPVAR(requestedSleepTime);

        digitalWrite(LED_BUILTIN, LOW);
        sleep(requestedSleepTime);    
    }
    else
    {
        DEBPMSG("LISTENING TIMEOUT");
    }
    
    //wakeup the controlled device
    digitalWrite(LED_BUILTIN, HIGH);

    //throw away any data that may have been received while trying to process the sleep
    //re-initializing forces the receive buffer to be flushed
    TinyWireS.begin(I2C_SLAVE_ADDRESS);
    TinyWireS.onReceive(onReceiveEvent);
    
    //this will allow the device to accept another request to sleep from the master
    requestedSleepTime = 0;
}