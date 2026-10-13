@echo off
REM ContainerSuite one-click pack (DEV build). Double-click to run.
REM Builds with -p:Diag=true -> CustomInfrastructure-dev.dll
REM -> dist\ContainerSuite-dev-v{ver}\ + .zip
REM Calls pack.ps1 with ExecutionPolicy Bypass so no signing needed.

setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pack.ps1" -Dev %*
echo.
echo Press any key to close...
pause >nul
endlocal
