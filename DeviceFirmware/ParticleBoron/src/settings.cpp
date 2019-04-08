#include "settings.h"
#include "Particle.h"

#define SERIALIZED_SETTINGS_SIZE JSON_OBJECT_SIZE(7) + 110

Settings DefaultSettings = {
    0, //version
    false, //brownout
    4.6, //brownoutVoltage
    0, //brownoutMinutes
    30, //sleepTime
    1, //diagnositicCycles
    false //useDeepSleep
};

DynamicJsonDocument& serialize(Settings* settings)
{
    DynamicJsonDocument jsonBuffer(SERIALIZED_SETTINGS_SIZE);
    jsonBuffer["version"] = settings->version;
    jsonBuffer["brownout"] = settings->brownout;
    jsonBuffer["brownoutVoltage"] = settings->brownoutVoltage;
    jsonBuffer["brownoutMinutes"] = settings->brownoutMinutes;
    jsonBuffer["sleepTime"] = settings->sleepTime;
    jsonBuffer["diagnositicCycles"] = settings->diagnositicCycles;
    jsonBuffer["useDeepSleep"] = settings->useDeepSleep;

    return jsonBuffer;
}

Settings* deserialize(const char* json)
{
    const size_t bufferSize = SERIALIZED_SETTINGS_SIZE;
    DynamicJsonDocument jsonBuffer(bufferSize);
    deserializeJson(jsonBuffer, json);

    Settings* result = (Settings*)malloc(sizeof(Settings));

    result->version = jsonBuffer["version"];
    result->brownout = jsonBuffer["brownout"];
    result->brownoutVoltage = jsonBuffer["brownoutVoltage"];
    result->brownoutMinutes = jsonBuffer["brownoutMinutes"];
    result->sleepTime = jsonBuffer["sleepTime"];
    result->diagnositicCycles = jsonBuffer["diagnositicCycles"];
    result->useDeepSleep = jsonBuffer["useDeepSleep"];

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