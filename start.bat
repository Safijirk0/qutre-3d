@echo off
setlocal
set "ROOT=%~dp0"
set "DOTNET=%USERPROFILE%\dotnet\dotnet.exe"

if not exist "%DOTNET%" set "DOTNET=dotnet"

echo [Qutre 3D] building...
"%DOTNET%" build "%ROOT%Qutre3D.sln" -c Release || goto :error

echo [Qutre 3D] launching...
start "" "%ROOT%src\Qutre3D\bin\Release\net8.0\Qutre3D.exe"
exit /b 0

:error
echo.
echo Build failed. Check the log above.
pause
exit /b 1
