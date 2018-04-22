#ifndef MODEL_H
#define MODEL_H

#include "AzureIoTHub.h"

// Define the Model
BEGIN_NAMESPACE(WeatherStation);

DECLARE_MODEL(Anemometer,
    WITH_DATA(ascii_char_ptr, DeviceId),
    WITH_DATA(int, WindSpeed),
    WITH_DATA(float, Temperature),
    WITH_DATA(float, Humidity),
    WITH_ACTION(TurnFanOn),
    WITH_ACTION(TurnFanOff),
    WITH_ACTION(SetAirResistance, int, Position));

END_NAMESPACE(WeatherStation);

#endif /* MODEL_H */
