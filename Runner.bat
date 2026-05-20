@echo off
title FarmCord Auto Updater

:loop

echo ============================
echo Checking for updates...
echo ============================

git pull

echo.
echo Restoring dependencies...
dotnet restore

echo.
echo Building FarmCord...
dotnet build -c Release

echo.
echo Stopping old FarmCord instance...
taskkill /F /IM dotnet.exe >nul 2>&1

echo.
echo Starting FarmCord...

start "" dotnet run -c Release

echo.
echo FarmCord running.
echo Waiting 12 hours before next update...
echo.

timeout /t 43200 /nobreak

goto loop