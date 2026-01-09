# Naner Architecture

This document provides a technical overview of Naner's architecture and design decisions.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                         User Interface                           │
│                     Windows Terminal (wt.exe)                    │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Naner Launcher Layer                        │
│                     (Invoke-Naner.ps1)                          │
│  • Profile Selection    • PATH Management    • Config Loading   │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Unified Environment                          │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────────┐ │
│  │ PowerShell  │  │  Unix Tools │  │   Development Tools     │ │
│  │   (pwsh)    │  │ (bash, git) │  │  (gcc, make, etc.)      │ │
│  └─────────────┘  └─────────────┘  └─────────────────────────┘ │
└───────────────────────────────┬─────────────────────────────────┘
                                │
                                ▼
┌─────────────────────────────────────────────────────────────────┐
│                      Vendor Dependencies                         │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────────┐  │
│  │  PowerShell  │  │   MSYS2      │  │  Windows Terminal   │  │
│  │    7.x       │  │  (Unix env)  │  │       (UI)          │  │
│  │  (pwsh.exe)  │  │ (bash, git)  │  │     (wt.exe)        │  │
│  └──────────────┘  └──────────────┘  └─────────────────────┘  │
└─────────────────────────────────────────────────────────────────┘
```

## Component Overview

### 1. Windows Terminal (wt.exe)
**Purpose**: Modern terminal UI with tabs, theming, and GPU-accelerated rendering

**Location**: `vendor/terminal/wt.exe`

**Portable Mode**: `.portable` file enables local settings storage

**Responsibilities**:
- Render terminal UI
- Handle tabs and panes
- Apply color schemes
- Manage window lifecycle
- Store settings locally (via `.portable` file)

**Why Vendored**: 
- Ensures consistent experience
- Not all Windows 10 users have it installed
- Allows version pinning for stability
- Portable mode keeps settings with Naner

**Portable Mode Explained**:
Windows Terminal checks for a `.portable` file in its directory. When present:
- Settings stored in: `vendor/terminal/LocalState/settings.json`
- State stored in: `vendor/terminal/LocalState/state.json`
- No writes to `%LOCALAPPDATA%\Packages\Microsoft.WindowsTerminal_*\`
- Complete isolation from system Windows Terminal
- Settings travel with Naner installation

### 2. PowerShell 7.x
**Purpose**: Modern shell with cross-platform cmdlets and improved performance

**Location**: `vendor/powershell/pwsh.exe`

**Responsibilities**:
- Primary shell environment
- Script execution
- .NET integration
- Pipeline processing

**Why Vendored**:
- Windows ships with PowerShell 5.1 by default
- Version 7.x has significant improvements
- Avoids conflicts with system PowerShell

### 3. MSYS2
**Purpose**: Complete Unix-like environment for Windows

**Location**: `vendor/msys64/`

**Responsibilities**:
- Provide Unix tools (bash, grep, sed, awk, etc.)
- Git version control
- Compilation tools (gcc, make)
- Package management (pacman)

**Key Directories**:
```
msys64/
├── usr/bin/          # MSYS environment (POSIX emulation)
│   ├── bash.exe      # Bash shell
│   ├── git.exe       # Git
│   └── ...
├── mingw64/bin/      # MINGW64 environment (native Windows)
│   ├── gcc.exe       # GCC compiler
│   ├── make.exe      # GNU Make
│   └── ...
└── home/             # User home directories
```

**Why Both MSYS and MINGW64**:
- **MSYS** (`usr/bin/`): For tools needing POSIX emulation (fork, signals)
  - Example: bash, ssh, rsync
- **MINGW64** (`mingw64/bin/`): For native Windows binaries (better performance)
  - Example: gcc, make, compiled programs

## PATH Management Strategy

### PATH Precedence Order

```
1. %NANER_ROOT%\bin                        # Naner utilities (highest priority)
2. %NANER_ROOT%\vendor\msys64\mingw64\bin  # Native Windows binaries
3. %NANER_ROOT%\vendor\msys64\usr\bin      # POSIX-emulated tools
4. %NANER_ROOT%\vendor\powershell          # PowerShell 7
5. %NANER_ROOT%\opt                        # User tools
6. System PATH                             # Windows system paths (lowest priority)
```

### Why This Order?

**Naner utilities first**: Allows overriding any tool with custom wrapper scripts

**MINGW64 before MSYS**: 
- Native binaries perform better
- Avoids POSIX path translation issues
- Better Windows integration

**Vendor tools before system**:
- Ensures consistent versions
- Prevents conflicts with outdated system tools
- Portable across different Windows installations

**User tools (opt) last**:
- Allows users to override default tools
- Maintains vendored tool priority
- Flexible customization

### Conflict Resolution Example

If multiple versions of `git` exist:

```
C:\Program Files\Git\bin\git.exe        # System Git (low priority)
vendor\msys64\usr\bin\git.exe           # MSYS2 Git (high priority)
opt\custom-git\git.exe                  # User Git (highest priority)
```

User gets: `opt\custom-git\git.exe`

## Configuration System

### Configuration Hierarchy

```
1. naner.json                    # Main configuration
   ├── VendorPaths              # Paths to vendored tools
   ├── Environment              # PATH and env vars
   ├── Profiles                 # Built-in profiles
   └── CustomProfiles           # User-defined profiles

2. Environment Variables
   ├── NANER_ROOT               # Set by launcher
   ├── MSYSTEM                  # MSYS2 environment type
   └── Custom vars              # User-defined

3. Runtime Arguments            # Command-line overrides
   ├── -Profile                 # Profile selection
   ├── -StartingDirectory       # Working directory
   └── -DebugMode               # Verbose output
```

### Path Expansion

Paths in configuration support multiple formats:

```json
{
  "paths": {
    "windows_style": "%NANER_ROOT%\\bin",
    "powershell_style": "$env:NANER_ROOT\\bin",
    "mixed": "%USERPROFILE%\\$env:USERNAME"
  }
}
```

Expansion order:
1. Replace `%NANER_ROOT%` with actual path
2. Expand Windows-style variables (`%VAR%`)
3. Expand PowerShell-style variables (`$env:VAR`)

## Profile System

### Profile Types

**1. Built-in Profiles** (in `Profiles` section):
- Unified: PowerShell + Unix tools (default)
- PowerShell: Pure PowerShell 7
- Bash: Native Bash environment
- CMD: Command Prompt + tools

**2. Custom Profiles** (in `CustomProfiles` section):
- User-defined configurations
- Can override any setting
- Inherit from built-ins

### Profile Resolution

```
User specifies profile → Check CustomProfiles → Check Profiles → Use DefaultProfile
```

### Profile Attributes

```json
{
  "Name": "Display name",
  "Description": "Descriptive text",
  "Shell": "PowerShell | Bash | CMD",
  "StartingDirectory": "Path",
  "Icon": "Icon path",
  "ColorScheme": "Terminal color scheme",
  "UseVendorPath": true,
  "CustomShell": {
    "ExecutablePath": "Shell binary path",
    "Arguments": "Shell arguments"
  }
}
```

## Environment Isolation

### Why Isolation Matters

**Problem without isolation**:
```
System PATH: C:\Program Files\Git\bin;C:\Python39;C:\Node
Naner PATH:  vendor\msys64\usr\bin;vendor\powershell

Result: Conflicts! Which git? Which python?
```

**Solution with isolation**:
```
Naner PATH:  vendor\msys64\usr\bin;vendor\powershell;[System PATH]

Result: Vendored tools first, predictable behavior
```

### Controlled System PATH Inheritance

```json
{
  "Environment": {
    "InheritSystemPath": true  // Append system PATH
  }
}
```

When `true`:
- Vendored tools take precedence
- System tools available as fallback
- Best of both worlds

When `false`:
- Pure vendored environment
- Maximum isolation
- No system conflicts

## MSYS2 Integration

### Environment Types

MSYS2 provides three environments:

**1. MSYS** (`msys2_shell.cmd -msys`):
- POSIX emulation via msys-2.0.dll
- Unix-like behavior
- Tools: bash, ssh, rsync
- Use for: Scripts expecting Unix paths

**2. MINGW64** (`msys2_shell.cmd -mingw64`):
- Native Windows binaries
- No POSIX emulation
- Tools: gcc, make, utilities
- Use for: Compiling Windows programs

**3. MINGW32** (not used in Naner):
- 32-bit Windows binaries
- Legacy support only

### Naner's Choice: Unified MINGW64

Naner uses **MINGW64 as primary** because:
- Better Windows integration
- Native performance
- No path translation issues
- Still includes bash and Unix tools

Environment variable set:
```
MSYSTEM=MINGW64
```

This tells MSYS2 tools to operate in MINGW64 mode.

### Path Translation

MSYS2 tools understand both Windows and Unix paths:

```bash
# All work in MINGW64 mode:
cd C:\Users\Name      # Windows path
cd /c/Users/Name      # Unix path
cd ~/                 # Home directory

# Tools handle conversion automatically:
git clone C:\Projects\repo
git clone /c/Projects/repo
```

## Bootstrap Process

### Launch Sequence

```
1. User runs: Invoke-Naner.ps1
                  │
                  ▼
2. Find-NanerRoot
   └─> Traverse up directory tree
   └─> Locate bin/, vendor/, config/
                  │
                  ▼
3. Load Configuration
   └─> Read naner.json
   └─> Validate paths
   └─> Expand variables
                  │
                  ▼
4. Select Profile
   └─> Use -Profile argument OR
   └─> Use DefaultProfile
                  │
                  ▼
5. Build Environment
   └─> Construct PATH
   └─> Set environment variables
   └─> Resolve shell command
                  │
                  ▼
6. Launch Windows Terminal
   └─> Start wt.exe with:
       • Shell path
       • Arguments
       • Environment
       • Starting directory
```

### Error Handling

Each step includes validation:

```powershell
# Step 2: Root validation
if (-not (Test-Path $binPath)) {
    throw "Invalid Naner root"
}

# Step 3: Config validation
if (-not (Test-Path $configPath)) {
    throw "Configuration not found"
}

# Step 5: Vendor validation
if (-not (Test-Path $wtPath)) {
    throw "Windows Terminal not found. Run Setup-NanerVendor.ps1"
}
```

## Vendor Management

### Setup Process

```
Setup-NanerVendor.ps1
        │
        ▼
1. Download
   ├─> PowerShell zip
   ├─> Windows Terminal msixbundle
   └─> MSYS2 tarball
        │
        ▼
2. Extract
   ├─> PowerShell → vendor/powershell/
   ├─> Windows Terminal → vendor/terminal/
   └─> MSYS2 → vendor/msys64/
        │
        ▼
3. Configure
   ├─> Initialize MSYS2
   ├─> Update package database
   └─> Install essential packages
        │
        ▼
4. Create Manifest
   └─> vendor/vendor-manifest.json
```

### Version Tracking

```json
{
  "Version": "1.0.0",
  "Created": "2024-01-01 12:00:00",
  "Dependencies": {
    "PowerShell": {
      "Name": "PowerShell",
      "Version": "7.4.6",
      "ExtractDir": "powershell"
    },
    ...
  }
}
```

## Size Optimization

### Optimization Strategy

**Before optimization**: ~850MB
**After optimization**: ~600MB
**Compressed (7z)**: ~250MB

### What Gets Removed

```
1. Package Cache
   vendor/msys64/var/cache/          # ~100MB

2. Documentation
   vendor/msys64/usr/share/doc/      # ~50MB
   vendor/msys64/usr/share/man/      # ~30MB

3. Development Files
   vendor/msys64/usr/share/info/     # ~20MB
   
4. Git GUI
   vendor/msys64/mingw64/share/git-gui/  # ~10MB

5. Download Cache
   vendor/.downloads/                 # Variable
```

### What Gets Kept

- All executables
- All libraries
- Runtime data
- Configuration files
- License files

## Security Considerations

### Code Signing

**Recommendation**: Sign all executables for distribution

```powershell
# Sign with Authenticode
Set-AuthenticodeSignature -FilePath "Invoke-Naner.ps1" `
    -Certificate $cert `
    -TimestampServer "http://timestamp.digicert.com"
```

### PATH Injection Prevention

**Risk**: Malicious tools in PATH

**Mitigation**:
1. Vendored tools first in PATH
2. Validate all paths on load
3. User tools in `opt/` clearly separated
4. Documentation on security best practices

### Update Security

**Risk**: Malicious updates

**Mitigation**:
1. Download from official sources only
2. Verify checksums when available
3. Use HTTPS for all downloads
4. Consider signed update manifests

## Performance Considerations

### Startup Time

**Target**: <2 seconds from launch to shell prompt

**Optimization**:
1. Lazy loading of modules
2. Minimal PATH entries
3. Cached configuration
4. No unnecessary initializations

### Memory Usage

**Baseline**: ~50MB (Windows Terminal + Shell)
**With tools**: ~100MB

**Profile**:
- Windows Terminal: ~30MB
- PowerShell: ~50MB
- MSYS2 runtime: ~20MB

### Disk I/O

**Minimize reads**:
- Cache configuration
- Don't scan directories unnecessarily
- Use absolute paths where possible

## Comparison with Alternatives

### vs. Git Bash

| Feature | Git Bash | Naner |
|---------|----------|-------|
| Unix tools | ✓ Limited | ✓ Full MSYS2 |
| PowerShell | ✗ | ✓ |
| Windows Terminal | ✗ | ✓ |
| Portable | ✓ | ✓ |
| Package manager | ✗ | ✓ pacman |
| Development tools | ✗ | ✓ gcc, make |

### vs. Cmder

| Feature | Cmder | Naner |
|---------|-------|-------|
| Terminal | ConEmu | Windows Terminal |
| Unix tools | ✓ Limited | ✓ Full MSYS2 |
| PowerShell | PS 5.1 | PS 7.x |
| Portable | ✓ | ✓ |
| Configuration | INI files | JSON |
| Maintenance | Inactive | Active |

### vs. WSL2

| Feature | WSL2 | Naner |
|---------|------|-------|
| Unix compatibility | ✓✓✓ Full Linux | ✓✓ MSYS2 |
| Windows integration | ✓ Good | ✓✓ Native |
| Portable | ✗ Requires install | ✓ |
| Performance | ✓✓ VM overhead | ✓✓✓ Native |
| Admin required | ✓ | ✗ |

## Future Architecture Considerations

### Modular Vendor System

**Concept**: Allow users to choose which vendor components to install

```json
{
  "VendorModules": {
    "required": ["WindowsTerminal"],
    "optional": ["PowerShell", "MSYS2", "Python", "Node"]
  }
}
```

### Plugin System

**Concept**: Allow third-party extensions

```
plugins/
├── docker-integration/
├── cloud-tools/
└── ide-integration/
```

### Cloud Sync

**Concept**: Sync configuration and user tools across machines

```json
{
  "Cloud": {
    "Provider": "OneDrive",
    "SyncPaths": ["config/", "opt/"],
    "Exclude": ["vendor/"]
  }
}
```

## Contributing to Architecture

When proposing architectural changes:

1. **Document the problem** clearly
2. **Consider alternatives** with pros/cons
3. **Impact analysis**: What breaks? What improves?
4. **Migration path**: How do users upgrade?
5. **Backward compatibility**: Can we maintain it?

## References

- Windows Terminal Docs: https://docs.microsoft.com/windows/terminal/
- PowerShell Docs: https://docs.microsoft.com/powershell/
- MSYS2 Wiki: https://www.msys2.org/wiki/
- Cmder Source: https://github.com/cmderdev/cmder
