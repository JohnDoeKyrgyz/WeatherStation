#include "Adafruit_DHT/Adafruit_DHT.h"

// Sensor type
#define DHTTYPE DHT22    	// DHT 22 (AM2302)

#define DHT_SENSOR_PIN D6
DHT dht(DHT_SENSOR_PIN, DHTTYPE);

void setup() {
    Serial.begin(115200);

    // Give power to the sensor
    dht.begin();
}

void loop() {
        // Read Sensor
    float temperature = dht.getTempCelcius();
    float humidity = dht.getHumidity();

    Serial.println(String::format("Temp %f, Humidity %f", temperature, humidity));
    
    delay(500);
}