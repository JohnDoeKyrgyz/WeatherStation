#include <avr/sleep.h>
#include <math.h>
#include <SoftwareSerial.h>

#define RX 3   // *** D3, Pin 2
#define TX 4   // *** D4, Pin 3
SoftwareSerial Serial(RX, TX);

#include "DebugMacros.h"
#include "Parameters.h"

#define I2C_SLAVE_ADDRESS 0x4
#include "TinyWireS.h"

// Utility macros
#define adc_disable() (ADCSRA &= ~(1 << ADEN)) // disable ADC (before power-off)
#define adc_enable() (ADCSRA |= (1 << ADEN))   // re-enable ADC

#define LED_BUILTIN 1

//table of the time increments in milliseconds that the ATtiny85 watchdog can sleep
int prescales[] = {16, 32, 64, 128, 250, 500, 1000, 2000, 4000, 8000};

int requestedSleepTime = -1;
void onReceiveEvent(uint8_t length)
{
    if (length == 2)
    {        
        requestedSleepTime = 0;
        requestedSleepTime = TinyWireS.receive();
        requestedSleepTime &= (TinyWireS.receive()) >> 8;        
    }
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
    digitalWrite(LED_BUILTIN, LOW);

    DEBINIT // to be able to use debug output later on
    DEBPSTATUS  // print debug status

    blink();

    DEBPMSG("Restart Timer");
}

void setup_watchdog(int timerPrescaler)
{
    byte bb = timerPrescaler & 7;
    if (timerPrescaler > 7) bb |= (1 << 5); //Set the special 5th bit if necessary

    //This order of commands is important and cannot be combined
    MCUSR &= ~(1 << WDRF);             //Clear the watch dog reset
    WDTCR |= (1 << WDCE) | (1 << WDE); //Set WD_change enable, set WD enable
    WDTCR = bb;                        //Set new watchdog timeout value
    WDTCR |= _BV(WDIE);                //Set the interrupt enable, this will keep unit from resetting after each int
}

volatile int timerCounter;
int timerPrescaler;

//This runs each time the watch dog wakes us up from sleep
ISR(WDT_vect)
{
    DEBPMSG("Wakeup");
    DEBPVAR(timerCounter)

    timerCounter--;    
    if(timerCounter > 0)
    {
        sleep_enable();
        sleep_cpu();
    }
}

void sleep(int milliseconds)
{
    timerPrescaler = 9;
    while(prescales[timerPrescaler] > milliseconds && timerPrescaler > 0) timerPrescaler--;
    int prescale = prescales[timerPrescaler];

    DEBPVAR(timerPrescaler)
    DEBPVAR(prescale)

    timerCounter = milliseconds / prescale;
    int remainder = milliseconds - (timerCounter * prescale);

    DEBPVAR(timerCounter)
    DEBPVAR(remainder)

    setup_watchdog(timerPrescaler);
    sleep_enable();
    sleep_cpu();
    
    if(remainder > prescales[0]) sleep(remainder);    
}

void blink()
{
    digitalWrite(LED_BUILTIN, HIGH); // turn the LED on (HIGH is the voltage level)
    delay(500);
    digitalWrite(LED_BUILTIN, LOW); // turn the LED on (HIGH is the voltage level)
}

// the loop function runs over and over again forever
void loop()
{
    while(requestedSleepTime == -1)
    {
        tws_delay(1000);
        DEBPMSG("WAITING");
    }
    
    DEBPMSG("SLEEP REQUESTED");
    DEBPVAR(requestedSleepTime);

    sleep(requestedSleepTime);
    blink();    
}