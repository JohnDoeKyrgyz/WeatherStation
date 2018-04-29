#ifndef SETTINGS_H
#define SETTINGS_H


#ifdef __cplusplus
extern "C" {
#endif
    #include <Arduino.h>

    typedef struct SettingsTag {
        uint64_t SleepInterval;
    } Settings;

    typedef struct SettingsTag* SETTINGS_HANDLE;

    SETTINGS_HANDLE getDefaults();
    SETTINGS_HANDLE getSettings();

#ifdef __cplusplus
}
#endif
#endif