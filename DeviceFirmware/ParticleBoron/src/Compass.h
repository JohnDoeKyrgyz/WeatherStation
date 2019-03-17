#ifndef COMPASS_h
#define COMPASS_h

#include <Adafruit_Sensor.h>
#include <Adafruit_HMC5883.h>

typedef struct CompassReading
{
    float x;
    float y;
    float z;
} CompassReading;

class Compass {
    Adafruit_HMC5883_Unified magnetometer;
  public:
    Compass(int id);
    CompassReading getReading();
};


#endif
