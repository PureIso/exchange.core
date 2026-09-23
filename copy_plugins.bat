@echo off
setlocal
powershell.exe -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "%~dp0scripts\package-plugins.ps1" %*
exit /b %ERRORLEVEL%
