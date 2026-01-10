<#
.SYNOPSIS
    Create a new project from Naner templates.

.DESCRIPTION
    Scaffolds a new project from pre-built Naner templates with automatic
    variable substitution and dependency installation.

.PARAMETER Type
    The type of project to create. Available types:
    - react-vite-ts: React + Vite + TypeScript
    - nodejs-express-api: Node.js REST API with Express
    - python-cli: Python CLI tool with Click
    - static-website: HTML/CSS/JS static site

.PARAMETER Name
    The name of the project (used for directory and placeholders).

.PARAMETER Path
    Optional path where the project should be created. Defaults to current directory.

.PARAMETER NoInstall
    Skip automatic dependency installation.

.EXAMPLE
    New-NanerProject -Type react-vite-ts -Name my-app
    Creates a React + Vite + TypeScript project in ./my-app/

.EXAMPLE
    New-NanerProject -Type nodejs-express-api -Name my-api -Path C:\Projects
    Creates a Node.js Express API in C:\Projects\my-api/

.EXAMPLE
    New-NanerProject -Type python-cli -Name mytool -NoInstall
    Creates a Python CLI project without installing dependencies
#>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('react-vite-ts', 'nodejs-express-api', 'python-cli', 'static-website')]
    [string]$Type,

    [Parameter(Mandatory=$true)]
    [string]$Name,

    [Parameter(Mandatory=$false)]
    [string]$Path = $PWD,

    [Parameter(Mandatory=$false)]
    [switch]$NoInstall
)

# Import common utilities - REQUIRED
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$commonModule = Join-Path $scriptDir "Common.psm1"
if (-not (Test-Path $commonModule)) {
    throw "Common.psm1 module not found at: $commonModule`nThis module is required for New-NanerProject.ps1 to function."
}

Import-Module $commonModule -Force

function Write-ProjectHeader {
    Write-Host ""
    Write-Host "╔════════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "║          Naner Project Generator               ║" -ForegroundColor Cyan
    Write-Host "╚════════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
}

function Get-TemplateInfo {
    param([string]$TemplateType)

    $templates = @{
        'react-vite-ts' = @{
            Name = 'React + Vite + TypeScript'
            Description = 'Modern React app with TypeScript and Vite'
            InstallCmd = 'npm install'
            RunCmd = 'npm run dev'
        }
        'nodejs-express-api' = @{
            Name = 'Node.js Express API'
            Description = 'REST API with Express.js'
            InstallCmd = 'npm install'
            RunCmd = 'npm run dev'
        }
        'python-cli' = @{
            Name = 'Python CLI Tool'
            Description = 'CLI application with Click and Rich'
            InstallCmd = 'pip install -e ".[dev]"'
            RunCmd = $Name
        }
        'static-website' = @{
            Name = 'Static Website'
            Description = 'HTML/CSS/JavaScript static site'
            InstallCmd = $null
            RunCmd = 'python -m http.server 8000'
        }
    }

    return $templates[$TemplateType]
}

function Copy-TemplateWithSubstitution {
    param(
        [string]$SourcePath,
        [string]$DestPath,
        [string]$ProjectName
    )

    # Create destination directory
    if (-not (Test-Path $DestPath)) {
        New-Item -ItemType Directory -Path $DestPath -Force | Out-Null
    }

    # Get all files and directories
    $items = Get-ChildItem -Path $SourcePath -Recurse

    foreach ($item in $items) {
        # Calculate relative path
        $relativePath = $item.FullName.Substring($SourcePath.Length + 1)

        # Substitute {{PROJECT_NAME}} in path
        $relativePath = $relativePath -replace '\{\{PROJECT_NAME\}\}', $ProjectName

        $destItemPath = Join-Path $DestPath $relativePath

        if ($item.PSIsContainer) {
            # Create directory
            if (-not (Test-Path $destItemPath)) {
                New-Item -ItemType Directory -Path $destItemPath -Force | Out-Null
            }
        } else {
            # Create parent directory if needed
            $destItemDir = Split-Path $destItemPath -Parent
            if (-not (Test-Path $destItemDir)) {
                New-Item -ItemType Directory -Path $destItemDir -Force | Out-Null
            }

            # Read file content
            $content = Get-Content -Path $item.FullName -Raw -ErrorAction SilentlyContinue

            if ($content) {
                # Substitute {{PROJECT_NAME}} in content
                $content = $content -replace '\{\{PROJECT_NAME\}\}', $ProjectName

                # Write to destination
                Set-Content -Path $destItemPath -Value $content -NoNewline
            } else {
                # Binary file or empty file, just copy
                Copy-Item -Path $item.FullName -Destination $destItemPath -Force
            }
        }
    }
}

# Main script
Write-ProjectHeader

# Get template info
$templateInfo = Get-TemplateInfo -TemplateType $Type

Write-Host "📦 Creating new project..." -ForegroundColor Cyan
Write-Host "   Template: $($templateInfo.Name)" -ForegroundColor Gray
Write-Host "   Name: $Name" -ForegroundColor Gray
Write-Host "   Location: $Path" -ForegroundColor Gray
Write-Host ""

# Determine template source
$nanerRoot = $env:NANER_ROOT
if (-not $nanerRoot) {
    $nanerRoot = Get-NanerRootSimple -ScriptRoot $scriptDir
}

$templateSource = Join-Path $nanerRoot "home\Templates\$Type"

if (-not (Test-Path $templateSource)) {
    Write-Error "Template not found: $templateSource"
    exit 1
}

# Create destination path
$projectPath = Join-Path $Path $Name

if (Test-Path $projectPath) {
    Write-Error "Directory already exists: $projectPath"
    Write-Host "Please choose a different name or remove the existing directory." -ForegroundColor Yellow
    exit 1
}

# Copy template with substitution
try {
    Write-Host "📋 Copying template files..." -ForegroundColor Cyan
    Copy-TemplateWithSubstitution -SourcePath $templateSource -DestPath $projectPath -ProjectName $Name
    Write-Success "✓ Template files copied"
} catch {
    Write-Error "Failed to copy template: $_"
    exit 1
}

# Install dependencies if not skipped
if (-not $NoInstall -and $templateInfo.InstallCmd) {
    Write-Host ""
    Write-Host "📦 Installing dependencies..." -ForegroundColor Cyan

    Push-Location $projectPath
    try {
        $installResult = Invoke-Expression $templateInfo.InstallCmd 2>&1
        Write-Success "✓ Dependencies installed"
    } catch {
        Write-Warning "Failed to install dependencies: $_"
        Write-Host "You can install them manually by running: $($templateInfo.InstallCmd)" -ForegroundColor Yellow
    } finally {
        Pop-Location
    }
}

# Show success message
Write-Host ""
Write-Success "🎉 Project created successfully!"
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  cd $Name" -ForegroundColor Gray

if ($templateInfo.InstallCmd -and $NoInstall) {
    Write-Host "  $($templateInfo.InstallCmd)" -ForegroundColor Gray
}

Write-Host "  $($templateInfo.RunCmd)" -ForegroundColor Gray
Write-Host ""
