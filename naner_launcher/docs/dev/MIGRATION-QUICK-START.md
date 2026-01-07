# C# Migration Quick Start Guide

**🎯 Goal:** Convert Naner from PowerShell scripts to a native C# executable

**📁 Full Details:** See [CSHARP-MIGRATION-ROADMAP.md](CSHARP-MIGRATION-ROADMAP.md)

---

## Why Migrate?

| PowerShell (Current) | C# (Target) |
|---------------------|-------------|
| Requires PS runtime | ✅ Single .exe file |
| ~500-800ms startup | ✅ ~50-100ms startup |
| Multiple .ps1 files | ✅ One executable |
| Harder to distribute | ✅ Easy distribution |
| Script-level debugging | ✅ Professional IDE support |

---

## Three-Phase Approach

### Phase 1: Quick Win (2-3 weeks)
**Output:** `naner.exe` that wraps PowerShell scripts

```
C# Wrapper
  └── Embeds: All .ps1 files
  └── Size: ~30MB
  └── Startup: <1s
```

**Advantages:**
- ✅ Fast to implement
- ✅ Single file distribution
- ✅ Works without PowerShell installed
- ✅ All existing features preserved

### Phase 2: Core Migration (4-6 weeks)
**Output:** Hybrid C#/PowerShell solution

```
naner.exe (C#)
  ├── Naner.Common.dll         ← From Common.psm1
  ├── Naner.Configuration.dll  ← Config loading
  ├── Naner.Launcher.dll       ← Invoke-Naner.ps1
  └── Setup-NanerVendor.ps1    ← Keep as PowerShell
```

**Advantages:**
- ✅ ~15MB file size
- ✅ <500ms startup
- ✅ Native C# core
- ✅ Setup flexibility retained

### Phase 3: Pure C# (2-3 weeks)
**Output:** 100% native C# executable

```
naner.exe (100% C#)
  ├── Naner.Common.dll
  ├── Naner.Configuration.dll
  ├── Naner.Launcher.dll
  └── Naner.Vendor.dll         ← From Setup-NanerVendor.ps1
```

**Advantages:**
- ✅ ~10MB file size
- ✅ ~50-100ms startup
- ✅ No PowerShell dependency
- ✅ Production ready

---

## Getting Started

### Prerequisites
```bash
# Install .NET 8 SDK
winget install Microsoft.DotNet.SDK.8

# Verify installation
dotnet --version  # Should show 8.0.x
```

### Phase 1 Setup
```bash
# Create solution
mkdir naner-csharp
cd naner-csharp
dotnet new sln -n Naner

# Create launcher project
dotnet new console -n Naner.Launcher -o src/Naner.Launcher
dotnet sln add src/Naner.Launcher

# Add dependencies
cd src/Naner.Launcher
dotnet add package System.Management.Automation --version 7.4.0
dotnet add package CommandLineParser --version 2.9.1

# Build
dotnet build
```

### Quick Test
```bash
# Build single-file executable
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Test
.\bin\Release\net8.0\win-x64\publish\naner.exe --help
```

---

## Key Advantages of Recent Refactoring

The DRY/SOLID refactoring **directly benefits** C# migration:

| PowerShell Refactoring | C# Benefit |
|----------------------|------------|
| ✅ Common.psm1 module | → Clean `Naner.Common.dll` |
| ✅ Modular functions | → C# methods |
| ✅ No duplication | → No duplicate C# code |
| ✅ Single responsibility | → Clear class boundaries |
| ✅ Well-documented | → Easier migration |

---

## Migration Mapping

### PowerShell → C# Structure

```
Common.psm1
  ├── Write-Status()              → Logger.Status()
  ├── Find-NanerRoot()            → PathUtilities.FindRoot()
  ├── Expand-NanerPath()          → PathUtilities.Expand()
  └── Get-LatestGitHubRelease()   → GitHubClient.GetRelease()

Invoke-Naner.ps1
  ├── Main logic                  → TerminalLauncher class
  ├── Build-UnifiedPath()         → PathBuilder.BuildUnified()
  └── Get-ShellCommand()          → ShellResolver.GetCommand()

Setup-NanerVendor.ps1
  ├── Download functions          → Downloader.DownloadAsync()
  ├── Extract functions           → ArchiveExtractor.Extract()
  └── Vendor config               → VendorDefinition classes
```

---

## Timeline

```
Week 1-2  : Phase 1 Implementation     [■■□□□□□□□]
Week 3-6  : Phase 2 Core Migration     [■■■■■□□□□]
Week 7-9  : Phase 3 Complete Native    [■■■■■■■■■]

Total: 9 weeks (~160-300 hours)
```

---

## Success Metrics

### Performance Targets

| Metric | Current (PS) | Phase 1 | Phase 2 | Phase 3 |
|--------|-------------|---------|---------|---------|
| Startup | 500-800ms | <1s | <500ms | <150ms |
| File Size | N/A | ~30MB | ~15MB | ~10MB |
| Memory | ~50MB | ~45MB | ~35MB | ~30MB |

### Quality Targets

- ✅ 100% feature parity
- ✅ 85%+ test coverage
- ✅ Zero breaking changes
- ✅ Clear error messages
- ✅ Production ready

---

## Quick Commands Reference

```bash
# Build for development
dotnet build

# Build release (single file)
dotnet publish -c Release -r win-x64 --self-contained -p:PublishSingleFile=true

# Run tests
dotnet test

# Run with debugging
dotnet run -- --debug

# Build optimized (Phase 3)
dotnet publish -c Release -r win-x64 --self-contained \
  -p:PublishSingleFile=true \
  -p:PublishTrimmed=true \
  -p:PublishReadyToRun=true
```

---

## Next Steps

1. **Review** [CSHARP-MIGRATION-ROADMAP.md](CSHARP-MIGRATION-ROADMAP.md) for full details
2. **Decide** which phase to target initially
3. **Set up** development environment
4. **Begin** Phase 1 implementation
5. **Test** thoroughly at each phase
6. **Ship** early and often

---

## Questions?

- 📖 Full roadmap: [CSHARP-MIGRATION-ROADMAP.md](CSHARP-MIGRATION-ROADMAP.md)
- 🏗️ Architecture: [ARCHITECTURE.md](../ARCHITECTURE.md)
- 🧪 Testing: [TESTING-GUIDE.md](TESTING-GUIDE.md)
- 💻 Code quality: [CODE-QUALITY-ANALYSIS.md](CODE-QUALITY-ANALYSIS.md)

---

**Recommendation:** Start with **Phase 1** for quick wins and user feedback, then decide whether to continue to Phase 2/3.
