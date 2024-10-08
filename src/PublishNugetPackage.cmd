@echo off
".nuget\NuGet.exe" push "Xid.Net\Bin\Release\%1" -Source https://www.nuget.org/api/v2/package -SkipDuplicate

echo Press any key to end
pause