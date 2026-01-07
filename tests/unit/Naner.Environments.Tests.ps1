<#
.SYNOPSIS
    Unit tests for Naner.Environments.psm1 module.

.DESCRIPTION
    Comprehensive tests for environment management functionality.
#>

BeforeAll {
    # Import required modules
    $modulePath = Join-Path $PSScriptRoot "..\..\src\powershell\Naner.Environments.psm1"
    $commonPath = Join-Path $PSScriptRoot "..\..\src\powershell\Common.psm1"

    Import-Module $commonPath -Force
    Import-Module $modulePath -Force

    # Create a temporary test directory
    $script:TestRoot = Join-Path $TestDrive "naner-test"
    New-Item -Path $TestRoot -ItemType Directory -Force | Out-Null

    # Create directory structure
    $homeDir = Join-Path $TestRoot "home"
    $configDir = Join-Path $TestRoot "config"
    New-Item -Path $homeDir -ItemType Directory -Force | Out-Null
    New-Item -Path $configDir -ItemType Directory -Force | Out-Null

    # Helper function to clean up test environments
    function Remove-TestEnvironments {
        param([string]$NanerRoot)

        $envsDir = Join-Path $NanerRoot "home\environments"
        if (Test-Path $envsDir) {
            Remove-Item -Path $envsDir -Recurse -Force -ErrorAction SilentlyContinue
        }

        $activeEnvFile = Join-Path $NanerRoot "config\active-environment.txt"
        if (Test-Path $activeEnvFile) {
            Remove-Item -Path $activeEnvFile -Force -ErrorAction SilentlyContinue
        }
    }
}

AfterAll {
    # Cleanup
    if (Test-Path $script:TestRoot) {
        Remove-Item -Path $script:TestRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Describe "New-NanerEnvironment" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot
    }

    Context "Basic environment creation" {
        It "Creates a new environment successfully" {
            { New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot } | Should -Not -Throw

            $envPath = Join-Path $script:TestRoot "home\environments\test-env"
            Test-Path $envPath | Should -Be $true
        }

        It "Creates environment metadata file" {
            New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

            $configPath = Join-Path $script:TestRoot "home\environments\test-env\.naner-env.json"
            Test-Path $configPath | Should -Be $true

            $metadata = Get-Content $configPath | ConvertFrom-Json
            $metadata.name | Should -Be "test-env"
            $metadata.version | Should -Be "1.0"
        }

        It "Creates base directory structure" {
            New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

            $envPath = Join-Path $script:TestRoot "home\environments\test-env"

            $expectedDirs = @(
                ".ssh",
                ".config\powershell",
                "Documents\PowerShell\Modules",
                ".vscode",
                "go\bin",
                ".cargo",
                ".npm-global"
            )

            foreach ($dir in $expectedDirs) {
                $dirPath = Join-Path $envPath $dir
                Test-Path $dirPath | Should -Be $true -Because "$dir should exist"
            }
        }

        It "Accepts a description parameter" {
            New-NanerEnvironment -Name "test-env" -Description "Test environment" -NanerRoot $script:TestRoot

            $configPath = Join-Path $script:TestRoot "home\environments\test-env\.naner-env.json"
            $metadata = Get-Content $configPath | ConvertFrom-Json

            $metadata.description | Should -Be "Test environment"
        }
    }

    Context "Environment name validation" {
        It "Rejects 'default' as environment name" {
            { New-NanerEnvironment -Name "default" -NanerRoot $script:TestRoot } |
                Should -Throw -ExpectedMessage "*Cannot create an environment named 'default'*"
        }

        It "Only accepts valid environment names (alphanumeric, dash, underscore)" {
            # Valid names should work
            { New-NanerEnvironment -Name "work-env" -NanerRoot $script:TestRoot } | Should -Not -Throw
            Remove-TestEnvironments -NanerRoot $script:TestRoot

            { New-NanerEnvironment -Name "test_env" -NanerRoot $script:TestRoot } | Should -Not -Throw
            Remove-TestEnvironments -NanerRoot $script:TestRoot

            { New-NanerEnvironment -Name "env123" -NanerRoot $script:TestRoot } | Should -Not -Throw
        }

        It "Rejects duplicate environment names" {
            New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

            { New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot } |
                Should -Throw -ExpectedMessage "*already exists*"
        }
    }

    Context "Copying from existing environment" {
        BeforeEach {
            # Create a source environment with some files
            $sourceEnvPath = Join-Path $script:TestRoot "home"

            # Create .gitconfig
            $gitconfig = @"
[user]
    name = Test User
    email = test@example.com
"@
            Set-Content -Path (Join-Path $sourceEnvPath ".gitconfig") -Value $gitconfig

            # Create .bashrc
            $bashrc = "export TEST_VAR=hello"
            Set-Content -Path (Join-Path $sourceEnvPath ".bashrc") -Value $bashrc
        }

        It "Copies configuration from default environment" {
            New-NanerEnvironment -Name "test-env" -CopyFrom "default" -NanerRoot $script:TestRoot

            $envPath = Join-Path $script:TestRoot "home\environments\test-env"

            # Check if files were copied
            Test-Path (Join-Path $envPath ".gitconfig") | Should -Be $true
            Test-Path (Join-Path $envPath ".bashrc") | Should -Be $true

            # Verify content
            $gitconfig = Get-Content (Join-Path $envPath ".gitconfig") -Raw
            $gitconfig | Should -Match "Test User"
        }

        It "Records the source environment in metadata" {
            New-NanerEnvironment -Name "test-env" -CopyFrom "default" -NanerRoot $script:TestRoot

            $configPath = Join-Path $script:TestRoot "home\environments\test-env\.naner-env.json"
            $metadata = Get-Content $configPath | ConvertFrom-Json

            $metadata.copiedFrom | Should -Be "default"
        }

        It "Throws error when copying from non-existent environment" {
            { New-NanerEnvironment -Name "test-env" -CopyFrom "nonexistent" -NanerRoot $script:TestRoot } |
                Should -Throw -ExpectedMessage "*not found*"
        }
    }
}

Describe "Use-NanerEnvironment" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot

        # Create a test environment
        New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot
    }

    It "Sets the active environment" {
        Use-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

        $activeEnvFile = Join-Path $script:TestRoot "config\active-environment.txt"
        Test-Path $activeEnvFile | Should -Be $true

        $activeEnv = Get-Content $activeEnvFile -Raw
        $activeEnv.Trim() | Should -Be "test-env"
    }

    It "Accepts 'default' as an environment name" {
        Use-NanerEnvironment -Name "default" -NanerRoot $script:TestRoot

        $activeEnvFile = Join-Path $script:TestRoot "config\active-environment.txt"
        $activeEnv = Get-Content $activeEnvFile -Raw
        $activeEnv.Trim() | Should -Be "default"
    }

    It "Throws error when switching to non-existent environment" {
        { Use-NanerEnvironment -Name "nonexistent" -NanerRoot $script:TestRoot } |
            Should -Throw -ExpectedMessage "*not found*"
    }

    It "Overwrites previous active environment" {
        # Create another environment
        New-NanerEnvironment -Name "env2" -NanerRoot $script:TestRoot

        # Set first environment as active
        Use-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

        # Switch to second environment
        Use-NanerEnvironment -Name "env2" -NanerRoot $script:TestRoot

        $activeEnvFile = Join-Path $script:TestRoot "config\active-environment.txt"
        $activeEnv = Get-Content $activeEnvFile -Raw
        $activeEnv.Trim() | Should -Be "env2"
    }
}

Describe "Get-NanerEnvironment" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot

        # Create test environments
        New-NanerEnvironment -Name "work" -Description "Work environment" -NanerRoot $script:TestRoot
        New-NanerEnvironment -Name "personal" -Description "Personal projects" -NanerRoot $script:TestRoot
    }

    Context "Listing all environments" {
        It "Lists all environments without throwing" {
            { Get-NanerEnvironment -NanerRoot $script:TestRoot } | Should -Not -Throw
        }

        It "Shows 'default' environment" {
            # Capture Write-Host output
            $output = Get-NanerEnvironment -NanerRoot $script:TestRoot 6>&1 | Out-String
            $output | Should -Match "default"
        }

        It "Shows created environments" {
            # Capture Write-Host output
            $output = Get-NanerEnvironment -NanerRoot $script:TestRoot 6>&1 | Out-String
            $output | Should -Match "work"
            $output | Should -Match "personal"
        }

        It "Marks active environment with asterisk" {
            Use-NanerEnvironment -Name "work" -NanerRoot $script:TestRoot

            # Capture Write-Host output
            $output = Get-NanerEnvironment -NanerRoot $script:TestRoot 6>&1 | Out-String
            $output | Should -Match "\*\s+work"
        }
    }

    Context "Getting specific environment details" {
        It "Shows details for a specific environment" {
            { Get-NanerEnvironment -Name "work" -NanerRoot $script:TestRoot } | Should -Not -Throw
        }

        It "Shows environment metadata" {
            # Capture Write-Host output
            $output = Get-NanerEnvironment -Name "work" -NanerRoot $script:TestRoot 6>&1 | Out-String
            $output | Should -Match "Work environment"
            $output | Should -Match "Created:"
        }

        It "Throws error for non-existent environment" {
            { Get-NanerEnvironment -Name "nonexistent" -NanerRoot $script:TestRoot } |
                Should -Throw -ExpectedMessage "*not found*"
        }
    }
}

Describe "Get-ActiveNanerEnvironment" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot
        New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot
    }

    It "Returns 'default' when no active environment is set" {
        $activeEnv = Get-ActiveNanerEnvironment -NanerRoot $script:TestRoot
        $activeEnv | Should -Be "default"
    }

    It "Returns the current active environment" {
        Use-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

        $activeEnv = Get-ActiveNanerEnvironment -NanerRoot $script:TestRoot
        $activeEnv | Should -Be "test-env"
    }
}

Describe "Remove-NanerEnvironment" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot
        New-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot
    }

    It "Removes an environment with -Force parameter" {
        Remove-NanerEnvironment -Name "test-env" -Force -NanerRoot $script:TestRoot

        $envPath = Join-Path $script:TestRoot "home\environments\test-env"
        Test-Path $envPath | Should -Be $false
    }

    It "Cannot remove 'default' environment" {
        { Remove-NanerEnvironment -Name "default" -Force -NanerRoot $script:TestRoot } |
            Should -Throw -ExpectedMessage "*Cannot remove the 'default' environment*"
    }

    It "Cannot remove currently active environment" {
        Use-NanerEnvironment -Name "test-env" -NanerRoot $script:TestRoot

        { Remove-NanerEnvironment -Name "test-env" -Force -NanerRoot $script:TestRoot } |
            Should -Throw -ExpectedMessage "*currently active*"
    }

    It "Can remove inactive environment" {
        New-NanerEnvironment -Name "env2" -NanerRoot $script:TestRoot
        Use-NanerEnvironment -Name "env2" -NanerRoot $script:TestRoot

        # Should be able to remove test-env since env2 is active
        { Remove-NanerEnvironment -Name "test-env" -Force -NanerRoot $script:TestRoot } |
            Should -Not -Throw
    }

    It "Throws error when removing non-existent environment" {
        { Remove-NanerEnvironment -Name "nonexistent" -Force -NanerRoot $script:TestRoot } |
            Should -Throw -ExpectedMessage "*not found*"
    }
}

Describe "Integration tests" {
    BeforeEach {
        Remove-TestEnvironments -NanerRoot $script:TestRoot
    }

    It "Complete workflow: create, switch, list, remove" {
        # Create environments
        New-NanerEnvironment -Name "work" -NanerRoot $script:TestRoot
        New-NanerEnvironment -Name "personal" -NanerRoot $script:TestRoot

        # Switch to work
        Use-NanerEnvironment -Name "work" -NanerRoot $script:TestRoot
        Get-ActiveNanerEnvironment -NanerRoot $script:TestRoot | Should -Be "work"

        # List environments - capture Write-Host output
        $output = Get-NanerEnvironment -NanerRoot $script:TestRoot 6>&1 | Out-String
        $output | Should -Match "work"
        $output | Should -Match "personal"

        # Switch to personal
        Use-NanerEnvironment -Name "personal" -NanerRoot $script:TestRoot
        Get-ActiveNanerEnvironment -NanerRoot $script:TestRoot | Should -Be "personal"

        # Remove work (should succeed since personal is active)
        Remove-NanerEnvironment -Name "work" -Force -NanerRoot $script:TestRoot
        $workPath = Join-Path $script:TestRoot "home\environments\work"
        Test-Path $workPath | Should -Be $false
    }

    It "Environment isolation: separate configurations" {
        # Create two environments with different configs
        New-NanerEnvironment -Name "env1" -NanerRoot $script:TestRoot
        New-NanerEnvironment -Name "env2" -NanerRoot $script:TestRoot

        # Add different .gitconfig to each
        $env1Path = Join-Path $script:TestRoot "home\environments\env1\.gitconfig"
        $env2Path = Join-Path $script:TestRoot "home\environments\env2\.gitconfig"

        "[user]`n    name = User One" | Set-Content $env1Path
        "[user]`n    name = User Two" | Set-Content $env2Path

        # Verify isolation - read as single string with -Raw
        $env1Config = Get-Content $env1Path -Raw
        $env2Config = Get-Content $env2Path -Raw

        $env1Config | Should -Match "User One"
        $env2Config | Should -Match "User Two"
    }

    It "Copy environment preserves configuration" {
        # Create base environment with config
        $defaultPath = Join-Path $script:TestRoot "home"
        $gitconfig = "[user]`n    name = Base User"
        Set-Content (Join-Path $defaultPath ".gitconfig") -Value $gitconfig

        # Copy to new environment
        New-NanerEnvironment -Name "copied" -CopyFrom "default" -NanerRoot $script:TestRoot

        # Verify config was copied
        $copiedPath = Join-Path $script:TestRoot "home\environments\copied\.gitconfig"
        Test-Path $copiedPath | Should -Be $true

        $copiedConfig = Get-Content $copiedPath -Raw
        $copiedConfig | Should -Match "Base User"
    }
}
