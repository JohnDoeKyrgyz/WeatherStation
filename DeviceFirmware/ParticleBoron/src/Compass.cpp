#include "Compass.h"
#include "Particle.h"

#include <Wire.h>

/* Assign a unique ID to this sensor at the same time */
Adafruit_HMC5883_Unified magnetometer;

void displaySensorDetails(void)
{
  sensor_t sensor;
  magnetometer.getSensor(&sensor);
  Serial.println("------------------------------------");
  Serial.print  ("Sensor:       "); Serial.println(sensor.name);
  Serial.print  ("Driver Ver:   "); Serial.println(sensor.version);
  Serial.print  ("Unique ID:    "); Serial.println(sensor.sensor_id);
  Serial.print  ("Max Value:    "); Serial.print(sensor.max_value); Serial.println(" uT");
  Serial.print  ("Min Value:    "); Serial.print(sensor.min_value); Serial.println(" uT");
  Serial.print  ("Resolution:   "); Serial.print(sensor.resolution); Serial.println(" uT");
  Serial.println("------------------------------------");
  Serial.println("");
  delay(500);
}

Compass::Compass(int id)
{
    magnetometer = Adafruit_HMC5883_Unified(id);

    /* Initialise the sensor */
    if(!magnetometer.begin())
    {
        /* There was a problem detecting the HMC5883 ... check your connections */
        Serial.println("Ooops, no HMC5883 detected ... Check your wiring!");
        while(1);
    }

    /* Display some basic information on this sensor */
    displaySensorDetails();
}

CompassReading Compass::getReading(){
    CompassReading result;

    /* Get a new sensor event */
    sensors_event_t event;
    magnetometer.getEvent(&event);

    result.x = event.magnetic.x;
    result.y = event.magnetic.y;
    result.z = event.magnetic.z;

    return result;
}