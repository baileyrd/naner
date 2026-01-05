# Test-NanerConfig.ps1
# Diagnostic script to test Naner configuration loading

param(
    [string]$Profile = $null
)

Write-Host "Naner Configuration Diagnostic Tool" -ForegroundColor Cyan
Write-Host "====================================" -ForegroundColor Cyan
Write-Host ""

# Source the main script functions
$scriptPath = Join-Path $PSScriptRoot "Launch-Naner.ps1"
if (-not (Test-Path $scriptPath)) {
    Write-Host "ERROR: Launch-Naner.ps1 not found at: $scriptPath" -ForegroundColor Red
    exit 1
}

# Extract and source the functions we need
. $scriptPath

Write-Host "[1] Finding Naner Root..." -ForegroundColor Yellow
$nanerRoot = Get-NanerRoot
if ($nanerRoot) {
    Write-Host "    SUCCESS: $nanerRoot" -ForegroundColor Green
}
else {
    Write-Host "    FAILED: Could not find Naner root" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "[2] Loading Configuration..." -ForegroundColor Yellow
$config = Get-NanerConfig
if ($config) {
    Write-Host "    SUCCESS: Configuration loaded" -ForegroundColor Green
    Write-Host "    Config Path: $($config.ConfigPath)" -ForegroundColor Gray
}
else {
    Write-Host "    FAILED: Could not load configuration" -ForegroundColor Red
    exit 1
}
Write-Host ""

Write-Host "[3] Configuration Details:" -ForegroundColor Yellow
Write-Host "    Naner Root: $($config.NanerRoot)" -ForegroundColor White
Write-Host "    Default Profile: $($config.DefaultProfile)" -ForegroundColor White
Write-Host "    Startup Dir: $($config.StartupDir)" -ForegroundColor White
Write-Host "    Windows Terminal Path: $($config.WindowsTerminalPath)" -ForegroundColor White
Write-Host ""

Write-Host "[4] Custom Profiles:" -ForegroundColor Yellow
if ($config.CustomProfiles -and $config.CustomProfiles.Count -gt 0) {
    Write-Host "    Found $($config.CustomProfiles.Count) custom profile(s)" -ForegroundColor Green
    foreach ($key in $config.CustomProfiles.Keys) {
        Write-Host ""
        Write-Host "    Profile: '$key'" -ForegroundColor Cyan
        $profileConfig = $config.CustomProfiles[$key]
        Write-Host "      Shell Path: $($profileConfig.ShellPath)" -ForegroundColor White
        Write-Host "      Arguments: $($profileConfig.Arguments)" -ForegroundColor White
        Write-Host "      Title: $($profileConfig.Title)" -ForegroundColor White
        
        # Test if shell path exists
        if ($profileConfig.ShellPath) {
            if (Test-Path $profileConfig.ShellPath) {
                Write-Host "      Status: Shell exists ✓" -ForegroundColor Green
            }
            else {
                Write-Host "      Status: Shell NOT FOUND ✗" -ForegroundColor Red
            }
        }
    }
}
else {
    Write-Host "    No custom profiles defined" -ForegroundColor Gray
}
Write-Host ""

Write-Host "[5] Testing Profile Selection:" -ForegroundColor Yellow
if ($Profile) {
    Write-Host "    Testing profile: '$Profile'" -ForegroundColor White
    
    # Check if it's a custom profile
    if ($config.CustomProfiles -and $config.CustomProfiles.ContainsKey($Profile)) {
        Write-Host "    ✓ Custom profile found!" -ForegroundColor Green
        $profileConfig = $config.CustomProfiles[$Profile]
        Write-Host "      Will use: $($profileConfig.ShellPath)" -ForegroundColor Cyan
        Write-Host "      With args: $($profileConfig.Arguments)" -ForegroundColor Cyan
    }
    else {
        Write-Host "    ✗ Not found in custom profiles" -ForegroundColor Yellow
        Write-Host "      Will try as standard Windows Terminal profile" -ForegroundColor Gray
    }
}
else {
    Write-Host "    No profile specified, using default: $($config.DefaultProfile)" -ForegroundColor White
    
    if ($config.CustomProfiles -and $config.CustomProfiles.ContainsKey($config.DefaultProfile)) {
        Write-Host "    ✓ Default profile is a custom profile!" -ForegroundColor Green
    }
}
Write-Host ""

Write-Host "[6] Raw Configuration File:" -ForegroundColor Yellow
$configFile = Join-Path $config.ConfigPath "user-settings.json"
if (Test-Path $configFile) {
    Write-Host "    Location: $configFile" -ForegroundColor White
    Write-Host "    Contents:" -ForegroundColor Gray
    Write-Host "    ----------------------------------------" -ForegroundColor Gray
    Get-Content $configFile | ForEach-Object { Write-Host "    $_" -ForegroundColor Gray }
    Write-Host "    ----------------------------------------" -ForegroundColor Gray
}
else {
    Write-Host "    Configuration file not found!" -ForegroundColor Red
}
Write-Host ""

Write-Host "[7] Environment Variable Expansion Test:" -ForegroundColor Yellow
if ($config.CustomProfiles -and $config.CustomProfiles.Count -gt 0) {
    foreach ($key in $config.CustomProfiles.Keys) {
        $profileConfig = $config.CustomProfiles[$key]
        if ($profileConfig.ShellPath) {
            Write-Host "    Profile '$key':" -ForegroundColor White
            Write-Host "      Expanded path: $($profileConfig.ShellPath)" -ForegroundColor Cyan
        }
    }
}
Write-Host ""

Write-Host "====================================" -ForegroundColor Cyan
Write-Host "Diagnostic Complete!" -ForegroundColor Green
Write-Host ""
Write-Host "To launch with a specific profile:" -ForegroundColor Yellow
Write-Host "  .\Launch-Naner.ps1 -Profile 'YourProfileName' -Verbose" -ForegroundColor White
Write-Host ""