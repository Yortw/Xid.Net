@echo off
echo Press any key to publish
pause
".nuget\NuGet.exe" push Xid.Net.1.0.1.nupkg -Source https://www.nuget.org/api/v2/package
pause