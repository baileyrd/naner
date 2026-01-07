<#
.SYNOPSIS
    Naner Plugin Management Module

.DESCRIPTION
    This module provides plugin/extension functionality for Naner.
    It handles plugin discovery, loading, lifecycle management, and hook execution.

.NOTES
    Part of the Naner portable development environment.
    Requires: Common.psm1

    Module Dependencies:
    - Common.psm1 - Logging and utility functions

.EXAMPLE
    Import-Module Naner.Plugins
    Get-NanerPlugin
    Install-NanerPlugin -PluginId "java-jdk"
    Enable-NanerPlugin -PluginId "java-jdk"
#>

# Import Common module for logging
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

#region Plugin Discovery and Configuration

function Get-PluginDirectory {
    <#
    .SYNOPSIS
        Gets the plugin directory path.

    .DESCRIPTION
        Returns the path to the plugins directory, creating it if necessary.

    .OUTPUTS
        String - Path to plugins directory

    .EXAMPLE
        $pluginDir = Get-PluginDirectory
    #>
    [CmdletBinding()]
    param()

    $nanerRoot = $env:NANER_ROOT
    if (-not $nanerRoot) {
        throw "NANER_ROOT environment variable not set"
    }

    $pluginDir = Join-Path $nanerRoot "plugins"
    if (-not (Test-Path $pluginDir)) {
        New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
    }

    return $pluginDir
}

function Get-PluginManifestPath {
    <#
    .SYNOPSIS
        Gets the path to a plugin's manifest file.

    .PARAMETER PluginId
        The plugin identifier.

    .OUTPUTS
        String - Path to plugin manifest

    .EXAMPLE
        $manifestPath = Get-PluginManifestPath -PluginId "java-jdk"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PluginId
    )

    $pluginDir = Get-PluginDirectory
    $pluginPath = Join-Path $pluginDir $PluginId
    return Join-Path $pluginPath "plugin.json"
}

function Test-PluginManifest {
    <#
    .SYNOPSIS
        Validates a plugin manifest.

    .PARAMETER ManifestPath
        Path to the plugin manifest file.

    .OUTPUTS
        Boolean - True if valid, False otherwise

    .EXAMPLE
        $isValid = Test-PluginManifest -ManifestPath "C:\naner\plugins\java-jdk\plugin.json"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ManifestPath
    )

    if (-not (Test-Path $ManifestPath)) {
        Write-NanerWarning "Plugin manifest not found: $ManifestPath"
        return $false
    }

    try {
        $manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json

        # Required fields
        $requiredFields = @('id', 'name', 'version', 'description', 'author')
        foreach ($field in $requiredFields) {
            if (-not $manifest.PSObject.Properties.Name.Contains($field)) {
                Write-NanerWarning "Plugin manifest missing required field: $field"
                return $false
            }
        }

        # Validate version format (semver)
        if ($manifest.version -notmatch '^\d+\.\d+\.\d+$') {
            Write-NanerWarning "Invalid version format: $($manifest.version). Expected semver (e.g., 1.0.0)"
            return $false
        }

        return $true
    }
    catch {
        Write-NanerWarning "Failed to parse plugin manifest: $_"
        return $false
    }
}

function Get-NanerPlugin {
    <#
    .SYNOPSIS
        Gets installed Naner plugins.

    .DESCRIPTION
        Retrieves information about installed plugins. Can filter by ID or list all.

    .PARAMETER PluginId
        Optional plugin ID to retrieve specific plugin.

    .PARAMETER EnabledOnly
        Only return enabled plugins.

    .OUTPUTS
        Array of plugin objects or single plugin object

    .EXAMPLE
        Get-NanerPlugin

    .EXAMPLE
        Get-NanerPlugin -PluginId "java-jdk"

    .EXAMPLE
        Get-NanerPlugin -EnabledOnly
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$PluginId,

        [Parameter()]
        [switch]$EnabledOnly
    )

    $pluginDir = Get-PluginDirectory
    $plugins = @()

    if ($PluginId) {
        $manifestPath = Get-PluginManifestPath -PluginId $PluginId
        if (-not (Test-Path $manifestPath)) {
            Write-NanerWarning "Plugin not found: $PluginId"
            return $null
        }

        if (-not (Test-PluginManifest -ManifestPath $manifestPath)) {
            Write-NanerWarning "Invalid plugin manifest: $PluginId"
            return $null
        }

        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $pluginPath = Split-Path $manifestPath -Parent

        # Add runtime properties
        $manifest | Add-Member -NotePropertyName "path" -NotePropertyValue $pluginPath -Force
        $manifest | Add-Member -NotePropertyName "manifestPath" -NotePropertyValue $manifestPath -Force

        if ($EnabledOnly -and -not $manifest.enabled) {
            return $null
        }

        return $manifest
    }

    # Get all plugins
    $pluginDirs = Get-ChildItem -Path $pluginDir -Directory -ErrorAction SilentlyContinue
    foreach ($dir in $pluginDirs) {
        $manifestPath = Join-Path $dir.FullName "plugin.json"
        if (Test-Path $manifestPath) {
            if (Test-PluginManifest -ManifestPath $manifestPath) {
                $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json

                # Add runtime properties
                $manifest | Add-Member -NotePropertyName "path" -NotePropertyValue $dir.FullName -Force
                $manifest | Add-Member -NotePropertyName "manifestPath" -NotePropertyValue $manifestPath -Force

                if (-not $EnabledOnly -or $manifest.enabled) {
                    $plugins += $manifest
                }
            }
        }
    }

    return $plugins
}

#endregion

#region Plugin Installation

function Install-NanerPlugin {
    <#
    .SYNOPSIS
        Installs a Naner plugin from a directory or archive.

    .DESCRIPTION
        Installs a plugin by copying it to the plugins directory and validating
        the manifest. Can install from a local directory, zip archive, or Git repository.

    .PARAMETER PluginPath
        Path to plugin directory or archive file.

    .PARAMETER PluginId
        Optional custom plugin ID (overrides manifest ID).

    .PARAMETER Force
        Force reinstall if plugin already exists.

    .OUTPUTS
        Plugin object

    .EXAMPLE
        Install-NanerPlugin -PluginPath "C:\dev\my-plugin"

    .EXAMPLE
        Install-NanerPlugin -PluginPath "C:\downloads\plugin.zip"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PluginPath,

        [Parameter()]
        [string]$PluginId,

        [Parameter()]
        [switch]$Force
    )

    if (-not (Test-Path $PluginPath)) {
        throw "Plugin path not found: $PluginPath"
    }

    $pluginDir = Get-PluginDirectory
    $tempDir = $null

    try {
        # Handle different source types
        if ((Get-Item $PluginPath).PSIsContainer) {
            # Directory
            $sourceDir = $PluginPath
        }
        elseif ($PluginPath -match '\.(zip|tar\.gz|tgz)$') {
            # Archive file
            Write-NanerInfo "Extracting plugin archive..."
            $tempDir = Join-Path $env:TEMP "naner-plugin-$([guid]::NewGuid().ToString())"
            New-Item -ItemType Directory -Path $tempDir -Force | Out-Null

            # Extract using 7-Zip if available, otherwise use built-in
            if (Test-Path "$env:NANER_ROOT\vendor\7zip\7z.exe") {
                & "$env:NANER_ROOT\vendor\7zip\7z.exe" x $PluginPath -o"$tempDir" -y | Out-Null
            }
            else {
                Expand-Archive -Path $PluginPath -DestinationPath $tempDir -Force
            }

            $sourceDir = $tempDir
        }
        else {
            throw "Unsupported plugin source: $PluginPath"
        }

        # Find manifest
        $manifestPath = Join-Path $sourceDir "plugin.json"
        if (-not (Test-Path $manifestPath)) {
            throw "Plugin manifest not found in source: plugin.json"
        }

        # Validate manifest
        if (-not (Test-PluginManifest -ManifestPath $manifestPath)) {
            throw "Invalid plugin manifest"
        }

        $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
        $targetPluginId = if ($PluginId) { $PluginId } else { $manifest.id }
        $targetDir = Join-Path $pluginDir $targetPluginId

        # Check if plugin exists
        if ((Test-Path $targetDir) -and -not $Force) {
            throw "Plugin already installed: $targetPluginId. Use -Force to reinstall."
        }

        # Remove existing if Force
        if ((Test-Path $targetDir) -and $Force) {
            Write-NanerInfo "Removing existing plugin: $targetPluginId"
            Remove-Item -Path $targetDir -Recurse -Force
        }

        # Copy plugin
        Write-NanerInfo "Installing plugin: $targetPluginId"
        Copy-Item -Path $sourceDir -Destination $targetDir -Recurse -Force

        # Update manifest ID if custom ID provided
        if ($PluginId -and $PluginId -ne $manifest.id) {
            $targetManifestPath = Join-Path $targetDir "plugin.json"
            $manifest = Get-Content $targetManifestPath -Raw | ConvertFrom-Json
            $manifest.id = $PluginId
            $manifest | ConvertTo-Json -Depth 10 | Set-Content $targetManifestPath -Force
        }

        Write-NanerSuccess "Plugin installed successfully: $targetPluginId"
        return Get-NanerPlugin -PluginId $targetPluginId
    }
    catch {
        Write-NanerError "Failed to install plugin: $_"
        throw
    }
    finally {
        # Clean up temp directory
        if ($tempDir -and (Test-Path $tempDir)) {
            Remove-Item -Path $tempDir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

function Uninstall-NanerPlugin {
    <#
    .SYNOPSIS
        Uninstalls a Naner plugin.

    .PARAMETER PluginId
        Plugin ID to uninstall.

    .PARAMETER Force
        Skip confirmation prompt.

    .EXAMPLE
        Uninstall-NanerPlugin -PluginId "java-jdk"
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$PluginId,

        [Parameter()]
        [switch]$Force
    )

    $plugin = Get-NanerPlugin -PluginId $PluginId
    if (-not $plugin) {
        Write-NanerWarning "Plugin not found: $PluginId"
        return
    }

    if (-not $Force -and -not $PSCmdlet.ShouldProcess($PluginId, "Uninstall plugin")) {
        return
    }

    try {
        Write-NanerInfo "Uninstalling plugin: $PluginId"

        # Run uninstall hook if it exists
        Invoke-PluginHook -PluginId $PluginId -HookName "uninstall" -ErrorAction SilentlyContinue

        # Remove plugin directory
        Remove-Item -Path $plugin.path -Recurse -Force

        Write-NanerSuccess "Plugin uninstalled successfully: $PluginId"
    }
    catch {
        Write-NanerError "Failed to uninstall plugin: $_"
        throw
    }
}

#endregion

#region Plugin Lifecycle Management

function Enable-NanerPlugin {
    <#
    .SYNOPSIS
        Enables a Naner plugin.

    .PARAMETER PluginId
        Plugin ID to enable.

    .EXAMPLE
        Enable-NanerPlugin -PluginId "java-jdk"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PluginId
    )

    $plugin = Get-NanerPlugin -PluginId $PluginId
    if (-not $plugin) {
        throw "Plugin not found: $PluginId"
    }

    if ($plugin.enabled) {
        Write-NanerInfo "Plugin already enabled: $PluginId"
        return
    }

    try {
        Write-NanerInfo "Enabling plugin: $PluginId"

        # Update manifest
        $manifest = Get-Content $plugin.manifestPath -Raw | ConvertFrom-Json
        $manifest | Add-Member -NotePropertyName "enabled" -NotePropertyValue $true -Force
        $manifest | ConvertTo-Json -Depth 10 | Set-Content $plugin.manifestPath -Force

        # Run enable hook
        Invoke-PluginHook -PluginId $PluginId -HookName "enable"

        Write-NanerSuccess "Plugin enabled: $PluginId"
        Write-NanerInfo "Restart your terminal for changes to take effect"
    }
    catch {
        Write-NanerError "Failed to enable plugin: $_"
        throw
    }
}

function Disable-NanerPlugin {
    <#
    .SYNOPSIS
        Disables a Naner plugin.

    .PARAMETER PluginId
        Plugin ID to disable.

    .EXAMPLE
        Disable-NanerPlugin -PluginId "java-jdk"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PluginId
    )

    $plugin = Get-NanerPlugin -PluginId $PluginId
    if (-not $plugin) {
        throw "Plugin not found: $PluginId"
    }

    if (-not $plugin.enabled) {
        Write-NanerInfo "Plugin already disabled: $PluginId"
        return
    }

    try {
        Write-NanerInfo "Disabling plugin: $PluginId"

        # Run disable hook
        Invoke-PluginHook -PluginId $PluginId -HookName "disable"

        # Update manifest
        $manifest = Get-Content $plugin.manifestPath -Raw | ConvertFrom-Json
        $manifest | Add-Member -NotePropertyName "enabled" -NotePropertyValue $false -Force
        $manifest | ConvertTo-Json -Depth 10 | Set-Content $plugin.manifestPath -Force

        Write-NanerSuccess "Plugin disabled: $PluginId"
        Write-NanerInfo "Restart your terminal for changes to take effect"
    }
    catch {
        Write-NanerError "Failed to disable plugin: $_"
        throw
    }
}

#endregion

#region Plugin Hooks

function Invoke-PluginHook {
    <#
    .SYNOPSIS
        Invokes a plugin hook.

    .DESCRIPTION
        Executes a plugin hook script if it exists. Hooks are PowerShell scripts
        in the hooks/ subdirectory of the plugin.

    .PARAMETER PluginId
        Plugin ID.

    .PARAMETER HookName
        Name of the hook to invoke (e.g., "install", "enable", "disable", "uninstall", "env-setup").

    .PARAMETER Parameters
        Optional hashtable of parameters to pass to the hook script.

    .EXAMPLE
        Invoke-PluginHook -PluginId "java-jdk" -HookName "env-setup"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$PluginId,

        [Parameter(Mandatory)]
        [string]$HookName,

        [Parameter()]
        [hashtable]$Parameters = @{}
    )

    $plugin = Get-NanerPlugin -PluginId $PluginId
    if (-not $plugin) {
        throw "Plugin not found: $PluginId"
    }

    $hooksDir = Join-Path $plugin.path "hooks"
    $hookScript = Join-Path $hooksDir "$HookName.ps1"

    if (-not (Test-Path $hookScript)) {
        Write-NanerDebug "Hook not found: $HookName for plugin $PluginId"
        return
    }

    try {
        Write-NanerDebug "Invoking hook: $HookName for plugin $PluginId"

        # Prepare context for hook
        $context = @{
            PluginId = $PluginId
            PluginPath = $plugin.path
            NanerRoot = $env:NANER_ROOT
            HookName = $HookName
        }

        # Merge with provided parameters
        foreach ($key in $Parameters.Keys) {
            $context[$key] = $Parameters[$key]
        }

        # Execute hook with context
        & $hookScript -Context $context
    }
    catch {
        Write-NanerError "Failed to invoke hook '$HookName' for plugin '$PluginId': $_"
        throw
    }
}

function Invoke-PluginEnvironmentSetup {
    <#
    .SYNOPSIS
        Invokes environment setup hooks for all enabled plugins.

    .DESCRIPTION
        Called during environment initialization to allow plugins to modify
        PATH, set environment variables, etc.

    .EXAMPLE
        Invoke-PluginEnvironmentSetup
    #>
    [CmdletBinding()]
    param()

    $plugins = Get-NanerPlugin -EnabledOnly
    if (-not $plugins -or $plugins.Count -eq 0) {
        Write-NanerDebug "No enabled plugins found"
        return
    }

    Write-NanerDebug "Setting up environment for $($plugins.Count) enabled plugin(s)"

    foreach ($plugin in $plugins) {
        try {
            Invoke-PluginHook -PluginId $plugin.id -HookName "env-setup" -ErrorAction SilentlyContinue
        }
        catch {
            Write-NanerWarning "Failed to setup environment for plugin '$($plugin.id)': $_"
        }
    }
}

#endregion

#region Export Functions

Export-ModuleMember -Function @(
    'Get-PluginDirectory',
    'Get-PluginManifestPath',
    'Test-PluginManifest',
    'Get-NanerPlugin',
    'Install-NanerPlugin',
    'Uninstall-NanerPlugin',
    'Enable-NanerPlugin',
    'Disable-NanerPlugin',
    'Invoke-PluginHook',
    'Invoke-PluginEnvironmentSetup'
)

#endregion
