SET pver=1.0.1
Echo Version: "%pver%"
dotnet nuget push nupkg\Proxemity.%pver%.nupkg -source https://api.nuget.org/v3/index.json
pause