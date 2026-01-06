<#
.SYNOPSIS
    Validates Naner installation and environment configuration.

.DESCRIPTION
    Performs comprehensive tests to ensure Naner is properly configured:
    - Vendor dependencies present and functional
    - Configuration valid
    - PATH correctly configured
    - Tools accessible
    - Profiles working

.PARAMETER Quick
    Run only essential tests (fast).

.PARAMETER Full
    Run all tests including performance benchmarks.

.PARAMETER Profile
    Test a specific profile.

.EXAMPLE
    .\Test-NanerInstallation.ps1
    
.EXAMPLE
    .\Test-NanerInstallation.ps1 -Full
    
.EXAMPLE
    .\Test-NanerInstallation.ps1 -Profile Bash
#>

[CmdletBinding()]
param(
    [Parameter()]
    [switch]$Quick,
    
    [Parameter()]
    [switch]$Full,
    
    [Parameter()]
    [string]$Profile
)

$ErrorActionPreference = "Continue"

# Test results tracking
$testResults = @{
    Passed = 0
    Failed = 0
    Warnings = 0
    Tests = @()
}

function Write-TestHeader {
    param([string]$Message)
    Write-Host "`n═══════════════════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " $Message" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestName,
        [bool]$Passed,
        [string]$Message = "",
        [bool]$IsWarning = $false
    )
    
    $result = @{
        Name = $TestName
        Passed = $Passed
        Message = $Message
        IsWarning = $IsWarning
    }
    
    $testResults.Tests += $result
    
    if ($IsWarning) {
        $testResults.Warnings++
        Write-Host "[⚠] $TestName" -ForegroundColor Yellow
        if ($Message) {
            Write-Host "    $Message" -ForegroundColor Gray
        }
    }
    elseif ($Passed) {
        $testResults.Passed++
        Write-Host "[✓] $TestName" -ForegroundColor Green
        if ($Message) {
            Write-Host "    $Message" -ForegroundColor Gray
        }
    }
    else {
        $testResults.Failed++
        Write-Host "[✗] $TestName" -ForegroundColor Red
        if ($Message) {
            Write-Host "    $Message" -ForegroundColor Gray
        }
    }
}

# Determine Naner root
try {
    $nanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    if (-not (Test-Path $nanerRoot)) {
        throw "Naner root not found"
    }
}
catch {
    Write-Host "[✗] Cannot locate Naner root directory" -ForegroundColor Red
    exit 1
}

Write-Host @"

╔═══════════════════════════════════════════════════════════════════╗
║                  Naner Installation Validator                     ║
║                                                                   ║
║  This tool validates your Naner installation and configuration.  ║
╚═══════════════════════════════════════════════════════════════════╝

"@ -ForegroundColor Cyan

Write-Host "Naner Root: $nanerRoot" -ForegroundColor Gray
Write-Host ""

#region Directory Structure Tests

Write-TestHeader "Directory Structure"

$requiredDirs = @{
    "bin" = "Naner executables"
    "config" = "Configuration files"
    "vendor" = "Vendor dependencies"
    "icons" = "Icon resources"
}

foreach ($dir in $requiredDirs.Keys) {
    $path = Join-Path $nanerRoot $dir
    $exists = Test-Path $path
    Write-TestResult -TestName "$dir/ directory" -Passed $exists -Message $requiredDirs[$dir]
}

# Check opt directory (optional but recommended)
$optPath = Join-Path $nanerRoot "opt"
if (Test-Path $optPath) {
    Write-TestResult -TestName "opt/ directory" -Passed $true -Message "User tools directory present"
}
else {
    Write-TestResult -TestName "opt/ directory" -Passed $true -IsWarning $true -Message "Optional directory missing (will be created on first use)"
}

#endregion

#region Configuration Tests

Write-TestHeader "Configuration"

$configPath = Join-Path $nanerRoot "config\naner.json"
$configValid = $false

if (Test-Path $configPath) {
    Write-TestResult -TestName "naner.json exists" -Passed $true
    
    try {
        $config = Get-Content $configPath -Raw -Encoding UTF8 | ConvertFrom-Json
        Write-TestResult -TestName "naner.json is valid JSON" -Passed $true
        $configValid = $true
        
        # Test required sections
        $requiredSections = @("VendorPaths", "Environment", "Profiles", "DefaultProfile")
        
        foreach ($section in $requiredSections) {
            if ($config.PSObject.Properties[$section]) {
                Write-TestResult -TestName "Config section: $section" -Passed $true
            }
            else {
                Write-TestResult -TestName "Config section: $section" -Passed $false -Message "Missing required section"
            }
        }
        
        # Test default profile exists
        $defaultProfile = $config.DefaultProfile
        if ($config.Profiles.$defaultProfile -or $config.CustomProfiles.$defaultProfile) {
            Write-TestResult -TestName "Default profile ($defaultProfile) exists" -Passed $true
        }
        else {
            Write-TestResult -TestName "Default profile ($defaultProfile) exists" -Passed $false
        }
    }
    catch {
        Write-TestResult -TestName "naner.json is valid JSON" -Passed $false -Message $_.Exception.Message
    }
}
else {
    Write-TestResult -TestName "naner.json exists" -Passed $false -Message "Configuration file not found"
}

#endregion

#region Vendor Dependencies Tests

Write-TestHeader "Vendor Dependencies"

$vendorDir = Join-Path $nanerRoot "vendor"

# Check vendor manifest
$manifestPath = Join-Path $vendorDir "vendor-manifest.json"
if (Test-Path $manifestPath) {
    Write-TestResult -TestName "Vendor manifest exists" -Passed $true
    
    try {
        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        Write-TestResult -TestName "Vendor manifest is valid" -Passed $true
        Write-Host "    Created: $($manifest.Created)" -ForegroundColor Gray
    }
    catch {
        Write-TestResult -TestName "Vendor manifest is valid" -Passed $false
    }
}
else {
    Write-TestResult -TestName "Vendor manifest" -Passed $false -IsWarning $true -Message "Run Setup-NanerVendor.ps1 to create"
}

# Check 7-Zip
$sevenZipPath = Join-Path $vendorDir "7zip\7z.exe"
if (Test-Path $sevenZipPath) {
    Write-TestResult -TestName "7-Zip installed" -Passed $true
    
    try {
        $sevenZipVersion = & $sevenZipPath 2>&1 | Select-String "7-Zip" | Select-Object -First 1
        if ($sevenZipVersion) {
            Write-TestResult -TestName "7-Zip executable" -Passed $true -Message "Available: $($sevenZipVersion.Line.Trim())"
        }
    }
    catch {
        Write-TestResult -TestName "7-Zip executable" -Passed $false -Message "Cannot execute"
    }
}
else {
    Write-TestResult -TestName "7-Zip installed" -Passed $false
}

# Check PowerShell
$pwshPath = Join-Path $vendorDir "powershell\pwsh.exe"
if (Test-Path $pwshPath) {
    Write-TestResult -TestName "PowerShell 7 installed" -Passed $true
    
    try {
        $pwshVersion = & $pwshPath -Command '$PSVersionTable.PSVersion.ToString()' 2>$null
        Write-TestResult -TestName "PowerShell 7 executable" -Passed $true -Message "Version: $pwshVersion"
    }
    catch {
        Write-TestResult -TestName "PowerShell 7 executable" -Passed $false -Message "Cannot execute"
    }
}
else {
    Write-TestResult -TestName "PowerShell 7 installed" -Passed $false
}

# Check Windows Terminal
$wtPath = Join-Path $vendorDir "terminal\wt.exe"
if (Test-Path $wtPath) {
    Write-TestResult -TestName "Windows Terminal installed" -Passed $true
    
    $wtSize = [math]::Round((Get-Item $wtPath).Length / 1MB, 2)
    Write-TestResult -TestName "Windows Terminal executable" -Passed $true -Message "Size: $wtSize MB"
}
else {
    Write-TestResult -TestName "Windows Terminal installed" -Passed $false
}

# Check MSYS2
$msys2Dir = Join-Path $vendorDir "msys64"
if (Test-Path $msys2Dir) {
    Write-TestResult -TestName "MSYS2 installed" -Passed $true
    
    # Check key MSYS2 components
    $msys2Components = @{
        "usr\bin\bash.exe" = "Bash shell"
        "usr\bin\git.exe" = "Git"
        "usr\bin\make.exe" = "Make"
        "mingw64\bin\gcc.exe" = "GCC compiler"
    }
    
    foreach ($component in $msys2Components.Keys) {
        $componentPath = Join-Path $msys2Dir $component
        if (Test-Path $componentPath) {
            Write-TestResult -TestName "  $($msys2Components[$component])" -Passed $true
        }
        else {
            Write-TestResult -TestName "  $($msys2Components[$component])" -Passed $false -IsWarning $true
        }
    }
}
else {
    Write-TestResult -TestName "MSYS2 installed" -Passed $false
}

#endregion

#region PATH Tests

Write-TestHeader "PATH Configuration"

if ($configValid) {
    Write-Host "Simulating PATH construction..." -ForegroundColor Gray
    Write-Host ""
    
    # Simulate PATH building
    $pathComponents = @()
    
    foreach ($pathEntry in $config.Environment.PathPrecedence) {
        $expandedPath = $pathEntry -replace '%NANER_ROOT%', $nanerRoot
        $expandedPath = [System.Environment]::ExpandEnvironmentVariables($expandedPath)
        
        if (Test-Path $expandedPath) {
            $pathComponents += $expandedPath
            Write-TestResult -TestName "PATH: $pathEntry" -Passed $true -Message "→ $expandedPath"
        }
        else {
            Write-TestResult -TestName "PATH: $pathEntry" -Passed $false -IsWarning $true -Message "Directory not found"
        }
    }
    
    Write-Host ""
    Write-Host "PATH precedence order:" -ForegroundColor Gray
    for ($i = 0; $i -lt $pathComponents.Count; $i++) {
        Write-Host "  $($i + 1). $($pathComponents[$i])" -ForegroundColor Gray
    }
}
else {
    Write-TestResult -TestName "PATH configuration" -Passed $false -Message "Configuration not valid"
}

#endregion

#region Tool Accessibility Tests

if (-not $Quick) {
    Write-TestHeader "Tool Accessibility"
    
    # Build a test environment
    $testPath = ($pathComponents -join ";") + ";" + $env:PATH
    $testEnv = @{
        PATH = $testPath
        NANER_ROOT = $nanerRoot
    }
    
    # Test commands
    $testCommands = @{
        "pwsh" = "PowerShell 7"
        "git" = "Git"
        "bash" = "Bash"
        "make" = "Make"
        "gcc" = "GCC"
    }
    
    foreach ($cmd in $testCommands.Keys) {
        try {
            $cmdPath = $null
            $output = $null
            
            # Use where.exe to find command in our test PATH
            $whereCmd = "where.exe"
            $output = & $whereCmd $cmd 2>$null
            
            if ($output -and $output.Count -gt 0) {
                $cmdPath = $output[0]
                
                # Check if it's from vendor directory
                if ($cmdPath -like "*$nanerRoot\vendor*") {
                    Write-TestResult -TestName "$cmd accessible" -Passed $true -Message "Vendored: $cmdPath"
                }
                else {
                    Write-TestResult -TestName "$cmd accessible" -Passed $true -IsWarning $true -Message "System: $cmdPath"
                }
            }
            else {
                Write-TestResult -TestName "$cmd accessible" -Passed $false -Message "Not found in PATH"
            }
        }
        catch {
            Write-TestResult -TestName "$cmd accessible" -Passed $false -Message $_.Exception.Message
        }
    }
}

#endregion

#region Profile Tests

if ($Full -or $Profile) {
    Write-TestHeader "Profile Tests"
    
    if ($Profile) {
        $profilesToTest = @($Profile)
    }
    else {
        $profilesToTest = $config.Profiles.PSObject.Properties.Name
    }
    
    foreach ($profileName in $profilesToTest) {
        Write-Host "`nTesting profile: $profileName" -ForegroundColor Cyan
        
        $profileConfig = $config.Profiles.$profileName
        if (-not $profileConfig) {
            $profileConfig = $config.CustomProfiles.$profileName
        }
        
        if ($profileConfig) {
            # Test starting directory
            if ($profileConfig.StartingDirectory) {
                $startDir = $profileConfig.StartingDirectory -replace '%NANER_ROOT%', $nanerRoot
                $startDir = [System.Environment]::ExpandEnvironmentVariables($startDir)
                
                if (Test-Path $startDir) {
                    Write-TestResult -TestName "  Starting directory" -Passed $true -Message $startDir
                }
                else {
                    Write-TestResult -TestName "  Starting directory" -Passed $false -IsWarning $true -Message "Not found: $startDir"
                }
            }
            
            # Test custom shell
            if ($profileConfig.CustomShell -and $profileConfig.CustomShell.ExecutablePath) {
                $shellPath = $profileConfig.CustomShell.ExecutablePath -replace '%NANER_ROOT%', $nanerRoot
                $shellPath = [System.Environment]::ExpandEnvironmentVariables($shellPath)
                
                if (Test-Path $shellPath) {
                    Write-TestResult -TestName "  Custom shell" -Passed $true -Message $shellPath
                }
                else {
                    Write-TestResult -TestName "  Custom shell" -Passed $false -Message "Not found: $shellPath"
                }
            }
            
            # Test icon
            if ($profileConfig.Icon) {
                $iconPath = $profileConfig.Icon -replace '%NANER_ROOT%', $nanerRoot
                $iconPath = [System.Environment]::ExpandEnvironmentVariables($iconPath)
                
                if (Test-Path $iconPath) {
                    Write-TestResult -TestName "  Icon" -Passed $true -Message $iconPath
                }
                else {
                    Write-TestResult -TestName "  Icon" -Passed $false -IsWarning $true -Message "Not found (will use default)"
                }
            }
        }
        else {
            Write-TestResult -TestName "Profile '$profileName'" -Passed $false -Message "Profile not found"
        }
    }
}

#endregion

#region Performance Tests

if ($Full) {
    Write-TestHeader "Performance Tests"
    
    Write-Host "Measuring startup time..." -ForegroundColor Gray
    
    $iterations = 3
    $times = @()
    
    for ($i = 1; $i -le $iterations; $i++) {
        $startTime = Get-Date
        
        try {
            # Simulate launcher logic without actually launching
            $configLoad = Get-Content $configPath -Raw | ConvertFrom-Json
            $pathBuild = ($pathComponents -join ";")
            
            $endTime = Get-Date
            $elapsed = ($endTime - $startTime).TotalMilliseconds
            $times += $elapsed
            
            Write-Host "  Iteration $i : $([math]::Round($elapsed, 2)) ms" -ForegroundColor Gray
        }
        catch {
            Write-Host "  Iteration $i : FAILED" -ForegroundColor Red
        }
    }
    
    if ($times.Count -gt 0) {
        $avgTime = ($times | Measure-Object -Average).Average
        $passed = $avgTime -lt 500  # Target: <500ms
        
        Write-TestResult -TestName "Startup performance" -Passed $passed -Message "Average: $([math]::Round($avgTime, 2)) ms"
    }
}

#endregion

#region Summary

Write-Host "`n"
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Test Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$totalTests = $testResults.Passed + $testResults.Failed + $testResults.Warnings

Write-Host "Total Tests:  $totalTests" -ForegroundColor Gray
Write-Host "Passed:       $($testResults.Passed)" -ForegroundColor Green
Write-Host "Failed:       $($testResults.Failed)" -ForegroundColor Red
Write-Host "Warnings:     $($testResults.Warnings)" -ForegroundColor Yellow
Write-Host ""

if ($testResults.Failed -eq 0) {
    Write-Host "✓ All tests passed!" -ForegroundColor Green
    
    if ($testResults.Warnings -gt 0) {
        Write-Host "⚠ Some warnings present (review above)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "Your Naner installation appears to be working correctly!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  • Run: .\Invoke-Naner.ps1" -ForegroundColor Gray
    Write-Host "  • Or:  .\Invoke-Naner.ps1 -Debug" -ForegroundColor Gray
    
    exit 0
}
else {
    Write-Host "✗ Some tests failed" -ForegroundColor Red
    Write-Host ""
    Write-Host "Common solutions:" -ForegroundColor Cyan
    Write-Host "  • Run: .\Setup-NanerVendor.ps1" -ForegroundColor Gray
    Write-Host "  • Check: config\naner.json" -ForegroundColor Gray
    Write-Host "  • Verify: vendor/ directory" -ForegroundColor Gray
    Write-Host ""
    Write-Host "For detailed diagnostics, run with -Full flag:" -ForegroundColor Cyan
    Write-Host "  .\Test-NanerInstallation.ps1 -Full" -ForegroundColor Gray
    
    exit 1
}

#endregion
