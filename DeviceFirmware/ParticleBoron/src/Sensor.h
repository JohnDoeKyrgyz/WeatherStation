#ifndef SENSOR_h
#define SENSOR_h

class Sensor {
  public:
    virtual int getReadingSize();
    virtual bool getReading(char* reading);
    virtual bool begin();
};


#endif
