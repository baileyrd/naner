# Quick Start: New Two-Executable Architecture

## What Changed?

Naner now uses two executables instead of one:

| Executable | Size | Purpose |
|------------|------|---------|
| **naner-init.exe** | ~10 MB | Initializer, updater, and launcher |
| **naner.exe** | ~11 MB | Main terminal launcher |

## Quick Start

### For New Users

1. **Download `naner-init.exe`** (only file you need!)
2. **Run it:**
   ```bash
   naner-init
   ```
3. It will automatically:
   - Download latest `naner.exe` from GitHub
   - Download config template
   - Set up directory structure
   - Launch Naner

### For Existing Users

1. **Build the new executables:**
   ```powershell
   cd src\csharp
   .\build-all.ps1
   ```

2. **Use `naner-init` instead of `naner`:**
   ```bash
   # Old way
   naner

   # New way (recommended)
   naner-init
   ```

## Key Commands

### naner-init (New!)

```bash
# Launch Naner (checks for updates automatically)
naner-init

# Initialize fresh installation
naner-init init

# Update to latest version
naner-init update

# Check if update available
naner-init check-update

# Pass arguments through to naner.exe
naner-init --profile PowerShell
naner-init Unified
```

### naner.exe (Still works!)

All existing commands still work:

```bash
# Launch terminal
naner

# Launch specific profile
naner --profile Bash

# Download vendors (now uses latest versions!)
naner setup-vendors

# Diagnostics
naner --diagnose
```

## What's Better?

### ✅ Always Latest Versions

**Before:**
- PowerShell 7.4.6 (hardcoded, outdated)
- Windows Terminal 1.21.2361.0 (hardcoded, outdated)
- You kept getting "update available" messages

**Now:**
- PowerShell 7.x.x (fetched from GitHub, always latest)
- Windows Terminal 1.x.x (fetched from GitHub, always latest)
- No more update messages!

### ✅ Automatic Updates

**Before:**
- Had to rebuild naner.exe for updates
- No way to check for new versions

**Now:**
- `naner-init update` downloads latest from GitHub
- Automatic update check on launch
- No rebuilding needed

### ✅ Simpler Distribution

**Before:**
- Needed entire naner repository to use

**Now:**
- Just download `naner-init.exe`
- It downloads everything else

## Build Commands

```powershell
# Build both executables
.\src\csharp\build-all.ps1

# Build just naner-init.exe
.\src\csharp\build-all.ps1 -InitOnly

# Build just naner.exe
.\src\csharp\build-all.ps1 -NanerOnly

# Build in Debug mode
.\src\csharp\build-all.ps1 -Configuration Debug
```

## Important Notes

### ⚠️ Before Publishing to GitHub

Update the GitHub repository information in:

**File:** `src\csharp\Naner.Init\NanerUpdater.cs` (lines 15-16)

```csharp
private const string GithubOwner = "your-username";  // Update this!
private const string GithubRepo = "naner";
```

### 📦 GitHub Release Requirements

When creating a release, include these assets:
- `naner.exe` (required)
- `naner.json` (optional, but recommended)

Example release:
```
Tag: v1.0.0
Assets:
  - naner.exe
  - naner.json
```

## Workflow Diagram

```
┌─────────────────┐
│  naner-init.exe │  (You run this)
└────────┬────────┘
         │
         ├─→ First time? ─→ Download naner.exe from GitHub
         │                  Download naner.json
         │                  Create directories
         │
         ├─→ Update available? ─→ Show notification
         │
         └─→ Launch naner.exe with your arguments
                    │
                    ├─→ Load config
                    ├─→ Setup environment
                    └─→ Launch Windows Terminal
```

## Troubleshooting

### Can't reach GitHub?

naner-init will warn you but continue launching. You can still use naner.exe directly.

### Update fails?

Your current version keeps working. Try `naner-init update` manually later.

### Want to force update?

```bash
naner-init update
```

## Documentation

Detailed documentation:
- [Two-Executable Architecture](docs/development/TWO-EXECUTABLE-ARCHITECTURE.md)
- [Implementation Summary](docs/development/IMPLEMENTATION-SUMMARY-2026-01-09.md)

## Migration Checklist

- [ ] Build new executables with `build-all.ps1`
- [ ] Update GitHub repo info in `NanerUpdater.cs`
- [ ] Test `naner-init --version`
- [ ] Test `naner-init --help`
- [ ] Create first GitHub release (v1.0.0)
- [ ] Upload naner.exe and naner.json as release assets
- [ ] Test full initialization: `naner-init init`
- [ ] Test update workflow: `naner-init update`
- [ ] Update main README with new instructions

## Questions?

**Q: Do I need both executables?**
A: For end users, just `naner-init.exe`. It downloads `naner.exe` automatically.

**Q: Can I still use `naner.exe` directly?**
A: Yes! All existing functionality works. But `naner-init` gives you auto-updates.

**Q: What if GitHub is down?**
A: naner-init will skip the update check and launch naner.exe normally.

**Q: How do I disable update checks?**
A: Currently always-on. A config file for this is a future enhancement.

**Q: Can I use this in CI/CD?**
A: Yes! naner-init has non-interactive mode. Use `naner.exe` directly in CI/CD if you prefer.

---

**TL;DR:** Use `naner-init` instead of `naner`. It handles updates automatically and always downloads the latest vendor versions. 🚀
