# Build both naner.exe and naner-init.exe
# Creates single-file self-contained executables for Windows

param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [switch]$Clean,

    [switch]$NoBuild,

    [switch]$InitOnly,

    [switch]$NanerOnly,

    [switch]$IncrementVersion
)

$ErrorActionPreference = "Stop"

# Paths
$LauncherProjectPath = "$PSScriptRoot\Naner.Launcher\Naner.Launcher.csproj"
$InitProjectPath = "$PSScriptRoot\Naner.Init\Naner.Init.csproj"
$NanerOutputPath = "$PSScriptRoot\..\..\vendor\bin"
$InitOutputPath = "$PSScriptRoot\..\..\vendor\bin"
$DotNetExe = "$PSScriptRoot\..\..\vendor\dotnet-sdk\dotnet.exe"
$DirectoryBuildPropsPath = "$PSScriptRoot\Directory.Build.props"

# Check for system dotnet if vendor dotnet not found
if (-not (Test-Path $DotNetExe)) {
    $DotNetExe = "dotnet"
    # Test if dotnet is in PATH
    try {
        & $DotNetExe --version | Out-Null
    } catch {
        Write-Host "[-] .NET SDK not found in vendor or PATH" -ForegroundColor Red
        Write-Host ""
        Write-Host "Please install .NET SDK:" -ForegroundColor Yellow
        Write-Host "  Option 1: Install vendor SDK with Setup-NanerVendor.ps1 -VendorId DotNetSDK" -ForegroundColor Yellow
        Write-Host "  Option 2: Install from https://dotnet.microsoft.com/download" -ForegroundColor Yellow
        Write-Host ""
        exit 1
    }
}

# Colors
function Write-Status { param($msg) Write-Host "[*] $msg" -ForegroundColor Cyan }
function Write-Success { param($msg) Write-Host "[+] $msg" -ForegroundColor Green }
function Write-Failure { param($msg) Write-Host "[-] $msg" -ForegroundColor Red }

# Function to increment patch version in Directory.Build.props
function Update-PatchVersion {
    if (-not (Test-Path $DirectoryBuildPropsPath)) {
        Write-Failure "Directory.Build.props not found at $DirectoryBuildPropsPath"
        return $false
    }

    $content = Get-Content $DirectoryBuildPropsPath -Raw

    # Extract current version
    if ($content -match '<Version>(\d+)\.(\d+)\.(\d+)</Version>') {
        $major = [int]$Matches[1]
        $minor = [int]$Matches[2]
        $patch = [int]$Matches[3]
        $oldVersion = "$major.$minor.$patch"
        $newPatch = $patch + 1
        $newVersion = "$major.$minor.$newPatch"

        Write-Status "Incrementing version: $oldVersion -> $newVersion"

        # Replace all version occurrences
        $content = $content -replace "<Version>$oldVersion</Version>", "<Version>$newVersion</Version>"
        $content = $content -replace "<AssemblyVersion>$oldVersion\.0</AssemblyVersion>", "<AssemblyVersion>$newVersion.0</AssemblyVersion>"
        $content = $content -replace "<FileVersion>$oldVersion\.0</FileVersion>", "<FileVersion>$newVersion.0</FileVersion>"
        $content = $content -replace "<InformationalVersion>$oldVersion</InformationalVersion>", "<InformationalVersion>$newVersion</InformationalVersion>"

        Set-Content $DirectoryBuildPropsPath -Value $content -NoNewline
        Write-Success "Version updated to $newVersion"
        return $true
    } else {
        Write-Failure "Could not parse version from Directory.Build.props"
        return $false
    }
}

Write-Host ""
Write-Host "Naner C# Build Script - Build All" -ForegroundColor Cyan
Write-Host "==================================" -ForegroundColor Cyan
Write-Host ""

Write-Success "Using .NET SDK: $DotNetExe"
Write-Host ""

# Increment version if requested
if ($IncrementVersion) {
    if (-not (Update-PatchVersion)) {
        exit 1
    }
    Write-Host ""
}

# Function to build a project
function Build-Project {
    param(
        [string]$ProjectPath,
        [string]$ProjectName,
        [string]$OutputPath,
        [string]$ExeName
    )

    # Clean if requested
    if ($Clean) {
        Write-Status "Cleaning $ProjectName..."
        & $DotNetExe clean $ProjectPath -c $Configuration
        if ($LASTEXITCODE -ne 0) {
            Write-Failure "Clean failed for $ProjectName"
            return $false
        }
        Write-Success "Clean complete for $ProjectName"
    }

    if ($NoBuild) {
        Write-Host "Skipping build for $ProjectName (-NoBuild specified)" -ForegroundColor Yellow
        return $true
    }

    # Build and publish
    Write-Status "Building $ProjectName ($Configuration)..."
    Write-Host ""

    & $DotNetExe publish $ProjectPath `
        -c $Configuration `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:IncludeNativeLibrariesForSelfExtract=true `
        -p:PublishTrimmed=true `
        -o $OutputPath

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Failure "Build failed for $ProjectName"
        return $false
    }

    Write-Host ""
    Write-Success "Build successful for $ProjectName!"
    Write-Host ""

    # Show output info
    $exePath = Join-Path $OutputPath $ExeName
    if (Test-Path $exePath) {
        $exeSize = (Get-Item $exePath).Length / 1MB
        Write-Host "  Location: $exePath" -ForegroundColor Gray
        Write-Host "  Size: $([math]::Round($exeSize, 2)) MB" -ForegroundColor Gray
        Write-Host ""
    } else {
        Write-Failure "$ExeName not found in output directory"
        return $false
    }

    return $true
}

# Build naner-init.exe
if (-not $NanerOnly) {
    if (Build-Project -ProjectPath $InitProjectPath -ProjectName "naner-init" -OutputPath $InitOutputPath -ExeName "naner-init.exe") {
        Write-Success "naner-init.exe built successfully"
    } else {
        Write-Failure "Failed to build naner-init.exe"
        exit 1
    }
    Write-Host ""
}

# Build naner.exe
if (-not $InitOnly) {
    if (Build-Project -ProjectPath $LauncherProjectPath -ProjectName "naner" -OutputPath $NanerOutputPath -ExeName "naner.exe") {
        Write-Success "naner.exe built successfully"
    } else {
        Write-Failure "Failed to build naner.exe"
        exit 1
    }
    Write-Host ""
}

Write-Host ""
Write-Success "All builds completed successfully!"
Write-Host ""
Write-Host "Test it:" -ForegroundColor Yellow
Write-Host "  .\vendor\bin\naner-init.exe --version" -ForegroundColor Yellow
Write-Host "  .\vendor\bin\naner-init.exe --help" -ForegroundColor Yellow
Write-Host "  .\vendor\bin\naner.exe --version" -ForegroundColor Yellow
Write-Host "  .\vendor\bin\naner.exe --help" -ForegroundColor Yellow
Write-Host ""
