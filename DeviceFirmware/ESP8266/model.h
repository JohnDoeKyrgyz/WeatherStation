#ifndef MODEL_H
#define MODEL_H

#include "AzureIoTHub.h"

// Define the Model for readings
BEGIN_NAMESPACE(WeatherStation);

DECLARE_MODEL(Anemometer,
    WITH_DATA(ascii_char_ptr, DeviceId),
    WITH_DATA(int, WindSpeed),
    WITH_DATA(float, DhtTemperature),
    WITH_DATA(float, DhtHumidity),
    WITH_DATA(float, BatteryVoltage),
    WITH_ACTION(FirmwareUpdate),
    WITH_ACTION(SetInterval, int, Position),
    WITH_ACTION(SetDiagnosticMode, bool, Diagnostic));

END_NAMESPACE(WeatherStation);

BEGIN_NAMESPACE(AtwoodIoT);

// NOTE: For callbacks defined in the serializer model to be fired for desired properties, IoTHubClient_SetDeviceTwinCallback must not be invoked.
//       Please comment out the call to IoTHubClient_SetDeviceTwinCallback further down to enable the callbacks defined in the model below. 
DECLARE_MODEL(Settings,
    WITH_DESIRED_PROPERTY(uint8_t, Interval, onSettingsChanged),
    WITH_DESIRED_PROPERTY(bool, DeepSleep, onSettingsChanged),
    WITH_DESIRED_PROPERTY(bool, DiagnosticMode, onSettingsChanged),
);

DECLARE_DEVICETWIN_MODEL(Settings);

END_NAMESPACE(AtwoodIoT);

#endif /* MODEL_H */
