# Naner Portable PowerShell Profile
# This profile is automatically loaded when PowerShell starts in Naner
# Location: %NANER_ROOT%\home\.config\powershell\profile.ps1

# Welcome message
Write-Host "Naner PowerShell Environment" -ForegroundColor Cyan
Write-Host "=============================" -ForegroundColor Cyan
Write-Host ""

# Display environment info
if ($env:NANER_ROOT) {
    Write-Host "Naner Root: " -NoNewline -ForegroundColor Gray
    Write-Host $env:NANER_ROOT -ForegroundColor Green
}

if ($env:HOME) {
    Write-Host "Home:       " -NoNewline -ForegroundColor Gray
    Write-Host $env:HOME -ForegroundColor Green
}

Write-Host ""

# Set PSModulePath to use portable modules
$portableModulesPath = Join-Path $env:HOME "Documents\PowerShell\Modules"
if ($env:PSModulePath -notlike "*$portableModulesPath*") {
    $env:PSModulePath = "$portableModulesPath;$env:PSModulePath"
}

# Add portable scripts to PATH
$portableScriptsPath = Join-Path $env:HOME "Documents\PowerShell\Scripts"
if (Test-Path $portableScriptsPath) {
    if ($env:PATH -notlike "*$portableScriptsPath*") {
        $env:PATH = "$portableScriptsPath;$env:PATH"
    }
}

# Custom aliases (examples)
Set-Alias -Name ll -Value Get-ChildItem -Option AllScope
Set-Alias -Name which -Value Get-Command -Option AllScope

# Custom functions (examples)
function prompt {
    $location = Get-Location
    $host.UI.RawUI.WindowTitle = "Naner - $location"

    # Show git branch if in a git repo
    $gitBranch = ""
    if (Test-Path .git) {
        try {
            $branch = git rev-parse --abbrev-ref HEAD 2>$null
            if ($branch) {
                $gitBranch = " (git:$branch)"
            }
        }
        catch { }
    }

    Write-Host "[Naner] " -NoNewline -ForegroundColor Cyan
    Write-Host (Split-Path $location -Leaf) -NoNewline -ForegroundColor Green

    if ($gitBranch) {
        Write-Host $gitBranch -NoNewline -ForegroundColor Yellow
    }

    return "> "
}

# Git shortcuts (if git is available)
if (Get-Command git -ErrorAction SilentlyContinue) {
    function gs { git status @args }
    function ga { git add @args }
    function gc { git commit @args }
    function gp { git push @args }
    function gl { git pull @args }
    function gd { git diff @args }
    function glog { git log --oneline --graph --decorate @args }
}

# Docker shortcuts (if docker is available)
if (Get-Command docker -ErrorAction SilentlyContinue) {
    function dps { docker ps @args }
    function di { docker images @args }
    function dex { docker exec -it @args }
}

# Utility functions
function Get-NanerInfo {
    Write-Host "`nNaner Environment Information" -ForegroundColor Cyan
    Write-Host "=============================" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Naner Root:    $env:NANER_ROOT"
    Write-Host "Home:          $env:HOME"
    Write-Host "SSH Home:      $env:SSH_HOME"
    Write-Host "Profile:       $PROFILE"
    Write-Host "PSModulePath:  $($env:PSModulePath -split ';' | Select-Object -First 3 | ForEach-Object { "`n               $_" })"
    Write-Host ""
}

# Quick access to Naner scripts
function Invoke-NanerSetup {
    & (Join-Path $env:NANER_ROOT "naner_launcher\src\powershell\Setup-NanerVendor.ps1") @args
}

function Test-NanerInstall {
    & (Join-Path $env:NANER_ROOT "naner_launcher\src\powershell\Test-NanerInstallation.ps1") @args
}

# Initialize Oh My Posh if available (optional)
# Uncomment and customize if you install Oh My Posh
# if (Get-Command oh-my-posh -ErrorAction SilentlyContinue) {
#     oh-my-posh init pwsh --config "$env:HOME\.config\oh-my-posh\theme.json" | Invoke-Expression
# }

# Load PSReadLine with custom key bindings (if available)
if (Get-Module -ListAvailable -Name PSReadLine) {
    Import-Module PSReadLine

    # Set prediction source
    Set-PSReadLineOption -PredictionSource History

    # Set edit mode to Emacs (or Vi if preferred)
    Set-PSReadLineOption -EditMode Emacs

    # Custom key bindings
    Set-PSReadLineKeyHandler -Key UpArrow -Function HistorySearchBackward
    Set-PSReadLineKeyHandler -Key DownArrow -Function HistorySearchForward
    Set-PSReadLineKeyHandler -Key Tab -Function MenuComplete
}

# Display helpful tips
Write-Host "Quick Commands:" -ForegroundColor Cyan
Write-Host "  Get-NanerInfo          - Show Naner environment info" -ForegroundColor Gray
Write-Host "  Invoke-NanerSetup      - Run vendor setup" -ForegroundColor Gray
Write-Host "  Test-NanerInstall      - Test Naner installation" -ForegroundColor Gray
Write-Host ""
Write-Host "Git Shortcuts: " -NoNewline -ForegroundColor Cyan
Write-Host "gs, ga, gc, gp, gl, gd, glog" -ForegroundColor Gray
Write-Host ""

# User customizations below this line
# ====================================
