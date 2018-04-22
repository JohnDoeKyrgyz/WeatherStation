#include <stdlib.h>

#include <stdio.h>
#include <stdint.h>

#include "AzureIoTHub.h"

// Define the Model - it is a car.
BEGIN_NAMESPACE(AtwoodIoT);

// NOTE: For callbacks defined in the serializer model to be fired for desired properties, IoTHubClient_SetDeviceTwinCallback must not be invoked.
//       Please comment out the call to IoTHubClient_SetDeviceTwinCallback further down to enable the callbacks defined in the model below. 
DECLARE_MODEL(Settings,
    WITH_DESIRED_PROPERTY(uint8_t, interval, onSettingsChanged),
    WITH_DESIRED_PROPERTY(bool, deepSleep, onSettingsChanged)
);

DECLARE_DEVICETWIN_MODEL(Settings);

END_NAMESPACE(AtwoodIoT);

void deviceTwinReportStateCallback(int status_code, void* userContextCallback)
{
    (void)(userContextCallback);
    printf("IoTHub: reported properties delivered with status_code = %d\n", status_code);
}

static void deviceTwinGetStateCallback(DEVICE_TWIN_UPDATE_STATE update_state, const unsigned char* payLoad, size_t size, void* userContextCallback)
{
    (void)userContextCallback;
    printf("Device Twin properties received: update=%s payload=%s, size=%zu\n", ENUM_TO_STRING(DEVICE_TWIN_UPDATE_STATE, update_state), payLoad, size);
}

void onSettingsChanged(void* argument)
{
    // Note: The argument is NOT a pointer to desired_maxSpeed, but instead a pointer to the MODEL 
    //       that contains desired_maxSpeed as one of its arguments.  In this case, it
    //       is CarSettings*.

    Settings* car = argument;
    printf("received a new desired_maxSpeed = %" PRIu8 "\n", car->interval);
}

void manageDeviceTwin(IOTHUB_CLIENT_LL_HANDLE iotHubClientHandle)
{    
    Settings* car = IoTHubDeviceTwin_CreateSettings(iotHubClientHandle);
    if (car == NULL)
    {
        printf("Failure in IoTHubDeviceTwin_CreateCar");
    }
    else
    {
        /*setting values for reported properties*/
        /*
        car->lastOilChangeDate = "2016";
        car->maker.makerName = "Fabrikam";
        car->maker.style = "sedan";
        car->maker.year = 2014;
        car->state.reported_maxSpeed = 100;
        car->state.softwareVersion = 1;
        car->state.vanityPlate = "1I1";
        */

        // IoTHubDeviceTwin_SendReportedStateCar sends the reported status back to IoT Hub
        // to the associated device twin.
        //
        // IoTHubDeviceTwin_SendReportedStateCar is an auto-generated function, created 
        // via the macro DECLARE_DEVICETWIN_MODEL(Car,...).  It resolves to the underlying function
        // IoTHubDeviceTwin_SendReportedState_Impl().
        if (IoTHubDeviceTwin_SendReportedStateSettings(car, deviceTwinReportStateCallback, NULL) != IOTHUB_CLIENT_OK)
        {
            (void)printf("Failed sending serialized reported state\n");
        }
        else
        {
            printf("Reported state will be send to IoTHub\n");

            // Comment out the following three lines if you want to enable callback(s) for updates of the existing model (example: onDesiredMaxSpeed)
            if (IoTHubClient_SetDeviceTwinCallback(iotHubClientHandle, deviceTwinGetStateCallback, NULL) != IOTHUB_CLIENT_OK)
            {
                (void)printf("Failed subscribing for device twin properties\n");
            }
        }
    }
}
