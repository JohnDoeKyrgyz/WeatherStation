#ifndef COMPASS_h
#define COMPASS_h

#include <QMC5883L.h>

// configure the compass as reqiured
#define OSR 0b00               // over sampling rate set to 512. 0b01 is 256, 0b10 is 128 and 0b11 is 64
#define RNG 0b00               // Full Scale set to +/- 2 Gauss, 0b01 is +/- 8.
#define ODR 0b00               // output data rate set to 10Hz, 0b01 is 50Hz, 0b10 is 100Hz, 0b11 is 200Hz
#define MODE 0b01              // continuous measurement mode, 0b00 is standby
#define CR2 0b00000000          // control register 2: disable soft reset and pointer rollover, interrupt pin not enabled
#define RESETPERIOD 0b00000001  // datasheet recommends this be 1, not sure why!

typedef struct CompassReading
{
    float x;
    float y;
    float z;
} CompassReading;

class Compass {
  private:
    QMC5883L compass;
  public:
    Compass();
    CompassReading getReading();
    bool begin();
};


#endif
