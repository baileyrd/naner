<#
.SYNOPSIS
    Sync Naner configuration with cloud storage.

.DESCRIPTION
    Synchronizes portable Naner configurations with cloud storage providers:
    - OneDrive
    - Dropbox
    - Google Drive
    - Custom sync paths

    Uses selective sync based on .syncignore patterns to exclude:
    - Package caches
    - Binary files
    - SSH private keys (by default)
    - Large vendor downloads

.PARAMETER SyncProvider
    Cloud storage provider: OneDrive, Dropbox, GoogleDrive, or Custom.

.PARAMETER SyncPath
    Custom sync path (required for -SyncProvider Custom).

.PARAMETER Direction
    Sync direction: Push (local → cloud), Pull (cloud → local), or Sync (bidirectional).

.PARAMETER IncludeSSHKeys
    Include SSH private keys in sync (NOT RECOMMENDED).

.PARAMETER Force
    Overwrite conflicts without prompting.

.PARAMETER WhatIf
    Preview sync operations without making changes.

.EXAMPLE
    .\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push
    Push local configs to OneDrive

.EXAMPLE
    .\Sync-NanerConfig.ps1 -SyncProvider Dropbox -Direction Pull
    Pull configs from Dropbox to local

.EXAMPLE
    .\Sync-NanerConfig.ps1 -SyncProvider Custom -SyncPath "D:\Cloud\Naner" -Direction Sync
    Bidirectional sync with custom path

.EXAMPLE
    .\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push -WhatIf
    Preview what would be synced without making changes
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('OneDrive', 'Dropbox', 'GoogleDrive', 'Custom')]
    [string]$SyncProvider,

    [Parameter(Mandatory=$false)]
    [string]$SyncPath,

    [Parameter(Mandatory=$false)]
    [ValidateSet('Push', 'Pull', 'Sync')]
    [string]$Direction = 'Sync',

    [Parameter(Mandatory=$false)]
    [switch]$IncludeSSHKeys,

    [Parameter(Mandatory=$false)]
    [switch]$Force,

    [Parameter(Mandatory=$false)]
    [switch]$WhatIf
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

# Determine sync path based on provider
if ($SyncProvider -eq 'Custom') {
    if (-not $SyncPath) {
        Write-Error "-SyncPath is required when using -SyncProvider Custom"
        exit 1
    }
} else {
    # Auto-detect cloud provider paths
    $cloudPaths = @{
        'OneDrive' = Join-Path $env:USERPROFILE "OneDrive\Naner-Config"
        'Dropbox' = Join-Path $env:USERPROFILE "Dropbox\Naner-Config"
        'GoogleDrive' = Join-Path $env:USERPROFILE "Google Drive\Naner-Config"
    }

    $SyncPath = $cloudPaths[$SyncProvider]
}

# Create sync path if it doesn't exist
if (-not (Test-Path $SyncPath)) {
    if ($Direction -eq 'Push' -or $Direction -eq 'Sync') {
        Write-Host "Creating sync directory: $SyncPath" -ForegroundColor Cyan
        New-Item -ItemType Directory -Path $SyncPath -Force | Out-Null
    } else {
        Write-Error "Sync path does not exist: $SyncPath"
        exit 1
    }
}

Write-Host ""
Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
Write-Host "║         Naner Configuration Sync              ║" -ForegroundColor Cyan
Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
Write-Host ""
Write-Host "Provider:  $SyncProvider" -ForegroundColor Gray
Write-Host "Path:      $SyncPath" -ForegroundColor Gray
Write-Host "Direction: $Direction" -ForegroundColor Gray
Write-Host ""

if ($WhatIf) {
    Write-Host "DRY RUN MODE - No changes will be made" -ForegroundColor Yellow
    Write-Host ""
}

# Load .syncignore patterns
$syncIgnorePath = Join-Path $nanerRoot ".syncignore"
$ignorePatterns = @()

if (Test-Path $syncIgnorePath) {
    $ignorePatterns = Get-Content $syncIgnorePath | Where-Object {
        $_ -and $_ -notmatch '^\s*#' -and $_ -notmatch '^\s*$'
    }
    Write-Host "Loaded $($ignorePatterns.Count) ignore patterns from .syncignore" -ForegroundColor Gray
} else {
    Write-Warning "No .syncignore file found - syncing all files"
    Write-Host "  Consider creating .syncignore to exclude caches and binaries" -ForegroundColor Yellow
}

# Default ignore patterns (always excluded)
$defaultIgnores = @(
    "*.lock",
    ".bash_history",
    ".bash_sessions",
    ".viminfo",
    "*.log",
    ".DS_Store",
    "Thumbs.db"
)

# Add SSH key patterns if not including
if (-not $IncludeSSHKeys) {
    $defaultIgnores += ".ssh/id_*"
    $defaultIgnores += ".ssh/*.pem"
    $defaultIgnores += ".ssh/known_hosts"
}

$allIgnores = $ignorePatterns + $defaultIgnores

# Function to check if path should be ignored
function Test-ShouldIgnore {
    param([string]$Path, [string]$BaseDir)

    $relativePath = $Path.Substring($BaseDir.Length).TrimStart('\', '/')

    foreach ($pattern in $allIgnores) {
        # Convert glob pattern to regex
        $regexPattern = $pattern -replace '\*', '.*' -replace '\?', '.'

        if ($relativePath -match $regexPattern) {
            return $true
        }
    }

    return $false
}

# Get files to sync
$sourceDir = if ($Direction -eq 'Pull') { $SyncPath } else { $homeDir }
$destDir = if ($Direction -eq 'Pull') { $homeDir } else { $SyncPath }

$filesToSync = Get-ChildItem -Path $sourceDir -Recurse -File | Where-Object {
    -not (Test-ShouldIgnore -Path $_.FullName -BaseDir $sourceDir)
}

Write-Host "Found $($filesToSync.Count) files to sync" -ForegroundColor Cyan
Write-Host ""

# Sync statistics
$syncedCount = 0
$skippedCount = 0
$conflictCount = 0

# Perform sync
foreach ($file in $filesToSync) {
    $relativePath = $file.FullName.Substring($sourceDir.Length).TrimStart('\', '/')
    $destPath = Join-Path $destDir $relativePath
    $destParent = Split-Path $destPath -Parent

    # Check if file exists at destination
    $exists = Test-Path $destPath

    # Determine action
    $action = "copy"
    if ($exists) {
        $sourceTime = $file.LastWriteTime
        $destTime = (Get-Item $destPath).LastWriteTime

        if ($Direction -eq 'Sync') {
            # Bidirectional sync - use newest
            if ($sourceTime -gt $destTime) {
                $action = "update"
            } elseif ($destTime -gt $sourceTime) {
                $action = "skip"
            } else {
                $action = "skip"  # Same timestamp
            }
        } else {
            # Unidirectional - always overwrite
            $action = "update"
        }
    }

    # Skip if no action needed
    if ($action -eq "skip") {
        $skippedCount++
        continue
    }

    # Preview or execute
    if ($WhatIf) {
        if ($action -eq "copy") {
            Write-Host "✓ Would sync: $relativePath" -ForegroundColor Green
        } else {
            Write-Host "⚠ Would update: $relativePath" -ForegroundColor Yellow
            $conflictCount++
        }
        $syncedCount++
        continue
    }

    # Handle conflicts
    if ($action -eq "update" -and -not $Force) {
        Write-Host "⚠ File exists: $relativePath" -ForegroundColor Yellow
        $response = Read-Host "  Overwrite? (y/N)"

        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "  Skipped" -ForegroundColor DarkGray
            $skippedCount++
            continue
        }
    }

    # Create parent directory
    if ($destParent -and -not (Test-Path $destParent)) {
        New-Item -ItemType Directory -Path $destParent -Force | Out-Null
    }

    # Copy file
    try {
        Copy-Item -Path $file.FullName -Destination $destPath -Force

        if ($action -eq "copy") {
            Write-Host "✓ Synced: $relativePath" -ForegroundColor Green
        } else {
            Write-Host "✓ Updated: $relativePath" -ForegroundColor Cyan
            $conflictCount++
        }
        $syncedCount++
    } catch {
        Write-Warning "Failed to sync $relativePath: $_"
        $skippedCount++
    }
}

# Summary
Write-Host ""
if ($WhatIf) {
    Write-Host "DRY RUN COMPLETE - No changes were made" -ForegroundColor Yellow
} else {
    Write-Success "🎉 Sync complete!"
}

Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "  Files synced:    $syncedCount" -ForegroundColor Gray
Write-Host "  Files skipped:   $skippedCount" -ForegroundColor Gray
Write-Host "  Files updated:   $conflictCount" -ForegroundColor Gray
Write-Host ""

if ($IncludeSSHKeys -and -not $WhatIf) {
    Write-Warning "⚠ SSH keys were synced to cloud storage"
    Write-Host "  Ensure your cloud storage is encrypted and secure" -ForegroundColor Yellow
    Write-Host ""
}

if ($Direction -eq 'Sync') {
    Write-Host "Note: Bidirectional sync uses newest file timestamp" -ForegroundColor Cyan
    Write-Host "  Deletions are NOT synchronized automatically" -ForegroundColor Gray
    Write-Host ""
}
