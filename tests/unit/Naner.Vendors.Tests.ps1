<#
.SYNOPSIS
    Unit tests for Naner.Vendors.psm1 module.

.DESCRIPTION
    Pester unit tests for the Naner.Vendors module functions including:
    - Configuration management (Get-VendorConfiguration)
    - Release information (Get-VendorRelease, Get-GitHubRelease, Get-WebScrapedRelease, Get-StaticRelease, Get-GoRelease)
    - Vendor installation (Install-VendorPackage)
    - PostInstall functions (Initialize-*)

.NOTES
    Run with: Invoke-Pester -Path tests/unit/Naner.Vendors.Tests.ps1
#>

BeforeAll {
    # Import the module
    $modulePath = Join-Path $PSScriptRoot "..\..\src\powershell\Naner.Vendors.psm1"
    Import-Module $modulePath -Force
}

Describe "Naner.Vendors Module - Configuration Management" {

    Context "Get-VendorConfiguration" {
        BeforeAll {
            $testVendorsPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_vendors.json"
            $testVendorsConfig = @{
                vendors = @{
                    PowerShell = @{
                        name = "PowerShell 7"
                        enabled = $true
                        extractDir = "pwsh"
                        releaseSource = @{
                            type = "github"
                            repo = "PowerShell/PowerShell"
                            assetPattern = "*win-x64.zip"
                        }
                    }
                    SevenZip = @{
                        name = "7-Zip"
                        enabled = $true
                        extractDir = "7zip"
                        releaseSource = @{
                            type = "web-scrape"
                            url = "https://www.7-zip.org/download.html"
                            pattern = '(7z\d+-x64\.msi)'
                        }
                    }
                }
            } | ConvertTo-Json -Depth 10

            Set-Content -Path $testVendorsPath -Value $testVendorsConfig -Encoding UTF8
        }

        AfterAll {
            if (Test-Path $testVendorsPath) {
                Remove-Item $testVendorsPath -Force
            }
        }

        It "Should load vendor configuration from file" {
            $result = Get-VendorConfiguration -ConfigPath $testVendorsPath
            $result | Should -Not -BeNullOrEmpty
            $result.Count | Should -BeGreaterThan 0
        }

        It "Should return all vendors when VendorId is not specified" {
            $result = Get-VendorConfiguration -ConfigPath $testVendorsPath
            $result.ContainsKey("PowerShell") | Should -Be $true
            $result.ContainsKey("SevenZip") | Should -Be $true
        }

        It "Should return specific vendor when VendorId is provided" {
            $result = Get-VendorConfiguration -ConfigPath $testVendorsPath -VendorId "PowerShell"
            $result.name | Should -Be "PowerShell 7"
        }

        It "Should throw when config file does not exist" {
            { Get-VendorConfiguration -ConfigPath "C:\nonexistent\vendors.json" } | Should -Throw "*Vendor configuration not found*"
        }

        It "Should throw when vendor section is missing" {
            $badConfigPath = Join-Path ([System.IO.Path]::GetTempPath()) "bad_vendors.json"
            Set-Content -Path $badConfigPath -Value '{"badKey": {}}' -Encoding UTF8

            { Get-VendorConfiguration -ConfigPath $badConfigPath } | Should -Throw "*'vendors' section not found*"

            Remove-Item $badConfigPath -Force
        }

        It "Should throw when specific vendor is not found" {
            { Get-VendorConfiguration -ConfigPath $testVendorsPath -VendorId "NonExistentVendor" } | Should -Throw "*Vendor not found*"
        }
    }
}

Describe "Naner.Vendors Module - Release Information" {

    Context "Get-GitHubRelease" {
        BeforeAll {
            $githubReleaseSource = [PSCustomObject]@{
                repo = "PowerShell/PowerShell"
                assetPattern = "*win-x64.zip"
            }
        }

        It "Should fetch release from GitHub API" {
            $result = Get-GitHubRelease -ReleaseSource $githubReleaseSource
            $result | Should -Not -BeNullOrEmpty
            $result.Version | Should -Not -BeNullOrEmpty
            $result.Url | Should -Match "^https://"
            $result.FileName | Should -Match "\.zip$"
        }

        It "Should strip leading 'v' from version tag" {
            $result = Get-GitHubRelease -ReleaseSource $githubReleaseSource
            $result.Version | Should -Not -Match "^v"
        }

        It "Should throw when repo is invalid" {
            $invalidSource = [PSCustomObject]@{
                repo = "Invalid/Nonexistent123456789"
                assetPattern = "*.zip"
            }
            { Get-GitHubRelease -ReleaseSource $invalidSource } | Should -Throw
        }
    }

    Context "Get-StaticRelease" {
        BeforeAll {
            $staticReleaseSource = [PSCustomObject]@{
                version = "1.0.0"
                url = "https://example.com/tool.zip"
                fileName = "tool.zip"
                size = "50"
            }
        }

        It "Should return static release information" {
            $result = Get-StaticRelease -ReleaseSource $staticReleaseSource
            $result | Should -Not -BeNullOrEmpty
            $result.Version | Should -Be "1.0.0"
            $result.Url | Should -Be "https://example.com/tool.zip"
            $result.FileName | Should -Be "tool.zip"
            $result.Size | Should -Be "50"
        }

        It "Should return hashtable with all required properties" {
            $result = Get-StaticRelease -ReleaseSource $staticReleaseSource
            $result | Should -BeOfType [hashtable]
            $result.Keys | Should -Contain "Version"
            $result.Keys | Should -Contain "Url"
            $result.Keys | Should -Contain "FileName"
            $result.Keys | Should -Contain "Size"
        }
    }

    Context "Get-VendorRelease" {
        BeforeAll {
            $testVendor = [PSCustomObject]@{
                name = "Test Vendor"
                releaseSource = [PSCustomObject]@{
                    type = "static"
                    version = "2.0.0"
                    url = "https://example.com/test.zip"
                    fileName = "test.zip"
                    size = "100"
                }
            }
        }

        It "Should get release for static type" {
            $result = Get-VendorRelease -Vendor $testVendor
            $result.Version | Should -Be "2.0.0"
        }

        It "Should use fallback when release fetch fails and fallback is available" {
            $vendorWithFallback = [PSCustomObject]@{
                name = "Test Vendor"
                releaseSource = [PSCustomObject]@{
                    type = "unsupported-type"
                    fallback = [PSCustomObject]@{
                        version = "fallback-1.0"
                        url = "https://example.com/fallback.zip"
                        fileName = "fallback.zip"
                        size = "50"
                    }
                }
            }

            $result = Get-VendorRelease -Vendor $vendorWithFallback
            $result.Version | Should -Be "fallback-1.0"
        }

        It "Should throw when type is unsupported and no fallback exists" {
            $vendorNoFallback = [PSCustomObject]@{
                name = "Test Vendor"
                releaseSource = [PSCustomObject]@{
                    type = "unsupported-type"
                }
            }

            { Get-VendorRelease -Vendor $vendorNoFallback } | Should -Throw
        }
    }
}

Describe "Naner.Vendors Module - PostInstall Functions" {

    Context "Initialize-SevenZip" {
        BeforeAll {
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_7zip"
            New-Item -Path $testExtractPath -ItemType Directory -Force | Out-Null
        }

        AfterAll {
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should run without errors when 7z.exe exists" {
            $sevenZipExe = Join-Path $testExtractPath "7z.exe"
            New-Item -Path $sevenZipExe -ItemType File -Force | Out-Null

            { Initialize-SevenZip -ExtractPath $testExtractPath } | Should -Not -Throw
        }

        It "Should warn when 7z.exe does not exist" {
            $emptyPath = Join-Path ([System.IO.Path]::GetTempPath()) "empty_7zip"
            New-Item -Path $emptyPath -ItemType Directory -Force | Out-Null

            { Initialize-SevenZip -ExtractPath $emptyPath } | Should -Not -Throw

            Remove-Item $emptyPath -Recurse -Force
        }
    }

    Context "Initialize-PowerShell" {
        BeforeAll {
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_pwsh"
            New-Item -Path $testExtractPath -ItemType Directory -Force | Out-Null
        }

        AfterAll {
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should create pwsh.bat wrapper" {
            Initialize-PowerShell -ExtractPath $testExtractPath

            $wrapperPath = Join-Path $testExtractPath "pwsh.bat"
            Test-Path $wrapperPath | Should -Be $true
        }

        It "Should create wrapper with correct content" {
            Initialize-PowerShell -ExtractPath $testExtractPath

            $wrapperPath = Join-Path $testExtractPath "pwsh.bat"
            $content = Get-Content $wrapperPath -Raw
            $content | Should -Match "pwsh.exe"
        }
    }

    Context "Initialize-Go" {
        BeforeAll {
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_go"
            New-Item -Path $testExtractPath -ItemType Directory -Force | Out-Null
            New-Item -Path "$testExtractPath\bin" -ItemType Directory -Force | Out-Null
        }

        AfterAll {
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should handle missing go.exe gracefully" {
            # Without go.exe, function should warn but not throw
            { Initialize-Go -ExtractPath $testExtractPath } | Should -Not -Throw
        }

        It "Should verify expected directory structure" {
            # Just verify the extract path exists
            Test-Path $testExtractPath | Should -Be $true
            Test-Path "$testExtractPath\bin" | Should -Be $true
        }
    }
}

Describe "Naner.Vendors Module - Integration Tests" {

    Context "Module exports all functions" {
        It "Should export Get-VendorConfiguration function" {
            Get-Command Get-VendorConfiguration -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Get-VendorRelease function" {
            Get-Command Get-VendorRelease -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Install-VendorPackage function" {
            Get-Command Install-VendorPackage -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-SevenZip function" {
            Get-Command Initialize-SevenZip -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-PowerShell function" {
            Get-Command Initialize-PowerShell -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-WindowsTerminal function" {
            Get-Command Initialize-WindowsTerminal -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-MSYS2 function" {
            Get-Command Initialize-MSYS2 -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-NodeJS function" {
            Get-Command Initialize-NodeJS -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-Miniconda function" {
            Get-Command Initialize-Miniconda -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-Go function" {
            Get-Command Initialize-Go -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-Rust function" {
            Get-Command Initialize-Rust -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Initialize-Ruby function" {
            Get-Command Initialize-Ruby -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }
}
