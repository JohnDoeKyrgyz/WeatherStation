// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#ifndef IOT_CONFIGS_H
#define IOT_CONFIGS_H

/**
 * WiFi setup
 */
#define IOT_CONFIG_WIFI_SSID            "Atwood.Net"
#define IOT_CONFIG_WIFI_PASSWORD        "RileyEmmett"

/**
 * Find under Microsoft Azure IoT Suite -> DEVICES -> <your device> -> Device Details and Authentication Keys
 * String containing Hostname, Device Id & Device Key in the format:
 *  "HostName=<host_name>;DeviceId=<device_id>;SharedAccessKey=<device_key>"    
 */
#define IOT_CONFIG_CONNECTION_STRING    "HostName=AtwoodIoT.azure-devices.net;DeviceId=TemperatureMonitor;SharedAccessKey=9HdpnA2Irjpp0/TnbyLj2KPz+l16YlTRsha+hP+Z8O8=;GatewayHostName=ssl://AtwoodIot:8883"

/** 
 * Choose the transport protocol
 */
#define IOT_CONFIG_MQTT                 // uncomment this line for MQTT
// #define IOT_CONFIG_HTTP              // uncomment this line for HTTP

#endif /* IOT_CONFIGS_H */
