<#
.SYNOPSIS
    Restore Naner configuration from a backup.

.DESCRIPTION
    Restores portable Naner configurations from a previously created backup.

    Supports:
    - Directory backups
    - Compressed (.zip) backups
    - Selective restoration
    - Dry-run mode to preview changes

.PARAMETER BackupPath
    Path to the backup directory or .zip file created by Backup-NanerConfig.ps1.

.PARAMETER RestoreSSHKeys
    Restore SSH private keys from backup (if they were included).

.PARAMETER Force
    Overwrite existing files without prompting.

.PARAMETER WhatIf
    Preview what would be restored without making changes (dry-run).

.PARAMETER Exclude
    Array of patterns to exclude from restoration.
    Example: -Exclude ".vscode", "Templates"

.EXAMPLE
    .\Restore-NanerConfig.ps1 -BackupPath "C:\Backups\naner-backup-2026-01-07-143022"
    Restores configuration from backup directory

.EXAMPLE
    .\Restore-NanerConfig.ps1 -BackupPath "C:\Backups\naner-backup.zip" -Force
    Restores from compressed backup, overwriting existing files

.EXAMPLE
    .\Restore-NanerConfig.ps1 -BackupPath ".\backup" -WhatIf
    Preview what would be restored (dry-run)

.EXAMPLE
    .\Restore-NanerConfig.ps1 -BackupPath ".\backup" -Exclude ".vscode", "Templates"
    Restore everything except VS Code settings and templates
#>

param(
    [Parameter(Mandatory=$true)]
    [string]$BackupPath,

    [Parameter(Mandatory=$false)]
    [switch]$RestoreSSHKeys,

    [Parameter(Mandatory=$false)]
    [switch]$Force,

    [Parameter(Mandatory=$false)]
    [switch]$WhatIf,

    [Parameter(Mandatory=$false)]
    [string[]]$Exclude = @()
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

# Validate backup path
if (-not (Test-Path $BackupPath)) {
    Write-Error "Backup not found: $BackupPath"
    exit 1
}

# Handle compressed backups
$isZip = $BackupPath -like "*.zip"
$workingBackupPath = $BackupPath

if ($isZip) {
    Write-Host "Extracting compressed backup..." -ForegroundColor Cyan
    $tempExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "naner-restore-$(Get-Random)"

    try {
        Expand-Archive -Path $BackupPath -DestinationPath $tempExtractPath -Force
        $workingBackupPath = $tempExtractPath
        Write-Success "✓ Backup extracted to temporary location"
    } catch {
        Write-Error "Failed to extract backup: $_"
        exit 1
    }
}

# Check for backup manifest
$manifestPath = Join-Path $workingBackupPath "BACKUP-MANIFEST.json"
if (Test-Path $manifestPath) {
    $manifest = Get-Content $manifestPath | ConvertFrom-Json

    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║       Naner Configuration Restore             ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "Backup Information:" -ForegroundColor Cyan
    Write-Host "  Date:          $($manifest.BackupDate)" -ForegroundColor Gray
    Write-Host "  Version:       $($manifest.BackupVersion)" -ForegroundColor Gray
    Write-Host "  SSH Keys:      $(if ($manifest.IncludedSSHKeys) { 'Included' } else { 'Not included' })" -ForegroundColor Gray
    Write-Host "  Items backed:  $($manifest.ItemsBackedUp)" -ForegroundColor Gray
    Write-Host ""

    if ($manifest.IncludedSSHKeys -and -not $RestoreSSHKeys) {
        Write-Warning "This backup contains SSH keys but -RestoreSSHKeys was not specified"
        Write-Host "  SSH keys will NOT be restored. Use -RestoreSSHKeys to restore them." -ForegroundColor Yellow
        Write-Host ""
    }
} else {
    Write-Warning "No backup manifest found - proceeding anyway"
    Write-Host ""
}

if ($WhatIf) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Get all items from backup
$backupItems = Get-ChildItem -Path $workingBackupPath -Recurse -File | Where-Object {
    $_.Name -ne "BACKUP-MANIFEST.json"
}

if ($backupItems.Count -eq 0) {
    Write-Warning "No files found in backup"
    if ($isZip) {
        Remove-Item -Path $tempExtractPath -Recurse -Force -ErrorAction SilentlyContinue
    }
    exit 0
}

# Filter excluded patterns
if ($Exclude.Count -gt 0) {
    $backupItems = $backupItems | Where-Object {
        $relativePath = $_.FullName.Substring($workingBackupPath.Length + 1)
        $shouldExclude = $false

        foreach ($pattern in $Exclude) {
            if ($relativePath -like "*$pattern*") {
                $shouldExclude = $true
                break
            }
        }

        -not $shouldExclude
    }
}

# Restore files
$restoredCount = 0
$skippedCount = 0
$conflictCount = 0

foreach ($item in $backupItems) {
    $relativePath = $item.FullName.Substring($workingBackupPath.Length + 1)

    # Skip SSH keys unless explicitly requested
    if ($relativePath -like ".ssh/id_*" -and -not $RestoreSSHKeys) {
        Write-Host "⊘ Skipping SSH key: $relativePath (use -RestoreSSHKeys to restore)" -ForegroundColor DarkGray
        $skippedCount++
        continue
    }

    $destPath = Join-Path $homeDir $relativePath
    $destParent = Split-Path $destPath -Parent

    # Check if file exists
    $exists = Test-Path $destPath

    if ($WhatIf) {
        if ($exists) {
            Write-Host "⚠ Would overwrite: $relativePath" -ForegroundColor Yellow
            $conflictCount++
        } else {
            Write-Host "✓ Would restore: $relativePath" -ForegroundColor Green
        }
        $restoredCount++
        continue
    }

    # Handle conflicts
    if ($exists -and -not $Force) {
        Write-Host "⚠ File exists: $relativePath" -ForegroundColor Yellow
        $response = Read-Host "  Overwrite? (y/N)"

        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "  Skipped" -ForegroundColor DarkGray
            $skippedCount++
            continue
        }
    }

    # Create parent directory if needed
    if ($destParent -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    # Restore file
    try {
        Copy-Item -Path $item.FullName -Destination $destPath -Force

        if ($exists) {
            Write-Host "✓ Overwrote: $relativePath" -ForegroundColor Cyan
            $conflictCount++
        } else {
            Write-Host "✓ Restored: $relativePath" -ForegroundColor Green
        }
        $restoredCount++
    } catch {
        Write-Warning "Failed to restore $relativePath: $_"
        $skippedCount++
    }
}

# Cleanup temp extraction if zip
if ($isZip) {
    Remove-Item -Path $tempExtractPath -Recurse -Force -ErrorAction SilentlyContinue
}

# Summary
Write-Host ""
if ($WhatIf) {
    Write-Host "DRY RUN COMPLETE - No changes were made" -ForegroundColor Yellow
} else {
    Write-Success "🎉 Restore complete!"
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files restored:  $restoredCount" -ForegroundColor Gray
Write-Host "  Files skipped:   $skippedCount" -ForegroundColor Gray

if ($conflictCount -gt 0) {
    Write-Host "  Files replaced:  $conflictCount" -ForegroundColor Yellow
}

Write-Host ""

if ($WhatIf) {
    Write-Host "To perform the actual restore:" -ForegroundColor Cyan
    Write-Host "  Remove the -WhatIf flag and run again" -ForegroundColor Gray
    Write-Host ""
}

if ($RestoreSSHKeys -and -not $WhatIf) {
    Write-Host "⚠ SSH keys were restored" -ForegroundColor Yellow
    Write-Host "  Make sure to set correct permissions:" -ForegroundColor Yellow
    Write-Host "  chmod 600 ~/.ssh/id_*" -ForegroundColor Gray
    Write-Host ""
}
