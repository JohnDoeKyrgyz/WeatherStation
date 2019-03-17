# WeatherStation Functions

Server side logic to handle incomming messages from weather stations. Messages are logged in an Azure Storage table, as well as forwarded on to Wunderground.

## Development

To run functions locally you need to:

1. Install the [azure functions 2.0 core tools](https://docs.microsoft.com/en-us/azure/azure-functions/functions-run-local#install-the-azure-functions-core-tools)

```command
npm install -g azure-functions-core-tools
```

2. Install the azure cli (az cli)

3. Download the settings from the azure functions installation

```command
func azure functionapp fetch-app-settings WeatherStations
```

4. Register the extensions

```command
func extensions install
```
5. Change into the build output directory.

```command
cd bin\Debug\netstandard2.0
```

6. Run the functions

```command
func host start
```

Deployment attempts .