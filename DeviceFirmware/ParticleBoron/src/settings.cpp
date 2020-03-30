#include "settings.h"
#include "Particle.h"

#define SERIALIZED_SETTINGS_SIZE JSON_OBJECT_SIZE(8) + 110

Settings DefaultSettings = {
    0, //version
    false, //brownout
    0.2, //brownoutPercentage
    0, //brownoutMinutes
    30, //sleepTime
    1, //diagnositicCycles
    true, //useDeepSleep
    120 //panelOffMinutes
};

DynamicJsonDocument jsonBuffer(SERIALIZED_SETTINGS_SIZE);

DynamicJsonDocument& serialize(Settings& settings)
{    
    jsonBuffer["version"] = settings.version;
    jsonBuffer["brownout"] = settings.brownout;
    jsonBuffer["brownoutPercentage"] = settings.brownoutPercentage;
    jsonBuffer["brownoutMinutes"] = settings.brownoutMinutes;
    jsonBuffer["sleepTime"] = settings.sleepTime;
    jsonBuffer["diagnositicCycles"] = settings.diagnositicCycles;
    jsonBuffer["useDeepSleep"] = settings.useDeepSleep;
    jsonBuffer["panelOffMinutes"] = settings.panelOffMinutes;

    return jsonBuffer;
}

const Settings& deserialize(const char* json)
{
    const size_t bufferSize = SERIALIZED_SETTINGS_SIZE;
    DynamicJsonDocument jsonBuffer(bufferSize);
    deserializeJson(jsonBuffer, json);

    Settings* result = (Settings*)malloc(sizeof(Settings));

    result->version = jsonBuffer["version"];
    result->brownout = jsonBuffer["brownout"];
    result->brownoutPercentage = jsonBuffer["brownoutPercentage"];
    result->brownoutMinutes = jsonBuffer["brownoutMinutes"];
    result->sleepTime = jsonBuffer["sleepTime"];
    result->diagnositicCycles = jsonBuffer["diagnositicCycles"];
    result->useDeepSleep = jsonBuffer["useDeepSleep"];
    result->panelOffMinutes = jsonBuffer["panelOffMinutes"];

    return *result;
}

const Settings& loadSettings()
{
    Settings eepromSettings;
    EEPROM.get(0,eepromSettings);
    Settings& result = DefaultSettings;    

    if(eepromSettings.version < 0xFFFFFFFF)
    {
        result = *(Settings*)malloc(sizeof(Settings));
        result = eepromSettings;
    }
    return result;
}

void saveSettings(Settings& settings)
{
    EEPROM.put(0,settings);
}