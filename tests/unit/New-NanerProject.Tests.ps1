<#
.SYNOPSIS
    Unit tests for New-NanerProject.ps1

.DESCRIPTION
    Tests project scaffolding functionality including:
    - Template information retrieval
    - File copying with placeholder substitution
    - Project creation workflow
    - Dependency installation (mocked)
#>

BeforeAll {
    # Import Pester
    if (-not (Get-Module -ListAvailable -Name Pester)) {
        Install-Module Pester -MinimumVersion 5.0.0 -Force -SkipPublisherCheck
    }
    Import-Module Pester -MinimumVersion 5.0.0

    # Get script path
    $script:nanerRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
    $script:scriptPath = Join-Path $script:nanerRoot "src\powershell\New-NanerProject.ps1"

    # Mock NANER_ROOT if not set
    if (-not $env:NANER_ROOT) {
        $env:NANER_ROOT = $script:nanerRoot
    }

    # Create test template directory
    $script:testTemplateDir = Join-Path ([System.IO.Path]::GetTempPath()) "naner-test-templates-$(Get-Random)"
    New-Item -ItemType Directory -Path $script:testTemplateDir -Force | Out-Null

    # Create a simple test template
    $script:testTemplate = Join-Path $script:testTemplateDir "test-template"
    New-Item -ItemType Directory -Path $script:testTemplate -Force | Out-Null

    # Create template files with placeholders
    Set-Content -Path (Join-Path $script:testTemplate "README.md") -Value "# {{PROJECT_NAME}}`n`nA test project named {{PROJECT_NAME}}."
    Set-Content -Path (Join-Path $script:testTemplate "package.json") -Value '{"name": "{{PROJECT_NAME}}", "version": "1.0.0"}'

    # Create nested directory structure
    $testSubDir = Join-Path $script:testTemplate "src"
    New-Item -ItemType Directory -Path $testSubDir -Force | Out-Null
    Set-Content -Path (Join-Path $testSubDir "index.js") -Value "console.log('{{PROJECT_NAME}}');"

    # Source the functions from New-NanerProject.ps1 (dot-sourcing)
    # We'll load individual functions rather than executing the whole script
    $scriptContent = Get-Content $script:scriptPath -Raw
    # Extract functions
    $getFunctionPattern = 'function (Get-TemplateInfo|Copy-TemplateWithSubstitution|Write-ProjectHeader)\s*\{[\s\S]*?\n\}'
    $functions = [regex]::Matches($scriptContent, $getFunctionPattern)

    foreach ($match in $functions) {
        Invoke-Expression $match.Value
    }
}

AfterAll {
    # Clean up test template directory
    if (Test-Path $script:testTemplateDir) {
        Remove-Item -Path $script:testTemplateDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

Describe "Get-TemplateInfo" {
    Context "When retrieving template information" {
        It "Should return info for react-vite-ts template" {
            $info = Get-TemplateInfo -TemplateType "react-vite-ts"

            $info | Should -Not -BeNullOrEmpty
            $info.Name | Should -Be "React + Vite + TypeScript"
            $info.InstallCmd | Should -Be "npm install"
            $info.RunCmd | Should -Be "npm run dev"
        }

        It "Should return info for nodejs-express-api template" {
            $info = Get-TemplateInfo -TemplateType "nodejs-express-api"

            $info | Should -Not -BeNullOrEmpty
            $info.Name | Should -Be "Node.js Express API"
            $info.InstallCmd | Should -Be "npm install"
            $info.RunCmd | Should -Be "npm run dev"
        }

        It "Should return info for python-cli template" {
            $info = Get-TemplateInfo -TemplateType "python-cli"

            $info | Should -Not -BeNullOrEmpty
            $info.Name | Should -Be "Python CLI Tool"
            $info.InstallCmd | Should -Be 'pip install -e ".[dev]"'
        }

        It "Should return info for static-website template" {
            $info = Get-TemplateInfo -TemplateType "static-website"

            $info | Should -Not -BeNullOrEmpty
            $info.Name | Should -Be "Static Website"
            $info.InstallCmd | Should -BeNullOrEmpty
            $info.RunCmd | Should -Be "python -m http.server 8000"
        }

        It "Should return null for invalid template" {
            $info = Get-TemplateInfo -TemplateType "invalid-template"

            $info | Should -BeNullOrEmpty
        }
    }
}

Describe "Copy-TemplateWithSubstitution" {
    Context "When copying template files" {
        BeforeEach {
            # Create temporary destination
            $testDestDir = Join-Path ([System.IO.Path]::GetTempPath()) "naner-test-dest-$(Get-Random)"
        }

        AfterEach {
            # Clean up
            if (Test-Path $testDestDir) {
                Remove-Item -Path $testDestDir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "Should create destination directory" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "myproject"

            Test-Path $testDestDir | Should -Be $true
        }

        It "Should copy all files" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "myproject"

            Test-Path (Join-Path $testDestDir "README.md") | Should -Be $true
            Test-Path (Join-Path $testDestDir "package.json") | Should -Be $true
            Test-Path (Join-Path $testDestDir "src\index.js") | Should -Be $true
        }

        It "Should substitute {{PROJECT_NAME}} in file contents" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "myproject"

            $readmeContent = Get-Content (Join-Path $testDestDir "README.md") -Raw
            $readmeContent | Should -Match "# myproject"
            $readmeContent | Should -Match "A test project named myproject"
            $readmeContent | Should -Not -Match "\{\{PROJECT_NAME\}\}"
        }

        It "Should substitute {{PROJECT_NAME}} in JSON files" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "my-awesome-app"

            $packageContent = Get-Content (Join-Path $testDestDir "package.json") -Raw
            $packageContent | Should -Match '"name": "my-awesome-app"'
            $packageContent | Should -Not -Match "\{\{PROJECT_NAME\}\}"
        }

        It "Should substitute in nested directories" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "nested-project"

            $indexContent = Get-Content (Join-Path $testDestDir "src\index.js") -Raw
            $indexContent | Should -Match "console.log\('nested-project'\)"
            $indexContent | Should -Not -Match "\{\{PROJECT_NAME\}\}"
        }

        It "Should preserve directory structure" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "myproject"

            Test-Path (Join-Path $testDestDir "src") | Should -Be $true
            (Get-Item (Join-Path $testDestDir "src")).PSIsContainer | Should -Be $true
        }

        It "Should handle project names with special characters" {
            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "my-project_v2"

            $readmeContent = Get-Content (Join-Path $testDestDir "README.md") -Raw
            $readmeContent | Should -Match "# my-project_v2"
        }
    }

    Context "When handling edge cases" {
        BeforeEach {
            $testDestDir = Join-Path ([System.IO.Path]::GetTempPath()) "naner-test-dest-$(Get-Random)"

            # Create template with binary file
            $script:binaryTemplate = Join-Path $script:testTemplateDir "binary-template"
            New-Item -ItemType Directory -Path $script:binaryTemplate -Force | Out-Null

            # Create a small "binary" file (just a text file with no content for testing)
            $binaryFile = Join-Path $script:binaryTemplate "image.png"
            [System.IO.File]::WriteAllBytes($binaryFile, @(137, 80, 78, 71))  # PNG header bytes
        }

        AfterEach {
            if (Test-Path $testDestDir) {
                Remove-Item -Path $testDestDir -Recurse -Force -ErrorAction SilentlyContinue
            }
            if (Test-Path $script:binaryTemplate) {
                Remove-Item -Path $script:binaryTemplate -Recurse -Force -ErrorAction SilentlyContinue
            }
        }

        It "Should handle empty files" {
            $emptyFile = Join-Path $script:testTemplate ".gitignore"
            Set-Content -Path $emptyFile -Value ""

            Copy-TemplateWithSubstitution -SourcePath $script:testTemplate -DestPath $testDestDir -ProjectName "myproject"

            Test-Path (Join-Path $testDestDir ".gitignore") | Should -Be $true
        }

        It "Should copy binary files without modification" {
            Copy-TemplateWithSubstitution -SourcePath $script:binaryTemplate -DestPath $testDestDir -ProjectName "myproject"

            Test-Path (Join-Path $testDestDir "image.png") | Should -Be $true
            $bytes = [System.IO.File]::ReadAllBytes((Join-Path $testDestDir "image.png"))
            $bytes[0] | Should -Be 137  # PNG header preserved
        }
    }
}

Describe "Write-ProjectHeader" {
    Context "When displaying project header" {
        It "Should not throw errors" {
            { Write-ProjectHeader } | Should -Not -Throw
        }
    }
}

Describe "New-NanerProject Script Integration" {
    Context "When validating parameters" {
        It "Should require Type parameter" {
            # This test verifies the parameter is mandatory via ValidateSet
            $scriptContent = Get-Content $script:scriptPath -Raw

            $scriptContent | Should -Match '\[Parameter\(Mandatory=\$true\)\]'
            $scriptContent | Should -Match "ValidateSet\('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website'\)"
        }

        It "Should require Name parameter" {
            $scriptContent = Get-Content $script:scriptPath -Raw

            # Count how many Mandatory=$true parameters
            $mandatoryCount = ([regex]::Matches($scriptContent, '\[Parameter\(Mandatory=\$true\)\]')).Count
            $mandatoryCount | Should -BeGreaterThan 1  # Type and Name
        }

        It "Should have optional Path parameter with default" {
            $scriptContent = Get-Content $script:scriptPath -Raw

            $scriptContent | Should -Match '\[Parameter\(Mandatory=\$false\)\][\s\S]*?\[string\]\$Path = \$PWD'
        }

        It "Should have optional NoInstall switch" {
            $scriptContent = Get-Content $script:scriptPath -Raw

            $scriptContent | Should -Match '\[switch\]\$NoInstall'
        }
    }

    Context "When checking template existence" {
        It "Should verify all required templates exist" {
            $templates = @('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website')

            foreach ($template in $templates) {
                $templatePath = Join-Path $env:NANER_ROOT "home\Templates\$template"
                Test-Path $templatePath | Should -Be $true -Because "Template $template should exist"
            }
        }

        It "Should verify template README files exist" {
            $templates = @('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website')

            foreach ($template in $templates) {
                $readmePath = Join-Path $env:NANER_ROOT "home\Templates\$template\README.md"
                Test-Path $readmePath | Should -Be $true -Because "Template $template should have README"
            }
        }
    }
}

Describe "Template Content Validation" {
    Context "When checking template placeholders" {
        It "Should contain {{PROJECT_NAME}} placeholders in react-vite-ts" {
            $templatePath = Join-Path $env:NANER_ROOT "home\Templates\react-vite-ts"
            $readmePath = Join-Path $templatePath "README.md"

            if (Test-Path $readmePath) {
                $content = Get-Content $readmePath -Raw
                $content | Should -Match '\{\{PROJECT_NAME\}\}'
            }
        }

        It "Should contain {{PROJECT_NAME}} placeholders in nodejs-express-api" {
            $templatePath = Join-Path $env:NANER_ROOT "home\Templates\nodejs-express-api"
            $readmePath = Join-Path $templatePath "README.md"

            if (Test-Path $readmePath) {
                $content = Get-Content $readmePath -Raw
                $content | Should -Match '\{\{PROJECT_NAME\}\}'
            }
        }

        It "Should contain {{PROJECT_NAME}} placeholders in python-cli" {
            $templatePath = Join-Path $env:NANER_ROOT "home\Templates\python-cli"
            $readmePath = Join-Path $templatePath "README.md"

            if (Test-Path $readmePath) {
                $content = Get-Content $readmePath -Raw
                $content | Should -Match '\{\{PROJECT_NAME\}\}'
            }
        }

        It "Should contain {{PROJECT_NAME}} placeholders in static-website" {
            $templatePath = Join-Path $env:NANER_ROOT "home\Templates\static-website"
            $readmePath = Join-Path $templatePath "README.md"

            if (Test-Path $readmePath) {
                $content = Get-Content $readmePath -Raw
                $content | Should -Match '\{\{PROJECT_NAME\}\}'
            }
        }
    }

    Context "When checking required template files" {
        It "Should have .gitignore in each template" {
            $templates = @('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website')

            foreach ($template in $templates) {
                $gitignorePath = Join-Path $env:NANER_ROOT "home\Templates\$template\.gitignore"
                Test-Path $gitignorePath | Should -Be $true -Because "Template $template should have .gitignore"
            }
        }

        It "Should have README.md in each template" {
            $templates = @('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website')

            foreach ($template in $templates) {
                $readmePath = Join-Path $env:NANER_ROOT "home\Templates\$template\README.md"
                Test-Path $readmePath | Should -Be $true -Because "Template $template should have README.md"
            }
        }

        It "Should have package.json in npm-based templates" {
            $npmTemplates = @('react-vite-ts', 'nodejs-express-api')

            foreach ($template in $npmTemplates) {
                $packagePath = Join-Path $env:NANER_ROOT "home\Templates\$template\package.json"
                Test-Path $packagePath | Should -Be $true -Because "Template $template should have package.json"
            }
        }
    }
}
