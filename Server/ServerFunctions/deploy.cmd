cd Server\ServerFunctions
dotnet restore WeatherStationFunctions.fsproj
dotnet build WeatherStationFunctions.fsproj

dotnet restore extensions.csproj
dotnet build extensions.csproj

dotnet publish