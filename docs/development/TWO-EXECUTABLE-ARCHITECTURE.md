# Naner Two-Executable Architecture

## Overview

Starting with version 1.0.0, Naner uses a two-executable architecture that separates initialization and updates from the main launcher functionality.

## Architecture

### 1. **naner-init.exe** - Standalone Initializer and Launcher

A small, standalone executable (~5-10 MB) that handles:

- **First-time initialization**: Downloads the latest naner.exe from GitHub releases
- **Update management**: Checks for and installs updates to naner.exe
- **Launch wrapper**: Acts as a lightweight launcher that passes control to naner.exe
- **Self-contained**: No dependencies on other Naner components

**Key Features:**
- Downloads latest naner.exe from GitHub releases
- Downloads naner.json config template from GitHub releases (if available)
- Automatic update checking on every launch
- Version comparison using semantic versioning
- Configurable update behavior

**Location:** `vendor/bin/naner-init.exe`

### 2. **naner.exe** - Main Terminal Launcher

The full-featured executable (~90+ MB) that handles:

- **Terminal launching**: Launches Windows Terminal with configured profiles
- **Profile management**: Manages terminal profiles and configurations
- **Vendor management**: Downloads and manages vendor dependencies (PowerShell, Git, etc.)
- **Environment setup**: Configures PATH and environment variables
- **All core Naner functionality**

**Location:** `vendor/bin/naner.exe`

## Workflow

```
User runs: naner-init.exe
  ↓
First run? ──→ Yes ──→ Download latest naner.exe from GitHub
           │         Download naner.json template from GitHub
           │         Create directory structure
           │         Mark as initialized
           ↓
Check for naner.exe updates
  ↓
Update available? ──→ Yes ──→ Download latest naner.exe
  ↓
Launch: naner.exe [args passed through]
```

## Commands

### naner-init Commands

```bash
# Launch Naner (auto-initialize if needed)
naner-init

# Initialize Naner (download from GitHub)
naner-init init

# Update Naner to the latest version
naner-init update

# Check if an update is available
naner-init check-update

# Pass arguments to naner.exe
naner-init [profile] [options]

# Show version
naner-init --version

# Show help
naner-init --help
```

### naner.exe Commands

All existing naner commands work as before:

```bash
# Launch with default profile
naner.exe

# Launch with specific profile
naner.exe --profile PowerShell

# Setup vendor dependencies
naner.exe setup-vendors

# Show diagnostics
naner.exe --diagnose

# Show version
naner.exe --version
```

**Note:** The `naner init` command is now deprecated in favor of `naner-init`.

## Dynamic Vendor Downloads

### Problem Solved

Previously, vendor downloads used hardcoded URLs with specific versions:
- PowerShell 7.4.6 (outdated)
- Windows Terminal 1.21.2361.0 (outdated)
- MSYS2 20240727 (outdated)
- 7-Zip 24.08 (may be outdated)

### New Solution

The new `DynamicVendorDownloader` class fetches the latest versions dynamically:

**GitHub API Integration:**
- Fetches latest releases from GitHub API
- Supports asset pattern matching
- Automatic fallback to hardcoded URLs if API fails

**Web Scraping:**
- Scrapes download pages for latest versions
- Uses regex patterns to extract download URLs
- Useful for vendors without GitHub releases API

### Supported Vendors

| Vendor | Fetch Type | Source |
|--------|------------|--------|
| 7-Zip | Web Scrape | https://www.7-zip.org/download.html |
| PowerShell | GitHub API | PowerShell/PowerShell releases |
| Windows Terminal | GitHub API | microsoft/terminal releases |
| MSYS2 | Web Scrape | https://repo.msys2.org/distrib/x86_64/ |

## Version Management

### Version File

naner-init stores the installed version in `.naner-version`:

```
vendor/bin/.naner-version
```

This file contains the version tag from the GitHub release (e.g., "v1.0.0").

### Version Comparison

Version comparison uses semantic versioning:
- Supports formats: "v1.0.0", "1.0.0", "1.0.0-beta"
- Compares major.minor.patch components
- Strips prefixes and suffixes for comparison

### Update Checking

By default, naner-init checks for updates on every launch:
- Silently checks GitHub releases API
- Shows warning if update available
- Non-intrusive (doesn't block launch)

## Building

### Build Both Executables

```powershell
.\src\csharp\build-all.ps1
```

### Build naner-init Only

```powershell
.\src\csharp\build-all.ps1 -InitOnly
```

### Build naner.exe Only

```powershell
.\src\csharp\build-all.ps1 -NanerOnly
```

### Build for Release

```powershell
.\src\csharp\build-all.ps1 -Configuration Release
```

## Directory Structure

```
naner/
├── vendor/
│   ├── bin/
│   │   ├── naner-init.exe      # Standalone initializer/launcher
│   │   ├── naner.exe            # Main terminal launcher
│   │   └── .naner-version       # Installed version file
│   ├── powershell/              # PowerShell vendor
│   ├── terminal/                # Windows Terminal vendor
│   ├── msys64/                  # MSYS2 (Git/Bash) vendor
│   └── 7zip/                    # 7-Zip vendor
├── config/
│   └── naner.json               # Configuration file
├── bin/                         # User binaries
├── home/                        # User home directory
└── .naner-initialized           # Initialization marker
```

## GitHub Releases Integration

### Required Release Assets

For naner-init to work properly, GitHub releases should include:

1. **naner.exe** - Main launcher executable (required)
2. **naner.json** - Configuration template (optional, but recommended)

### Release Tagging

Use semantic versioning for release tags:
- Format: `v1.0.0`, `v1.1.0`, `v2.0.0-beta`, etc.
- naner-init uses the tag to determine version
- Version comparison strips the 'v' prefix

### Creating a Release

1. Build both executables:
   ```powershell
   .\src\csharp\build-all.ps1 -Configuration Release
   ```

2. Create a GitHub release with tag (e.g., `v1.0.0`)

3. Upload assets:
   - `vendor/bin/naner.exe`
   - `config/naner.json` (optional)

4. Publish the release

### GitHub Repository Configuration

Update the GitHub repository information in `NanerUpdater.cs`:

```csharp
private const string GithubOwner = "your-username";
private const string GithubRepo = "naner";
```

## Migration from Previous Versions

### For Users

If you're already using Naner:

1. Build or download `naner-init.exe`
2. Place it in `vendor/bin/`
3. Run `naner-init` instead of `naner.exe`
4. naner-init will check for updates automatically

### For Developers

The old initialization code in naner.exe is deprecated but still functional:
- `naner init` command shows deprecation warning
- First-run detection recommends using naner-init
- Vendor downloads now use DynamicVendorDownloader

## Benefits

### User Benefits
- **Always up-to-date**: Automatic update notifications
- **Simpler setup**: One command to initialize and launch
- **Smaller initial download**: Only need naner-init.exe to start
- **Latest vendors**: Always downloads the most recent vendor versions

### Developer Benefits
- **Separation of concerns**: Init logic separated from launcher logic
- **Easier updates**: Users can update without rebuilding
- **Simpler distribution**: Only distribute naner-init.exe
- **Better versioning**: Clear version management with GitHub releases

## Configuration

### Disable Update Checks

To disable automatic update checking, you could extend naner-init with a config file or flag (future enhancement).

### Custom GitHub Repository

If you're forking Naner, update the GitHub repository information in:
- `src/csharp/Naner.Init/NanerUpdater.cs` (lines 15-16)

## Troubleshooting

### naner-init can't reach GitHub

If GitHub API is unavailable:
- naner-init will show a warning but continue launching
- You can still use naner.exe directly

### Update download fails

If update download fails:
- Your current version continues working
- Try running `naner-init update` manually
- Check your network connection

### Version mismatch

If `.naner-version` is missing or corrupted:
- naner-init will attempt to read version from naner.exe directly
- Falls back to "0.0.0" if version can't be determined

## Future Enhancements

Potential improvements:
- [ ] Configuration file for naner-init (update behavior, GitHub repo, etc.)
- [ ] Silent update mode (auto-update without prompting)
- [ ] Rollback capability (revert to previous version)
- [ ] Multiple release channels (stable, beta, nightly)
- [ ] Differential updates (download only changed files)
- [ ] Update notifications via Windows notifications

## Technical Details

### Projects

- **Naner.Init** (`src/csharp/Naner.Init/`)
  - GitHubReleasesClient.cs - GitHub API integration
  - VersionComparer.cs - Semantic version comparison
  - NanerUpdater.cs - Update and initialization logic
  - ConsoleHelper.cs - Console output formatting
  - Program.cs - Main entry point

- **Naner.Common** (`src/csharp/Naner.Common/`)
  - DynamicVendorDownloader.cs - Dynamic vendor fetching

### Dependencies

naner-init has minimal dependencies:
- .NET 8.0 Runtime (bundled with executable)
- System.Text.Json (for JSON parsing)

### Size Optimization

naner-init is optimized for size:
- `IlcOptimizationPreference=Size` in project file
- Trimmed and self-contained
- Target size: ~5-10 MB (vs ~90+ MB for naner.exe)

## See Also

- [C# Migration Roadmap](CSHARP-MIGRATION-ROADMAP.md)
- [Capability Assessment 2026](CAPABILITY-ASSESSMENT-2026.md)
- [Main README](../../README.md)
