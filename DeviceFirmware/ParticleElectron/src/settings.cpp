#include "settings.h"

Settings DefaultSettings = {
    false, //brownout
    0, //brownoutMinutes
    30, //sleepTime
    1 //diagnositicCycles
};

Settings* loadSettings()
{
    return &DefaultSettings;
}

void saveSettings(Settings* settings)
{

}