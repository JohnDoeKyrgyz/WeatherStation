#include <stdio.h>
#include <stdlib.h>

#include <AzureIoTHub.h>
#include <AzureIoTProtocol_MQTT.h>

DEFINE_ENUM_STRINGS(DEVICE_TWIN_UPDATE_STATE, DEVICE_TWIN_UPDATE_STATE_VALUES);

static char msgText[1024];
static char propText[1024];
static bool stateReported;

static void deviceTwinCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payLoad, size_t size, void* userContextCallback)
{
    (void)userContextCallback;
    printf("Device Twin update received (state=%s, size=%u): \r\n", ENUM_TO_STRING(DEVICE_TWIN_UPDATE_STATE, update_state), size);
    for (size_t n = 0; n < size; n++)
    {
        printf("%c", payLoad[n]);
    }
    printf("\r\n");
}

static void reportedStateCallback(int status_code, void* userContextCallback)
{
    (void)userContextCallback;
    printf("Device Twin reported properties update completed with result: %d\r\n", status_code);
    stateReported = false;
}

static bool deviceTwinUpdateComplete()
{
    return stateReported;
}

IOTHUB_CLIENT_RESULT beginDeviceTwinSync(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle)
{
    bool traceOn = true;
    // This json-format reportedState is created as a string for simplicity. In a real application
    // this would likely be done with parson (which the Azure IoT SDK uses) or a similar tool.
    const char* reportedState = "{ 'device_property': 'new_value'}";
    size_t reportedStateSize = strlen(reportedState);

    IOTHUB_CLIENT_RESULT result;
    if((result = IoTHubClient_LL_SetOption(iotHubClientHandle, OPTION_LOG_TRACE, &traceOn)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not initialize logging.\r\n");
    }
    else if((result = IoTHubClient_LL_SetDeviceTwinCallback(iotHubClientHandle, deviceTwinCallback, iotHubClientHandle)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not set device twin callback.\r\n");
    } 
    else if((result = 
        IoTHubClient_LL_SendReportedState(
            iotHubClientHandle, 
            (const unsigned char*)reportedState, 
            reportedStateSize, 
            reportedStateCallback, 
            iotHubClientHandle)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not send report state.\r\n");
    }
    return result;
}
