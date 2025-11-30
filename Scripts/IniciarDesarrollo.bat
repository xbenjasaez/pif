@echo off
setlocal

rem Inicia la aplicación en modo desarrollo (más rápido, sin publish)
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%LaunchBiblioteca.ps1"

if not exist "%PS_SCRIPT%" (
    echo [ERROR] No se encontró %PS_SCRIPT%.
    pause
    exit /b 1
)

echo === Modo Desarrollo (rapido) ===
powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" -DevMode %*
exit /b %ERRORLEVEL%

