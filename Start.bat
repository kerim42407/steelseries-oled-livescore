@echo off
title Live Score
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0LiveScore.ps1"
echo.
echo Stopped. Press any key to close...
pause >nul
