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
dotnet Tests\WeatherStationFunctions.Tests\bin\Debug\netcoreapp2.0\WeatherStationFunctions.Tests.dll