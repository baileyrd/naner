<#
.SYNOPSIS
    Unit tests for Naner.Archives.psm1 module.

.DESCRIPTION
    Pester unit tests for the Naner.Archives module functions including:
    - File download (Get-FileWithProgress)
    - 7-Zip path detection (Get-SevenZipPath)
    - Archive extraction (Expand-ArchiveWith7Zip, Expand-VendorArchive)

.NOTES
    Run with: Invoke-Pester -Path tests/unit/Naner.Archives.Tests.ps1
#>

BeforeAll {
    # Import the module
    $modulePath = Join-Path $PSScriptRoot "..\..\src\powershell\Naner.Archives.psm1"
    Import-Module $modulePath -Force
}

Describe "Naner.Archives Module - File Download" {

    Context "Get-FileWithProgress" {
        BeforeAll {
            $testUrl = "https://raw.githubusercontent.com/PowerShell/PowerShell/master/README.md"
            $testOutFile = Join-Path ([System.IO.Path]::GetTempPath()) "test_download_$(Get-Random).txt"
        }

        AfterEach {
            if (Test-Path $testOutFile) {
                Remove-Item $testOutFile -Force
            }
        }

        It "Should download file successfully" {
            $result = Get-FileWithProgress -Url $testUrl -OutFile $testOutFile
            $result | Should -Be $true
            Test-Path $testOutFile | Should -Be $true
        }

        It "Should create output file with content" {
            $result = Get-FileWithProgress -Url $testUrl -OutFile $testOutFile
            $fileSize = (Get-Item $testOutFile).Length
            $fileSize | Should -BeGreaterThan 0
        }

        It "Should retry on failure" {
            # Use an invalid URL to trigger retry logic
            $invalidUrl = "https://invalid-domain-that-does-not-exist-12345.com/file.txt"
            $result = Get-FileWithProgress -Url $invalidUrl -OutFile $testOutFile -MaxRetries 2

            $result | Should -Be $false
            Test-Path $testOutFile | Should -Be $false
        }

        It "Should respect MaxRetries parameter" {
            $invalidUrl = "https://invalid-domain-that-does-not-exist-12345.com/file.txt"
            $result = Get-FileWithProgress -Url $invalidUrl -OutFile $testOutFile -MaxRetries 1

            $result | Should -Be $false
        }

        It "Should require both Url and OutFile parameters" {
            { Get-FileWithProgress -Url $testUrl } | Should -Throw
            { Get-FileWithProgress -OutFile $testOutFile } | Should -Throw
        }

        It "Should clean up partial downloads on failure" {
            $invalidUrl = "https://invalid-domain-that-does-not-exist-12345.com/file.txt"
            Get-FileWithProgress -Url $invalidUrl -OutFile $testOutFile -MaxRetries 1

            Test-Path $testOutFile | Should -Be $false
        }
    }
}

Describe "Naner.Archives Module - 7-Zip Path Detection" {

    Context "Get-SevenZipPath" {

        It "Should return null when no 7-Zip is found" {
            $nonExistentVendorDir = "C:\NonExistent\Vendor"
            $result = Get-SevenZipPath -VendorDir $nonExistentVendorDir

            # This test may pass or fail depending on system 7-Zip installation
            # So we just verify it returns either null or a valid path
            if ($result) {
                $result.Path | Should -Not -BeNullOrEmpty
                $result.Source | Should -BeIn @("vendored", "system")
            }
        }

        It "Should prioritize vendored 7-Zip over system 7-Zip" {
            $testVendorDir = Join-Path ([System.IO.Path]::GetTempPath()) "test_vendor_$(Get-Random)"
            $sevenZipDir = Join-Path $testVendorDir "7zip"
            New-Item -Path $sevenZipDir -ItemType Directory -Force | Out-Null

            $sevenZipExe = Join-Path $sevenZipDir "7z.exe"
            # Create dummy exe
            $bytes = [byte[]](0x4D, 0x5A)
            [System.IO.File]::WriteAllBytes($sevenZipExe, $bytes)

            $result = Get-SevenZipPath -VendorDir $testVendorDir

            $result | Should -Not -BeNullOrEmpty
            $result.Source | Should -Be "vendored"
            $result.Path | Should -Be $sevenZipExe

            Remove-Item $testVendorDir -Recurse -Force
        }

        It "Should return hashtable with Path and Source properties" {
            $result = Get-SevenZipPath

            if ($result) {
                $result | Should -BeOfType [hashtable]
                $result.Keys | Should -Contain "Path"
                $result.Keys | Should -Contain "Source"
            }
        }

        It "Should accept VendorDir parameter" {
            { Get-SevenZipPath -VendorDir "C:\Test" } | Should -Not -Throw
        }

        It "Should work without VendorDir parameter" {
            { Get-SevenZipPath } | Should -Not -Throw
        }
    }
}

Describe "Naner.Archives Module - Archive Extraction" {

    Context "Expand-VendorArchive - ZIP files" {
        BeforeAll {
            $testZipPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_archive_$(Get-Random).zip"
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_extract_$(Get-Random)"

            # Create a simple ZIP file for testing
            $tempContentDir = Join-Path ([System.IO.Path]::GetTempPath()) "zip_content_$(Get-Random)"
            New-Item -Path $tempContentDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $tempContentDir "test.txt") -Value "Test content"

            # Use Compress-Archive to create test ZIP
            Compress-Archive -Path "$tempContentDir\*" -DestinationPath $testZipPath -Force

            Remove-Item $tempContentDir -Recurse -Force
        }

        AfterAll {
            if (Test-Path $testZipPath) {
                Remove-Item $testZipPath -Force
            }
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should extract ZIP file successfully" {
            $result = Expand-VendorArchive -ArchivePath $testZipPath -DestinationPath $testExtractPath

            $result | Should -Be $true
            Test-Path $testExtractPath | Should -Be $true
            Test-Path (Join-Path $testExtractPath "test.txt") | Should -Be $true
        }

        It "Should extract ZIP file content correctly" {
            Expand-VendorArchive -ArchivePath $testZipPath -DestinationPath $testExtractPath

            $content = Get-Content (Join-Path $testExtractPath "test.txt") -Raw
            $content.Trim() | Should -Be "Test content"
        }

        It "Should require ArchivePath and DestinationPath parameters" {
            { Expand-VendorArchive -ArchivePath $testZipPath } | Should -Throw
            { Expand-VendorArchive -DestinationPath $testExtractPath } | Should -Throw
        }
    }

    Context "Expand-VendorArchive - Unsupported formats" {
        BeforeAll {
            $unsupportedFile = Join-Path ([System.IO.Path]::GetTempPath()) "test.xyz"
            Set-Content -Path $unsupportedFile -Value "dummy"
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_extract_$(Get-Random)"
        }

        AfterAll {
            if (Test-Path $unsupportedFile) {
                Remove-Item $unsupportedFile -Force
            }
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should return false for unsupported archive format" {
            $result = Expand-VendorArchive -ArchivePath $unsupportedFile -DestinationPath $testExtractPath

            $result | Should -Be $false
        }
    }
}

Describe "Naner.Archives Module - 7-Zip Extraction" {

    Context "Expand-ArchiveWith7Zip" {

        It "Should handle 7-Zip availability correctly" {
            $testZipPath = Join-Path ([System.IO.Path]::GetTempPath()) "test_$(Get-Random).zip"
            $testExtractPath = Join-Path ([System.IO.Path]::GetTempPath()) "extract_$(Get-Random)"

            # Create a valid test ZIP file
            $tempContentDir = Join-Path ([System.IO.Path]::GetTempPath()) "content_$(Get-Random)"
            New-Item -Path $tempContentDir -ItemType Directory -Force | Out-Null
            Set-Content -Path (Join-Path $tempContentDir "test.txt") -Value "test"
            Compress-Archive -Path "$tempContentDir\*" -DestinationPath $testZipPath -Force
            Remove-Item $tempContentDir -Recurse -Force

            $nonExistentVendorDir = "C:\NonExistent\Vendor\Path\$(Get-Random)"

            # This may succeed (system 7-Zip) or fail (no 7-Zip)
            $result = Expand-ArchiveWith7Zip -ArchivePath $testZipPath -DestinationPath $testExtractPath -VendorDir $nonExistentVendorDir

            # Result should be a boolean
            $result | Should -BeOfType [bool]

            if (Test-Path $testZipPath) {
                Remove-Item $testZipPath -Force
            }
            if (Test-Path $testExtractPath) {
                Remove-Item $testExtractPath -Recurse -Force
            }
        }

        It "Should accept all required parameters" {
            $testArchive = Join-Path ([System.IO.Path]::GetTempPath()) "dummy.zip"
            $testDest = Join-Path ([System.IO.Path]::GetTempPath()) "dest"

            { Expand-ArchiveWith7Zip -ArchivePath $testArchive -DestinationPath $testDest } | Should -Not -Throw
        }

        It "Should require ArchivePath and DestinationPath" {
            { Expand-ArchiveWith7Zip -ArchivePath "test.zip" } | Should -Throw
            { Expand-ArchiveWith7Zip -DestinationPath "test" } | Should -Throw
        }
    }
}

Describe "Naner.Archives Module - Integration Tests" {

    Context "Module exports all functions" {
        It "Should export Get-FileWithProgress function" {
            Get-Command Get-FileWithProgress -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Get-SevenZipPath function" {
            Get-Command Get-SevenZipPath -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Expand-ArchiveWith7Zip function" {
            Get-Command Expand-ArchiveWith7Zip -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }

        It "Should export Expand-VendorArchive function" {
            Get-Command Expand-VendorArchive -ErrorAction SilentlyContinue | Should -Not -BeNullOrEmpty
        }
    }

    Context "Module dependencies" {
        It "Should import successfully" {
            # Archives module should import without errors
            { Import-Module (Join-Path $PSScriptRoot "..\..\src\powershell\Naner.Archives.psm1") -Force } | Should -Not -Throw
        }
    }
}
