#ifndef SETTINGS_h
#define SETTINGS_h

#include <ArduinoJson.h>

typedef struct Settings
{
    int version;
    bool brownout;
    int brownoutMinutes;
    long sleepTime;
    int diagnositicCycles;
} Settings;

JsonObject& serialize(Settings* settings);
Settings* deserialize(const char* serializedSettings);
Settings* loadSettings();
void saveSettings(Settings* settings);

#endif
