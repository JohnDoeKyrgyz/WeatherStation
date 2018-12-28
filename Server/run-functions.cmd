cd ServerFunctions
dotnet clean ServerFunctions.sln
dotnet restore ServerFunctions.sln
dotnet build ServerFunctions.sln
call func extensions install

cd bin\Debug\netstandard2.0
func host start