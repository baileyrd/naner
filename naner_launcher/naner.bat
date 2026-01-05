@echo off
REM Naner Windows Terminal Launcher - Batch Wrapper
REM This allows users to run the PowerShell script easily

setlocal

REM Get the directory where this batch file is located
set "SCRIPT_DIR=%~dp0"
set "PS_SCRIPT=%SCRIPT_DIR%src\powershell\Launch-Naner.ps1"

REM Check if PowerShell script exists
if not exist "%PS_SCRIPT%" (
    echo ERROR: PowerShell script not found at %PS_SCRIPT%
    exit /b 1
)

REM Execute PowerShell script with all arguments
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%PS_SCRIPT%" %*

endlocal
