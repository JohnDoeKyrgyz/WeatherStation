#include <stdio.h>
#include <stdlib.h>

#include <AzureIoTHub.h>
#include <AzureIoTProtocol_MQTT.h>
#include "sdk\parson.h"

DEFINE_ENUM_STRINGS(DEVICE_TWIN_UPDATE_STATE, DEVICE_TWIN_UPDATE_STATE_VALUES);

static bool stateReported = true;

static void(*settingsCallback)(JSON_Value *jsonValue);

static void printJson(JSON_Value *json)
{
    printf("JSON\r\n");
    printf("TYPE %d\r\n", json_value_get_type(json));
    char* pretty = json_serialize_to_string_pretty(json);
    int size = strlen(pretty);
    for (size_t n = 0; n < size; n++)
    {
        printf("%c", pretty[n]);
    }
    printf("JSON\r\n");
}

static void deviceTwinCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payload, size_t size, void* userContextCallback)
{
    printf("Device Twin update received (state=%s, size=%u): \r\n", ENUM_TO_STRING(DEVICE_TWIN_UPDATE_STATE, update_state), size);
    for (size_t n = 0; n < size; n++)
    {
        printf("%c", payload[n]);
    }
    printf("\r\n");

    if(settingsCallback != NULL)
    {
        const JSON_Value *deviceUpdateJson = json_parse_string(payload);
        printJson(deviceUpdateJson);

        const JSON_Object *jsonSettings = json_value_get_object(deviceUpdateJson);
        printJson(jsonSettings);

        jsonSettings = json_object_dotget_object(jsonSettings, "desired");
        printJson(jsonSettings);

        settingsCallback(jsonSettings);
        json_value_free(deviceUpdateJson);
    }
}

static void reportedStateCallback(int status_code, void* userContextCallback)
{
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

    settingsCallback = onSettingsReceived;

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
            onSettingsReceived)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not send report state.\r\n");
    }
    return result;
}
