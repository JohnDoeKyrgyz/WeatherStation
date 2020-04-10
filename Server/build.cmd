@echo off
cls

dotnet clean
dotnet restore
dotnet build
"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start
dotnet run --project Tests\WeatherStationFunctions.Tests\WeatherStationFunctions.Tests.fsproj
dotnet run --project Tests\Server.Tests\Server.Tests.fsproj

dotnet fake build --target build