<#
.SYNOPSIS
    Diagnostic tool for Windows Terminal launch issues.

.DESCRIPTION
    Tests various methods of launching Windows Terminal to identify issues.
#>

param(
    [string]$NanerRoot
)

$ErrorActionPreference = "Continue"

# Import common utilities
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Windows Terminal Launch Diagnostics" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

# Determine Naner root
if (-not $NanerRoot) {
    if (Get-Command Get-NanerRootSimple -ErrorAction SilentlyContinue) {
        $NanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
    } else {
        # Fallback for standalone usage
        $NanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    }
}

Write-Host "Naner Root: $NanerRoot" -ForegroundColor Gray
Write-Host ""

# Test 1: Check if Windows Terminal exists
Write-Host "[Test 1] Checking Windows Terminal files..." -ForegroundColor Yellow
$wtPath = Join-Path $NanerRoot "vendor\terminal\wt.exe"

if (Test-Path $wtPath) {
    Write-Host "  ✓ wt.exe found: $wtPath" -ForegroundColor Green
    $wtSize = [math]::Round((Get-Item $wtPath).Length / 1MB, 2)
    Write-Host "    Size: $wtSize MB" -ForegroundColor Gray
} else {
    Write-Host "  ✗ wt.exe NOT found at: $wtPath" -ForegroundColor Red
    Write-Host "    Run Setup-NanerVendor.ps1 first" -ForegroundColor Yellow
    exit 1
}

# Check for required DLLs
Write-Host ""
Write-Host "[Test 2] Checking required DLLs..." -ForegroundColor Yellow
$terminalDir = Join-Path $NanerRoot "vendor\terminal"
$requiredFiles = @("WindowsTerminal.exe", "OpenConsole.exe")

foreach ($file in $requiredFiles) {
    $filePath = Join-Path $terminalDir $file
    if (Test-Path $filePath) {
        Write-Host "  ✓ $file found" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $file NOT found" -ForegroundColor Red
    }
}

# Count DLLs
$dllCount = (Get-ChildItem $terminalDir -Filter "*.dll" -ErrorAction SilentlyContinue).Count
Write-Host "  ℹ Found $dllCount DLL files" -ForegroundColor Gray

# Test 3: Try simple launch
Write-Host ""
Write-Host "[Test 3] Attempting simple launch..." -ForegroundColor Yellow
Write-Host "  Command: wt.exe" -ForegroundColor Gray

try {
    $process = Start-Process -FilePath $wtPath -PassThru -WindowStyle Normal
    Start-Sleep -Seconds 2
    
    if ($process.HasExited) {
        Write-Host "  ✗ Process exited immediately" -ForegroundColor Red
        Write-Host "    Exit code: $($process.ExitCode)" -ForegroundColor Gray
    } else {
        Write-Host "  ✓ Process started (PID: $($process.Id))" -ForegroundColor Green
        Write-Host "    Check if window appeared..." -ForegroundColor Gray
        
        $response = Read-Host "    Did Windows Terminal window appear? (y/n)"
        if ($response -eq "y" -or $response -eq "Y") {
            Write-Host "  ✓ Simple launch works!" -ForegroundColor Green
            $process | Stop-Process -Force -ErrorAction SilentlyContinue
        } else {
            Write-Host "  ✗ Window did not appear (process running but hidden?)" -ForegroundColor Red
            $process | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    }
} catch {
    Write-Host "  ✗ Launch failed: $_" -ForegroundColor Red
}

# Test 4: Try with PowerShell
Write-Host ""
Write-Host "[Test 4] Attempting launch with PowerShell shell..." -ForegroundColor Yellow

$pwshPath = Join-Path $NanerRoot "vendor\powershell\pwsh.exe"
if (-not (Test-Path $pwshPath)) {
    Write-Host "  ✗ PowerShell not found: $pwshPath" -ForegroundColor Red
} else {
    Write-Host "  Command: wt.exe -- pwsh.exe -NoExit" -ForegroundColor Gray
    
    try {
        $args = @("--", "$pwshPath", "-NoExit")
        $process = Start-Process -FilePath $wtPath -ArgumentList $args -PassThru
        Start-Sleep -Seconds 2
        
        if ($process.HasExited) {
            Write-Host "  ✗ Process exited immediately" -ForegroundColor Red
            Write-Host "    Exit code: $($process.ExitCode)" -ForegroundColor Gray
        } else {
            Write-Host "  ✓ Process started (PID: $($process.Id))" -ForegroundColor Green
            
            $response = Read-Host "    Did Windows Terminal appear with PowerShell? (y/n)"
            if ($response -eq "y" -or $response -eq "Y") {
                Write-Host "  ✓ Launch with shell works!" -ForegroundColor Green
            } else {
                Write-Host "  ✗ Window did not appear" -ForegroundColor Red
            }
            $process | Stop-Process -Force -ErrorAction SilentlyContinue
        }
    } catch {
        Write-Host "  ✗ Launch failed: $_" -ForegroundColor Red
    }
}

# Test 5: Check system Windows Terminal
Write-Host ""
Write-Host "[Test 5] Checking system Windows Terminal..." -ForegroundColor Yellow

$systemWT = Get-Command wt.exe -ErrorAction SilentlyContinue
if ($systemWT) {
    Write-Host "  ✓ System Windows Terminal found: $($systemWT.Source)" -ForegroundColor Green
    Write-Host "    Try launching system WT:" -ForegroundColor Gray
    Write-Host "    wt.exe" -ForegroundColor Cyan
} else {
    Write-Host "  ℹ System Windows Terminal not in PATH" -ForegroundColor Gray
}

# Test 6: Check for portable mode
Write-Host ""
Write-Host "[Test 6] Checking portable mode..." -ForegroundColor Yellow

$portableFile = Join-Path $terminalDir ".portable"
if (Test-Path $portableFile) {
    Write-Host "  ✓ .portable file exists" -ForegroundColor Green
} else {
    Write-Host "  ✗ .portable file missing" -ForegroundColor Red
    Write-Host "    Creating it now..." -ForegroundColor Yellow
    New-Item -Path $portableFile -ItemType File -Force | Out-Null
    Write-Host "  ✓ Created .portable file" -ForegroundColor Green
}

# Test 7: Try with cmd.exe (simpler test)
Write-Host ""
Write-Host "[Test 7] Attempting launch with cmd.exe..." -ForegroundColor Yellow
Write-Host "  Command: wt.exe -- cmd.exe" -ForegroundColor Gray

try {
    $args = @("--", "cmd.exe")
    $process = Start-Process -FilePath $wtPath -ArgumentList $args -PassThru
    Start-Sleep -Seconds 2
    
    if ($process.HasExited) {
        Write-Host "  ✗ Process exited (Exit code: $($process.ExitCode))" -ForegroundColor Red
    } else {
        Write-Host "  ✓ Process running (PID: $($process.Id))" -ForegroundColor Green
        
        $response = Read-Host "    Did CMD window appear? (y/n)"
        if ($response -eq "y" -or $response -eq "Y") {
            Write-Host "  ✓ Launch works with cmd.exe!" -ForegroundColor Green
        }
        $process | Stop-Process -Force -ErrorAction SilentlyContinue
    }
} catch {
    Write-Host "  ✗ Launch failed: $_" -ForegroundColor Red
}

# Summary and Recommendations
Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Recommendations" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

Write-Host "Try these manual tests:" -ForegroundColor Yellow
Write-Host ""
Write-Host "1. Simple launch:" -ForegroundColor Cyan
Write-Host "   cd vendor\terminal" -ForegroundColor Gray
Write-Host "   .\wt.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "2. With cmd.exe:" -ForegroundColor Cyan
Write-Host "   .\wt.exe -- cmd.exe" -ForegroundColor Gray
Write-Host ""
Write-Host "3. With PowerShell:" -ForegroundColor Cyan
Write-Host "   .\wt.exe -- ..\powershell\pwsh.exe -NoExit" -ForegroundColor Gray
Write-Host ""
Write-Host "4. Check Task Manager:" -ForegroundColor Cyan
Write-Host "   Look for WindowsTerminal.exe or OpenConsole.exe processes" -ForegroundColor Gray
Write-Host ""
Write-Host "5. Check Event Viewer:" -ForegroundColor Cyan
Write-Host "   Application logs for Windows Terminal errors" -ForegroundColor Gray
Write-Host ""
