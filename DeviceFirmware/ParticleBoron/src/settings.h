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

DynamicJsonDocument& serialize(Settings& settings);
const Settings& deserialize(const char* serializedSettings);
const Settings& loadSettings();
void saveSettings(Settings& settings);

#endif
