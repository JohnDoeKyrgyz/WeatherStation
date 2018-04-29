#include <stdio.h>
#include <stdlib.h>

#include <AzureIoTHub.h>
#include <AzureIoTProtocol_MQTT.h>
#include "sdk\parson.h"

DEFINE_ENUM_STRINGS(DEVICE_TWIN_UPDATE_STATE, DEVICE_TWIN_UPDATE_STATE_VALUES);

static char msgText[1024];
static char propText[1024];
static bool stateReported;

static void deviceTwinCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payload, size_t size, void* userContextCallback)
{
    if(userContextCallback != NULL)
    {
        void(*callback)(JSON_Value*) = userContextCallback;

        JSON_Value *deviceUpdateJson = json_parse_string(payload);
        JSON_Object *jsonSettings = json_value_get_object(deviceUpdateJson);
        jsonSettings = json_object_get_object(jsonSettings, "desired");

        callback(jsonSettings);
        json_value_free(deviceUpdateJson);
    }
    
    printf("Device Twin update received (state=%s, size=%u): \r\n", ENUM_TO_STRING(DEVICE_TWIN_UPDATE_STATE, update_state), size);
    for (size_t n = 0; n < size; n++)
    {
        printf("%c", payload[n]);
    }
    printf("\r\n");
}

static void reportedStateCallback(int status_code, void* userContextCallback)
{
    (void)userContextCallback;
    printf("Device Twin reported properties update completed with result: %d\r\n", status_code);
    stateReported = true;
}

bool deviceTwinUpdateComplete()
{
    return stateReported;
}

IOTHUB_CLIENT_RESULT beginDeviceTwinSync(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, JSON_Value* settings, void(*onSettingsReceived)(JSON_Value *jsonValue))
{
    stateReported = false;

    const char* reportedState = json_serialize_to_string(settings);
    size_t reportedStateSize = strlen(reportedState);

    IOTHUB_CLIENT_RESULT result;
    if((result = IoTHubClient_LL_SetDeviceTwinCallback(iotHubClientHandle, deviceTwinCallback, iotHubClientHandle)) != IOTHUB_CLIENT_OK)
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
