# Naner PowerShell Profile
# This profile is loaded when using Naner terminal launcher

# Display Naner banner
Write-Host "Naner Environment" -ForegroundColor Cyan
Write-Host "================" -ForegroundColor Cyan
Write-Host ""

# Set environment variables from Naner configuration
if ($env:NANER_ROOT) {
    Write-Host "NANER_ROOT: $env:NANER_ROOT" -ForegroundColor Green
} else {
    Write-Host "Warning: NANER_ROOT not set" -ForegroundColor Yellow
}

# Display vendor tools status
Write-Host ""
Write-Host "Vendor Tools:" -ForegroundColor Cyan

$vendorTools = @{
    "PowerShell" = "$env:NANER_ROOT\vendor\powershell\pwsh.exe"
    "Git Bash"   = "$env:NANER_ROOT\vendor\msys64\usr\bin\bash.exe"
    "7-Zip"      = "$env:NANER_ROOT\vendor\7zip\7z.exe"
    "Terminal"   = "$env:NANER_ROOT\vendor\terminal\wt.exe"
}

foreach ($tool in $vendorTools.GetEnumerator()) {
    $exists = Test-Path $tool.Value
    $status = if ($exists) { "✓" } else { "✗" }
    $color = if ($exists) { "Green" } else { "Red" }
    Write-Host "  $status $($tool.Key)" -ForegroundColor $color
}

Write-Host ""

# Set up aliases
Set-Alias -Name vim -Value nvim -ErrorAction SilentlyContinue
Set-Alias -Name ll -Value Get-ChildItem -ErrorAction SilentlyContinue

# Custom prompt (optional)
function prompt {
    $location = Get-Location
    $host.UI.RawUI.WindowTitle = "Naner - $location"

    Write-Host "naner" -NoNewline -ForegroundColor Magenta
    Write-Host " | " -NoNewline -ForegroundColor DarkGray
    Write-Host "$location" -NoNewline -ForegroundColor Blue
    Write-Host " > " -NoNewline -ForegroundColor Green
    return " "
}

# Welcome message
Write-Host "Type 'Get-Command' to see available commands" -ForegroundColor DarkGray
Write-Host ""
