#ifndef DEVICE_TWIN_H
#define DEVICE_TWIN_H

#include "model.h"
#include "sdk\parson.h"

#ifdef __cplusplus
extern "C" {
#endif
    bool deviceTwinUpdateComplete();
    IOTHUB_CLIENT_RESULT beginDeviceTwinSync(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, JSON_Value* settings, void(*onSettingsReceived)(JSON_Value *jsonValue));
#ifdef __cplusplus
}
#endif

#endif /* DEVICE_TWIN_H */
