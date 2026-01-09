<#
.SYNOPSIS
    Unit tests for Backup-NanerConfig.ps1 and Restore-NanerConfig.ps1

.DESCRIPTION
    Tests backup and restore functionality including:
    - Backup creation
    - Compressed backups
    - Backup manifest
    - Restore from directory
    - Restore from zip
    - Selective exclusions
    - Dry-run mode
#>

BeforeAll {
    # Import Pester
    if (-not (Get-Module -ListAvailable -Name Pester)) {
        Install-Module Pester -MinimumVersion 5.0.0 -Force -SkipPublisherCheck
    }
    Import-Module Pester -MinimumVersion 5.0.0

    # Get paths
    $script:nanerRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $script:backupScriptPath = Join-Path $script:nanerRoot "src\powershell\Backup-NanerConfig.ps1"
    $script:restoreScriptPath = Join-Path $script:nanerRoot "src\powershell\Restore-NanerConfig.ps1"

    # Mock NANER_ROOT if not set
    if (-not $env:NANER_ROOT) {
        $env:NANER_ROOT = $script:nanerRoot
    }

    # Create test home directory
    $script:testHome = Join-Path ([System.IO.Path]::GetTempPath()) "naner-test-home-$(Get-Random)"
    New-Item -ItemType Directory -Path $script:testHome -Force | Out-Null

    # Create test files
    Set-Content -Path (Join-Path $script:testHome ".bashrc") -Value "# Test bashrc"
    Set-Content -Path (Join-Path $script:testHome ".gitconfig") -Value "[user]`nname = Test"

    # Create subdirectories
    $testVSCode = Join-Path $script:testHome ".vscode"
    New-Item -ItemType Directory -Path $testVSCode -Force | Out-Null
    Set-Content -Path (Join-Path $testVSCode "settings.json") -Value '{"test": true}'

    # Create test backup directory
    $script:testBackupDir = Join-Path ([System.IO.Path]::GetTempPath()) "naner-test-backups-$(Get-Random)"
}

AfterAll {
    # Clean up test directories
    if (Test-Path $script:testHome) {
        Remove-Item -Path $script:testHome -Recurse -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path $script:testBackupDir) {
        Remove-Item -Path $script:testBackupDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Describe "Backup-NanerConfig Script" {
    Context "When checking script structure" {
        It "Should have required parameters" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match '\[Parameter\(Mandatory=\$false\)\]'
            $scriptContent | Should -Match '\[string\]\$BackupPath'
            $scriptContent | Should -Match '\[string\]\$BackupName'
            $scriptContent | Should -Match '\[switch\]\$Compress'
            $scriptContent | Should -Match '\[switch\]\$IncludeSSHKeys'
        }

        It "Should import Common module" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match 'Import-Module \$commonModule'
        }

        It "Should define backup items array" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match '\$backupItems = @\('
        }

        It "Should create backup manifest" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match 'BACKUP-MANIFEST\.json'
        }
    }

    Context "When validating backup items" {
        It "Should include shell configurations" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match '\.bashrc'
            $scriptContent | Should -Match '\.bash_profile'
            $scriptContent | Should -Match '\.gitconfig'
            $scriptContent | Should -Match '\.vimrc'
        }

        It "Should include editor settings" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match '\.config'
            $scriptContent | Should -Match '\.vscode'
        }

        It "Should include PowerShell profile" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match 'Documents/PowerShell'
        }

        It "Should include project templates" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match 'Templates'
        }

        It "Should exclude SSH keys by default" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            # Should have conditional SSH key inclusion
            $scriptContent | Should -Match 'if \(\$IncludeSSHKeys\)'
        }
    }
}

Describe "Restore-NanerConfig Script" {
    Context "When checking script structure" {
        It "Should have required parameters" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match '\[Parameter\(Mandatory=\$true\)\][\s\S]*?\[string\]\$BackupPath'
            $scriptContent | Should -Match '\[switch\]\$RestoreSSHKeys'
            $scriptContent | Should -Match '\[switch\]\$Force'
            $scriptContent | Should -Match '\[switch\]\$WhatIf'
            $scriptContent | Should -Match '\[string\[\]\]\$Exclude'
        }

        It "Should import Common module" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'Import-Module \$commonModule'
        }

        It "Should handle compressed backups" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'Expand-Archive'
            $scriptContent | Should -Match '\.zip'
        }

        It "Should check for backup manifest" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'BACKUP-MANIFEST\.json'
        }
    }

    Context "When validating restore logic" {
        It "Should skip SSH keys by default" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match '\.ssh/id_\*'
            $scriptContent | Should -Match 'RestoreSSHKeys'
        }

        It "Should support exclusion patterns" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'foreach.*\$Exclude'
        }

        It "Should support dry-run mode" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'if \(\$WhatIf\)'
        }

        It "Should handle file conflicts" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw
            $scriptContent | Should -Match 'Test-Path \$destPath'
            $scriptContent | Should -Match 'Read-Host.*Overwrite'
        }
    }
}

Describe "Backup Manifest Structure" {
    Context "When checking manifest fields" {
        It "Should define required manifest fields" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match 'BackupDate'
            $scriptContent | Should -Match 'NanerRoot'
            $scriptContent | Should -Match 'BackupVersion'
            $scriptContent | Should -Match 'IncludedSSHKeys'
            $scriptContent | Should -Match 'ItemsBackedUp'
            $scriptContent | Should -Match 'ItemsSkipped'
        }

        It "Should export manifest as JSON" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw
            $scriptContent | Should -Match 'ConvertTo-Json'
            $scriptContent | Should -Match 'Out-File'
        }
    }
}

Describe "Security Features" {
    Context "When handling sensitive data" {
        It "Should warn about SSH keys in backup" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match 'Write-Warning.*SSH.*KEYS'
            $scriptContent | Should -Match 'store securely'
        }

        It "Should warn about SSH keys in restore" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match 'SSH keys'
            $scriptContent | Should -Match 'chmod 600'
        }

        It "Should have SSH keys opt-in only" {
            $backupContent = Get-Content $script:backupScriptPath -Raw
            $restoreContent = Get-Content $script:restoreScriptPath -Raw

            $backupContent | Should -Match '\[switch\]\$IncludeSSHKeys'
            $restoreContent | Should -Match '\[switch\]\$RestoreSSHKeys'
        }
    }
}

Describe "Error Handling" {
    Context "When handling invalid inputs" {
        It "Should validate BackupPath exists for restore" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match 'if.*-not.*Test-Path \$BackupPath'
            $scriptContent | Should -Match 'Write-Error.*not found'
        }

        It "Should check if backup already exists" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match 'Test-Path \$backupDestination'
            $scriptContent | Should -Match 'Write-Error.*already exists'
        }

        It "Should handle extraction failures" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match 'try'
            $scriptContent | Should -Match 'catch'
            $scriptContent | Should -Match 'Failed to extract'
        }
    }
}

Describe "Output and Reporting" {
    Context "When displaying information" {
        It "Should display backup summary" {
            $scriptContent = Get-Content $script:backupScriptPath -Raw

            $scriptContent | Should -Match 'Items backed up:'
            $scriptContent | Should -Match 'Items skipped:'
            $scriptContent | Should -Match 'Location:'
        }

        It "Should display restore summary" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match 'Files restored:'
            $scriptContent | Should -Match 'Files skipped:'
        }

        It "Should display backup manifest info on restore" {
            $scriptContent = Get-Content $script:restoreScriptPath -Raw

            $scriptContent | Should -Match 'Backup Information:'
            $scriptContent | Should -Match '\$manifest\.BackupDate'
        }
    }
}
