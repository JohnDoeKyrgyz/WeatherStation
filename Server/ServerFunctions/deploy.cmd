dotnet restore Server\ServerFunctions\extensions.csproj
dotnet build Server\ServerFunctions\extensions.csproj

dotnet restore Server\ServerFunctions\WeatherStationFunctions.fsproj
dotnet build Server\ServerFunctions\WeatherStationFunctions.fsproj

dotnet restore Server\Tests\WeatherStationFunctions.Tests\WeatherStationFunctions.Tests.fsproj
dotnet build Server\Tests\WeatherStationFunctions.Tests\WeatherStationFunctions.Tests.fsproj
dotnet run --project Server\Tests\WeatherStationFunctions.Tests\WeatherStationFunctions.Tests.fsproj