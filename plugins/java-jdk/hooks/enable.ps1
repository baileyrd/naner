param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Enabling Java JDK plugin..." -ForegroundColor Cyan

$javaHome = Join-Path $Context.NanerRoot "vendor\java"

# Check if JDK is installed
if (-not (Test-Path $javaHome)) {
    Write-Warning "Java JDK not found in vendor directory."
    Write-Host "Please run: Setup-NanerVendor.ps1 -VendorId java-jdk" -ForegroundColor Yellow
    return
}

# Verify Java installation
$javaExe = Join-Path $javaHome "bin\java.exe"
if (Test-Path $javaExe) {
    $version = & $javaExe -version 2>&1 | Select-Object -First 1
    Write-Host "Java JDK enabled: $version" -ForegroundColor Green
}

Write-Host "Java environment variables will be set on next shell launch" -ForegroundColor Green
