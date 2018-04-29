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

#endif /* MODEL_H */
