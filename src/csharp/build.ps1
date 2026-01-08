# Build naner.exe
# Creates a single-file self-contained executable for Windows

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean,

    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"

# Paths
$ProjectPath = "$PSScriptRoot\Naner.Launcher\Naner.Launcher.csproj"
$OutputPath = "$PSScriptRoot\..\..\bin"
$DotNetExe = "$PSScriptRoot\..\..\vendor\dotnet-sdk\dotnet.exe"

# Colors
function Write-Status { param($msg) Write-Host "[*] $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[+] $msg" -ForegroundColor Green }
function Write-Failure { param($msg) Write-Host "[-] $msg" -ForegroundColor Red }

Write-Host ""
Write-Host "Naner C# Build Script" -ForegroundColor Cyan
Write-Host "=====================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is available
if (-not (Test-Path $DotNetExe)) {
    Write-Failure ".NET SDK not found at: $DotNetExe"
    Write-Host ""
    Write-Host "Please install .NET SDK vendor first:" -ForegroundColor Yellow
    Write-Host "  .\src\powershell\Setup-NanerVendor.ps1 -VendorId DotNetSDK" -ForegroundColor Yellow
    Write-Host ""
    exit 1
}

Write-Success "Found .NET SDK: $DotNetExe"

# Clean if requested
if ($Clean) {
    Write-Status "Cleaning previous builds..."
    & $DotNetExe clean $ProjectPath -c $Configuration
    if ($LASTEXITCODE -ne 0) {
        Write-Failure "Clean failed"
        exit 1
    }
    Write-Success "Clean complete"
}

if ($NoBuild) {
    Write-Host "Skipping build (-NoBuild specified)" -ForegroundColor Yellow
    exit 0
}

# Build and publish
Write-Status "Building naner.exe ($Configuration)..."
Write-Host ""

& $DotNetExe publish $ProjectPath `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:PublishTrimmed=false `
    -o $OutputPath

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Failure "Build failed"
    exit 1
}

Write-Host ""
Write-Success "Build successful!"
Write-Host ""

# Show output info
$exePath = Join-Path $OutputPath "naner.exe"
if (Test-Path $exePath) {
    $exeSize = (Get-Item $exePath).Length / 1MB
    Write-Host "Output:" -ForegroundColor Gray
    Write-Host "  Location: $exePath" -ForegroundColor Gray
    Write-Host "  Size: $([math]::Round($exeSize, 2)) MB" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Test it:" -ForegroundColor Yellow
    Write-Host "  .\bin\naner.exe --version" -ForegroundColor Yellow
    Write-Host "  .\bin\naner.exe --help" -ForegroundColor Yellow
    Write-Host "  .\bin\naner.exe --profile PowerShell" -ForegroundColor Yellow
} else {
    Write-Failure "naner.exe not found in output directory"
    exit 1
}

Write-Host ""
