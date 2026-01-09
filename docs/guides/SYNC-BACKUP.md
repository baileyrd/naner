# Naner Sync & Backup Guide

Complete guide to backing up and synchronizing your Naner configuration across machines.

## Overview

Naner provides three powerful tools for configuration management:

1. **Backup-NanerConfig.ps1** - Create timestamped backups of your configurations
2. **Restore-NanerConfig.ps1** - Restore configurations from backups
3. **Sync-NanerConfig.ps1** - Synchronize with cloud storage (OneDrive, Dropbox, Google Drive)

**Benefits:**
- Protect against accidental configuration loss
- Quickly set up Naner on new machines
- Keep configurations synchronized across multiple computers
- Selective backup/sync with smart filtering
- Version control through timestamped backups

## Quick Start

### Create a Backup

```powershell
# Simple backup to Desktop
.\src\powershell\Backup-NanerConfig.ps1

# Compressed backup
.\src\powershell\Backup-NanerConfig.ps1 -Compress

# Custom location
.\src\powershell\Backup-NanerConfig.ps1 -BackupPath "D:\Backups"
```

### Restore from Backup

```powershell
# Restore from backup directory
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "C:\Users\You\Desktop\Naner-Backups\naner-backup-2026-01-07-143022"

# Restore from compressed backup
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "D:\Backups\naner-backup.zip"

# Preview what would be restored (dry-run)
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -WhatIf
```

### Sync with Cloud

```powershell
# Push to OneDrive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push

# Pull from Dropbox
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Dropbox -Direction Pull

# Bidirectional sync with Google Drive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider GoogleDrive -Direction Sync
```

## What Gets Backed Up?

### ✅ Included (Safe to Sync)

**Shell Configurations:**
- `.bashrc` - Bash configuration
- `.bash_profile` - Bash login profile
- `.bash_aliases` - Shell aliases
- `.gitconfig` - Git configuration
- `.vimrc` - Vim configuration
- `.nanorc` - Nano configuration

**Editor Settings:**
- `.config/` - Editor configurations (Windows Terminal, etc.)
- `.vscode/` - VS Code settings, keybindings, extensions list

**SSH Configuration:**
- `.ssh/config` - SSH host configurations
- `.ssh/config.example` - SSH config template
- **NOT** `.ssh/id_*` - Private keys (excluded by default)

**PowerShell:**
- `Documents/PowerShell/` - PowerShell profile, modules, scripts

**Project Templates:**
- `Templates/` - All project templates

**Total Size:** Typically 1-5 MB (very portable!)

### ❌ Excluded (NOT Synced)

**Package Caches (Large, platform-specific):**
- `.cargo/registry/`, `.cargo/git/` - Rust package cache
- `.conda/pkgs/`, `.conda/envs/` - Python environments
- `.gem/ruby/*/cache/` - Ruby gem cache
- `.npm-cache/`, `.npm-global/lib/` - npm packages
- `go/pkg/`, `go/bin/` - Go build artifacts

**Secrets & Credentials:**
- `.ssh/id_*`, `.ssh/*.pem` - SSH private keys
- `.git-credentials` - Git credentials
- `.bash_history` - Shell command history

**Temporary Files:**
- `*.log`, `*.tmp`, `*.temp`
- `.DS_Store`, `Thumbs.db`, `desktop.ini`
- `.viminfo`, `.lesshst`

**Lock Files (Platform-specific):**
- `package-lock.json`, `yarn.lock`
- `Gemfile.lock`, `Cargo.lock`, `go.sum`

**Binary Files:**
- `*.exe`, `*.dll`, `*.so`, `*.dylib`
- `*.zip`, `*.tar.gz`, `*.7z`

**Why exclude these?**
- **Size:** Caches can be 100s of MB or even GBs
- **Platform-specific:** Won't work on different machines
- **Security:** Private keys should never be in cloud storage
- **Regenerable:** Can be recreated by package managers

## Backup Operations

### Basic Backup

```powershell
# Creates: Desktop\Naner-Backups\naner-backup-2026-01-07-143022\
.\src\powershell\Backup-NanerConfig.ps1
```

**What happens:**
1. Creates timestamped backup directory
2. Copies all configuration files
3. Creates `BACKUP-MANIFEST.json` with metadata
4. Displays summary of backed-up files

**Backup Manifest Example:**
```json
{
    "BackupDate": "2026-01-07 14:30:22",
    "NanerRoot": "C:\\Users\\You\\dev\\naner",
    "BackupVersion": "1.0",
    "IncludedSSHKeys": false,
    "ItemsBackedUp": 15,
    "ItemsSkipped": 2
}
```

### Compressed Backup

```powershell
# Creates: Desktop\Naner-Backups\naner-backup-2026-01-07-143022.zip
.\src\powershell\Backup-NanerConfig.ps1 -Compress
```

**Benefits:**
- Smaller file size (typically 60-80% compression)
- Single file to transfer
- Easier to store and share

**Use cases:**
- Emailing backup to yourself
- Uploading to cloud storage
- Creating archives for versioning

### Custom Backup Location

```powershell
# Backup to external drive
.\src\powershell\Backup-NanerConfig.ps1 -BackupPath "E:\Naner-Backups"

# Backup to network share
.\src\powershell\Backup-NanerConfig.ps1 -BackupPath "\\\\server\\backups\\naner"

# Named backup (for specific events)
.\src\powershell\Backup-NanerConfig.ps1 -BackupName "pre-update-backup"
```

### Including SSH Keys (⚠ Use with Caution)

```powershell
# Include SSH private keys in backup
.\src\powershell\Backup-NanerConfig.ps1 -IncludeSSHKeys
```

**Security Warnings:**
- ⚠ SSH keys are SENSITIVE - do not upload to cloud without encryption
- ⚠ Keys in backup can access your servers if compromised
- ✅ Only use for local encrypted backups or secure offline storage
- ✅ Consider using password-protected SSH keys

**Recommended workflow with SSH keys:**
```powershell
# 1. Create backup with keys
.\src\powershell\Backup-NanerConfig.ps1 -IncludeSSHKeys -Compress -BackupName "full-backup"

# 2. Encrypt the zip file (using 7-Zip, GPG, or BitLocker)
7z a -p -mhe=on naner-encrypted.7z naner-backup-full.zip

# 3. Store encrypted backup in cloud
# 4. Delete unencrypted backup
Remove-Item naner-backup-full.zip
```

## Restore Operations

### Basic Restore

```powershell
# Restore from directory backup
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "C:\Backups\naner-backup-2026-01-07-143022"
```

**What happens:**
1. Reads backup manifest
2. Lists backup information
3. Prompts for overwrites if files exist
4. Restores all files
5. Displays summary

### Restore from Compressed Backup

```powershell
# Automatically extracts and restores
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "C:\Backups\naner-backup.zip"
```

**Process:**
1. Extracts zip to temporary location
2. Restores files from temp
3. Cleans up temporary extraction
4. Displays summary

### Dry-Run Mode (Preview)

```powershell
# Preview what would be restored without making changes
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -WhatIf
```

**Output:**
```
✓ Would restore: .bashrc
✓ Would restore: .gitconfig
⚠ Would overwrite: .vscode/settings.json
✓ Would restore: Documents/PowerShell/Microsoft.PowerShell_profile.ps1

DRY RUN COMPLETE - No changes were made
```

### Force Overwrite

```powershell
# Overwrite all existing files without prompting
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -Force
```

**Use cases:**
- Automated restore scripts
- Fresh machine setup
- Rolling back after bad changes

### Selective Restore

```powershell
# Restore everything except VS Code settings and templates
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -Exclude ".vscode", "Templates"

# Restore only shell configs
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -Exclude ".config", ".vscode", "Documents", "Templates"
```

### Restore SSH Keys

```powershell
# Restore SSH keys (if included in backup)
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath ".\backup" -RestoreSSHKeys
```

**Post-restore for SSH keys:**
```bash
# Set correct permissions (in Bash)
chmod 600 ~/.ssh/id_*
chmod 644 ~/.ssh/*.pub
chmod 600 ~/.ssh/config
```

## Cloud Sync Operations

### OneDrive Sync

```powershell
# Push local configs to OneDrive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push

# Pull configs from OneDrive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Pull

# Bidirectional sync (uses newest files)
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Sync
```

**Default OneDrive Path:**
- `%USERPROFILE%\OneDrive\Naner-Config`
- Synced automatically across devices with OneDrive

### Dropbox Sync

```powershell
# Push to Dropbox
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Dropbox -Direction Push

# Pull from Dropbox
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Dropbox -Direction Pull
```

**Default Dropbox Path:**
- `%USERPROFILE%\Dropbox\Naner-Config`
- Synced across all devices with Dropbox

### Google Drive Sync

```powershell
# Push to Google Drive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider GoogleDrive -Direction Push

# Pull from Google Drive
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider GoogleDrive -Direction Pull
```

**Default Google Drive Path:**
- `%USERPROFILE%\Google Drive\Naner-Config`
- Synced via Google Drive desktop app

### Custom Sync Path

```powershell
# Sync to custom location
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Custom -SyncPath "D:\MyCloud\Naner" -Direction Sync

# Sync to network share
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Custom -SyncPath "\\\\server\\share\\naner" -Direction Push
```

### Sync Directions Explained

#### Push (Local → Cloud)
```powershell
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push
```
- Copies local files to cloud
- Overwrites cloud files
- **Use when:** You've made local changes to sync to other machines

#### Pull (Cloud → Local)
```powershell
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Pull
```
- Copies cloud files to local
- Overwrites local files
- **Use when:** Setting up a new machine or syncing changes from another PC

#### Sync (Bidirectional)
```powershell
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Sync
```
- Compares timestamps
- Uses newest version of each file
- **Use when:** Regular synchronization between machines
- **Note:** Deletions are NOT synced automatically

### Dry-Run Cloud Sync

```powershell
# Preview sync operations
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Sync -WhatIf
```

**Output:**
```
✓ Would sync: .bashrc
⚠ Would update: .gitconfig (cloud is newer)
✓ Would sync: Documents/PowerShell/Microsoft.PowerShell_profile.ps1

DRY RUN COMPLETE - No changes were made
Summary:
  Files synced:  15
  Files skipped: 3
  Files updated: 1
```

## Sync Ignore Patterns

### Understanding .syncignore

The `.syncignore` file controls what gets synchronized to cloud storage. Located at:
- `%NANER_ROOT%\.syncignore`

**Format:**
- One pattern per line
- `#` for comments
- Glob patterns supported: `*` (any chars), `?` (single char)
- Applies to sync operations only (backup includes everything)

### Default Exclusions

```
# Package caches (large, platform-specific)
.cargo/registry/*
.conda/pkgs/*
.gem/ruby/*/cache/*
.npm-cache/*

# SSH keys (security)
.ssh/id_*
.ssh/*.pem

# Shell history
.bash_history
.zsh_history

# Lock files (platform-specific)
*.lock
package-lock.json
Cargo.lock
```

### Customizing .syncignore

Edit `.syncignore` to add your own patterns:

```
# Default patterns...

# Custom Exclusions
# My large project files
Documents/PowerShell/Modules/MyHugeModule/*

# Temporary workspace configs
.vscode/workspace.code-workspace

# Custom caches
.custom-cache/*
```

### Testing Ignore Patterns

```powershell
# Preview what would be synced with current .syncignore
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push -WhatIf
```

## Common Workflows

### Workflow 1: New Machine Setup

**On existing machine:**
```powershell
# 1. Create backup
.\src\powershell\Backup-NanerConfig.ps1 -Compress -BackupName "machine-transfer"

# 2. Upload to cloud or copy to USB drive
# (Manual step)
```

**On new machine:**
```powershell
# 1. Clone Naner repository
git clone https://github.com/your-fork/naner.git
cd naner

# 2. Install required vendors
.\src\powershell\Setup-Naner.ps1

# 3. Restore configuration
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "path\to\machine-transfer.zip" -Force

# 4. Done! Your environment is ready
```

### Workflow 2: Multi-Machine Sync

**Initial setup (once per machine):**
```powershell
# On first machine - push to cloud
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push

# On second machine - pull from cloud
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Pull
```

**Daily workflow (on any machine):**
```powershell
# Before starting work - pull latest
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Pull

# After making changes - push updates
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push
```

**Or use bidirectional sync:**
```powershell
# Before and after work - automatic sync
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Sync
```

### Workflow 3: Scheduled Backups

**Create scheduled task (PowerShell):**
```powershell
# Create backup script
@"
cd C:\path\to\naner
.\src\powershell\Backup-NanerConfig.ps1 -Compress
"@ | Out-File -FilePath "C:\Scripts\naner-backup.ps1"

# Register scheduled task (run as Administrator)
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-File C:\Scripts\naner-backup.ps1"
$trigger = New-ScheduledTaskTrigger -Daily -At 9am
Register-ScheduledTask -TaskName "Naner Daily Backup" -Action $action -Trigger $trigger
```

**Or use Windows Task Scheduler GUI:**
1. Open Task Scheduler
2. Create Basic Task
3. Name: "Naner Backup"
4. Trigger: Daily at 9:00 AM
5. Action: Start a program
6. Program: `powershell.exe`
7. Arguments: `-File "C:\path\to\naner\src\powershell\Backup-NanerConfig.ps1" -Compress`

### Workflow 4: Version Control with Git

Since Naner configs are in the `home/` directory, you can use Git:

```powershell
# In Naner root
cd home

# Initialize git (if not already tracked)
git init

# Add configurations
git add .bashrc .gitconfig .vscode/ Documents/PowerShell/

# Commit
git commit -m "Update shell aliases"

# Push to personal config repo
git remote add config https://github.com/yourname/naner-config.git
git push config main
```

**Benefits:**
- Full version history
- Diff between changes
- Rollback to any previous state
- Separate public/private configs

### Workflow 5: Pre-Update Backup

**Before major Naner updates:**
```powershell
# Create named backup before update
.\src\powershell\Backup-NanerConfig.ps1 -Compress -BackupName "pre-v2.0-update"

# Update Naner
git pull origin main

# If something breaks, restore
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath "Desktop\Naner-Backups\pre-v2.0-update.zip" -Force
```

## Advanced Scenarios

### Migrating Between Windows and WSL

**Export from Windows Naner:**
```powershell
.\src\powershell\Backup-NanerConfig.ps1 -Compress -BackupPath "C:\temp"
```

**Import to WSL Naner:**
```bash
# In WSL
cd ~/naner

# Copy backup from Windows filesystem
cp /mnt/c/temp/naner-backup-*.zip ./

# Extract
unzip naner-backup-*.zip -d ~/naner-backup

# Manually copy configs (paths may differ)
cp ~/naner-backup/.bashrc ~/naner/home/
cp ~/naner-backup/.gitconfig ~/naner/home/
# etc.
```

**Considerations:**
- Line endings (CRLF vs LF)
- Path separators (\ vs /)
- Some configs may need adjustment

### Encrypting Backups

**Using 7-Zip with password:**
```powershell
# Create backup
.\src\powershell\Backup-NanerConfig.ps1 -Compress -BackupName "sensitive-backup"

# Encrypt with 7-Zip (requires 7-Zip installed)
7z a -p -mhe=on naner-encrypted.7z Desktop\Naner-Backups\sensitive-backup.zip

# Delete unencrypted backup
Remove-Item Desktop\Naner-Backups\sensitive-backup.zip
```

**Using GPG:**
```bash
# Encrypt backup
gpg -c naner-backup.zip
# Creates naner-backup.zip.gpg

# Decrypt later
gpg naner-backup.zip.gpg
```

### Syncing to Git Repository

**Create private config repo:**
```powershell
# In Naner home directory
cd home

# Initialize repo
git init
git remote add origin https://github.com/yourname/naner-private-config.git

# Add .gitignore (use .syncignore patterns)
Copy-Item ..\.syncignore .gitignore

# Commit and push
git add .
git commit -m "Initial Naner config"
git push -u origin main
```

**On new machine:**
```powershell
# Clone Naner
git clone https://github.com/your-fork/naner.git
cd naner

# Clone your private configs into home/
cd home
git init
git remote add origin https://github.com/yourname/naner-private-config.git
git pull origin main
```

## Troubleshooting

### Backup Issues

#### "Backup already exists"
**Error:** Directory already exists at backup location

**Solutions:**
```powershell
# Use custom name
.\src\powershell\Backup-NanerConfig.ps1 -BackupName "backup-$(Get-Random)"

# Or delete existing
Remove-Item "Desktop\Naner-Backups\naner-backup-*" -Recurse -Force

# Or use different location
.\src\powershell\Backup-NanerConfig.ps1 -BackupPath "D:\Backups"
```

#### "Failed to backup X"
**Cause:** Permission denied or file in use

**Solutions:**
```powershell
# Run as Administrator
# Or close programs using the files
# Or check file permissions
```

### Restore Issues

#### "Backup not found"
**Error:** Cannot find backup at specified path

**Solutions:**
```powershell
# Verify path exists
Test-Path "C:\Backups\naner-backup-*.zip"

# Use tab completion for path
.\src\powershell\Restore-NanerConfig.ps1 -BackupPath [TAB]

# Check for typos in path
```

#### "Failed to extract backup"
**Error:** Cannot extract .zip file

**Solutions:**
```powershell
# Check if file is corrupted
# Download again if from cloud
# Try extracting manually first
Expand-Archive -Path backup.zip -DestinationPath C:\temp\test
```

### Sync Issues

#### "Sync path does not exist"
**Error:** Cloud storage folder not found

**Solutions:**
```powershell
# Check if OneDrive/Dropbox is installed
Test-Path "$env:USERPROFILE\OneDrive"

# Use custom path
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider Custom -SyncPath "D:\MyCloud\Naner" -Direction Push

# Create directory manually
New-Item -ItemType Directory -Path "$env:USERPROFILE\OneDrive\Naner-Config" -Force
```

#### "No changes synced"
**Cause:** All files filtered by .syncignore

**Solutions:**
```powershell
# Preview with -WhatIf
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push -WhatIf

# Check .syncignore patterns
Get-Content .syncignore

# Temporarily remove patterns to test
```

#### "Sync conflicts"
**Cause:** Files exist at destination with different timestamps

**Solutions:**
```powershell
# Use -Force to overwrite all
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push -Force

# Use -WhatIf to preview conflicts
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Sync -WhatIf

# Manually resolve conflicts first
# Then sync
```

## Best Practices

### ✅ DO

- **Backup regularly** - At least weekly or before major changes
- **Use compressed backups** for cloud storage - Saves space and transfer time
- **Test restores periodically** - Verify backups work
- **Use .syncignore** - Exclude large caches and binaries
- **Version backups** - Keep multiple timestamped backups
- **Encrypt sensitive backups** - Especially if including SSH keys
- **Use bidirectional sync** for multi-machine workflows
- **Preview with -WhatIf** before major operations

### ❌ DON'T

- **Don't include SSH keys in cloud sync** unless encrypted
- **Don't sync package caches** - Wastes space and bandwidth
- **Don't delete all old backups** - Keep some history
- **Don't force overwrite** without understanding conflicts
- **Don't sync without .syncignore** - Will sync unnecessary files
- **Don't forget to pull before push** in multi-machine setups

## Performance Tips

### Reduce Backup Size

```powershell
# Use compression
.\src\powershell\Backup-NanerConfig.ps1 -Compress

# Exclude large files manually
# Edit .syncignore before backup
```

**Typical sizes:**
- Uncompressed backup: 2-5 MB
- Compressed backup: 500 KB - 2 MB
- With SSH keys: +1-2 MB
- With caches (not recommended): 100+ MB

### Speed Up Sync

```powershell
# Use Push/Pull instead of Sync for one-way operations
# Faster than comparing timestamps

# Push only (no comparison)
.\src\powershell\Sync-NanerConfig.ps1 -SyncProvider OneDrive -Direction Push -Force
```

### Minimize Cloud Storage Usage

**Optimize .syncignore:**
```
# Add aggressive patterns
*.log
**/cache/*
**/temp/*
**/tmp/*
```

**Compress before manual upload:**
```powershell
.\src\powershell\Backup-NanerConfig.ps1 -Compress
# Upload the .zip to cloud manually
```

## Security Considerations

### SSH Keys

**❌ NEVER sync private keys to unsecured cloud storage**
- Default: `.ssh/id_*` excluded via .syncignore
- If needed: Use encrypted backup + secure offline storage

**✅ Safe approach:**
```powershell
# Local encrypted backup only
.\src\powershell\Backup-NanerConfig.ps1 -IncludeSSHKeys -Compress -BackupPath "E:\SecureBackup"
# E:\ is encrypted USB drive or encrypted local folder
```

### Git Credentials

**`.git-credentials` is excluded by default**
- Contains plain-text credentials
- Use Git credential helpers instead

### Sensitive Configurations

**For configs with secrets:**
1. Store secrets in separate `.env` files
2. Add `.env` to .syncignore
3. Document required secrets in README
4. Use Windows Credential Manager or similar

**Example:**
```bash
# .env (not synced)
GITHUB_TOKEN=ghp_xxxxxxxxxxxx
API_KEY=sk_xxxxxxxxxxxx

# .env.example (synced)
GITHUB_TOKEN=your_github_token_here
API_KEY=your_api_key_here
```

## Related Documentation

- [Portable SSH](PORTABLE-SSH.md) - SSH key management
- [Portable Git](PORTABLE-GIT.md) - Git configuration
- [Portable PowerShell](PORTABLE-POWERSHELL.md) - PowerShell profiles
- [Capability Roadmap](CAPABILITY-ROADMAP.md) - Phase 9.3 details

## References

- [OneDrive Documentation](https://support.microsoft.com/en-us/office/sync-files-with-onedrive)
- [Dropbox Help](https://help.dropbox.com/sync/sync-overview)
- [Google Drive Desktop](https://support.google.com/drive/answer/7329379)

---

**Version:** 1.0
**Last Updated:** 2026-01-07
**Phase:** 9.3 - Sync & Backup Integration

