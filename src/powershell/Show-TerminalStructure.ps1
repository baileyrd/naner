<#
.SYNOPSIS
    Shows the actual folder structure of extracted Windows Terminal.

.DESCRIPTION
    Displays the directory tree to see if structure was preserved.
#>

param(
    [string]$NanerRoot
)

# Import common utilities
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

# Determine Naner root
if (-not $NanerRoot) {
    if (Get-Command Get-NanerRootSimple -ErrorAction SilentlyContinue) {
        $NanerRoot = Get-NanerRootSimple -ScriptRoot $PSScriptRoot
    } else {
        # Fallback for standalone usage
        $NanerRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    }
}

$terminalDir = Join-Path $NanerRoot "vendor\terminal"

Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Windows Terminal Folder Structure" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""
Write-Host "Location: $terminalDir" -ForegroundColor Gray
Write-Host ""

if (-not (Test-Path $terminalDir)) {
    Write-Host "✗ Directory not found!" -ForegroundColor Red
    exit 1
}

# Show directory tree
function Show-Tree {
    param(
        [string]$Path,
        [int]$Indent = 0,
        [int]$MaxDepth = 3,
        [int]$CurrentDepth = 0
    )
    
    if ($CurrentDepth -ge $MaxDepth) {
        return
    }
    
    $items = Get-ChildItem $Path -ErrorAction SilentlyContinue
    
    $dirs = $items | Where-Object { $_.PSIsContainer } | Sort-Object Name
    $files = $items | Where-Object { -not $_.PSIsContainer } | Sort-Object Name
    
    # Show directories first
    foreach ($dir in $dirs) {
        $prefix = "  " * $Indent
        Write-Host "$prefix├── 📁 $($dir.Name)/" -ForegroundColor Cyan
        Show-Tree -Path $dir.FullName -Indent ($Indent + 1) -MaxDepth $MaxDepth -CurrentDepth ($CurrentDepth + 1)
    }
    
    # Show files
    foreach ($file in $files) {
        $prefix = "  " * $Indent
        $size = [math]::Round($file.Length / 1KB, 1)
        $icon = if ($file.Extension -eq ".exe") { "⚙️" } elseif ($file.Extension -eq ".dll") { "📦" } else { "📄" }
        Write-Host "$prefix├── $icon $($file.Name) ($size KB)" -ForegroundColor Gray
    }
}

Write-Host "vendor/terminal/" -ForegroundColor Yellow
Show-Tree -Path $terminalDir -MaxDepth 3

Write-Host ""
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host " Summary" -ForegroundColor Cyan
Write-Host "═══════════════════════════════════════════" -ForegroundColor Cyan
Write-Host ""

$allItems = Get-ChildItem $terminalDir -Recurse
$totalFiles = ($allItems | Where-Object { -not $_.PSIsContainer }).Count
$totalDirs = ($allItems | Where-Object { $_.PSIsContainer }).Count

Write-Host "Total Directories: $totalDirs" -ForegroundColor Gray
Write-Host "Total Files: $totalFiles" -ForegroundColor Gray
Write-Host ""

# Check for expected structure
$hasSubdirs = $totalDirs -gt 0
if ($hasSubdirs) {
    Write-Host "✓ Has subdirectories (structure preserved)" -ForegroundColor Green
} else {
    Write-Host "✗ No subdirectories (flat structure - WRONG!)" -ForegroundColor Red
}

# Check for critical files in root
$wtExe = Join-Path $terminalDir "wt.exe"
$wtTerminal = Join-Path $terminalDir "WindowsTerminal.exe"

Write-Host ""
if (Test-Path $wtExe) {
    Write-Host "✓ wt.exe in root" -ForegroundColor Green
} else {
    Write-Host "✗ wt.exe NOT in root" -ForegroundColor Red
    # Search for it
    $found = Get-ChildItem $terminalDir -Filter "wt.exe" -Recurse -File | Select-Object -First 1
    if ($found) {
        Write-Host "  Found at: $($found.FullName.Replace($terminalDir, '.'))" -ForegroundColor Yellow
    }
}

if (Test-Path $wtTerminal) {
    Write-Host "✓ WindowsTerminal.exe in root" -ForegroundColor Green
} else {
    Write-Host "✗ WindowsTerminal.exe NOT in root" -ForegroundColor Red
    $found = Get-ChildItem $terminalDir -Filter "WindowsTerminal.exe" -Recurse -File | Select-Object -First 1
    if ($found) {
        Write-Host "  Found at: $($found.FullName.Replace($terminalDir, '.'))" -ForegroundColor Yellow
    }
}

Write-Host ""
