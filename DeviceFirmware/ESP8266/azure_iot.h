#ifndef AZURE_IOT_H
#define AZURE_IOT_H

#include "model.h"

#ifdef __cplusplus
extern "C" {
#endif
    typedef enum SEND_STATE_TAG {
        SENDING,
        COMPLETE,
        NONE
    } SEND_STATE;
    SEND_STATE getSendState();
    IOTHUB_CLIENT_LL_HANDLE initializeAzureIot(const char* connectionString);
    Anemometer *initializeAnemometer(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, const char* deviceId);
    void destroyAnemometer(Anemometer *instance);
    void sendUpdate(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, Anemometer *myWeather);
    void doWork(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle);
    void destroyAzureIoT(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle);

#ifdef __cplusplus
}
#endif

#endif /* AZURE_IOT_H */
