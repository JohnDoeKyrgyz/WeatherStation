#include <stdio.h>
#include <stdlib.h>

#include <AzureIoTHub.h>
#include <AzureIoTProtocol_MQTT.h>

#include <ArduinoJson.h>

DEFINE_ENUM_STRINGS(DEVICE_TWIN_UPDATE_STATE, DEVICE_TWIN_UPDATE_STATE_VALUES);

bool stateReported = true;

void(*settingsCallback)(JsonObject& jsonValue);

void deviceTwinCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payload, size_t size, void* userContextCallback)
{
    printf("deviceTwin Callback Address = %p\r\n", userContextCallback);
    printf("settingsCallback Callback Address = %p\r\n", settingsCallback);

    printf("Device Twin update received (state=%s, size=%u): \r\n", ENUM_TO_STRING(DEVICE_TWIN_UPDATE_STATE, update_state), size);
    for (size_t n = 0; n < size; n++)
    {
        printf("%c", payload[n]);
    }
    printf("\r\n");

    if(settingsCallback != NULL)
    {
        const int capacity = JSON_OBJECT_SIZE(100);
        StaticJsonBuffer<capacity> jsonBuffer;

        JsonObject& deviceUpdateJson = jsonBuffer.parseObject(payload);

        if(deviceUpdateJson.success()){
            JsonObject& desired = deviceUpdateJson["desired"];
            printf("SleepInterval = %d\r\n", desired["SleepInterval"]);
            settingsCallback(deviceUpdateJson);
        } else {
            printf("Could not parse device twin json\r\n");
        }
    }
}

static void reportedStateCallback(int status_code, void* userContextCallback)
{
    printf("reportedStateCallback Callback Address = %p\r\n", userContextCallback);
    printf("Device Twin reported properties update completed with result: %d\r\n", status_code);
    stateReported = true;
}

bool deviceTwinUpdateComplete()
{
    return stateReported;
}

IOTHUB_CLIENT_RESULT beginDeviceTwinSync(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle, JsonObject& settings, void(*onSettingsReceived)(JsonObject& jsonValue))
{
    stateReported = false;

    settingsCallback = onSettingsReceived;
    printf("beginDeviceTwinSync Callback Address = %p\r\n", onSettingsReceived);

    String output;
    settings.printTo(output);

    printf("Reporting\r\n");
    printf(output.c_str());
    printf("\r\n");
    
    IOTHUB_CLIENT_RESULT result;
    if((result = IoTHubClient_LL_SetDeviceTwinCallback(iotHubClientHandle, deviceTwinCallback, iotHubClientHandle)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not set device twin callback.\r\n");
    } 
    else if((result = 
        IoTHubClient_LL_SendReportedState(
            iotHubClientHandle, 
            (const unsigned char*)output.c_str(), 
            output.length(), 
            reportedStateCallback, 
            (void*)onSettingsReceived)) != IOTHUB_CLIENT_OK)
    {
        printf("Could not send report state.\r\n");
    }
    return result;
}
