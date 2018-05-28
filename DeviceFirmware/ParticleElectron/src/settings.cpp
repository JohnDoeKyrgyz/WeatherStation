#include "settings.h"
#include "Particle.h"

#define SERIALIZED_SETTINGS_SIZE JSON_OBJECT_SIZE(6) + 110

Settings DefaultSettings = {
    0, //version
    false, //brownout
    0, //brownoutMinutes
    30, //sleepTime
    1, //diagnositicCycles
    false //useDeepSleep
};

JsonObject& serialize(Settings* settings)
{
    DynamicJsonBuffer jsonBuffer(SERIALIZED_SETTINGS_SIZE);

    JsonObject& root = jsonBuffer.createObject();
    root["version"] = settings->version;
    root["brownout"] = settings->brownout;
    root["brownoutMinutes"] = settings->brownoutMinutes;
    root["sleepTime"] = settings->sleepTime;
    root["diagnositicCycles"] = settings->diagnositicCycles;
    root["useDeepSleep"] = settings->useDeepSleep;

    return root;
}

Settings* deserialize(const char* json)
{
    const size_t bufferSize = SERIALIZED_SETTINGS_SIZE;
    DynamicJsonBuffer jsonBuffer(bufferSize);
    JsonObject& root = jsonBuffer.parseObject(json);

    Settings* result = (Settings*)malloc(sizeof(Settings));

    result->version = root["version"];
    result->brownout = root["brownout"];
    result->brownoutMinutes = root["brownoutMinutes"];
    result->sleepTime = root["sleepTime"];
    result->diagnositicCycles = root["diagnositicCycles"];
    result->useDeepSleep = root["useDeepSleep"];

    return result;
}

Settings* loadSettings()
{
    Settings eepromSettings;
    EEPROM.get(0,eepromSettings);
    Settings* result = &DefaultSettings;

    if(eepromSettings.version >= 0)
    {
        result = (Settings*)malloc(sizeof(Settings));
        *result = eepromSettings;
    }
    return result;
}

void saveSettings(Settings* settings)
{
    Settings* existingSettings = loadSettings();
    
    settings->version = existingSettings->version + 1;
    Serial.print("SAVING SETTINGS ");
    Serial.print(settings->version);
    Serial.println();
    
    EEPROM.put(0,*settings);
}