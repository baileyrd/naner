# Naner Vendor System Implementation Guide

This package contains everything you need to implement the vendor-based architecture for Naner.

## 📦 What's Included

### Core Scripts
1. **Setup-NanerVendor.ps1** (12KB)
   - Downloads and configures vendor dependencies
   - Initializes MSYS2 with essential packages
   - Creates vendor manifest
   - Handles Windows Terminal MSIX extraction

2. **Invoke-Naner.ps1** (12KB)
   - Updated launcher with vendor support
   - Unified PATH management
   - Profile selection with vendor paths
   - Environment variable handling

3. **Manage-NanerVendor.ps1** (6.2KB)
   - Version checking and updates
   - Vendor dependency management
   - Manifest inspection

4. **Build-NanerDistribution.ps1** (8.6KB)
   - Creates portable packages
   - Size optimization
   - License compliance
   - Distribution preparation

5. **Test-NanerInstallation.ps1** (17KB)
   - Comprehensive installation validation
   - Directory structure verification
   - Tool accessibility testing
   - Profile validation
   - Performance benchmarks

### Configuration
6. **naner.json** (2.7KB)
   - Updated configuration schema
   - Vendor path definitions
   - Unified environment settings
   - Profile examples

### Documentation
7. **README-VENDOR.md** (9KB)
   - Complete vendor system documentation
   - Setup instructions
   - Troubleshooting guide
   - Version management

8. **QUICK-START.md** (8.3KB)
   - User-friendly quick start guide
   - Usage examples
   - Common patterns
   - Tips and tricks

9. **ARCHITECTURE.md** (17KB)
   - Technical architecture overview
   - Design decisions
   - Component relationships
   - PATH management strategy
   - Comparison with alternatives

## 🚀 Implementation Steps

### Phase 1: Setup Your Project Structure

```powershell
# Navigate to your Naner project
cd C:\Users\BAILEYRD\dev\naner\naner_launcher

# Create the required directories
mkdir -p vendor, opt, icons

# Expected structure:
# naner_launcher/
# ├── bin/
# ├── config/
# ├── icons/
# ├── opt/
# ├── vendor/
# └── src/
#     └── powershell/
```

### Phase 2: Copy Files to Project

```powershell
# Copy scripts to appropriate locations
Copy-Item Setup-NanerVendor.ps1 -> src/powershell/
Copy-Item Invoke-Naner.ps1 -> src/powershell/ (replace existing)
Copy-Item Manage-NanerVendor.ps1 -> src/powershell/
Copy-Item Build-NanerDistribution.ps1 -> src/powershell/
Copy-Item Test-NanerInstallation.ps1 -> src/powershell/

# Copy configuration
Copy-Item naner.json -> config/ (merge with existing or replace)

# Copy documentation
Copy-Item README-VENDOR.md -> docs/
Copy-Item QUICK-START.md -> docs/
Copy-Item ARCHITECTURE.md -> docs/
```

### Phase 3: Initial Setup

```powershell
# Run vendor setup (requires internet)
cd src/powershell
.\Setup-NanerVendor.ps1

# This will:
# - Download 7-Zip (~2MB) - for extracting other dependencies
# - Download PowerShell 7.x (~100MB)
# - Download Windows Terminal (~50MB)
# - Download MSYS2 (~400MB)
# - Initialize MSYS2
# - Install essential packages (git, make, gcc, etc.)
# - Create vendor manifest

# Total download: ~552MB
# No external dependencies required!
```

### Phase 4: Test Installation

```powershell
# Run validation tests
.\Test-NanerInstallation.ps1

# Or run comprehensive tests
.\Test-NanerInstallation.ps1 -Full

# Expected output:
# ✓ All tests passed!
# Your Naner installation appears to be working correctly!
```

### Phase 5: Test Launcher

```powershell
# Test with debug output
.\Invoke-Naner.ps1 -DebugMode

# Test specific profile
.\Invoke-Naner.ps1 -Profile Bash -Debug

# Normal launch
.\Invoke-Naner.ps1
```

## 🔧 Configuration Adjustments

### Update Existing Config

If you have an existing `naner.json`, merge these sections:

```json
{
  "VendorPaths": {
    "PowerShell": "%NANER_ROOT%\\vendor\\powershell\\pwsh.exe",
    "WindowsTerminal": "%NANER_ROOT%\\vendor\\terminal\\wt.exe",
    "GitBash": "%NANER_ROOT%\\vendor\\msys64\\usr\\bin\\bash.exe"
  },
  
  "Environment": {
    "UnifiedPath": true,
    "PathPrecedence": [
      "%NANER_ROOT%\\bin",
      "%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin",
      "%NANER_ROOT%\\vendor\\msys64\\usr\\bin",
      "%NANER_ROOT%\\vendor\\powershell",
      "%NANER_ROOT%\\opt"
    ]
  }
}
```

### Remove Old Configuration

Remove or update any references to:
- System-installed Windows Terminal
- System PowerShell paths
- Separate Git Bash/MSYS2 profile configurations

## 📝 Migration from Current System

### What Changes

**Before** (Separate profiles):
```json
{
  "CustomProfiles": {
    "GitBash": {
      "ExecutablePath": "C:\\tools\\git\\bin\\bash.exe"
    },
    "MSYS2": {
      "ExecutablePath": "C:\\msys64\\msys2_shell.cmd"
    }
  }
}
```

**After** (Unified environment):
```json
{
  "DefaultProfile": "Unified",
  "Profiles": {
    "Unified": {
      "Shell": "PowerShell",
      "UseVendorPath": true
    }
  }
}
```

### Benefits of New Approach

1. **Single Environment**: All tools accessible from one profile
2. **Portable**: No external dependencies
3. **Consistent**: Same tools across all installations
4. **Maintainable**: Centralized version management

## 🎯 Testing Checklist

- [ ] Vendor setup completes successfully
- [ ] All tools accessible: `git`, `bash`, `make`, `gcc`, `pwsh`
- [ ] Windows Terminal launches
- [ ] PATH precedence correct (vendored tools first)
- [ ] Configuration valid
- [ ] Profiles work
- [ ] Starting directories resolve
- [ ] Environment variables set correctly

## 📊 Expected Sizes

### Before Optimization
- 7-Zip: ~2MB
- PowerShell: ~100MB
- Windows Terminal: ~50MB
- MSYS2: ~400MB
- **Total**: ~552MB

### After Optimization
- Removed caches: -100MB
- Removed docs: -80MB
- **Optimized**: ~372MB

### Compressed Distribution
- 7z compression: ~150MB
- ZIP compression: ~200MB

## 🐛 Common Issues & Solutions

### Issue: MSYS2 Extraction Fails

**Symptoms**:
```
[✗] Cannot extract .tar.xz files without 7-Zip
```

**Solution**:
This shouldn't happen as 7-Zip is now bundled. If it does:

```powershell
# Verify 7-Zip was extracted
Test-Path vendor\7zip\7z.exe

# If false, re-run setup
Remove-Item vendor -Recurse -Force
.\Setup-NanerVendor.ps1
```

**Why this happens**:
- 7-Zip is extracted first automatically
- If extraction chain fails, re-running setup resolves it
- No manual installation needed

### Issue: Download Fails

**Solution**:
```powershell
# Check network connectivity
Test-NetConnection github.com

# Use cached downloads if available
.\Setup-NanerVendor.ps1 -SkipDownload

# Or download manually and place in vendor/.downloads/
```

### Issue: MSYS2 Initialization Hangs

**Solution**:
```powershell
# Kill any hung processes
Get-Process msys* | Stop-Process -Force

# Re-run setup
.\Setup-NanerVendor.ps1 -ForceDownload
```

### Issue: PATH Not Working

**Solution**:
```powershell
# Check PATH in debug mode
.\Invoke-Naner.ps1 -DebugMode

# Verify config PathPrecedence order
# Ensure vendor directories exist
```

### Issue: Windows Terminal Extraction Fails

**Solution**:
```powershell
# Install 7-Zip or use tar
winget install 7zip.7zip

# Re-run setup
.\Setup-NanerVendor.ps1 -ForceDownload
```

## 🔮 Next Steps

### Immediate
1. Implement and test vendor system
2. Update configuration
3. Test all profiles
4. Validate tool accessibility

### Short Term
1. Create distribution package
2. Write migration guide for users
3. Update main README
4. Create release notes

### Long Term
1. Implement automatic updates
2. Add more vendor modules (Python, Node.js)
3. Create MSI installer
4. GUI configuration tool

## 📚 Documentation Structure

Recommended documentation layout:

```
docs/
├── README.md              # Main documentation
├── QUICK-START.md         # User quick start (this file)
├── README-VENDOR.md       # Vendor system details
├── ARCHITECTURE.md        # Technical architecture
├── CONTRIBUTING.md        # Contribution guide
└── CHANGELOG.md           # Version history
```

## 🤝 Migration Path for Users

### For New Users
1. Download portable package
2. Extract and run
3. Everything works out of the box

### For Existing Users
1. Backup existing installation
2. Update to new version
3. Run `Setup-NanerVendor.ps1`
4. Test with existing projects
5. Remove old external dependencies

## 💡 Tips

### Development
- Use `-DebugMode` flag for troubleshooting
- Test on clean Windows installations
- Validate on Windows 10 and 11

### Distribution
- Test portable package on multiple machines
- Include all documentation
- Provide clear upgrade instructions

### Maintenance
- Pin vendor versions in manifest
- Test updates before release
- Keep documentation current

## 🎓 Learning Resources

### For Understanding Components
- **PowerShell**: https://docs.microsoft.com/powershell/
- **MSYS2**: https://www.msys2.org/docs/
- **Windows Terminal**: https://docs.microsoft.com/windows/terminal/

### For Development
- **PowerShell Best Practices**: https://poshcode.gitbook.io/powershell-practice-and-style/
- **Git for Windows**: https://gitforwindows.org/
- **Packaging**: https://wixtoolset.org/

## 📞 Support

When requesting help, include:

```powershell
# Run diagnostic
.\Test-NanerInstallation.ps1 -Full > diagnostic.txt

# Include:
# - diagnostic.txt output
# - Your configuration (config/naner.json)
# - Error messages
# - Steps to reproduce
```

## ✅ Success Criteria

Your implementation is complete when:

1. ✓ `Setup-NanerVendor.ps1` runs without errors
2. ✓ `Test-NanerInstallation.ps1` passes all tests
3. ✓ `Invoke-Naner.ps1` launches Windows Terminal
4. ✓ All Unix tools accessible in unified environment
5. ✓ Git, make, gcc work as expected
6. ✓ PowerShell 7 available
7. ✓ No external dependencies required

## 🎉 What You Get

A truly portable, self-contained terminal environment with:
- Modern Windows Terminal UI
- PowerShell 7.x
- Complete Unix toolchain
- Git version control
- C/C++ development tools
- Package manager (pacman)
- Zero external dependencies
- Consistent across machines

**Congratulations on building the next generation of Naner!** 🚀

---

**Questions or issues?** Review the documentation or run tests with `-Full` flag for detailed diagnostics.
