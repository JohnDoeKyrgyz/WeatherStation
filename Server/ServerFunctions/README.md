# ServerFunctions

## Overview

The ServerFunctions project creates an assembly which contains Azure Functions. The main function is WundergroundForwarder which responds to events, and forwards them on to Wunderground.

## Development Setup

Start in the ServerFunctions Directory

```cmd
cd ServerFunctions
```

Install the Azure Function Tools. See [Microsoft Documentation](https://blogs.msdn.microsoft.com/appserviceteam/2017/09/25/develop-azure-functions-on-any-platform/) for full details.

```cmd
npm i -g azure-functions-core-tools@core
```

Install the EventHubTrigger extension See [Microsoft Documentation](https://docs.microsoft.com/en-us/azure/azure-functions/functions-triggers-bindings#local-development-azure-functions-core-tools)

```cmd
func extensions install --package Microsoft.Azure.WebJobs.Extensions.EventHubs --version 3.0.0-beta8
```

Set the Azure Functions Runtime in the local.settings.json file

```cmd
func settings add FUNCTIONS_WORKER_RUNTIME dotnet
func settings add FUNCTIONS_EXTENSION_VERSION ~2
```

Run the functions locally

```cmd
func host start --script-root .\bin\Debug\netstandard2.0\
```
