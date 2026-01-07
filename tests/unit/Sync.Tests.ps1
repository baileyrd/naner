<#
.SYNOPSIS
    Unit tests for Sync-NanerConfig.ps1

.DESCRIPTION
    Tests cloud sync functionality including:
    - Provider detection
    - Sync directions (Push, Pull, Sync)
    - .syncignore pattern matching
    - Conflict resolution
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
    $script:syncScriptPath = Join-Path $script:nanerRoot "src\powershell\Sync-NanerConfig.ps1"
    $script:syncIgnorePath = Join-Path $script:nanerRoot ".syncignore"

    # Mock NANER_ROOT if not set
    if (-not $env:NANER_ROOT) {
        $env:NANER_ROOT = $script:nanerRoot
    }
}

Describe "Sync-NanerConfig Script" {
    Context "When checking script structure" {
        It "Should have required parameters" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\[Parameter\(Mandatory=\$true\)\]'
            $scriptContent | Should -Match '\[ValidateSet\(''OneDrive'', ''Dropbox'', ''GoogleDrive'', ''Custom''\)\]'
            $scriptContent | Should -Match '\[string\]\$SyncProvider'
            $scriptContent | Should -Match '\[string\]\$SyncPath'
            $scriptContent | Should -Match '\[ValidateSet\(''Push'', ''Pull'', ''Sync''\)\]'
            $scriptContent | Should -Match '\[string\]\$Direction'
        }

        It "Should import Common module" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw
            $scriptContent | Should -Match 'Import-Module \$commonModule'
        }

        It "Should define cloud provider paths" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'OneDrive'
            $scriptContent | Should -Match 'Dropbox'
            $scriptContent | Should -Match 'GoogleDrive'
        }

        It "Should support custom sync path" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'if \(\$SyncProvider -eq ''Custom''\)'
            $scriptContent | Should -Match '-SyncPath is required'
        }
    }

    Context "When checking sync directions" {
        It "Should support Push direction" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw
            $scriptContent | Should -Match 'Push'
        }

        It "Should support Pull direction" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw
            $scriptContent | Should -Match 'Pull'
        }

        It "Should support bidirectional Sync" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw
            $scriptContent | Should -Match 'Sync'
            $scriptContent | Should -Match 'Bidirectional'
        }

        It "Should have direction logic" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$Direction -eq ''Pull'''
            $scriptContent | Should -Match '\$Direction -eq ''Sync'''
        }
    }

    Context "When checking .syncignore handling" {
        It "Should load .syncignore patterns" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\.syncignore'
            $scriptContent | Should -Match 'Get-Content.*syncIgnorePath'
        }

        It "Should define default ignore patterns" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$defaultIgnores = @\('
            $scriptContent | Should -Match '\.bash_history'
            $scriptContent | Should -Match '\.log'
        }

        It "Should have pattern matching function" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'function Test-ShouldIgnore'
            $scriptContent | Should -Match 'foreach.*\$pattern'
        }

        It "Should exclude SSH keys by default" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'if \(-not \$IncludeSSHKeys\)'
            $scriptContent | Should -Match '\.ssh/id_\*'
        }
    }

    Context "When checking sync logic" {
        It "Should compare file timestamps for bidirectional sync" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'LastWriteTime'
            $scriptContent | Should -Match '\$sourceTime -gt \$destTime'
        }

        It "Should support dry-run mode" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\[switch\]\$WhatIf'
            $scriptContent | Should -Match 'if \(\$WhatIf\)'
        }

        It "Should handle file conflicts" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'Test-Path \$destPath'
            $scriptContent | Should -Match 'Read-Host.*Overwrite'
            $scriptContent | Should -Match '\[switch\]\$Force'
        }
    }

    Context "When checking security features" {
        It "Should warn about SSH keys in sync" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'Write-Warning.*SSH keys'
            $scriptContent | Should -Match 'cloud storage'
        }

        It "Should have SSH keys opt-in" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw
            $scriptContent | Should -Match '\[switch\]\$IncludeSSHKeys'
        }
    }

    Context "When checking output and reporting" {
        It "Should display sync summary" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'Files synced:'
            $scriptContent | Should -Match 'Files skipped:'
            $scriptContent | Should -Match 'Files updated:'
        }

        It "Should display provider information" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'Provider:'
            $scriptContent | Should -Match 'Path:'
            $scriptContent | Should -Match 'Direction:'
        }

        It "Should display dry-run notice" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'DRY RUN MODE'
            $scriptContent | Should -Match 'No changes will be made'
        }
    }
}

Describe ".syncignore File" {
    Context "When checking ignore patterns" {
        It "Should exist" {
            Test-Path $script:syncIgnorePath | Should -Be $true
        }

        It "Should be readable" {
            { Get-Content $script:syncIgnorePath } | Should -Not -Throw
        }

        It "Should exclude package caches" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\.cargo'
            $content | Should -Match '\.conda'
            $content | Should -Match '\.gem'
            $content | Should -Match '\.npm-cache'
        }

        It "Should exclude SSH private keys" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\.ssh/id_'
            $content | Should -Match '\.ssh/\*\.pem'
        }

        It "Should exclude shell history" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\.bash_history'
            $content | Should -Match '\.zsh_history'
        }

        It "Should exclude lock files" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\*\.lock'
            $content | Should -Match 'package-lock\.json'
            $content | Should -Match 'Cargo\.lock'
        }

        It "Should exclude binary files" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\*\.exe'
            $content | Should -Match '\*\.dll'
            $content | Should -Match '\*\.zip'
        }

        It "Should exclude temporary files" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\*\.log'
            $content | Should -Match '\*\.tmp'
        }

        It "Should exclude OS-specific files" {
            $content = Get-Content $script:syncIgnorePath -Raw

            $content | Should -Match '\.DS_Store'
            $content | Should -Match 'Thumbs\.db'
        }

        It "Should have comments" {
            $content = Get-Content $script:syncIgnorePath

            $comments = $content | Where-Object { $_ -match '^#' }
            $comments.Count | Should -BeGreaterThan 5
        }
    }

    Context "When validating pattern format" {
        It "Should use glob patterns" {
            $content = Get-Content $script:syncIgnorePath -Raw

            # Should have wildcard patterns
            $content | Should -Match '\*'
        }

        It "Should have organized sections" {
            $content = Get-Content $script:syncIgnorePath -Raw

            # Should have section headers
            $content | Should -Match 'Package Caches'
            $content | Should -Match 'SSH Private Keys'
            $content | Should -Match 'Shell History'
        }
    }
}

Describe "Error Handling" {
    Context "When handling invalid inputs" {
        It "Should require SyncPath for Custom provider" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'if \(\$SyncProvider -eq ''Custom''\)'
            $scriptContent | Should -Match 'if.*-not \$SyncPath'
            $scriptContent | Should -Match 'Write-Error.*required'
        }

        It "Should validate sync path existence for Pull" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match 'if.*-not.*Test-Path \$SyncPath'
            $scriptContent | Should -Match '\$Direction -eq ''Pull'''
        }

        It "Should create sync path for Push" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$Direction -eq ''Push'''
            $scriptContent | Should -Match 'New-Item.*Directory'
        }
    }
}

Describe "Integration Features" {
    Context "When checking cloud provider integration" {
        It "Should detect OneDrive path" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$env:USERPROFILE.*OneDrive'
        }

        It "Should detect Dropbox path" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$env:USERPROFILE.*Dropbox'
        }

        It "Should detect Google Drive path" {
            $scriptContent = Get-Content $script:syncScriptPath -Raw

            $scriptContent | Should -Match '\$env:USERPROFILE.*Google Drive'
        }
    }
}
