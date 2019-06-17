@echo off
cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore
if errorlevel 1 (
  exit /b %errorlevel%
)

dotnet clean
dotnet build
"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe" start
dotnet run --project Tests\WeatherStationFunctions.Tests\WeatherStationFunctions.Tests.fsproj
dotnet run --project Tests\Server.Tests\Server.Tests.fsproj

fake build --target build