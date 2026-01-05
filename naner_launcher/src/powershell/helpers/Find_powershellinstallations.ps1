# Find-PowerShellInstallations.ps1
# Helper script to locate all PowerShell installations on the system

Write-Host "Searching for PowerShell installations..." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

$foundInstallations = @()

# 1. Windows PowerShell 5.1 (built-in)
Write-Host "[1] Checking for Windows PowerShell 5.1..." -ForegroundColor Yellow
$ps5Path = "$env:SystemRoot\System32\WindowsPowerShell\v1.0\powershell.exe"
if (Test-Path $ps5Path) {
    Write-Host "    Found: $ps5Path" -ForegroundColor Green
    $version = & $ps5Path -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
    Write-Host "    Version: $version" -ForegroundColor Gray
    
    $foundInstallations += @{
        Name = "PowerShell5"
        Path = $ps5Path
        Version = $version
        EnvPath = "%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe"
    }
}
else {
    Write-Host "    Not found" -ForegroundColor Gray
}

Write-Host ""

# 2. PowerShell 7+ (stable releases)
Write-Host "[2] Checking for PowerShell 7+ (stable)..." -ForegroundColor Yellow
$ps7Locations = @(
    "$env:ProgramFiles\PowerShell\7",
    "$env:ProgramFiles\PowerShell\7.0",
    "$env:ProgramFiles\PowerShell\7.1",
    "$env:ProgramFiles\PowerShell\7.2",
    "$env:ProgramFiles\PowerShell\7.3",
    "$env:ProgramFiles\PowerShell\7.4",
    "$env:ProgramFiles\PowerShell\7.5"
)

foreach ($location in $ps7Locations) {
    $pwshPath = Join-Path $location "pwsh.exe"
    if (Test-Path $pwshPath) {
        Write-Host "    Found: $pwshPath" -ForegroundColor Green
        $version = & $pwshPath -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
        Write-Host "    Version: $version" -ForegroundColor Gray
        
        $folderName = Split-Path $location -Leaf
        $envPath = "%ProgramFiles%\PowerShell\$folderName\pwsh.exe"
        
        $foundInstallations += @{
            Name = "PowerShell$folderName"
            Path = $pwshPath
            Version = $version
            EnvPath = $envPath
        }
    }
}

if ($foundInstallations.Count -eq 1) {
    Write-Host "    Not found" -ForegroundColor Gray
}

Write-Host ""

# 3. PowerShell Preview
Write-Host "[3] Checking for PowerShell 7 Preview..." -ForegroundColor Yellow
$psPreviewLocations = @(
    "$env:ProgramFiles\PowerShell\7-preview"
)

foreach ($location in $psPreviewLocations) {
    $pwshPath = Join-Path $location "pwsh.exe"
    if (Test-Path $pwshPath) {
        Write-Host "    Found: $pwshPath" -ForegroundColor Green
        $version = & $pwshPath -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
        Write-Host "    Version: $version" -ForegroundColor Gray
        
        $foundInstallations += @{
            Name = "PowerShellPreview"
            Path = $pwshPath
            Version = $version
            EnvPath = "%ProgramFiles%\PowerShell\7-preview\pwsh.exe"
        }
    }
}

if ($foundInstallations.Count -lt 2) {
    Write-Host "    Not found" -ForegroundColor Gray
}

Write-Host ""

# 4. Check PATH for any PowerShell
Write-Host "[4] Checking PATH for PowerShell..." -ForegroundColor Yellow
$pwshInPath = Get-Command pwsh.exe -ErrorAction SilentlyContinue
if ($pwshInPath) {
    Write-Host "    Found in PATH: $($pwshInPath.Source)" -ForegroundColor Green
    $version = & $pwshInPath.Source -NoProfile -Command '$PSVersionTable.PSVersion.ToString()'
    Write-Host "    Version: $version" -ForegroundColor Gray
}
else {
    Write-Host "    pwsh.exe not found in PATH" -ForegroundColor Gray
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

if ($foundInstallations.Count -eq 0) {
    Write-Host "No PowerShell installations found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Windows PowerShell 5.1 should be built-in to Windows." -ForegroundColor Yellow
    Write-Host "To install PowerShell 7+:" -ForegroundColor Yellow
    Write-Host "  winget install Microsoft.PowerShell" -ForegroundColor White
    Write-Host "  Or download from: https://github.com/PowerShell/PowerShell/releases" -ForegroundColor White
}
else {
    Write-Host "Found $($foundInstallations.Count) PowerShell installation(s)!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Add these to your config/user-settings.json:" -ForegroundColor Yellow
    Write-Host ""
    
    Write-Host '  "CustomProfiles": {' -ForegroundColor White
    
    for ($i = 0; $i -lt $foundInstallations.Count; $i++) {
        $install = $foundInstallations[$i]
        $jsonPath = $install.EnvPath -replace '\\', '\\'
        
        Write-Host "    `"$($install.Name)`": {" -ForegroundColor White
        Write-Host "      `"ShellPath`": `"$jsonPath`"," -ForegroundColor White
        Write-Host "      `"Arguments`": `"-NoLogo`"," -ForegroundColor White
        Write-Host "      `"Title`": `"$($install.Name) ($($install.Version))`"" -ForegroundColor White
        
        if ($i -lt $foundInstallations.Count - 1) {
            Write-Host "    }," -ForegroundColor White
        }
        else {
            Write-Host "    }" -ForegroundColor White
        }
    }
    
    Write-Host '  }' -ForegroundColor White
    Write-Host ""
    
    Write-Host "Then launch with:" -ForegroundColor Yellow
    foreach ($install in $foundInstallations) {
        Write-Host "  .\Launch-Naner.ps1 -Profile `"$($install.Name)`"" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "==========================================" -ForegroundColor Cyan