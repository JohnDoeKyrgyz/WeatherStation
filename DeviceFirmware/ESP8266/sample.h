// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef SAMPLE_H
#define SAMPLE_H

#include "model.h"

#ifdef __cplusplus
extern "C" {
#endif
    IOTHUB_CLIENT_LL_HANDLE initializeAzureIot();
    Anemometer *initializeAnemometer(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle);
    void destroyAnemometer(Anemometer *instance);
    void sendUpdate(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, Anemometer *myWeather);
    void doWork(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle);
    void destroyAzureIoT(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle);

#ifdef __cplusplus
}
#endif

#endif /* SAMPLE_H */
