#ifndef SETTINGS_h
#define SETTINGS_h

#include <ArduinoJson.h>

typedef struct Settings
{
    int version;
    bool brownout;
    float brownoutVoltage;
    int brownoutMinutes;
    long sleepTime;
    int diagnositicCycles;
    bool useDeepSleep;
} Settings;

JsonObject& serialize(Settings* settings);
Settings* deserialize(const char* serializedSettings);
Settings* loadSettings();
void saveSettings(Settings* settings);

#endif
