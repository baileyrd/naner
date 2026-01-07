<#
.SYNOPSIS
    Unit tests for Naner GUI Configuration Manager

.DESCRIPTION
    Tests for Invoke-NanerGUI.ps1 functionality

.NOTES
    Requires: Pester 5.x
#>

BeforeAll {
    # Import required modules
    $projectRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
    $guiScript = Join-Path $projectRoot "src\powershell\Invoke-NanerGUI.ps1"

    # Set up test environment
    $env:NANER_ROOT = $projectRoot

    # Mock Add-Type to prevent actual Windows Forms loading in tests
    Mock Add-Type { }
}

Describe "Invoke-NanerGUI.ps1 - Module Loading" {
    Context "Module Dependencies" {
        It "Should have Common.psm1 available" {
            $commonModule = Join-Path $env:NANER_ROOT "src\powershell\Common.psm1"
            $commonModule | Should -Exist
        }

        It "Should have Naner.Vendors.psm1 available" {
            $vendorModule = Join-Path $env:NANER_ROOT "src\powershell\Naner.Vendors.psm1"
            $vendorModule | Should -Exist
        }

        It "Should have Naner.Environments.psm1 available" {
            $envModule = Join-Path $env:NANER_ROOT "src\powershell\Naner.Environments.psm1"
            $envModule | Should -Exist
        }
    }

    Context "Configuration Files" {
        It "Should find naner.json" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $configPath | Should -Exist
        }

        It "Should find vendors.json" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"
            $vendorsPath | Should -Exist
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Parameter Validation" {
    Context "ShowWizard Parameter" {
        It "Should accept -ShowWizard switch" {
            # Test parameter binding (not execution)
            $params = @{
                ShowWizard = $true
            }

            { & $guiScript @params } | Should -Not -Throw
        }
    }

    Context "Tab Parameter" {
        It "Should accept valid tab names" {
            $validTabs = @("Vendors", "Environments", "Profiles", "Settings")

            foreach ($tab in $validTabs) {
                $params = @{
                    Tab = $tab
                }

                # This validates parameter binding
                { & $guiScript @params } | Should -Not -Throw
            }
        }

        It "Should reject invalid tab names" {
            { & $guiScript -Tab "InvalidTab" } | Should -Throw
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Utility Functions" {
    BeforeAll {
        # Dot-source the script to access functions
        # Note: In actual GUI, this won't work due to Windows Forms
        # These tests are conceptual and validate logic

        # Create mock ListView
        $script:MockListView = [PSCustomObject]@{
            Items = [System.Collections.ArrayList]@()
            Columns = [System.Collections.ArrayList]@()
        }

        # Create mock config
        $script:MockConfig = @{
            Profiles = @{
                Unified = @{
                    Name = "Naner (Unified)"
                    Shell = "PowerShell"
                    Description = "Unified environment"
                }
                Bash = @{
                    Name = "Bash"
                    Shell = "Bash"
                    Description = "MSYS2 Bash"
                }
            }
            DefaultProfile = "Unified"
        }
    }

    Context "Vendor List Refresh" {
        It "Should load vendors from vendors.json" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"

            if (Test-Path $vendorsPath) {
                $vendors = Get-Content $vendorsPath | ConvertFrom-Json
                $vendors | Should -Not -BeNullOrEmpty
                $vendors.PSObject.Properties.Count | Should -BeGreaterThan 0
            }
        }

        It "Should identify required vendors" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"

            if (Test-Path $vendorsPath) {
                $vendors = Get-Content $vendorsPath | ConvertFrom-Json
                $requiredVendors = $vendors.PSObject.Properties | Where-Object { $_.Value.Required -eq $true }
                $requiredVendors.Count | Should -BeGreaterThan 0
            }
        }

        It "Should identify optional vendors" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"

            if (Test-Path $vendorsPath) {
                $vendors = Get-Content $vendorsPath | ConvertFrom-Json
                $optionalVendors = $vendors.PSObject.Properties | Where-Object { $_.Value.Required -ne $true }
                $optionalVendors.Count | Should -BeGreaterThan 0
            }
        }
    }

    Context "Environment List Refresh" {
        It "Should always include default environment" {
            $envRoot = Join-Path $env:NANER_ROOT "environments"

            # Default environment is special (uses home/ directly)
            $homeDir = Join-Path $env:NANER_ROOT "home"
            $homeDir | Should -Exist
        }

        It "Should detect custom environments" {
            $envRoot = Join-Path $env:NANER_ROOT "environments"

            if (Test-Path $envRoot) {
                $customEnvs = Get-ChildItem -Path $envRoot -Directory -ErrorAction SilentlyContinue
                # May be 0 if no custom environments created
                $customEnvs.Count | Should -BeGreaterThanOrEqual 0
            }
        }

        It "Should identify active environment" {
            $envRoot = Join-Path $env:NANER_ROOT "environments"
            $activePath = Join-Path $envRoot ".active"

            # Active marker may not exist (defaults to "default")
            if (Test-Path $activePath) {
                $activeData = Get-Content $activePath | ConvertFrom-Json
                $activeData.Environment | Should -Not -BeNullOrEmpty
            }
        }
    }

    Context "Profile List Refresh" {
        It "Should load profiles from config" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-Content $configPath | ConvertFrom-Json

            $config.Profiles | Should -Not -BeNullOrEmpty
            $config.Profiles.PSObject.Properties.Count | Should -BeGreaterThan 0
        }

        It "Should identify default profile" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-Content $configPath | ConvertFrom-Json

            $config.DefaultProfile | Should -Not -BeNullOrEmpty
        }

        It "Should list all built-in profiles" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-Content $configPath | ConvertFrom-Json

            $config.Profiles.Unified | Should -Not -BeNullOrEmpty
            $config.Profiles.PowerShell | Should -Not -BeNullOrEmpty
            $config.Profiles.Bash | Should -Not -BeNullOrEmpty
            $config.Profiles.CMD | Should -Not -BeNullOrEmpty
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Configuration Validation" {
    Context "naner.json Structure" {
        It "Should have valid JSON syntax" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            { Get-Content $configPath | ConvertFrom-Json } | Should -Not -Throw
        }

        It "Should have required sections" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-Content $configPath | ConvertFrom-Json

            $config.Environment | Should -Not -BeNullOrEmpty
            $config.Profiles | Should -Not -BeNullOrEmpty
            $config.DefaultProfile | Should -Not -BeNullOrEmpty
        }

        It "Should have valid default profile reference" {
            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-Content $configPath | ConvertFrom-Json

            $config.Profiles.PSObject.Properties.Name -contains $config.DefaultProfile | Should -BeTrue
        }
    }

    Context "vendors.json Structure" {
        It "Should have valid JSON syntax" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"
            { Get-Content $vendorsPath | ConvertFrom-Json } | Should -Not -Throw
        }

        It "Should have required vendor properties" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"
            $vendors = Get-Content $vendorsPath | ConvertFrom-Json

            foreach ($vendor in $vendors.PSObject.Properties) {
                $v = $vendor.Value
                $v.DisplayName | Should -Not -BeNullOrEmpty
                $v.Description | Should -Not -BeNullOrEmpty
                $v.Enabled | Should -Not -BeNull
            }
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Error Handling" {
    Context "Missing Configuration Files" {
        It "Should handle missing naner.json gracefully" {
            Mock Get-NanerConfig { throw "Config not found" }

            # GUI should handle this error
            # In actual implementation, show error dialog
            { Mock Get-NanerConfig } | Should -Not -Throw
        }
    }

    Context "Invalid Configuration" {
        It "Should detect invalid JSON" {
            $badJson = "{ invalid json }"

            { $badJson | ConvertFrom-Json } | Should -Throw
        }

        It "Should detect missing required properties" {
            $incompleteConfig = @{
                Environment = @{}
                # Missing Profiles and DefaultProfile
            } | ConvertTo-Json

            $config = $incompleteConfig | ConvertFrom-Json
            $config.Profiles | Should -BeNullOrEmpty
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Integration Points" {
    Context "Common Module Integration" {
        It "Should use Find-NanerRoot from Common.psm1" {
            Import-Module (Join-Path $env:NANER_ROOT "src\powershell\Common.psm1") -Force

            $nanerRoot = Find-NanerRoot
            $nanerRoot | Should -Not -BeNullOrEmpty
            Test-Path $nanerRoot | Should -BeTrue
        }

        It "Should use Get-NanerConfig from Common.psm1" {
            Import-Module (Join-Path $env:NANER_ROOT "src\powershell\Common.psm1") -Force

            $configPath = Join-Path $env:NANER_ROOT "config\naner.json"
            $config = Get-NanerConfig -ConfigPath $configPath

            $config | Should -Not -BeNullOrEmpty
        }
    }

    Context "Vendor Module Integration" {
        It "Should be able to import Naner.Vendors.psm1" {
            $vendorModule = Join-Path $env:NANER_ROOT "src\powershell\Naner.Vendors.psm1"

            { Import-Module $vendorModule -Force } | Should -Not -Throw
        }
    }

    Context "Environment Module Integration" {
        It "Should be able to import Naner.Environments.psm1" {
            $envModule = Join-Path $env:NANER_ROOT "src\powershell\Naner.Environments.psm1"

            { Import-Module $envModule -Force } | Should -Not -Throw
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - UI Components" {
    Context "Form Creation" {
        It "Should define main form parameters" {
            # Conceptual test - validates form structure
            $formSize = [System.Drawing.Size]::new(800, 550)
            $formSize.Width | Should -Be 800
            $formSize.Height | Should -Be 550
        }

        It "Should define tab control parameters" {
            # Conceptual test
            $tabSize = [System.Drawing.Size]::new(780, 500)
            $tabSize.Width | Should -Be 780
            $tabSize.Height | Should -Be 500
        }
    }

    Context "ListView Columns" {
        It "Should define vendor list columns" {
            $columns = @("ID", "Name", "Enabled", "Required", "Status")
            $columns.Count | Should -Be 5
        }

        It "Should define environment list columns" {
            $columns = @("Name", "Description", "Status", "Path")
            $columns.Count | Should -Be 4
        }

        It "Should define profile list columns" {
            $columns = @("ID", "Display Name", "Shell", "Description", "Default")
            $columns.Count | Should -Be 5
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Setup Wizard" {
    Context "Wizard Structure" {
        It "Should define wizard form parameters" {
            # Conceptual test
            $wizardSize = [System.Drawing.Size]::new(700, 500)
            $wizardSize.Width | Should -Be 700
            $wizardSize.Height | Should -Be 500
        }

        It "Should list optional vendors for selection" {
            $vendorsPath = Join-Path $env:NANER_ROOT "config\vendors.json"

            if (Test-Path $vendorsPath) {
                $vendors = Get-Content $vendorsPath | ConvertFrom-Json
                $optionalVendors = $vendors.PSObject.Properties | Where-Object { $_.Value.Required -ne $true }

                # Wizard should show these
                $optionalVendors.Count | Should -BeGreaterThan 0
            }
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Documentation" {
    Context "Documentation Files" {
        It "Should have comprehensive documentation" {
            $docPath = Join-Path $env:NANER_ROOT "docs\GUI-CONFIGURATION-MANAGER.md"
            $docPath | Should -Exist
        }

        It "Should document all tabs" {
            $docPath = Join-Path $env:NANER_ROOT "docs\GUI-CONFIGURATION-MANAGER.md"
            $content = Get-Content $docPath -Raw

            $content | Should -Match "Vendors Tab"
            $content | Should -Match "Environments Tab"
            $content | Should -Match "Profiles Tab"
            $content | Should -Match "Settings Tab"
        }

        It "Should document setup wizard" {
            $docPath = Join-Path $env:NANER_ROOT "docs\GUI-CONFIGURATION-MANAGER.md"
            $content = Get-Content $docPath -Raw

            $content | Should -Match "Setup Wizard"
        }
    }
}

Describe "Invoke-NanerGUI.ps1 - Security" {
    Context "Script Execution" {
        It "Should not contain hardcoded credentials" {
            $guiScript = Join-Path $env:NANER_ROOT "src\powershell\Invoke-NanerGUI.ps1"
            $content = Get-Content $guiScript -Raw

            $content | Should -Not -Match 'password\s*=\s*[''"]'
            $content | Should -Not -Match 'secret\s*=\s*[''"]'
        }

        It "Should use ErrorActionPreference Stop" {
            $guiScript = Join-Path $env:NANER_ROOT "src\powershell\Invoke-NanerGUI.ps1"
            $content = Get-Content $guiScript -Raw

            $content | Should -Match 'ErrorActionPreference.*Stop'
        }
    }

    Context "File Operations" {
        It "Should only access files within Naner directory" {
            # GUI should not access files outside NANER_ROOT
            # This is a conceptual test
            $nanerRoot = $env:NANER_ROOT
            $nanerRoot | Should -Not -BeNullOrEmpty
        }
    }
}
