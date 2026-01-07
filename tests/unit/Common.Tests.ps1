<#
.SYNOPSIS
    Unit tests for Common.psm1 module.

.DESCRIPTION
    Pester unit tests for the Common module functions including:
    - Logging functions (Write-Status, Write-Success, Write-Failure, Write-Info, Write-DebugInfo)
    - Path functions (Find-NanerRoot, Get-NanerRootSimple, Expand-NanerPath)
    - Configuration functions (Get-NanerConfig)
    - GitHub API functions (Get-LatestGitHubRelease)

.NOTES
    Run with: Invoke-Pester -Path tests/unit/Common.Tests.ps1
#>

BeforeAll {
    # Import the module
    $modulePath = Join-Path $PSScriptRoot "..\..\src\powershell\Common.psm1"
    Import-Module $modulePath -Force
}

Describe "Common Module - Logging Functions" {

    Context "Write-Status" {
        It "Should write cyan status message" {
            { Write-Status -Message "Test status" } | Should -Not -Throw
        }
    }

    Context "Write-Success" {
        It "Should write green success message" {
            { Write-Success -Message "Test success" } | Should -Not -Throw
        }
    }

    Context "Write-Failure" {
        It "Should write red failure message" {
            { Write-Failure -Message "Test failure" } | Should -Not -Throw
        }
    }

    Context "Write-Info" {
        It "Should write gray info message" {
            { Write-Info -Message "Test info" } | Should -Not -Throw
        }
    }

    Context "Write-DebugInfo" {
        It "Should not output when DebugMode is false" {
            { Write-DebugInfo -Message "Test debug" -DebugMode $false } | Should -Not -Throw
        }

        It "Should output when DebugMode is true" {
            { Write-DebugInfo -Message "Test debug" -DebugMode $true } | Should -Not -Throw
        }

        It "Should accept mandatory Message parameter" {
            { Write-DebugInfo -Message "Required message" } | Should -Not -Throw
        }
    }
}

Describe "Common Module - Path Functions" {

    Context "Get-NanerRootSimple" {
        It "Should return parent of parent directory" {
            $testPath = "C:\naner\src\powershell"
            $result = Get-NanerRootSimple -ScriptRoot $testPath
            $result | Should -Be "C:\naner"
        }

        It "Should handle paths with trailing slashes" {
            $testPath = "C:\naner\src\powershell\"
            $result = Get-NanerRootSimple -ScriptRoot $testPath
            $result | Should -Be "C:\naner"
        }

        It "Should require ScriptRoot parameter" {
            { Get-NanerRootSimple } | Should -Throw
        }
    }

    Context "Expand-NanerPath" {
        It "Should replace %NANER_ROOT% with actual path" {
            $path = "%NANER_ROOT%\vendor\pwsh"
            $nanerRoot = "C:\naner"
            $result = Expand-NanerPath -Path $path -NanerRoot $nanerRoot
            $result | Should -Be "C:\naner\vendor\pwsh"
        }

        It "Should handle multiple %NANER_ROOT% occurrences" {
            $path = "%NANER_ROOT%\bin\%NANER_ROOT%\test"
            $nanerRoot = "C:\naner"
            $result = Expand-NanerPath -Path $path -NanerRoot $nanerRoot
            $result | Should -Be "C:\naner\bin\C:\naner\test"
        }

        It "Should expand environment variables" {
            $env:TEST_VAR = "TestValue"
            $path = "%NANER_ROOT%\%TEST_VAR%"
            $nanerRoot = "C:\naner"
            $result = Expand-NanerPath -Path $path -NanerRoot $nanerRoot
            $result | Should -Be "C:\naner\TestValue"
            Remove-Item Env:\TEST_VAR
        }

        It "Should handle paths without variables" {
            $path = "C:\simple\path"
            $nanerRoot = "C:\naner"
            $result = Expand-NanerPath -Path $path -NanerRoot $nanerRoot
            $result | Should -Be "C:\simple\path"
        }

        It "Should require both parameters" {
            { Expand-NanerPath -Path "test" } | Should -Throw
            { Expand-NanerPath -NanerRoot "test" } | Should -Throw
        }
    }

    Context "Find-NanerRoot" {
        BeforeAll {
            # Create temporary directory structure for testing
            $testRoot = Join-Path ([System.IO.Path]::GetTempPath()) "naner_test_$([Guid]::NewGuid())"
            New-Item -Path "$testRoot\bin" -ItemType Directory -Force | Out-Null
            New-Item -Path "$testRoot\vendor" -ItemType Directory -Force | Out-Null
            New-Item -Path "$testRoot\config" -ItemType Directory -Force | Out-Null
            New-Item -Path "$testRoot\src\powershell" -ItemType Directory -Force | Out-Null
        }

        AfterAll {
            # Cleanup
            if (Test-Path $testRoot) {
                Remove-Item $testRoot -Recurse -Force
            }
        }

        It "Should find Naner root from nested directory" {
            $startPath = Join-Path $testRoot "src\powershell"
            $result = Find-NanerRoot -StartPath $startPath
            $result | Should -Be $testRoot
        }

        It "Should find Naner root from direct child directory" {
            $startPath = Join-Path $testRoot "bin"
            $result = Find-NanerRoot -StartPath $startPath
            $result | Should -Be $testRoot
        }

        It "Should throw when Naner root cannot be found" {
            $tempPath = Join-Path ([System.IO.Path]::GetTempPath()) "nonexistent_$([Guid]::NewGuid())"
            New-Item -Path $tempPath -ItemType Directory -Force | Out-Null

            { Find-NanerRoot -StartPath $tempPath } | Should -Throw "*Could not locate Naner root*"

            Remove-Item $tempPath -Force
        }

        It "Should respect MaxDepth parameter" {
            $deepPath = Join-Path $testRoot "a\b\c\d\e\f\g\h\i\j\k"
            New-Item -Path $deepPath -ItemType Directory -Force | Out-Null

            { Find-NanerRoot -StartPath $deepPath -MaxDepth 3 } | Should -Throw

            Remove-Item (Join-Path $testRoot "a") -Recurse -Force
        }
    }
}

Describe "Common Module - Configuration Functions" {

    Context "Get-NanerConfig" {
        BeforeAll {
            $testConfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "naner_config_test.json"
            $testNanerRoot = "C:\naner"

            $testConfig = @{
                Profiles = @{
                    Unified = @{
                        Shell = "pwsh"
                    }
                }
                VendorPaths = @{
                    PowerShell = "%NANER_ROOT%\vendor\pwsh"
                    MSYS2 = "%NANER_ROOT%\vendor\msys64"
                }
            } | ConvertTo-Json -Depth 10

            Set-Content -Path $testConfigPath -Value $testConfig -Encoding UTF8
        }

        AfterAll {
            if (Test-Path $testConfigPath) {
                Remove-Item $testConfigPath -Force
            }
        }

        It "Should load configuration from file" {
            $result = Get-NanerConfig -ConfigPath $testConfigPath -NanerRoot $testNanerRoot
            $result | Should -Not -BeNullOrEmpty
            $result.Profiles | Should -Not -BeNullOrEmpty
        }

        It "Should throw when config file does not exist" {
            { Get-NanerConfig -ConfigPath "C:\nonexistent\config.json" -NanerRoot $testNanerRoot } | Should -Throw "*Configuration file not found*"
        }

        It "Should throw when config file contains invalid JSON" {
            $badConfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "bad_config.json"
            Set-Content -Path $badConfigPath -Value "{ invalid json"

            { Get-NanerConfig -ConfigPath $badConfigPath -NanerRoot $testNanerRoot } | Should -Throw "*Failed to parse*"

            Remove-Item $badConfigPath -Force
        }

        It "Should validate vendor paths when switch is provided" {
            # This should issue warnings but not throw
            { Get-NanerConfig -ConfigPath $testConfigPath -NanerRoot $testNanerRoot -ValidateVendorPaths } | Should -Not -Throw
        }

        It "Should require mandatory parameters" {
            { Get-NanerConfig -ConfigPath $testConfigPath } | Should -Throw
            { Get-NanerConfig -NanerRoot $testNanerRoot } | Should -Throw
        }
    }
}

Describe "Common Module - GitHub API Functions" {

    Context "Get-LatestGitHubRelease" {

        It "Should accept required parameters" {
            # This test will actually call GitHub API - use a known stable repo
            { Get-LatestGitHubRelease -Repo "PowerShell/PowerShell" -AssetPattern "*win-x64.zip" } | Should -Not -Throw
        }

        It "Should return hashtable with required properties" {
            $result = Get-LatestGitHubRelease -Repo "PowerShell/PowerShell" -AssetPattern "*win-x64.zip"
            $result | Should -BeOfType [hashtable]
            $result.Version | Should -Not -BeNullOrEmpty
            $result.Url | Should -Not -BeNullOrEmpty
            $result.FileName | Should -Not -BeNullOrEmpty
            $result.Size | Should -Not -BeNullOrEmpty
        }

        It "Should use fallback URL when API fails and fallback is provided" {
            $fallbackUrl = "https://example.com/fallback.zip"
            # Use invalid repo to force API failure
            $result = Get-LatestGitHubRelease -Repo "InvalidOwner/InvalidRepo123456789" -AssetPattern "*.zip" -FallbackUrl $fallbackUrl

            $result.Url | Should -Be $fallbackUrl
            $result.Version | Should -Be "latest"
        }

        It "Should throw when API fails and no fallback is provided" {
            { Get-LatestGitHubRelease -Repo "InvalidOwner/InvalidRepo123456789" -AssetPattern "*.zip" } | Should -Throw
        }

        It "Should throw when asset pattern does not match" {
            { Get-LatestGitHubRelease -Repo "PowerShell/PowerShell" -AssetPattern "*nonexistent-pattern-xyz*.tar.gz" } | Should -Throw
        }
    }
}

Describe "Common Module - Integration Tests" {

    Context "Module exports all functions" {
        It "Should export Write-Status function" {
            Get-Command Write-Status -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Write-Success function" {
            Get-Command Write-Success -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Write-Failure function" {
            Get-Command Write-Failure -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Write-Info function" {
            Get-Command Write-Info -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Write-DebugInfo function" {
            Get-Command Write-DebugInfo -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Find-NanerRoot function" {
            Get-Command Find-NanerRoot -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Get-NanerRootSimple function" {
            Get-Command Get-NanerRootSimple -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Expand-NanerPath function" {
            Get-Command Expand-NanerPath -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Get-NanerConfig function" {
            Get-Command Get-NanerConfig -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Get-LatestGitHubRelease function" {
            Get-Command Get-LatestGitHubRelease -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }
}
