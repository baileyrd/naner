# Naner Quick Start Guide

Welcome to Naner! This guide will help you get started with your new portable terminal environment.

## First Time Setup

### 1. Download and Extract
Extract the Naner archive to any location on your system. No installation required!

```
C:\tools\naner\          # Example location
├── bin/
├── config/
├── icons/
├── opt/
└── vendor/
```

### 2. Run Setup (First Time Only)
Open PowerShell in the Naner directory and run:

```powershell
.\Setup-NanerVendor.ps1
```

This downloads and configures:
- PowerShell 7.x
- Windows Terminal
- MSYS2 (with Git and Unix tools)

**Note**: This requires an internet connection and takes 5-10 minutes.

### 3. Launch Naner
```powershell
.\Invoke-Naner.ps1
```

That's it! You now have a unified environment with PowerShell, Git, and Unix tools.

## Understanding the Unified Environment

### What You Get

When you launch Naner, you get **one environment** with access to all these tools:

#### Unix/Linux Tools
```bash
# Version control
git clone https://github.com/user/repo.git
git status

# File operations
ls -la
grep -r "pattern" .
find . -name "*.txt"

# Text processing
sed 's/old/new/g' file.txt
awk '{print $1}' data.txt

# Archives
tar -xzf archive.tar.gz
zip -r backup.zip folder/
```

#### Compilers & Build Tools
```bash
# C/C++ compilation
gcc -o program program.c
g++ -o app main.cpp

# Make
make build
make install
```

#### PowerShell
```powershell
# PowerShell commands
Get-ChildItem -Recurse
Get-Process | Where-Object CPU -gt 100

# .NET commands
[System.Environment]::OSVersion
```

#### Windows Commands
```cmd
where git
ipconfig
netstat -an
```

### The Magic of Unified Environment

**Old way** (Cmder/Git Bash):
- Switch between terminals for different tools
- CMD for Windows commands
- Git Bash for Unix tools
- PowerShell for .NET

**Naner way**:
- Everything in one place
- No switching needed
- Mix and match tools freely

## Common Usage Patterns

### Development Workflow

```powershell
# Start in your project directory
cd C:\Projects\myapp

# Use Git
git checkout -b new-feature
git pull origin main

# Build with make
make clean
make build

# Run tests with PowerShell
.\run-tests.ps1

# Package with Unix tools
tar -czf release.tar.gz dist/
```

### System Administration

```powershell
# Mix PowerShell and Unix tools
Get-Process | grep "chrome"

# Use sed with PowerShell output
Get-Content log.txt | sed 's/ERROR/⚠️ ERROR/g'

# Batch process files
ls *.log | xargs grep "error"
```

### Web Development

```bash
# Node.js development
npm install
npm run build

# Git workflow
git add .
git commit -m "Add feature"
git push

# Check disk usage
du -sh node_modules/
```

## Configuration

### Customizing Your Environment

Edit `config/naner.json` to customize:

#### Change Starting Directory
```json
{
  "Profiles": {
    "Unified": {
      "StartingDirectory": "C:\\Projects"
    }
  }
}
```

#### Add Custom Environment Variables
```json
{
  "Environment": {
    "EnvironmentVariables": {
      "MY_API_KEY": "your-key-here",
      "PROJECT_ROOT": "C:\\Projects\\main"
    }
  }
}
```

#### Modify PATH Order
```json
{
  "Environment": {
    "PathPrecedence": [
      "%NANER_ROOT%\\bin",
      "C:\\MyTools",                          // Your custom tools first
      "%NANER_ROOT%\\vendor\\msys64\\mingw64\\bin",
      "%NANER_ROOT%\\vendor\\msys64\\usr\\bin"
    ]
  }
}
```

### Adding Your Own Tools

Place executables in the `opt/` directory:

```
opt/
├── ffmpeg/
│   └── ffmpeg.exe
└── my-scripts/
    ├── deploy.ps1
    └── backup.sh
```

They'll automatically be available in your environment!

## Profiles Explained

While Naner defaults to a unified environment, you can use different profiles:

### Unified (Default)
- PowerShell with full Unix tool access
- Best for most users
- Launch: `.\Invoke-Naner.ps1`

### PowerShell
- Pure PowerShell 7 with vendored tools in PATH
- Launch: `.\Invoke-Naner.ps1 -Profile PowerShell`

### Bash
- Native Bash environment (MSYS2)
- Unix-first, Windows-aware
- Launch: `.\Invoke-Naner.ps1 -Profile Bash`

### CMD
- Windows Command Prompt with Unix tools in PATH
- Launch: `.\Invoke-Naner.ps1 -Profile CMD`

## Troubleshooting

### "Command not found" Error

**Check which command is being used**:
```bash
which git          # Unix way
where.exe git      # Windows way
```

**Check your PATH**:
```powershell
$env:PATH -split ';'
```

### Tool Version Conflicts

If you have Git or other tools installed system-wide, Naner's versions take precedence.

**To use system version instead**:
1. Remove from Naner PATH
2. Edit `config/naner.json`:
```json
{
  "Environment": {
    "PathPrecedence": [
      // Remove the conflicting path
    ]
  }
}
```

### MSYS2 Path Issues

Some Unix tools expect POSIX paths. Convert Windows paths:

```bash
# Windows path
C:\Users\Name\file.txt

# MSYS2 path
/c/Users/Name/file.txt

# Use cygpath to convert
cygpath -u 'C:\Users\Name\file.txt'
```

### Reset to Defaults

```powershell
# Re-run setup to restore defaults
.\Setup-NanerVendor.ps1 -ForceDownload

# Or manually restore config
Copy-Item config\naner.json.default config\naner.json
```

## Tips & Tricks

### Create Command Aliases

**PowerShell** (`$PROFILE` or `config/profile.ps1`):
```powershell
Set-Alias g git
Set-Alias ll 'ls -la'

function proj { cd C:\Projects }
```

**Bash** (`~/.bashrc`):
```bash
alias g='git'
alias ll='ls -la'
alias proj='cd /c/Projects'
```

### Quick Directory Navigation

```bash
# Bash
cd /c/Projects/myapp
cd ../other-app

# PowerShell
cd C:\Projects\myapp
cd ..\other-app
```

### Combine Tools Creatively

```powershell
# Use Unix grep with PowerShell
Get-ChildItem -Recurse | Select-Object FullName | grep "test"

# Use PowerShell formatting with Unix tools
git log --oneline | Select-Object -First 10

# Chain commands
curl https://api.example.com/data | jq '.items' | ConvertFrom-Json
```

### Background Jobs

```powershell
# PowerShell jobs
Start-Job { .\long-running-task.ps1 }
Get-Job
Receive-Job -Id 1

# Unix background jobs
./build.sh &
jobs
fg %1
```

## Advanced Usage

### Custom Profiles

Create your own profile in `config/naner.json`:

```json
{
  "CustomProfiles": {
    "Development": {
      "Name": "Development Environment",
      "Shell": "PowerShell",
      "StartingDirectory": "C:\\Dev",
      "Icon": "%NANER_ROOT%\\icons\\dev.ico",
      "UseVendorPath": true,
      "CustomShell": {
        "ExecutablePath": "%NANER_ROOT%\\vendor\\powershell\\pwsh.exe",
        "Arguments": "-NoExit -ExecutionPolicy Bypass -File %NANER_ROOT%\\config\\dev-init.ps1"
      }
    }
  }
}
```

Launch with:
```powershell
.\Invoke-Naner.ps1 -Profile Development
```

### Initialization Scripts

**PowerShell**: Create `config/init.ps1`:
```powershell
# Custom prompt
function prompt {
    "Naner $(Get-Location)> "
}

# Environment setup
$env:PROJECT_ROOT = "C:\Projects"

# Load modules
Import-Module posh-git
```

**Bash**: Create `config/bashrc`:
```bash
# Custom prompt
PS1='\[\033[01;32m\]naner\[\033[00m\]:\[\033[01;34m\]\w\[\033[00m\]\$ '

# Environment
export PROJECT_ROOT="/c/Projects"

# Aliases
alias gs='git status'
```

### Integration with IDEs

**VS Code**: Add to `settings.json`:
```json
{
  "terminal.integrated.profiles.windows": {
    "Naner": {
      "path": "C:\\tools\\naner\\vendor\\powershell\\pwsh.exe",
      "args": ["-NoExit", "-Command", "& { $env:PATH = 'C:\\tools\\naner\\vendor\\msys64\\usr\\bin;' + $env:PATH }"]
    }
  },
  "terminal.integrated.defaultProfile.windows": "Naner"
}
```

## Getting Help

### Documentation
- `README-VENDOR.md`: Detailed vendor system documentation
- `config/naner.json`: Configuration reference (with comments)

### Commands
```powershell
# Check configuration
.\Invoke-Naner.ps1 -DebugMode

# List installed versions
.\Manage-NanerVendor.ps1 -ListVersions

# Check for updates
.\Manage-NanerVendor.ps1 -CheckUpdates
```

### Community
- Report issues: [Your repository]
- Discussions: [Your forum/Discord]
- Contributing: See CONTRIBUTING.md

## What's Next?

### Explore
- Try mixing PowerShell and Unix commands
- Set up your development workflow
- Customize your environment
- Add your favorite tools to `opt/`

### Learn More
- PowerShell: https://docs.microsoft.com/powershell/
- Git: https://git-scm.com/doc
- MSYS2: https://www.msys2.org/

### Share
If you find Naner useful, share it with others!

---

**Happy terminal-ing!** 🚀
