@echo off
REM ContainerSuite one-click pack. Double-click to run.
REM Calls pack.ps1 with ExecutionPolicy Bypass so no signing needed.

setlocal
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0pack.ps1" %*
echo.
echo Press any key to close...
pause >nul
endlocal