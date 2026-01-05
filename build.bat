@echo off
REM Build script for Cmder Windows Terminal Launcher (C# version)

setlocal enabledelayedexpansion

echo ====================================
echo Cmder Launcher - C# Build Script
echo ====================================
echo.

REM Check if .NET SDK is installed
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found!
    echo.
    echo Please install .NET 6 SDK from:
    echo https://dotnet.microsoft.com/download/dotnet/6.0
    echo.
    exit /b 1
)

echo [1/5] .NET SDK found: 
dotnet --version
echo.

REM Navigate to C# project directory
cd /d "%~dp0src\csharp"

echo [2/5] Restoring NuGet packages...
dotnet restore
if %errorlevel% neq 0 (
    echo ERROR: Failed to restore packages
    exit /b 1
)
echo.

echo [3/5] Building project...
dotnet build -c Release
if %errorlevel% neq 0 (
    echo ERROR: Build failed
    exit /b 1
)
echo.

echo [4/5] Publishing single-file executable...
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
if %errorlevel% neq 0 (
    echo ERROR: Publish failed
    exit /b 1
)
echo.

echo [5/5] Copying to root directory...
copy /Y "bin\Release\net6.0-windows\win-x64\publish\Cmder.exe" "..\..\Cmder.exe"
echo.

echo ====================================
echo BUILD SUCCESSFUL!
echo ====================================
echo.
echo Executable location:
echo   %~dp0Cmder.exe
echo.
echo File size:
for %%F in ("%~dp0Cmder.exe") do echo   %%~zF bytes
echo.

REM Test the executable
echo Testing executable...
"%~dp0Cmder.exe" --help
echo.

echo ====================================
echo Build complete! Ready to use.
echo ====================================

endlocal
