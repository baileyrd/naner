<#
.SYNOPSIS
    Validates Windows Terminal installation completeness.

.DESCRIPTION
    Checks if all necessary Windows Terminal files are present and identifies what's missing.
#>

param(
    [string]$NanerRoot
)

$ErrorActionPreference = "Continue"

# Determine Naner root
if (-not $NanerRoot) {
    $NanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
}

$terminalDir = Join-Path $NanerRoot "vendor\terminal"

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Windows Terminal Installation Validator" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Checking: $terminalDir" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $terminalDir)) {
    Write-Host "✗ Windows Terminal directory not found!" -ForegroundColor Red
    Write-Host "  Run: .\Setup-NanerVendor.ps1" -ForegroundColor Yellow
    exit 1
}

# Critical executables
$criticalFiles = @{
    "wt.exe" = "Windows Terminal launcher"
    "WindowsTerminal.exe" = "Windows Terminal main executable"
    "OpenConsole.exe" = "Console host"
}

Write-Host "[Critical Executables]" -ForegroundColor Yellow
$criticalMissing = 0

foreach ($file in $criticalFiles.Keys) {
    $filePath = Join-Path $terminalDir $file
    if (Test-Path $filePath) {
        $size = [math]::Round((Get-Item $filePath).Length / 1KB, 2)
        Write-Host "  ✓ $file" -ForegroundColor Green
        Write-Host "    $($criticalFiles[$file]) - $size KB" -ForegroundColor Gray
    } else {
        Write-Host "  ✗ $file - MISSING!" -ForegroundColor Red
        Write-Host "    $($criticalFiles[$file])" -ForegroundColor Gray
        $criticalMissing++
    }
}

Write-Host ""

# Important DLLs (these are commonly needed)
$importantDLLs = @(
    "Microsoft.Terminal.Control.dll",
    "Microsoft.Terminal.Remoting.dll",
    "Microsoft.Terminal.Settings.dll",
    "Microsoft.UI.Xaml.dll",
    "TerminalApp.dll",
    "TerminalConnection.dll"
)

Write-Host "[Important DLLs]" -ForegroundColor Yellow
$dllsMissing = 0

foreach ($dll in $importantDLLs) {
    $dllPath = Join-Path $terminalDir $dll
    if (Test-Path $dllPath) {
        Write-Host "  ✓ $dll" -ForegroundColor Green
    } else {
        Write-Host "  ✗ $dll - MISSING" -ForegroundColor Red
        $dllsMissing++
    }
}

Write-Host ""

# Count all DLLs
$allDLLs = Get-ChildItem $terminalDir -Filter "*.dll" -ErrorAction SilentlyContinue
Write-Host "[DLL Summary]" -ForegroundColor Yellow
Write-Host "  Total DLLs found: $($allDLLs.Count)" -ForegroundColor Gray
Write-Host "  Expected: 30-50 DLLs for full installation" -ForegroundColor Gray

if ($allDLLs.Count -lt 20) {
    Write-Host "  ⚠ Warning: Very few DLLs present!" -ForegroundColor Red
    Write-Host "    Windows Terminal may not work properly" -ForegroundColor Red
}

Write-Host ""

# Check for .portable file
Write-Host "[Portable Mode]" -ForegroundColor Yellow
$portableFile = Join-Path $terminalDir ".portable"
if (Test-Path $portableFile) {
    Write-Host "  ✓ .portable file present" -ForegroundColor Green
} else {
    Write-Host "  ✗ .portable file missing" -ForegroundColor Red
    Write-Host "    Creating it now..." -ForegroundColor Yellow
    New-Item -Path $portableFile -ItemType File -Force | Out-Null
    Write-Host "  ✓ Created .portable file" -ForegroundColor Green
}

Write-Host ""

# List all files
Write-Host "[All Files]" -ForegroundColor Yellow
$allFiles = Get-ChildItem $terminalDir -File -ErrorAction SilentlyContinue
Write-Host "  Total files: $($allFiles.Count)" -ForegroundColor Gray
Write-Host ""

if ($allFiles.Count -lt 10) {
    Write-Host "⚠ Very few files present!" -ForegroundColor Red
    Write-Host "Expected: 40-80 files for complete installation" -ForegroundColor Yellow
    Write-Host ""
}

# Show file breakdown
$fileTypes = $allFiles | Group-Object Extension | Sort-Object Count -Descending
Write-Host "  File types:" -ForegroundColor Gray
foreach ($type in $fileTypes) {
    $ext = if ($type.Name) { $type.Name } else { "(no extension)" }
    Write-Host "    $ext : $($type.Count) files" -ForegroundColor Gray
}

Write-Host ""

# Final verdict
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Verdict" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

if ($criticalMissing -eq 0 -and $dllsMissing -lt 2 -and $allFiles.Count -gt 20) {
    Write-Host "✓ Windows Terminal installation appears COMPLETE" -ForegroundColor Green
    Write-Host ""
    Write-Host "You can now try:" -ForegroundColor Cyan
    Write-Host "  cd vendor\terminal" -ForegroundColor Gray
    Write-Host "  .\wt.exe" -ForegroundColor Gray
} elseif ($criticalMissing -gt 0) {
    Write-Host "✗ Installation INCOMPLETE - Critical files missing!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Solution:" -ForegroundColor Yellow
    Write-Host "  Remove-Item vendor\terminal -Recurse -Force" -ForegroundColor Gray
    Write-Host "  .\Setup-NanerVendor.ps1 -ForceDownload" -ForegroundColor Gray
} else {
    Write-Host "⚠ Installation may be INCOMPLETE" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  Missing $dllsMissing important DLLs" -ForegroundColor Gray
    Write-Host "  Total files: $($allFiles.Count)" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Recommended:" -ForegroundColor Yellow
    Write-Host "  Remove-Item vendor\terminal -Recurse -Force" -ForegroundColor Gray
    Write-Host "  .\Setup-NanerVendor.ps1 -ForceDownload" -ForegroundColor Gray
}

Write-Host ""

# Detailed file list if problematic
if ($allFiles.Count -lt 30) {
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host " Detailed File List" -ForegroundColor Cyan
    Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
    Write-Host ""
    
    $allFiles | Sort-Object Name | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  $($_.Name)" -ForegroundColor Gray
        Write-Host "    $size KB" -ForegroundColor DarkGray
    }
    Write-Host ""
}
