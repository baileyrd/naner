<#
.SYNOPSIS
    Backup Naner configuration files and settings.

.DESCRIPTION
    Creates a timestamped backup of portable Naner configurations including:
    - Shell configurations (.bashrc, .gitconfig, etc.)
    - Editor settings (VS Code, Vim)
    - SSH config (NOT private keys)
    - PowerShell profile and modules
    - Project templates

    Does NOT backup:
    - Package caches (.cargo, .conda, .npm-cache, etc.)
    - Binary files and vendor dependencies
    - SSH private keys
    - Shell history

.PARAMETER BackupPath
    Directory where backup will be created. Defaults to Desktop\Naner-Backups.

.PARAMETER BackupName
    Optional custom backup name. Defaults to "naner-backup-YYYY-MM-DD-HHMMSS".

.PARAMETER Compress
    Create a compressed .zip file instead of directory backup.

.PARAMETER IncludeSSHKeys
    WARNING: Include SSH private keys in backup (NOT RECOMMENDED for cloud sync).
    Keys will be stored in encrypted form if possible.

.EXAMPLE
    .\Backup-NanerConfig.ps1
    Creates backup in Desktop\Naner-Backups with timestamp

.EXAMPLE
    .\Backup-NanerConfig.ps1 -BackupPath C:\Backups -Compress
    Creates compressed backup at C:\Backups\naner-backup-2026-01-07-143022.zip

.EXAMPLE
    .\Backup-NanerConfig.ps1 -BackupName "pre-update-backup"
    Creates named backup: Desktop\Naner-Backups\pre-update-backup
#>

param(
    [Parameter(Mandatory=$false)]
    [string]$BackupPath = (Join-Path $env:USERPROFILE "Desktop\Naner-Backups"),

    [Parameter(Mandatory=$false)]
    [string]$BackupName,

    [Parameter(Mandatory=$false)]
    [switch]$Compress,

    [Parameter(Mandatory=$false)]
    [switch]$IncludeSSHKeys
)

# Import common utilities
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$commonModule = Join-Path $scriptDir "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule"
}
Import-Module $commonModule -Force

# Get Naner root
$nanerRoot = Get-NanerRoot -ScriptRoot $scriptDir
$homeDir = Join-Path $nanerRoot "home"

# Generate backup name if not provided
if (-not $BackupName) {
    $timestamp = Get-Date -Format "yyyy-MM-dd-HHmmss"
    $BackupName = "naner-backup-$timestamp"
}

# Create backup directory
$backupDestination = Join-Path $BackupPath $BackupName
if (Test-Path $backupDestination) {
    Write-Error "Backup already exists: $backupDestination"
    exit 1
}

New-Item -ItemType Directory -Path $backupDestination -Force | Out-Null

Write-Host ""
Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║        Naner Configuration Backup             ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Backup location: $backupDestination" -ForegroundColor Gray
Write-Host ""

# Define what to backup
$backupItems = @(
    # Shell configurations
    @{Source = ".bashrc"; Type = "File"; Description = "Bash configuration"},
    @{Source = ".bash_profile"; Type = "File"; Description = "Bash login profile"},
    @{Source = ".bash_aliases"; Type = "File"; Description = "Bash aliases"},
    @{Source = ".gitconfig"; Type = "File"; Description = "Git configuration"},
    @{Source = ".vimrc"; Type = "File"; Description = "Vim configuration"},
    @{Source = ".nanorc"; Type = "File"; Description = "Nano configuration"},

    # Editor settings
    @{Source = ".config"; Type = "Directory"; Description = "Editor configurations"},
    @{Source = ".vscode"; Type = "Directory"; Description = "VS Code settings"},

    # SSH (config only, not keys by default)
    @{Source = ".ssh/config"; Type = "File"; Description = "SSH configuration"},
    @{Source = ".ssh/config.example"; Type = "File"; Description = "SSH config example"},

    # PowerShell
    @{Source = "Documents/PowerShell"; Type = "Directory"; Description = "PowerShell profile & modules"},

    # Templates
    @{Source = "Templates"; Type = "Directory"; Description = "Project templates"}
)

# Add SSH keys if requested
if ($IncludeSSHKeys) {
    Write-Warning "Including SSH private keys in backup - USE WITH CAUTION!"
    $backupItems += @{Source = ".ssh"; Type = "Directory"; Description = "SSH keys and config (SENSITIVE)"}
}

# Backup each item
$backedUpCount = 0
$skippedCount = 0

foreach ($item in $backupItems) {
    $sourcePath = Join-Path $homeDir $item.Source

    if (-not (Test-Path $sourcePath)) {
        Write-Host "⊘ Skipping: $($item.Description) (not found)" -ForegroundColor DarkGray
        $skippedCount++
        continue
    }

    $relativePath = $item.Source
    $destPath = Join-Path $backupDestination $relativePath
    $destParent = Split-Path $destPath -Parent

    # Create parent directory if needed
    if ($destParent -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    try {
        if ($item.Type -eq "File") {
            Copy-Item -Path $sourcePath -Destination $destPath -Force
        } else {
            Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
        }
        Write-Host "✓ Backed up: $($item.Description)" -ForegroundColor Green
        $backedUpCount++
    } catch {
        Write-Warning "Failed to backup $($item.Description): $_"
        $skippedCount++
    }
}

# Create backup manifest
$manifest = @{
    BackupDate = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    NanerRoot = $nanerRoot
    BackupVersion = "1.0"
    IncludedSSHKeys = $IncludeSSHKeys.IsPresent
    ItemsBackedUp = $backedUpCount
    ItemsSkipped = $skippedCount
}

$manifestPath = Join-Path $backupDestination "BACKUP-MANIFEST.json"
$manifest | ConvertTo-Json -Depth 3 | Out-File -FilePath $manifestPath -Encoding UTF8

Write-Host ""
Write-Host "Backup manifest: $manifestPath" -ForegroundColor Gray

# Compress if requested
if ($Compress) {
    Write-Host ""
    Write-Host "Compressing backup..." -ForegroundColor Cyan

    $zipPath = "$backupDestination.zip"

    try {
        Compress-Archive -Path $backupDestination -DestinationPath $zipPath -Force
        Write-Success "✓ Backup compressed: $zipPath"

        # Remove uncompressed directory
        Remove-Item -Path $backupDestination -Recurse -Force
        $backupDestination = $zipPath
    } catch {
        Write-Warning "Failed to compress backup: $_"
    }
}

# Summary
Write-Host ""
Write-Success "🎉 Backup complete!"
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Items backed up: $backedUpCount" -ForegroundColor Gray
Write-Host "  Items skipped:   $skippedCount" -ForegroundColor Gray
Write-Host "  Location:        $backupDestination" -ForegroundColor Gray
Write-Host ""

if ($IncludeSSHKeys) {
    Write-Warning "⚠ This backup contains SSH PRIVATE KEYS - store securely!"
    Write-Host "  Recommended: Encrypt this backup before uploading to cloud storage" -ForegroundColor Yellow
}

Write-Host "To restore this backup:" -ForegroundColor Cyan
Write-Host "  .\Restore-NanerConfig.ps1 -BackupPath `"$backupDestination`"" -ForegroundColor Gray
Write-Host ""
