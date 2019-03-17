@echo off
cls

.paket\paket.bootstrapper.exe
if errorlevel 1 (
  exit /b %errorlevel%
)

.paket\paket.exe restore --force
if errorlevel 1 (
  exit /b %errorlevel%
)

dotnet clean
dotnet build
"C:\Program Files (x86)\Microsoft SDKs\Azure\Storage Emulator\AzureStorageEmulator.exe"
dotnet Tests\WeatherStationFunctions.Tests\bin\Debug\netcoreapp2.0\WeatherStationFunctions.Tests.dll
dotnet Tests\Server.Tests\bin\Debug\netcoreapp2.0\Server.Tests.dll

cd WebPortal
fake --target build