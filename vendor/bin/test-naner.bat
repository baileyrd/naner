@echo off
setlocal enabledelayedexpansion

echo =====================================
echo Naner Executable Test Suite
echo =====================================
echo.

:: Save original directory
set "ORIGINAL_DIR=%CD%"

:: Determine script directory and navigate there
set "SCRIPT_DIR=%~dp0"
cd /d "%SCRIPT_DIR%"

echo Current Directory: %CD%
echo Script Location: %SCRIPT_DIR%
echo.

echo =====================================
echo TEST 1: Version Check
echo =====================================
echo Command: naner.exe --version
echo.
naner.exe --version
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
if %EXIT_CODE% EQU 0 (
    echo Status: PASS
) else (
    echo Status: FAIL
)
echo.
echo Press any key to continue...
pause >nul
echo.

echo =====================================
echo TEST 2: Help Output
echo =====================================
echo Command: naner.exe --help
echo.
naner.exe --help
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
if %EXIT_CODE% EQU 0 (
    echo Status: PASS
) else (
    echo Status: FAIL
)
echo.
echo Press any key to continue...
pause >nul
echo.

echo =====================================
echo TEST 3: Default Launch (Dry Run)
echo =====================================
echo Command: naner.exe --debug
echo Note: This will show debug output but not launch
echo.
naner.exe --debug
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
if %EXIT_CODE% EQU 0 (
    echo Status: PASS
) else (
    echo Status: FAIL
)
echo.
echo Press any key to continue...
pause >nul
echo.

echo =====================================
echo TEST 4: Profile Selection
echo =====================================
echo Command: naner.exe --profile PowerShell --debug
echo Note: Check if profile selection works
echo.
naner.exe --profile PowerShell --debug
set "EXIT_CODE=%ERRORLEVEL%"
echo.
echo Exit Code: %EXIT_CODE%
if %EXIT_CODE% EQU 0 (
    echo Status: PASS
) else (
    echo Status: FAIL
)
echo.

echo =====================================
echo Test Suite Complete
echo =====================================
echo.
echo All tests executed.
echo Review the output above for any errors.
echo.
echo Press any key to exit...
pause >nul

:: Restore original directory
cd /d "%ORIGINAL_DIR%"
