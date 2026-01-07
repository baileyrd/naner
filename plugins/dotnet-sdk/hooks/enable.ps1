param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Enabling .NET SDK plugin..." -ForegroundColor Cyan

$dotnetRoot = Join-Path $Context.NanerRoot "vendor\dotnet"

# Check if .NET SDK is installed
if (-not (Test-Path $dotnetRoot)) {
    Write-Warning ".NET SDK not found in vendor directory."
    Write-Host "Please run: Setup-NanerVendor.ps1 -VendorId dotnet-sdk" -ForegroundColor Yellow
    return
}

# Verify .NET installation
$dotnetExe = Join-Path $dotnetRoot "dotnet.exe"
if (Test-Path $dotnetExe) {
    $version = & $dotnetExe --version 2>&1
    Write-Host ".NET SDK enabled: $version" -ForegroundColor Green
}

# Create NuGet packages directory
$nugetDir = Join-Path $Context.NanerRoot "home\.nuget\packages"
if (-not (Test-Path $nugetDir)) {
    New-Item -ItemType Directory -Path $nugetDir -Force | Out-Null
}

Write-Host ".NET environment variables will be set on next shell launch" -ForegroundColor Green
