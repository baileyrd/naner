# Find-WindowsTerminal.ps1
# Helper script to locate Windows Terminal executable

Write-Host "Searching for Windows Terminal..." -ForegroundColor Cyan
Write-Host ""

# Method 1: Check PATH
Write-Host "[1] Checking PATH..." -ForegroundColor Yellow
$wtInPath = Get-Command wt.exe -ErrorAction SilentlyContinue
if ($wtInPath) {
    Write-Host "    Found: $($wtInPath.Source)" -ForegroundColor Green
    $foundPath = $wtInPath.Source
}
else {
    Write-Host "    Not found in PATH" -ForegroundColor Gray
}

Write-Host ""

# Method 2: Check WindowsApps (Store installation)
Write-Host "[2] Checking Microsoft Store location..." -ForegroundColor Yellow
$storeAppPath = "${env:LOCALAPPDATA}\Microsoft\WindowsApps\wt.exe"
if (Test-Path $storeAppPath) {
    Write-Host "    Found: $storeAppPath" -ForegroundColor Green
    if (-not $foundPath) { $foundPath = $storeAppPath }
}
else {
    Write-Host "    Not found at: $storeAppPath" -ForegroundColor Gray
}

Write-Host ""

# Method 3: Search Program Files for regular version
Write-Host "[3] Checking Program Files (Regular)..." -ForegroundColor Yellow
$regularPaths = Get-ChildItem -Path "$env:ProgramFiles\WindowsApps" -Filter "wt.exe" -Recurse -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -like "*Microsoft.WindowsTerminal_*" -and $_.FullName -notlike "*Preview*" }

if ($regularPaths) {
    foreach ($path in $regularPaths) {
        Write-Host "    Found: $($path.FullName)" -ForegroundColor Green
        if (-not $foundPath) { $foundPath = $path.FullName }
    }
}
else {
    Write-Host "    Not found in Program Files" -ForegroundColor Gray
}

Write-Host ""

# Method 4: Search Program Files for Preview version
Write-Host "[4] Checking Program Files (Preview)..." -ForegroundColor Yellow
$previewPaths = Get-ChildItem -Path "$env:ProgramFiles\WindowsApps" -Filter "wt.exe" -Recurse -ErrorAction SilentlyContinue | 
    Where-Object { $_.FullName -like "*Microsoft.WindowsTerminalPreview_*" }

if ($previewPaths) {
    foreach ($path in $previewPaths) {
        Write-Host "    Found: $($path.FullName)" -ForegroundColor Green
        if (-not $foundPath) { $foundPath = $path.FullName }
    }
}
else {
    Write-Host "    Not found in Program Files (Preview)" -ForegroundColor Gray
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan

if ($foundPath) {
    Write-Host "RECOMMENDED PATH:" -ForegroundColor Green
    Write-Host $foundPath -ForegroundColor White
    Write-Host ""
    
    # Try to convert to environment variable format
    $portablePath = $foundPath
    $portablePath = $portablePath -replace [regex]::Escape($env:LOCALAPPDATA), '%LOCALAPPDATA%'
    $portablePath = $portablePath -replace [regex]::Escape($env:USERPROFILE), '%USERPROFILE%'
    $portablePath = $portablePath -replace [regex]::Escape($env:ProgramFiles), '%ProgramFiles%'
    
    Write-Host "PORTABLE PATH (Recommended - works on any PC):" -ForegroundColor Green
    Write-Host $portablePath -ForegroundColor White
    Write-Host ""
    
    Write-Host "Add this to your config/user-settings.json:" -ForegroundColor Yellow
    
    # Escape backslashes for JSON
    $jsonPath = $portablePath -replace '\\', '\\'
    
    Write-Host @"
{
  "DefaultProfile": "PowerShell",
  "StartupDir": null,
  "WindowsTerminalPath": "$jsonPath"
}
"@ -ForegroundColor White
    
    Write-Host ""
    Write-Host "Or copy this single line:" -ForegroundColor Yellow
    Write-Host "  `"WindowsTerminalPath`": `"$jsonPath`"" -ForegroundColor White
    Write-Host ""
    Write-Host "TIP: Using environment variables (%LOCALAPPDATA%, %USERPROFILE%, etc.)" -ForegroundColor Cyan
    Write-Host "     makes your config portable across different machines!" -ForegroundColor Cyan
}
else {
    Write-Host "Windows Terminal was not found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please install Windows Terminal from:" -ForegroundColor Yellow
    Write-Host "  Microsoft Store: https://aka.ms/terminal" -ForegroundColor White
    Write-Host "  Or download from: https://github.com/microsoft/terminal/releases" -ForegroundColor White
}

Write-Host ""
Write-Host "================================================" -ForegroundColor Cyan