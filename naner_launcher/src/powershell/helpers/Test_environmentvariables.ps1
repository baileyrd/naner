# Test-EnvironmentVariables.ps1
# Quick test to verify environment variable expansion works

Write-Host "Testing Environment Variable Expansion" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

# Test Windows-style variables
Write-Host "Windows-Style Variables (%VAR%):" -ForegroundColor Yellow
$testPaths = @(
    "%USERPROFILE%\Documents",
    "%LOCALAPPDATA%\Microsoft\WindowsApps\wt.exe",
    "%ProgramFiles%\Git\bin\bash.exe",
    "%TEMP%\test.txt"
)

foreach ($path in $testPaths) {
    $expanded = [System.Environment]::ExpandEnvironmentVariables($path)
    Write-Host "  Input:  $path" -ForegroundColor Gray
    Write-Host "  Output: $expanded" -ForegroundColor Green
    Write-Host ""
}

Write-Host ""
Write-Host "PowerShell-Style Variables (`$env:VAR):" -ForegroundColor Yellow

# Function to expand PowerShell-style variables
function Expand-PSEnvVars {
    param([string]$InputString)
    
    $pattern = '\$env:([A-Za-z_][A-Za-z0-9_]*)'
    $matches = [regex]::Matches($InputString, $pattern)
    
    $result = $InputString
    foreach ($match in $matches) {
        $varName = $match.Groups[1].Value
        $varValue = [System.Environment]::GetEnvironmentVariable($varName)
        if ($varValue) {
            $result = $result -replace [regex]::Escape($match.Value), $varValue
        }
    }
    
    return $result
}

$testPathsPS = @(
    "`$env:USERPROFILE\Documents",
    "`$env:LOCALAPPDATA\Microsoft\WindowsApps\wt.exe",
    "`$env:ProgramFiles\Git\bin\bash.exe",
    "`$env:TEMP\test.txt"
)

foreach ($path in $testPathsPS) {
    $expanded = Expand-PSEnvVars $path
    Write-Host "  Input:  $path" -ForegroundColor Gray
    Write-Host "  Output: $expanded" -ForegroundColor Green
    Write-Host ""
}

Write-Host ""
Write-Host "Mixed Style (both in one path):" -ForegroundColor Yellow
$mixedPath = "%USERPROFILE%\Projects\`$env:USERNAME-workspace"
$step1 = [System.Environment]::ExpandEnvironmentVariables($mixedPath)
$step2 = Expand-PSEnvVars $step1
Write-Host "  Input:  $mixedPath" -ForegroundColor Gray
Write-Host "  Step 1: $step1" -ForegroundColor Gray
Write-Host "  Final:  $step2" -ForegroundColor Green
Write-Host ""

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Common Environment Variables:" -ForegroundColor Yellow
Write-Host ""

$commonVars = @(
    "USERPROFILE",
    "LOCALAPPDATA",
    "APPDATA",
    "USERNAME",
    "COMPUTERNAME",
    "ProgramFiles",
    "SystemRoot",
    "TEMP"
)

foreach ($var in $commonVars) {
    $value = [System.Environment]::GetEnvironmentVariable($var)
    Write-Host "  $var = $value" -ForegroundColor White
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test your user-settings.json paths here!" -ForegroundColor Green
Write-Host ""