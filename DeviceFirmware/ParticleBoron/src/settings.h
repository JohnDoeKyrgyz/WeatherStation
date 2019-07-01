#ifndef SETTINGS_h
#define SETTINGS_h

#include <ArduinoJson.h>

typedef struct Settings
{
    unsigned int version;
    bool brownout;
    float brownoutPercentage;
    unsigned int brownoutMinutes;
    unsigned long sleepTime;
    unsigned int diagnositicCycles;
    bool useDeepSleep;
    unsigned long panelOffMinutes;
} Settings;

DynamicJsonDocument& serialize(Settings& settings);
const Settings& deserialize(const char* serializedSettings);
const Settings& loadSettings();
void saveSettings(Settings& settings);

#endif
