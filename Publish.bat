@echo off

echo =======================================================================
echo Bing.Utils
echo =======================================================================

::create nuget_packages
if not exist nuget_packages (
    md nuget_packages
    echo Create nuget_packages folder.
)

::clear nuget_packages
for /R "nuget_packages" %%s in (*) do (
    del %%s
)
echo Cleaned up all nuget packages.
echo.

::start to package all projects

::Utils
dotnet pack src/Bing.Utils -c Release -o nuget_packages
dotnet pack src/Bing.Utils.DateTime -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Text -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Drawing -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Drawing.ImageSharp -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Drawing.SkiaSharp -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Http -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Collections -c Release -o nuget_packages
dotnet pack src/Bing.Utils.Reflection -c Release -o nuget_packages
dotnet pack src/Bing.Utils.IdUtils -c Release -o nuget_packages

for /R "nuget_packages" %%s in (*symbols.nupkg) do (
    del %%s
)

echo.
echo.

set /p key=input key:
set source=https://api.nuget.org/v3/index.json

for /R "nuget_packages" %%s in (*.nupkg) do (
    call dotnet nuget push %%s -k %key% -s %source% --skip-duplicate
    echo.
)

pause