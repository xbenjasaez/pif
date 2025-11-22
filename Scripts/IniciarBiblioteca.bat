@echo off
setlocal

rem Este archivo solo delega en el script PowerShell con las mismas opciones.
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%LaunchBiblioteca.ps1"

if not exist "%PS_SCRIPT%" (
    echo [ERROR] No se encontró %PS_SCRIPT%.
    pause
    exit /b 1
)

powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*
exit /b %ERRORLEVEL%

