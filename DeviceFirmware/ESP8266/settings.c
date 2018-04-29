#include "settings.h"

SETTINGS_HANDLE getDefaults()
{
    Settings *settings = malloc(sizeof(Settings));
    settings->SleepInterval = 10e6;
    return settings;
}

SETTINGS_HANDLE getSettings()
{
    return getDefaults();
}