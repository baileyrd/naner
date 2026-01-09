<#
.SYNOPSIS
    Unit tests for Naner.Plugins.psm1

.DESCRIPTION
    Comprehensive Pester tests for the Naner plugin management system.
    Tests plugin discovery, installation, lifecycle management, and hooks.
#>

BeforeAll {
    # Import required modules
    $projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $commonModule = Join-Path $projectRoot "src\powershell\Common.psm1"
    $pluginsModule = Join-Path $projectRoot "src\powershell\Naner.Plugins.psm1"

    Import-Module $commonModule -Force
    Import-Module $pluginsModule -Force

    # Setup test environment
    $script:testNanerRoot = Join-Path $TestDrive "naner-test"
    $script:testPluginsDir = Join-Path $script:testNanerRoot "plugins"
    $env:NANER_ROOT = $script:testNanerRoot

    # Create directory structure
    New-Item -ItemType Directory -Path $script:testNanerRoot -Force | Out-Null
    New-Item -ItemType Directory -Path $script:testPluginsDir -Force | Out-Null

    # Helper function to create test plugin
    function New-TestPlugin {
        param(
            [string]$PluginId,
            [string]$Version = "1.0.0",
            [bool]$Enabled = $false,
            [hashtable]$ExtraFields = @{}
        )

        $pluginPath = Join-Path $script:testPluginsDir $PluginId
        New-Item -ItemType Directory -Path $pluginPath -Force | Out-Null

        $manifest = @{
            id = $PluginId
            name = "Test Plugin $PluginId"
            version = $Version
            description = "Test plugin for unit tests"
            author = "Test Suite"
            enabled = $Enabled
        }

        # Merge extra fields
        foreach ($key in $ExtraFields.Keys) {
            $manifest[$key] = $ExtraFields[$key]
        }

        $manifestPath = Join-Path $pluginPath "plugin.json"
        $manifest | ConvertTo-Json -Depth 10 | Set-Content $manifestPath -Force

        return $pluginPath
    }

    # Helper function to create test hook
    function New-TestHook {
        param(
            [string]$PluginId,
            [string]$HookName,
            [string]$ScriptContent = 'param([hashtable]$Context); Write-Host "Hook executed: $HookName"'
        )

        $pluginPath = Join-Path $script:testPluginsDir $PluginId
        $hooksDir = Join-Path $pluginPath "hooks"
        New-Item -ItemType Directory -Path $hooksDir -Force | Out-Null

        $hookPath = Join-Path $hooksDir "$HookName.ps1"
        $ScriptContent | Set-Content $hookPath -Force

        return $hookPath
    }
}

Describe "Naner.Plugins Module" {
    Context "Module Import" {
        It "Should import successfully" {
            { Import-Module $pluginsModule -Force } | Should -Not -Throw
        }

        It "Should export expected functions" {
            $functions = @(
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

            $module = Get-Module Naner.Plugins
            foreach ($func in $functions) {
                $module.ExportedFunctions.ContainsKey($func) | Should -Be $true
            }
        }
    }

    Context "Get-PluginDirectory" {
        It "Should return plugins directory path" {
            $result = Get-PluginDirectory
            $result | Should -Be $script:testPluginsDir
        }

        It "Should create directory if it doesn't exist" {
            $newRoot = Join-Path $TestDrive "new-naner"
            $env:NANER_ROOT = $newRoot

            $result = Get-PluginDirectory
            Test-Path $result | Should -Be $true

            $env:NANER_ROOT = $script:testNanerRoot
        }

        It "Should throw if NANER_ROOT not set" {
            $originalRoot = $env:NANER_ROOT
            $env:NANER_ROOT = $null

            { Get-PluginDirectory } | Should -Throw "*NANER_ROOT*"

            $env:NANER_ROOT = $originalRoot
        }
    }

    Context "Get-PluginManifestPath" {
        It "Should return correct manifest path" {
            $result = Get-PluginManifestPath -PluginId "test-plugin"
            $expected = Join-Path $script:testPluginsDir "test-plugin\plugin.json"
            $result | Should -Be $expected
        }
    }

    Context "Test-PluginManifest" {
        It "Should return false for non-existent manifest" {
            $result = Test-PluginManifest -ManifestPath "C:\nonexistent\plugin.json"
            $result | Should -Be $false
        }

        It "Should return true for valid manifest" {
            $pluginPath = New-TestPlugin -PluginId "valid-plugin"
            $manifestPath = Join-Path $pluginPath "plugin.json"

            $result = Test-PluginManifest -ManifestPath $manifestPath
            $result | Should -Be $true
        }

        It "Should return false for manifest missing required fields" {
            $pluginPath = New-TestPlugin -PluginId "invalid-plugin"
            $manifestPath = Join-Path $pluginPath "plugin.json"

            # Create invalid manifest (missing required fields)
            @{ id = "invalid" } | ConvertTo-Json | Set-Content $manifestPath -Force

            $result = Test-PluginManifest -ManifestPath $manifestPath
            $result | Should -Be $false
        }

        It "Should return false for invalid version format" {
            $pluginPath = New-TestPlugin -PluginId "bad-version"
            $manifestPath = Join-Path $pluginPath "plugin.json"

            $manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
            $manifest.version = "v1.0"  # Invalid semver
            $manifest | ConvertTo-Json | Set-Content $manifestPath -Force

            $result = Test-PluginManifest -ManifestPath $manifestPath
            $result | Should -Be $false
        }
    }

    Context "Get-NanerPlugin" {
        BeforeEach {
            New-TestPlugin -PluginId "plugin-1" -Enabled $true
            New-TestPlugin -PluginId "plugin-2" -Enabled $false
            New-TestPlugin -PluginId "plugin-3" -Enabled $true
        }

        It "Should return all plugins when no parameters" {
            $result = Get-NanerPlugin
            $result.Count | Should -Be 3
        }

        It "Should return specific plugin by ID" {
            $result = Get-NanerPlugin -PluginId "plugin-1"
            $result.id | Should -Be "plugin-1"
        }

        It "Should return null for non-existent plugin" {
            $result = Get-NanerPlugin -PluginId "nonexistent"
            $result | Should -BeNullOrEmpty
        }

        It "Should return only enabled plugins with -EnabledOnly" {
            $result = Get-NanerPlugin -EnabledOnly
            $result.Count | Should -Be 2
            $result.id | Should -Contain "plugin-1"
            $result.id | Should -Contain "plugin-3"
        }

        It "Should add runtime properties to plugin object" {
            $result = Get-NanerPlugin -PluginId "plugin-1"
            $result.PSObject.Properties.Name | Should -Contain "path"
            $result.PSObject.Properties.Name | Should -Contain "manifestPath"
        }
    }

    Context "Install-NanerPlugin" {
        It "Should install plugin from directory" {
            $sourceDir = Join-Path $TestDrive "source-plugin"
            New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

            $manifest = @{
                id = "installed-plugin"
                name = "Installed Plugin"
                version = "1.0.0"
                description = "Test installation"
                author = "Test"
            }
            $manifest | ConvertTo-Json | Set-Content (Join-Path $sourceDir "plugin.json") -Force

            $result = Install-NanerPlugin -PluginPath $sourceDir
            $result.id | Should -Be "installed-plugin"

            $installedPath = Join-Path $script:testPluginsDir "installed-plugin"
            Test-Path $installedPath | Should -Be $true
        }

        It "Should throw if plugin path doesn't exist" {
            { Install-NanerPlugin -PluginPath "C:\nonexistent" } | Should -Throw "*not found*"
        }

        It "Should throw if manifest missing" {
            $sourceDir = Join-Path $TestDrive "no-manifest"
            New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

            { Install-NanerPlugin -PluginPath $sourceDir } | Should -Throw "*manifest not found*"
        }

        It "Should throw if plugin already exists without -Force" {
            New-TestPlugin -PluginId "existing-plugin"

            $sourceDir = Join-Path $TestDrive "duplicate"
            New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

            @{
                id = "existing-plugin"
                name = "Duplicate"
                version = "2.0.0"
                description = "Duplicate"
                author = "Test"
            } | ConvertTo-Json | Set-Content (Join-Path $sourceDir "plugin.json") -Force

            { Install-NanerPlugin -PluginPath $sourceDir } | Should -Throw "*already installed*"
        }

        It "Should reinstall with -Force" {
            New-TestPlugin -PluginId "force-plugin" -Version "1.0.0"

            $sourceDir = Join-Path $TestDrive "force-update"
            New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

            @{
                id = "force-plugin"
                name = "Force Update"
                version = "2.0.0"
                description = "Updated"
                author = "Test"
            } | ConvertTo-Json | Set-Content (Join-Path $sourceDir "plugin.json") -Force

            $result = Install-NanerPlugin -PluginPath $sourceDir -Force
            $result.version | Should -Be "2.0.0"
        }

        It "Should use custom PluginId parameter" {
            $sourceDir = Join-Path $TestDrive "custom-id"
            New-Item -ItemType Directory -Path $sourceDir -Force | Out-Null

            @{
                id = "original-id"
                name = "Custom ID Test"
                version = "1.0.0"
                description = "Test custom ID"
                author = "Test"
            } | ConvertTo-Json | Set-Content (Join-Path $sourceDir "plugin.json") -Force

            $result = Install-NanerPlugin -PluginPath $sourceDir -PluginId "custom-id"
            $result.id | Should -Be "custom-id"

            $installedPath = Join-Path $script:testPluginsDir "custom-id"
            Test-Path $installedPath | Should -Be $true
        }
    }

    Context "Uninstall-NanerPlugin" {
        It "Should uninstall plugin" {
            New-TestPlugin -PluginId "uninstall-test"

            Uninstall-NanerPlugin -PluginId "uninstall-test" -Force

            $pluginPath = Join-Path $script:testPluginsDir "uninstall-test"
            Test-Path $pluginPath | Should -Be $false
        }

        It "Should warn if plugin not found" {
            $result = Uninstall-NanerPlugin -PluginId "nonexistent" -Force -WarningVariable warnings -WarningAction SilentlyContinue
            $warnings.Count | Should -BeGreaterThan 0
        }

        It "Should invoke uninstall hook if exists" {
            New-TestPlugin -PluginId "hook-uninstall"
            $hookPath = New-TestHook -PluginId "hook-uninstall" -HookName "uninstall"

            Uninstall-NanerPlugin -PluginId "hook-uninstall" -Force

            $pluginPath = Join-Path $script:testPluginsDir "hook-uninstall"
            Test-Path $pluginPath | Should -Be $false
        }
    }

    Context "Enable-NanerPlugin" {
        It "Should enable disabled plugin" {
            New-TestPlugin -PluginId "enable-test" -Enabled $false

            Enable-NanerPlugin -PluginId "enable-test"

            $plugin = Get-NanerPlugin -PluginId "enable-test"
            $plugin.enabled | Should -Be $true
        }

        It "Should not fail if already enabled" {
            New-TestPlugin -PluginId "already-enabled" -Enabled $true

            { Enable-NanerPlugin -PluginId "already-enabled" } | Should -Not -Throw
        }

        It "Should invoke enable hook if exists" {
            New-TestPlugin -PluginId "hook-enable" -Enabled $false
            New-TestHook -PluginId "hook-enable" -HookName "enable"

            { Enable-NanerPlugin -PluginId "hook-enable" } | Should -Not -Throw

            $plugin = Get-NanerPlugin -PluginId "hook-enable"
            $plugin.enabled | Should -Be $true
        }

        It "Should throw if plugin not found" {
            { Enable-NanerPlugin -PluginId "nonexistent" } | Should -Throw "*not found*"
        }
    }

    Context "Disable-NanerPlugin" {
        It "Should disable enabled plugin" {
            New-TestPlugin -PluginId "disable-test" -Enabled $true

            Disable-NanerPlugin -PluginId "disable-test"

            $plugin = Get-NanerPlugin -PluginId "disable-test"
            $plugin.enabled | Should -Be $false
        }

        It "Should not fail if already disabled" {
            New-TestPlugin -PluginId "already-disabled" -Enabled $false

            { Disable-NanerPlugin -PluginId "already-disabled" } | Should -Not -Throw
        }

        It "Should invoke disable hook if exists" {
            New-TestPlugin -PluginId "hook-disable" -Enabled $true
            New-TestHook -PluginId "hook-disable" -HookName "disable"

            { Disable-NanerPlugin -PluginId "hook-disable" } | Should -Not -Throw

            $plugin = Get-NanerPlugin -PluginId "hook-disable"
            $plugin.enabled | Should -Be $false
        }

        It "Should throw if plugin not found" {
            { Disable-NanerPlugin -PluginId "nonexistent" } | Should -Throw "*not found*"
        }
    }

    Context "Invoke-PluginHook" {
        It "Should invoke hook script with context" {
            New-TestPlugin -PluginId "hook-test"

            $hookContent = @'
param([hashtable]$Context)
$Context.PluginId | Out-File -FilePath "$env:TEMP\hook-test.txt" -Force
'@
            New-TestHook -PluginId "hook-test" -HookName "test-hook" -ScriptContent $hookContent

            $testFile = "$env:TEMP\hook-test.txt"
            if (Test-Path $testFile) { Remove-Item $testFile -Force }

            Invoke-PluginHook -PluginId "hook-test" -HookName "test-hook"

            Test-Path $testFile | Should -Be $true
            $content = Get-Content $testFile -Raw
            $content.Trim() | Should -Be "hook-test"

            Remove-Item $testFile -Force
        }

        It "Should not throw if hook doesn't exist" {
            New-TestPlugin -PluginId "no-hook"

            { Invoke-PluginHook -PluginId "no-hook" -HookName "nonexistent" } | Should -Not -Throw
        }

        It "Should pass parameters to hook" {
            New-TestPlugin -PluginId "param-test"

            $hookContent = @'
param([hashtable]$Context)
"$($Context.CustomParam)" | Out-File -FilePath "$env:TEMP\param-test.txt" -Force
'@
            New-TestHook -PluginId "param-test" -HookName "param-hook" -ScriptContent $hookContent

            $testFile = "$env:TEMP\param-test.txt"
            if (Test-Path $testFile) { Remove-Item $testFile -Force }

            Invoke-PluginHook -PluginId "param-test" -HookName "param-hook" -Parameters @{ CustomParam = "TestValue" }

            Test-Path $testFile | Should -Be $true
            $content = Get-Content $testFile -Raw
            $content.Trim() | Should -Be "TestValue"

            Remove-Item $testFile -Force
        }

        It "Should throw if plugin not found" {
            { Invoke-PluginHook -PluginId "nonexistent" -HookName "test" } | Should -Throw "*not found*"
        }
    }

    Context "Invoke-PluginEnvironmentSetup" {
        It "Should invoke env-setup hooks for enabled plugins" {
            New-TestPlugin -PluginId "env-plugin-1" -Enabled $true
            New-TestPlugin -PluginId "env-plugin-2" -Enabled $true
            New-TestPlugin -PluginId "env-plugin-3" -Enabled $false

            $hookContent = @'
param([hashtable]$Context)
"Env setup for $($Context.PluginId)" | Out-File -FilePath "$env:TEMP\env-$($Context.PluginId).txt" -Force
'@
            New-TestHook -PluginId "env-plugin-1" -HookName "env-setup" -ScriptContent $hookContent
            New-TestHook -PluginId "env-plugin-2" -HookName "env-setup" -ScriptContent $hookContent
            New-TestHook -PluginId "env-plugin-3" -HookName "env-setup" -ScriptContent $hookContent

            Invoke-PluginEnvironmentSetup

            Test-Path "$env:TEMP\env-env-plugin-1.txt" | Should -Be $true
            Test-Path "$env:TEMP\env-env-plugin-2.txt" | Should -Be $true
            Test-Path "$env:TEMP\env-env-plugin-3.txt" | Should -Be $false

            Remove-Item "$env:TEMP\env-env-plugin-1.txt" -Force -ErrorAction SilentlyContinue
            Remove-Item "$env:TEMP\env-env-plugin-2.txt" -Force -ErrorAction SilentlyContinue
        }

        It "Should handle no enabled plugins gracefully" {
            # Clear all plugins
            Get-ChildItem $script:testPluginsDir | Remove-Item -Recurse -Force

            { Invoke-PluginEnvironmentSetup } | Should -Not -Throw
        }

        It "Should continue on hook errors" {
            New-TestPlugin -PluginId "error-plugin" -Enabled $true
            New-TestPlugin -PluginId "good-plugin" -Enabled $true

            $errorHook = 'param([hashtable]$Context); throw "Hook error"'
            $goodHook = @'
param([hashtable]$Context)
"Success" | Out-File -FilePath "$env:TEMP\good-plugin.txt" -Force
'@

            New-TestHook -PluginId "error-plugin" -HookName "env-setup" -ScriptContent $errorHook
            New-TestHook -PluginId "good-plugin" -HookName "env-setup" -ScriptContent $goodHook

            Invoke-PluginEnvironmentSetup

            Test-Path "$env:TEMP\good-plugin.txt" | Should -Be $true
            Remove-Item "$env:TEMP\good-plugin.txt" -Force
        }
    }
}

AfterAll {
    # Cleanup
    if (Test-Path $script:testNanerRoot) {
        Remove-Item -Path $script:testNanerRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
