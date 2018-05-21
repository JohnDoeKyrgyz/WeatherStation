#ifndef SETTINGS_h
#define SETTINGS_h

typedef struct Settings
{
    bool brownout;
    int brownoutMinutes;
    long sleepTime;
    int diagnositicCycles;
} Settings;

Settings* loadSettings();
void saveSettings(Settings* settings);

#endif