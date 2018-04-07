#include "DHT.h"
#include "LaCrosse_TX23.h"
#include "Adafruit_BMP280.h"
#include "MultiPrint.h"
#include "Sensors.h"

#define FIRMWARE_VERSION "4.3"
#define DEBUG 1

/* Connections */
#define BAROMETER_CHIP_SELECT L07
#define DHTPIN R03
#define ANEMOMETER R04
#define SUPPLY_VOLTAGE R09
#define CHARGE_VOLTAGE L03

/* Brownout voltage level */
#define BROWNOUT_MILLIVOLTS 4750
#define BROWNOUT_MINUTES 180.0 //3 hours

/* Devices */
#define DHTTYPE DHT22   // DHT 22  (AM2302), AM2321

DHT dht(DHTPIN, DHTTYPE);
DHTSensorAdapter dhtAdapter = DHTSensorAdapter(&dht);

Adafruit_BMP280 bmp280(BAROMETER_CHIP_SELECT);
BMP280Sensor bmp280Adapter(BAROMETER_CHIP_SELECT, &bmp280);

VoltageSensor voltageSensor(SUPPLY_VOLTAGE, CHARGE_VOLTAGE);

LaCrosse_TX23 laCrosseTX23(ANEMOMETER);
Anemometer anemometer(&laCrosseTX23);

Sensor* sensors[] = {
  &dhtAdapter,
  &bmp280Adapter,
  &voltageSensor,
  &anemometer
  };

int sensorCount = (sizeof(sensors)/sizeof(*sensors));

DataReading reading;

Print* targets[] = {&Serial, &HologramCloud};
MultiPrint writer(targets, 2);
#if DEBUG
  MultiPrint dataWriter = writer;
#else
  MultiPrint dataWriter = HologramCloud;
#endif

/* Program State */
// Decrementing counter to indicate if LED should be on during loop
int diagnosticLightCount = 0;
// Number of seconds to sleep between readings
uint16_t deepSleepSeconds = 60;
// Send output as json
bool jsonOutput = false;

void sendMessage(){
  //Attempt to send the buffered message
  if(!HologramCloud.sendMessage()) {
    //if the message failed to send, try again
    int status = HologramCloud.getConnectionStatus();

    switch(status) {
      //Is the SIM card inserted?
      case CLOUD_ERR_SIM:
        Serial.println("ERROR: No SIM card");
        break;
      //Is the SIM card activated?
      case CLOUD_ERR_CONNECT:
        Serial.println("ERROR: Cannot connect to Hologram cloud");
        break;
      case CLOUD_ERR_SIGNAL:
        Serial.println("ERROR: Error sending cloud message");
        //Low signal, check the antenna, or move into better coverage area
        Dash.snooze(10000); //wait 10 seconds

        //The last message is buffered and can be re-sent again (on success
        //or failure).
        //Attempt to send the buffered message again.
        HologramCloud.sendMessage();

        //Any call to write to the buffer or attach a topic or
        //send a new message String will reset the buffer.
        HologramCloud.print("Test message");

        //Explicitly clear the message buffer. Usually not necessary unless
        //the buffer has been written but the contents should not be sent
        HologramCloud.clear();

        //"Test message" text is dropped, won't be sent
        break;
    }
  }
}

void error(String message, bool fatal = false){
  writer.println("ERROR: " + message);
  HologramCloud.attachTopic("ERROR");
  sendMessage();
  if(fatal){
    while(true);
  }
}

#define RECEIVE_BUFFER_SIZE 4096               //Inbound message size limit
char receiveBuffer[RECEIVE_BUFFER_SIZE];      //Holds inbound message

void processCommand(String message) {
  char command = message[0];
  int value = message.substring(1, message.length()).toInt();
  
  switch(command){
    case 'S':
      deepSleepSeconds = value;
      break;
    case 'D':
      diagnosticLightCount = value;
      break;
    case 'J':
      jsonOutput = value;
      break;
  }
  
  writer.println(message);
  HologramCloud.attachTopic("COMMAND");
  sendMessage();
}

//Process a received inbound message
void onCloudMessageReceived(int length) {
  receiveBuffer[length] = NULL; //NULL terminate the data for printing as a String
  String message = String(receiveBuffer);
  processCommand(message);  
}

void onSmsReceived(const String &sender, const rtc_datetime_t &timestamp, const String &message){
  writer.print("Sender: ");
  writer.println(sender);
  writer.print("Time: ");
  writer.println(timestamp);
  processCommand(message);
}

void bootMessage(){
  writer.print("Serial Number ");
  writer.println(Dash.serialNumber());

  writer.print("Boot Version ");
  writer.println(Dash.bootVersion());

  writer.print("Firmware Version ");
  writer.println(FIRMWARE_VERSION);

  writer.print("ICCID ");
  writer.println(HologramCloud.getICCID());
  
  writer.print("Network ");
  writer.println(HologramCloud.getNetworkOperator());

  writer.print("Signal Strength ");
  writer.println(HologramCloud.getSignalStrength());

  writer.println("Sensors");
  for(int i = 0; i < sensorCount; i++){
    writer.println(sensors[i]->Name());
  }
  
  HologramCloud.attachTopic("BOOT");
  sendMessage();
}

void checkBrownout(){
  if(Charger.batteryMillivolts() < BROWNOUT_MILLIVOLTS){
    Serial.println("BROWNOUT");
    HologramCloud.powerDown();

    /// Blink a pattern -.-
    for(int i = 0; i < 2; i++){
      Dash.setLED(true);
      Dash.snooze(500);
      Dash.setLED(false);
      Dash.snooze(200);
    }   

    Dash.deepSleepMin(BROWNOUT_MINUTES);
  }
}

void setup() {
  Serial.begin(115200);
  checkBrownout();

  Dash.setLED(true);

  bootMessage();

  HologramCloud.attachHandlerInbound(onCloudMessageReceived, receiveBuffer, RECEIVE_BUFFER_SIZE-1);
  HologramCloud.attachHandlerSMS(onSmsReceived);
  HologramCloud.listen(4010);

  //Initialize all sensors. Fail if one of the sensors cannot be initialized.
  bool continueSetup = true;
  for(int i = 0; i < sensorCount && continueSetup; i++){
    continueSetup = sensors[i]->init();
    if(!continueSetup){
      error("Could not initialize " + sensors[i]->Name(), true);
    }
  }

  Dash.pulseLED( 200, 200 );

  //sync clock with network time
  rtc_datetime_t currentTime;
  if(HologramCloud.getNetworkTime(currentTime)) Clock.setDateTime(currentTime);
  else error("Could not get network time");

  Dash.setLED(false);
}

void sendDataJson(rtc_datetime_t &currentTime){
  //Load the reading JSON object
  StaticJsonBuffer<1000> jsonBuffer;
  JsonObject& json = jsonBuffer.createObject();

  json["refreshIntervalSeconds"] = String(deepSleepSeconds);

  for(int i = 0; i < sensorCount; i++){
    sensors[i]->json(json, reading);
  }
    
  json["time"] = 
    String(currentTime.year) + "-" 
    + String(currentTime.month) + "-"
    + String(currentTime.day) + " " 
    + String(currentTime.hour) + ":" 
    + String(currentTime.minute) + ":" 
    + String(currentTime.second);

  json.printTo(dataWriter);
  HologramCloud.attachTopic("J");
}

void sendDataRaw(rtc_datetime_t &currentTime){
  dataWriter.print(deepSleepSeconds);
  dataWriter.print(":");
  
  for(int i = 0; i < sensorCount; i++){
    sensors[i]->print(dataWriter, reading);
  }

  dataWriter.print(currentTime.year);
  dataWriter.print(":");
  dataWriter.print(currentTime.month);
  dataWriter.print(":");
  dataWriter.print(currentTime.day);
  dataWriter.print(":");
  dataWriter.print(currentTime.hour);
  dataWriter.print(":");
  dataWriter.print(currentTime.minute);
  dataWriter.print(":");
  dataWriter.print(currentTime.second);
  HologramCloud.attachTopic("R");
}

void loop() {

  checkBrownout();

  //turn on system LED if diagnostic count down is still running
  if(diagnosticLightCount){
    Dash.setLED(true);
    diagnosticLightCount--;
  }

  //re-initialize the reading
  reading = {};

  //read all sensors
  bool sensorReadErrorOccured = false;
  for(int i = 0; i < sensorCount; i++){
    if(!sensors[i]->read(reading)){
      Serial.println(sensors[i]->Name());
      error("Could not read " + sensors[i]->Name());
      sensorReadErrorOccured = true;
    }
  }

  //If all sensors were read successfully post the reading to the cloud
  if(!sensorReadErrorOccured){    
    rtc_datetime_t currentTime;
    Clock.getDateTime(currentTime);
    
    if(jsonOutput) sendDataJson(currentTime);
    else sendDataRaw(currentTime);    
    
    sendMessage();

  } else {
    //write reading as JSON text to console
    StaticJsonBuffer<1000> jsonBuffer;
    JsonObject& json = jsonBuffer.createObject();

    json["refreshIntervalSeconds"] = String(deepSleepSeconds);

    for(int i = 0; i < sensorCount; i++){
      sensors[i]->json(json, reading);
    }
    json.printTo(Serial);
    Serial.println();
  }

  //Poll for events before sleeping, since there may be a message to configure the sleep interval
  HologramCloud.pollEvents();

  //turn off the system LED
  Dash.setLED(false);

  bool skipSleep = DEBUG && sensorReadErrorOccured;

  //if one of the devices encountered a read error avoid sleeping. This makes it easier to diagnose via USB
  if(skipSleep){
    delay(2000);
  }
  //sleep for the desired interval
  else if(deepSleepSeconds > 0){
    #if DEBUG
    Serial.print("Sleep for ");
    Serial.print(deepSleepSeconds);
    Serial.println(" seconds...");
    #endif
    HologramCloud.powerDown();
    Dash.deepSleepSec(deepSleepSeconds);
    #if DEBUG
    Serial.println("Continue...");
    #endif
  }
}
