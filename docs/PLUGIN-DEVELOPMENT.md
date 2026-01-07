# Naner Plugin Development Guide

**Version:** 1.0.0
**Date:** 2026-01-07
**Status:** Official Documentation

This guide explains how to create, distribute, and manage plugins for Naner, the portable development environment.

---

## Table of Contents

- [Introduction](#introduction)
- [Plugin Architecture](#plugin-architecture)
- [Creating a Plugin](#creating-a-plugin)
- [Plugin Manifest](#plugin-manifest)
- [Plugin Hooks](#plugin-hooks)
- [Environment Setup](#environment-setup)
- [Vendor Integration](#vendor-integration)
- [Testing Plugins](#testing-plugins)
- [Distributing Plugins](#distributing-plugins)
- [Best Practices](#best-practices)
- [Examples](#examples)
- [Troubleshooting](#troubleshooting)

---

## Introduction

### What are Plugins?

Naner plugins are modular extensions that add functionality to your portable development environment. Plugins can:

- Install and configure development tools (JDK, .NET SDK, databases)
- Add environment variables and PATH entries
- Provide custom scripts and commands
- Extend Naner's capabilities without modifying core code

### Plugin Benefits

- **Modularity** - Enable only what you need
- **Portability** - Plugins travel with your Naner installation
- **Isolation** - Each plugin has its own directory and configuration
- **Extensibility** - Community can create and share plugins
- **Maintainability** - Easy to enable, disable, update, or remove

---

## Plugin Architecture

### Directory Structure

```
plugins/
├── my-plugin/
│   ├── plugin.json          # Plugin manifest (required)
│   ├── README.md            # Plugin documentation (recommended)
│   ├── hooks/               # PowerShell hook scripts (optional)
│   │   ├── install.ps1      # Run on installation
│   │   ├── uninstall.ps1    # Run on uninstallation
│   │   ├── enable.ps1       # Run when enabled
│   │   ├── disable.ps1      # Run when disabled
│   │   └── env-setup.ps1    # Run during environment initialization
│   ├── scripts/             # Custom scripts (optional)
│   │   └── helper.ps1
│   └── config/              # Plugin-specific configs (optional)
│       └── settings.json
```

### Plugin Lifecycle

1. **Installation** - Plugin copied to `plugins/` directory
2. **Enablement** - Plugin marked as enabled in manifest
3. **Environment Setup** - Hooks run during shell initialization
4. **Runtime** - Plugin functionality available in environment
5. **Disablement** - Plugin marked as disabled
6. **Uninstallation** - Plugin removed from `plugins/` directory

---

## Creating a Plugin

### Step 1: Initialize Plugin Directory

```powershell
# Create plugin directory structure
$pluginName = "my-plugin"
$pluginDir = "$env:NANER_ROOT\plugins\$pluginName"

mkdir $pluginDir
mkdir "$pluginDir\hooks"
mkdir "$pluginDir\scripts"
mkdir "$pluginDir\config"
```

### Step 2: Create Plugin Manifest

Create `plugin.json` with required fields:

```json
{
  "$schema": "../../config/plugin-schema.json",
  "id": "my-plugin",
  "name": "My Plugin",
  "version": "1.0.0",
  "description": "A sample Naner plugin",
  "author": "Your Name",
  "enabled": false,
  "nanerVersion": "1.0.0"
}
```

### Step 3: Add Environment Configuration

Define environment variables and PATH entries:

```json
{
  "environment": {
    "variables": {
      "MY_TOOL_HOME": "%NANER_ROOT%\\vendor\\mytool",
      "MY_TOOL_CONFIG": "%NANER_ROOT%\\home\\.mytool\\config"
    },
    "pathPrecedence": [
      "%NANER_ROOT%\\vendor\\mytool\\bin"
    ]
  }
}
```

### Step 4: Create Hook Scripts

Create `hooks/env-setup.ps1`:

```powershell
param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

# Set environment variables
$env:MY_TOOL_HOME = Join-Path $Context.NanerRoot "vendor\mytool"
$env:MY_TOOL_CONFIG = Join-Path $Context.NanerRoot "home\.mytool\config"

# Add to PATH
$binPath = Join-Path $env:MY_TOOL_HOME "bin"
if ($env:PATH -notlike "*$binPath*") {
    $env:PATH = "$binPath;$env:PATH"
}
```

### Step 5: Test Your Plugin

```powershell
# Install the plugin
Install-NanerPlugin -PluginPath "path\to\my-plugin"

# Enable the plugin
Enable-NanerPlugin -PluginId "my-plugin"

# Restart terminal and verify
$env:MY_TOOL_HOME
```

---

## Plugin Manifest

### Required Fields

| Field | Type | Description |
|-------|------|-------------|
| `id` | string | Unique plugin identifier (lowercase, hyphen-separated) |
| `name` | string | Human-readable plugin name |
| `version` | string | Plugin version (semver: `1.0.0`) |
| `description` | string | Brief description of the plugin |
| `author` | string | Plugin author name or organization |

### Optional Fields

| Field | Type | Description |
|-------|------|-------------|
| `homepage` | string | Plugin homepage or repository URL |
| `license` | string | Plugin license (SPDX identifier: MIT, Apache-2.0) |
| `enabled` | boolean | Whether plugin is enabled (default: false) |
| `nanerVersion` | string | Minimum required Naner version |
| `dependencies` | array | List of plugin dependencies |
| `vendors` | array | Vendor tools to install |
| `environment` | object | Environment variables and PATH entries |
| `hooks` | object | Available hook scripts |
| `configuration` | object | Plugin-specific configuration |
| `tags` | array | Tags for categorization and search |

### Full Example

```json
{
  "$schema": "../../config/plugin-schema.json",
  "id": "java-jdk",
  "name": "Java Development Kit",
  "version": "1.0.0",
  "description": "Portable Java JDK for Java development",
  "author": "Naner",
  "homepage": "https://github.com/yourusername/naner",
  "license": "MIT",
  "enabled": false,
  "nanerVersion": "1.0.0",
  "dependencies": [],
  "vendors": [
    {
      "name": "Eclipse Temurin JDK",
      "description": "Open-source Java Development Kit",
      "extractDir": "java",
      "releaseSource": {
        "type": "github",
        "repo": "adoptium/temurin21-binaries",
        "assetPattern": "*jdk_x64_windows_hotspot*.zip"
      }
    }
  ],
  "environment": {
    "variables": {
      "JAVA_HOME": "%NANER_ROOT%\\vendor\\java",
      "CLASSPATH": ".;%JAVA_HOME%\\lib\\*"
    },
    "pathPrecedence": [
      "%NANER_ROOT%\\vendor\\java\\bin"
    ]
  },
  "hooks": {
    "enable": true,
    "disable": true,
    "envSetup": true
  },
  "tags": ["java", "jvm", "development", "compiler"]
}
```

---

## Plugin Hooks

### Available Hooks

| Hook | File | When Executed | Use Case |
|------|------|---------------|----------|
| Install | `hooks/install.ps1` | After plugin installation | Download files, create directories |
| Uninstall | `hooks/uninstall.ps1` | Before plugin removal | Cleanup, remove files |
| Enable | `hooks/enable.ps1` | When plugin is enabled | Verify installation, show messages |
| Disable | `hooks/disable.ps1` | When plugin is disabled | Cleanup, show warnings |
| Env Setup | `hooks/env-setup.ps1` | During shell initialization | Set env vars, modify PATH |

### Hook Context

All hooks receive a `$Context` hashtable parameter:

```powershell
param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

# Available context properties:
# $Context.PluginId      - Plugin ID
# $Context.PluginPath    - Plugin directory path
# $Context.NanerRoot     - Naner root directory
# $Context.HookName      - Name of the hook being executed
```

### Hook Example: Enable

```powershell
# hooks/enable.ps1
param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Enabling $($Context.PluginId) plugin..." -ForegroundColor Cyan

$toolPath = Join-Path $Context.NanerRoot "vendor\mytool"

# Check if tool is installed
if (-not (Test-Path $toolPath)) {
    Write-Warning "Tool not found. Please install first."
    return
}

# Verify installation
$exe = Join-Path $toolPath "bin\mytool.exe"
if (Test-Path $exe) {
    $version = & $exe --version 2>&1
    Write-Host "Tool enabled: $version" -ForegroundColor Green
}

Write-Host "Environment will be set on next shell launch" -ForegroundColor Green
```

### Hook Example: Env Setup

```powershell
# hooks/env-setup.ps1
param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

$toolPath = Join-Path $Context.NanerRoot "vendor\mytool"

if (Test-Path $toolPath) {
    # Set environment variables
    $env:MYTOOL_HOME = $toolPath
    $env:MYTOOL_CONFIG = Join-Path $Context.NanerRoot "home\.mytool"

    # Add to PATH
    $binPath = Join-Path $toolPath "bin"
    if ($env:PATH -notlike "*$binPath*") {
        $env:PATH = "$binPath;$env:PATH"
    }

    # Create config directory
    if (-not (Test-Path $env:MYTOOL_CONFIG)) {
        New-Item -ItemType Directory -Path $env:MYTOOL_CONFIG -Force | Out-Null
    }
}
```

---

## Environment Setup

### Environment Variables

Define variables in the `environment.variables` section:

```json
{
  "environment": {
    "variables": {
      "JAVA_HOME": "%NANER_ROOT%\\vendor\\java",
      "JDK_HOME": "%NANER_ROOT%\\vendor\\java",
      "CLASSPATH": ".;%JAVA_HOME%\\lib\\*"
    }
  }
}
```

**Variable Expansion:**
- `%NANER_ROOT%` - Expands to Naner root directory
- `%USERPROFILE%` - Expands to user profile directory
- Other Windows environment variables are supported

### PATH Management

Add directories to PATH in the `environment.pathPrecedence` section:

```json
{
  "environment": {
    "pathPrecedence": [
      "%NANER_ROOT%\\vendor\\java\\bin",
      "%NANER_ROOT%\\home\\.mytool\\bin"
    ]
  }
}
```

**PATH Priority:**
- Paths are added in the order specified
- First path has highest priority
- Avoids duplicate PATH entries

### Dynamic Environment Setup

For dynamic configuration, use `hooks/env-setup.ps1`:

```powershell
param([hashtable]$Context)

# Conditional environment variables
if (Test-Path "$env:NANER_ROOT\vendor\java") {
    $env:JAVA_HOME = "$env:NANER_ROOT\vendor\java"

    # Detect Java version
    $javaExe = "$env:JAVA_HOME\bin\java.exe"
    if (Test-Path $javaExe) {
        $version = & $javaExe -version 2>&1 | Select-Object -First 1
        if ($version -match "version \"(\d+)") {
            $env:JAVA_VERSION = $matches[1]
        }
    }
}
```

---

## Vendor Integration

### Defining Vendors

Plugins can install vendor dependencies:

```json
{
  "vendors": [
    {
      "name": "My Tool",
      "description": "My development tool",
      "extractDir": "mytool",
      "releaseSource": {
        "type": "github",
        "repo": "owner/repo",
        "assetPattern": "*windows-x64.zip"
      },
      "postInstallFunction": "MyTool.PostInstall"
    }
  ]
}
```

### Vendor Source Types

#### GitHub Releases

```json
{
  "type": "github",
  "repo": "owner/repository",
  "assetPattern": "*windows-x64.zip",
  "fallback": {
    "version": "1.0.0",
    "url": "https://github.com/owner/repo/releases/download/v1.0.0/tool.zip",
    "fileName": "tool.zip",
    "size": "~100"
  }
}
```

#### Web Scraping

```json
{
  "type": "web-scrape",
  "url": "https://example.com/downloads",
  "pattern": "href=\"([^\"]*tool-([\\d.]+)-x64\\.zip)\"",
  "fallback": {
    "version": "1.0.0",
    "url": "https://example.com/tool-1.0.0-x64.zip",
    "fileName": "tool-1.0.0-x64.zip"
  }
}
```

#### Static URL

```json
{
  "type": "static",
  "url": "https://example.com/downloads/tool-latest.zip",
  "fallback": {
    "version": "latest",
    "url": "https://example.com/downloads/tool-latest.zip",
    "fileName": "tool-latest.zip"
  }
}
```

### PostInstall Functions

Create a PostInstall function in your hook scripts or in `Naner.Vendors.psm1`:

```powershell
function Initialize-MyTool {
    param(
        [Parameter(Mandatory)]
        [string]$VendorPath
    )

    Write-NanerInfo "Initializing MyTool..."

    # Flatten nested directories
    $nestedDir = Get-ChildItem -Path $VendorPath -Directory | Select-Object -First 1
    if ($nestedDir) {
        Move-Item -Path "$($nestedDir.FullName)\*" -Destination $VendorPath -Force
        Remove-Item -Path $nestedDir.FullName -Recurse -Force
    }

    # Verify installation
    $exe = Join-Path $VendorPath "bin\mytool.exe"
    if (Test-Path $exe) {
        $version = & $exe --version 2>&1
        Write-NanerSuccess "MyTool $version installed successfully"
    }
}
```

---

## Testing Plugins

### Manual Testing

```powershell
# 1. Install plugin
Install-NanerPlugin -PluginPath "C:\dev\my-plugin"

# 2. Verify installation
Get-NanerPlugin -PluginId "my-plugin"

# 3. Enable plugin
Enable-NanerPlugin -PluginId "my-plugin"

# 4. Restart terminal

# 5. Verify environment
$env:MY_TOOL_HOME
Get-Command mytool

# 6. Test functionality
mytool --version

# 7. Disable plugin
Disable-NanerPlugin -PluginId "my-plugin"

# 8. Uninstall plugin
Uninstall-NanerPlugin -PluginId "my-plugin"
```

### Automated Testing

Create Pester tests for your plugin:

```powershell
# tests/unit/MyPlugin.Tests.ps1

Describe "My Plugin Tests" {
    BeforeAll {
        Import-Module "$PSScriptRoot\..\..\src\powershell\Naner.Plugins.psm1" -Force
    }

    Context "Plugin Installation" {
        It "Should install plugin successfully" {
            $result = Install-NanerPlugin -PluginPath "$PSScriptRoot\..\..\plugins\my-plugin"
            $result | Should -Not -BeNullOrEmpty
            $result.id | Should -Be "my-plugin"
        }

        It "Should have valid manifest" {
            $plugin = Get-NanerPlugin -PluginId "my-plugin"
            $plugin.id | Should -Be "my-plugin"
            $plugin.version | Should -Match '^\d+\.\d+\.\d+$'
        }
    }

    Context "Plugin Lifecycle" {
        It "Should enable plugin" {
            { Enable-NanerPlugin -PluginId "my-plugin" } | Should -Not -Throw
        }

        It "Should disable plugin" {
            { Disable-NanerPlugin -PluginId "my-plugin" } | Should -Not -Throw
        }
    }

    AfterAll {
        Uninstall-NanerPlugin -PluginId "my-plugin" -Force
    }
}
```

---

## Distributing Plugins

### Plugin Packaging

#### Option 1: Directory

Share the plugin directory:

```
my-plugin/
├── plugin.json
├── README.md
└── hooks/
    └── env-setup.ps1
```

Users install with:
```powershell
Install-NanerPlugin -PluginPath "C:\downloads\my-plugin"
```

#### Option 2: ZIP Archive

Create a ZIP archive:

```powershell
Compress-Archive -Path "my-plugin\*" -DestinationPath "my-plugin.zip"
```

Users install with:
```powershell
Install-NanerPlugin -PluginPath "C:\downloads\my-plugin.zip"
```

#### Option 3: Git Repository

Push to a Git repository:

```bash
git init
git add .
git commit -m "Initial commit"
git remote add origin https://github.com/user/my-plugin.git
git push -u origin main
```

Users install with:
```powershell
git clone https://github.com/user/my-plugin.git
Install-NanerPlugin -PluginPath "my-plugin"
```

### Plugin Registry (Future)

Future versions may support a plugin registry:

```powershell
# Future feature
Install-NanerPlugin -PluginId "java-jdk" -FromRegistry
```

---

## Best Practices

### Plugin Design

1. **Single Responsibility** - Each plugin should do one thing well
2. **Minimal Dependencies** - Avoid unnecessary plugin dependencies
3. **Clear Documentation** - Provide comprehensive README.md
4. **Semantic Versioning** - Use semver (1.0.0, 1.1.0, 2.0.0)
5. **Proper Licensing** - Include license information

### Environment Management

1. **Use Portable Paths** - Always use `%NANER_ROOT%` and relative paths
2. **Avoid Global Changes** - Don't modify system-wide settings
3. **Clean PATH** - Remove duplicate PATH entries
4. **Namespace Variables** - Prefix env vars to avoid conflicts (e.g., `MYTOOL_`)

### Hook Scripts

1. **Idempotent** - Hooks should be safe to run multiple times
2. **Error Handling** - Use try/catch and provide clear error messages
3. **User Feedback** - Use Write-Host for status messages
4. **Fast Execution** - env-setup.ps1 should be fast (< 1 second)

### Security

1. **Validate Input** - Check paths and parameters
2. **Avoid Hardcoded Secrets** - Don't include credentials
3. **Safe Downloads** - Verify checksums when possible
4. **Minimal Permissions** - Don't require admin rights

### Testing

1. **Test All Hooks** - Ensure all hook scripts work
2. **Test Enablement** - Verify enable/disable cycle
3. **Test Uninstallation** - Ensure clean removal
4. **Cross-Machine Testing** - Test on different Windows versions

---

## Examples

### Example 1: Simple Tool Plugin

**plugin.json:**
```json
{
  "id": "simple-tool",
  "name": "Simple Tool",
  "version": "1.0.0",
  "description": "A simple development tool",
  "author": "Me",
  "environment": {
    "pathPrecedence": [
      "%NANER_ROOT%\\vendor\\simple-tool\\bin"
    ]
  }
}
```

**hooks/env-setup.ps1:**
```powershell
param([hashtable]$Context)

$binPath = Join-Path $Context.NanerRoot "vendor\simple-tool\bin"
if ((Test-Path $binPath) -and ($env:PATH -notlike "*$binPath*")) {
    $env:PATH = "$binPath;$env:PATH"
}
```

### Example 2: SDK Plugin with Configuration

**plugin.json:**
```json
{
  "id": "my-sdk",
  "name": "My SDK",
  "version": "1.0.0",
  "description": "Custom SDK",
  "author": "Me",
  "environment": {
    "variables": {
      "MY_SDK_HOME": "%NANER_ROOT%\\vendor\\my-sdk",
      "MY_SDK_VERSION": "3.0"
    },
    "pathPrecedence": [
      "%NANER_ROOT%\\vendor\\my-sdk\\bin"
    ]
  },
  "configuration": {
    "enableDebug": false,
    "defaultProfile": "production"
  }
}
```

**hooks/enable.ps1:**
```powershell
param([hashtable]$Context)

Write-Host "Enabling My SDK..." -ForegroundColor Cyan

# Create config directory
$configDir = Join-Path $Context.NanerRoot "home\.my-sdk"
if (-not (Test-Path $configDir)) {
    New-Item -ItemType Directory -Path $configDir -Force | Out-Null
}

# Create default config
$configFile = Join-Path $configDir "config.json"
if (-not (Test-Path $configFile)) {
    @{
        enableDebug = $false
        defaultProfile = "production"
    } | ConvertTo-Json | Set-Content $configFile -Force
}

Write-Host "My SDK enabled successfully" -ForegroundColor Green
```

### Example 3: Database Client Plugin

See the [postgres-client plugin](../plugins/postgres-client/) for a complete example.

---

## Troubleshooting

### Plugin Not Found

**Problem:** `Get-NanerPlugin -PluginId "my-plugin"` returns null

**Solutions:**
1. Verify plugin directory exists: `Test-Path "$env:NANER_ROOT\plugins\my-plugin"`
2. Check manifest file exists: `Test-Path "$env:NANER_ROOT\plugins\my-plugin\plugin.json"`
3. Validate manifest: Test-PluginManifest -ManifestPath "path\to\plugin.json"

### Hook Not Executing

**Problem:** Hook script doesn't run when expected

**Solutions:**
1. Verify hook file exists in `hooks/` directory
2. Check hook name matches expected: `install.ps1`, `enable.ps1`, etc.
3. Ensure hook is marked in manifest: `"hooks": { "enable": true }`
4. Check for PowerShell syntax errors in hook script

### Environment Variables Not Set

**Problem:** Environment variables don't appear after enabling plugin

**Solutions:**
1. Restart your terminal after enabling
2. Verify `env-setup.ps1` hook exists
3. Check hook logic for errors
4. Manually source hook: `. .\hooks\env-setup.ps1 -Context @{NanerRoot=$env:NANER_ROOT; PluginId="my-plugin"}`

### PATH Not Updated

**Problem:** Tool commands not found after enabling plugin

**Solutions:**
1. Verify PATH in manifest: `"pathPrecedence": [...]`
2. Check env-setup.ps1 adds to PATH
3. Restart terminal
4. Verify tool exists in specified directory

### Plugin Conflicts

**Problem:** Two plugins conflict with each other

**Solutions:**
1. Check for duplicate environment variable names
2. Check for PATH conflicts
3. Disable conflicting plugin
4. Use plugin dependencies to enforce order

---

## API Reference

### Plugin Management Commands

#### Get-NanerPlugin

```powershell
Get-NanerPlugin [-PluginId <string>] [-EnabledOnly]
```

Gets installed plugins.

#### Install-NanerPlugin

```powershell
Install-NanerPlugin -PluginPath <string> [-PluginId <string>] [-Force]
```

Installs a plugin from directory or archive.

#### Uninstall-NanerPlugin

```powershell
Uninstall-NanerPlugin -PluginId <string> [-Force]
```

Uninstalls a plugin.

#### Enable-NanerPlugin

```powershell
Enable-NanerPlugin -PluginId <string>
```

Enables a plugin.

#### Disable-NanerPlugin

```powershell
Disable-NanerPlugin -PluginId <string>
```

Disables a plugin.

#### Invoke-PluginHook

```powershell
Invoke-PluginHook -PluginId <string> -HookName <string> [-Parameters <hashtable>]
```

Invokes a plugin hook manually.

#### Invoke-PluginEnvironmentSetup

```powershell
Invoke-PluginEnvironmentSetup
```

Runs env-setup hooks for all enabled plugins.

---

## Related Documentation

- [Capability Roadmap](CAPABILITY-ROADMAP.md) - Phase 9.1: Plugin System
- [Architecture](ARCHITECTURE.md) - Naner architecture overview
- [Vendor Lock Files](VENDOR-LOCK-FILE.md) - Vendor dependency management

---

## Contributing

### Submitting Plugins

To share your plugin with the community:

1. Test thoroughly on multiple machines
2. Write comprehensive documentation
3. Include example usage
4. Add appropriate license
5. Submit PR or create repository
6. Share on Naner community forums

### Plugin Guidelines

- Follow this guide's best practices
- Include tests where possible
- Maintain backward compatibility
- Document breaking changes
- Respond to issues and feedback

---

**Last Updated:** 2026-01-07
**Version:** 1.0.0
**Maintainer:** Naner Project
