#ifndef DEVICE_TWIN_H
#define DEVICE_TWIN_H

#include "model.h"
#include <ArduinoJson.h>

bool deviceTwinUpdateComplete();
IOTHUB_CLIENT_RESULT beginDeviceTwinSync(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, JsonObject* settings, void(*onSettingsReceived)(JsonObject& jsonValue));

#endif /* DEVICE_TWIN_H */
