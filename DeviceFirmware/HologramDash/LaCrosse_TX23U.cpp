/*************************************************************************************************
  LaCrosse_TX23.h - Library for reading LaCrosse TX23 anemometer data.
  
  LaCrosse TX23 is a wind speed and direction sensor. It uses 3 wires for communication and power:
  Pin1	Brown(Black)	DATA
  Pin2	Red				Vcc
  Pin3	Green			N/C
  Pin4	Yellow			GND
  
  DATA pin is to be connected directly to one of Arduino ports.
  
  
  
  Created by Krzysztof Egzmont, 06.11.2016.
  Based on https://www.john.geek.nz/2012/08/la-crosse-tx23u-anemometer-communication-protocol/
  Big thanks for John.
  
  Donations: 
  Bitcoin - 1JrHQwJWt1JNZepT1EXqPSioXcwD227nnU
  Ethereum - 0xFC9713013e78f6F14bA5064E36f875dbbB32B94a
  
  BSD license, all text above must be included in any redistribution
*************************************************************************************************/

#include "LaCrosse_TX23.h"

/*
The pulseIn command for the Dash is not quite right. Here is a replacement.
https://community.hologram.io/t/pulsein-on-the-dash/648/2
*/
uint8_t digitalPinToBitMask2(uint8_t pin) {
  if(IO_VALID(pin)) {
    return GPIO_PIN(pin); 
  }
  return 0;
}

uint8_t digitalPinToPort2(uint8_t pin) {
  if(IO_VALID(pin)) {
    return PINS_PORT(pin); 
  }
  return 0;
}

volatile uint32_t* portInputRegister2(uint8_t port) {
  GPIO_Type * pGPIO = (GPIO_Type *)(  PTA_BASE+(port*PERIPH_GPIO_SIZE));
  return &pGPIO->PDIR;
}

#define CYCLES_PER_ITERATION 34
unsigned long pulseIn2(uint8_t pin, uint8_t state, unsigned long timeout)
{
  // cache the port and bit of the pin in order to speed up the
  // pulse width measuring loop and achieve finer resolution.  calling
  // digitalRead() instead yields much coarser resolution.
  uint8_t bit = digitalPinToBitMask2(pin);
  uint8_t port = digitalPinToPort2(pin);
  uint8_t stateMask = (state ? bit : 0);
  unsigned long width = 0; // keep initialization out of time critical area
  
  // convert the timeout from microseconds to a number of times through
  // the initial loop; it takes 16 clock cycles per iteration.
  unsigned long numloops = 0;
  unsigned long maxloops = microsecondsToClockCycles(timeout) / CYCLES_PER_ITERATION;
  
  // wait for any previous pulse to end
  while ((*portInputRegister2(port) & bit) == stateMask)
    if (numloops++ == maxloops)
      return 0;
  
  // wait for the pulse to start
  while ((*portInputRegister2(port) & bit) != stateMask)
    if (numloops++ == maxloops)
      return 0;
  
  // wait for the pulse to stop
  while ((*portInputRegister2(port) & bit) == stateMask) {
    if (numloops++ == maxloops)
      return 0;
    width++;
  }
  
  return clockCyclesToMicroseconds(width * CYCLES_PER_ITERATION + 90); 
}

#define READ_TIMEOUT_MICROSECONDS 1e+6 //1 seconds

LaCrosse_TX23::LaCrosse_TX23(int pin)
{
pinMode(pin, INPUT);
_pin = pin;
}

void LaCrosse_TX23::pullBits(void *dst, bool *src, int count)
{
	uint8_t * d = (uint8_t*) dst;
	for (int i=0; i<count; i++)
	{
		d[i/8] |= src[i]<<(i%8);
	}	
}

bool LaCrosse_TX23::read(float &speed, int &direction)
{
	speed = 0;
	direction = 0;

	digitalWrite(_pin,LOW);
	pinMode(_pin,OUTPUT);
	delay(500);
	pinMode(_pin,INPUT);
	pulseIn2(_pin,LOW,READ_TIMEOUT_MICROSECONDS);

	unsigned bitLen = 1200;

	bool data[50];
	bool lastState = 1;
	unsigned long start = micros();
	for(unsigned long t = 0; t<50000; t = micros()-start)
	{
		bool state = digitalRead(_pin);
		unsigned bitNum = t/bitLen;
		if(t%bitLen>bitLen/2) data[bitNum] = state;
		if(state!=lastState)
		{
			unsigned delta = t%bitLen;
			if(delta<100)
				start -= delta;
			else if(delta>900)
				start += delta;
			lastState = state;
		}
	}

	uint8_t ctr = 0; pullBits(&ctr,data+0,5);
	uint8_t dir = 0; pullBits(&dir,data+5,4);
	uint16_t spd = 0; pullBits(&spd,data+9,12);
	uint8_t sum = 0; pullBits(&sum,data+21,4);
	uint8_t ndir = 0; pullBits(&ndir,data+25,4);
	ndir ^= 0x0f;
	uint16_t nspd = 0; pullBits(&nspd,data+29,12);
	nspd ^= 0x0fff;

	uint8_t csum = 0x0f & (dir + (spd&0x0f) + ((spd>>4)&0x0f) + ((spd>>8)&0x0f));

	if(ctr!=27) return false;
	if(csum!=sum) return false;
	if(spd!=nspd || dir!=ndir) return false;

	speed = spd/10.0;
	direction = dir;

	return true;
}
