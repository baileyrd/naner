<#
.SYNOPSIS
    Environment management module for Naner.

.DESCRIPTION
    Provides functionality to create, manage, and switch between multiple isolated
    Naner environments. Each environment has its own configuration, home directory,
    and isolated settings.
#>

#region Helper Functions

function Get-EnvironmentPath {
    <#
    .SYNOPSIS
        Gets the path to an environment's home directory.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$EnvironmentName,

        [Parameter(Mandatory)]
        [string]$NanerRoot
    )

    if ($EnvironmentName -eq "default") {
        return Join-Path $NanerRoot "home"
    }

    return Join-Path $NanerRoot "home\environments\$EnvironmentName"
}

function Get-EnvironmentConfigPath {
    <#
    .SYNOPSIS
        Gets the path to an environment's configuration file.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$EnvironmentName,

        [Parameter(Mandatory)]
        [string]$NanerRoot
    )

    $envPath = Get-EnvironmentPath -EnvironmentName $EnvironmentName -NanerRoot $NanerRoot
    return Join-Path $envPath ".naner-env.json"
}

function Get-ActiveEnvironmentPath {
    <#
    .SYNOPSIS
        Gets the path to the active environment file.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$NanerRoot
    )

    return Join-Path $NanerRoot "config\active-environment.txt"
}

#endregion

#region Public Functions

function New-NanerEnvironment {
    <#
    .SYNOPSIS
        Creates a new Naner environment.

    .DESCRIPTION
        Creates a new isolated environment with its own home directory and configuration.
        Optionally copies settings from an existing environment.

    .PARAMETER Name
        Name of the environment to create.

    .PARAMETER Description
        Optional description of the environment's purpose.

    .PARAMETER CopyFrom
        Name of an existing environment to copy settings from.

    .PARAMETER NanerRoot
        Path to Naner root directory. Defaults to auto-detection.

    .EXAMPLE
        New-NanerEnvironment -Name "work" -Description "Work projects"

    .EXAMPLE
        New-NanerEnvironment -Name "personal" -CopyFrom "default"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [ValidatePattern('^[a-zA-Z0-9_-]+$')]
        [string]$Name,

        [Parameter()]
        [string]$Description = "",

        [Parameter()]
        [string]$CopyFrom = "",

        [Parameter()]
        [string]$NanerRoot = ""
    )

    # Validate environment name
    if ($Name -eq "default") {
        throw "Cannot create an environment named 'default'. This is reserved for the base environment."
    }

    # Find Naner root
    if (-not $NanerRoot) {
        $commonModule = Join-Path $PSScriptRoot "Common.psm1"
        if (Test-Path $commonModule) {
            Import-Module $commonModule -Force
            $NanerRoot = Find-NanerRoot
        }
        else {
            throw "Cannot locate Naner root. Please specify -NanerRoot parameter."
        }
    }

    # Check if environment already exists
    $envPath = Get-EnvironmentPath -EnvironmentName $Name -NanerRoot $NanerRoot
    if (Test-Path $envPath) {
        throw "Environment '$Name' already exists at: $envPath"
    }

    Write-Host "Creating environment: $Name" -ForegroundColor Cyan
    Write-Host "Location: $envPath" -ForegroundColor Gray

    # Create environment directory
    New-Item -Path $envPath -ItemType Directory -Force | Out-Null

    # Create environment metadata
    $metadata = @{
        name = $Name
        description = $Description
        createdAt = (Get-Date -Format "o")
        version = "1.0"
    }

    # If copying from another environment
    if ($CopyFrom) {
        Write-Host "Copying from environment: $CopyFrom" -ForegroundColor Gray

        $sourceEnvPath = Get-EnvironmentPath -EnvironmentName $CopyFrom -NanerRoot $NanerRoot

        if (-not (Test-Path $sourceEnvPath)) {
            Remove-Item -Path $envPath -Recurse -Force
            throw "Source environment '$CopyFrom' not found at: $sourceEnvPath"
        }

        # Copy configuration files (excluding caches and binaries)
        $itemsToCopy = @(
            ".gitconfig",
            ".bashrc",
            ".bash_profile",
            ".bash_aliases",
            ".ssh\config",
            ".config",
            ".vscode",
            "Documents\PowerShell\profile.ps1",
            "Documents\PowerShell\Modules"
        )

        foreach ($item in $itemsToCopy) {
            $sourcePath = Join-Path $sourceEnvPath $item
            $destPath = Join-Path $envPath $item

            if (Test-Path $sourcePath) {
                $destDir = Split-Path $destPath -Parent
                if (-not (Test-Path $destDir)) {
                    New-Item -Path $destDir -ItemType Directory -Force | Out-Null
                }

                if ((Get-Item $sourcePath).PSIsContainer) {
                    Copy-Item -Path $sourcePath -Destination $destPath -Recurse -Force
                    Write-Host "  Copied: $item\" -ForegroundColor Green
                }
                else {
                    Copy-Item -Path $sourcePath -Destination $destPath -Force
                    Write-Host "  Copied: $item" -ForegroundColor Green
                }
            }
        }

        $metadata.copiedFrom = $CopyFrom
    }
    else {
        # Create basic directory structure
        Write-Host "Creating directory structure..." -ForegroundColor Gray

        $directories = @(
            ".ssh",
            ".config\powershell",
            ".config\windows-terminal",
            "Documents\PowerShell\Modules",
            ".vscode",
            "go\bin",
            "go\pkg",
            "go\src",
            ".cargo",
            ".rustup",
            ".gem",
            ".npm-global",
            ".npm-cache",
            ".conda\pkgs",
            ".conda\envs",
            ".local"
        )

        foreach ($dir in $directories) {
            $dirPath = Join-Path $envPath $dir
            if (-not (Test-Path $dirPath)) {
                New-Item -Path $dirPath -ItemType Directory -Force | Out-Null
            }
        }

        Write-Host "  Created base directory structure" -ForegroundColor Green
    }

    # Save environment metadata
    $configPath = Get-EnvironmentConfigPath -EnvironmentName $Name -NanerRoot $NanerRoot
    $metadata | ConvertTo-Json -Depth 10 | Set-Content -Path $configPath -Encoding UTF8

    Write-Host ""
    Write-Host "Environment '$Name' created successfully!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Cyan
    Write-Host "  1. Switch to this environment:" -ForegroundColor Gray
    Write-Host "     Use-NanerEnvironment -Name '$Name'" -ForegroundColor White
    Write-Host "  2. Launch Naner with this environment:" -ForegroundColor Gray
    Write-Host "     .\Invoke-Naner.ps1 -Environment '$Name'" -ForegroundColor White
    Write-Host ""
}

function Use-NanerEnvironment {
    <#
    .SYNOPSIS
        Switches the active Naner environment.

    .DESCRIPTION
        Sets the specified environment as the active environment. All subsequent
        Naner launches will use this environment unless overridden.

    .PARAMETER Name
        Name of the environment to activate.

    .PARAMETER NanerRoot
        Path to Naner root directory. Defaults to auto-detection.

    .EXAMPLE
        Use-NanerEnvironment -Name "work"

    .EXAMPLE
        Use-NanerEnvironment -Name "default"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter()]
        [string]$NanerRoot = ""
    )

    # Find Naner root
    if (-not $NanerRoot) {
        $commonModule = Join-Path $PSScriptRoot "Common.psm1"
        if (Test-Path $commonModule) {
            Import-Module $commonModule -Force
            $NanerRoot = Find-NanerRoot
        }
        else {
            throw "Cannot locate Naner root. Please specify -NanerRoot parameter."
        }
    }

    # Verify environment exists
    $envPath = Get-EnvironmentPath -EnvironmentName $Name -NanerRoot $NanerRoot
    if (-not (Test-Path $envPath)) {
        throw "Environment '$Name' not found at: $envPath"
    }

    # Set as active environment
    $activeEnvFile = Get-ActiveEnvironmentPath -NanerRoot $NanerRoot
    $Name | Set-Content -Path $activeEnvFile -Encoding UTF8 -Force

    Write-Host "Active environment: $Name" -ForegroundColor Green
    Write-Host "Path: $envPath" -ForegroundColor Gray
    Write-Host ""
    Write-Host "Launch Naner to use this environment:" -ForegroundColor Cyan
    Write-Host "  .\Invoke-Naner.ps1" -ForegroundColor White
    Write-Host ""
}

function Get-NanerEnvironment {
    <#
    .SYNOPSIS
        Lists all Naner environments or gets details about a specific environment.

    .DESCRIPTION
        Without parameters, lists all available environments and shows which is active.
        With -Name parameter, shows detailed information about a specific environment.

    .PARAMETER Name
        Optional name of a specific environment to get details for.

    .PARAMETER NanerRoot
        Path to Naner root directory. Defaults to auto-detection.

    .EXAMPLE
        Get-NanerEnvironment

    .EXAMPLE
        Get-NanerEnvironment -Name "work"
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$Name = "",

        [Parameter()]
        [string]$NanerRoot = ""
    )

    # Find Naner root
    if (-not $NanerRoot) {
        $commonModule = Join-Path $PSScriptRoot "Common.psm1"
        if (Test-Path $commonModule) {
            Import-Module $commonModule -Force
            $NanerRoot = Find-NanerRoot
        }
        else {
            throw "Cannot locate Naner root. Please specify -NanerRoot parameter."
        }
    }

    # Get active environment
    $activeEnvFile = Get-ActiveEnvironmentPath -NanerRoot $NanerRoot
    $activeEnv = if (Test-Path $activeEnvFile) {
        (Get-Content $activeEnvFile -Raw).Trim()
    }
    else {
        "default"
    }

    # If specific environment requested
    if ($Name) {
        $envPath = Get-EnvironmentPath -EnvironmentName $Name -NanerRoot $NanerRoot

        if (-not (Test-Path $envPath)) {
            throw "Environment '$Name' not found at: $envPath"
        }

        $configPath = Get-EnvironmentConfigPath -EnvironmentName $Name -NanerRoot $NanerRoot

        Write-Host "Environment: $Name" -ForegroundColor Cyan
        Write-Host "Path: $envPath" -ForegroundColor Gray
        Write-Host "Active: $($Name -eq $activeEnv)" -ForegroundColor $(if ($Name -eq $activeEnv) { "Green" } else { "Gray" })

        if (Test-Path $configPath) {
            $metadata = Get-Content $configPath -Raw | ConvertFrom-Json

            if ($metadata.description) {
                Write-Host "Description: $($metadata.description)" -ForegroundColor Gray
            }

            Write-Host "Created: $($metadata.createdAt)" -ForegroundColor Gray

            if ($metadata.copiedFrom) {
                Write-Host "Copied from: $($metadata.copiedFrom)" -ForegroundColor Gray
            }
        }

        Write-Host ""
        return
    }

    # List all environments
    Write-Host "Naner Environments" -ForegroundColor Cyan
    Write-Host "==================" -ForegroundColor Cyan
    Write-Host ""

    # Always show default environment
    $defaultPath = Get-EnvironmentPath -EnvironmentName "default" -NanerRoot $NanerRoot
    $isActive = ($activeEnv -eq "default")
    $marker = if ($isActive) { "* " } else { "  " }

    Write-Host "$marker" -NoNewline -ForegroundColor Green
    Write-Host "default" -NoNewline -ForegroundColor $(if ($isActive) { "Green" } else { "White" })
    Write-Host " - Base environment" -ForegroundColor Gray

    # List custom environments
    $envsDir = Join-Path $NanerRoot "home\environments"

    if (Test-Path $envsDir) {
        $envs = Get-ChildItem -Path $envsDir -Directory | Sort-Object Name

        foreach ($env in $envs) {
            $envName = $env.Name
            $configPath = Get-EnvironmentConfigPath -EnvironmentName $envName -NanerRoot $NanerRoot

            $isActive = ($activeEnv -eq $envName)
            $marker = if ($isActive) { "* " } else { "  " }

            Write-Host "$marker" -NoNewline -ForegroundColor Green
            Write-Host "$envName" -NoNewline -ForegroundColor $(if ($isActive) { "Green" } else { "White" })

            if (Test-Path $configPath) {
                $metadata = Get-Content $configPath -Raw | ConvertFrom-Json
                if ($metadata.description) {
                    Write-Host " - $($metadata.description)" -ForegroundColor Gray
                }
                else {
                    Write-Host ""
                }
            }
            else {
                Write-Host ""
            }
        }
    }

    Write-Host ""
    Write-Host "* = Active environment" -ForegroundColor Gray
    Write-Host ""
}

function Remove-NanerEnvironment {
    <#
    .SYNOPSIS
        Removes a Naner environment.

    .DESCRIPTION
        Deletes an environment and all its associated files. Cannot remove the
        'default' environment or the currently active environment.

    .PARAMETER Name
        Name of the environment to remove.

    .PARAMETER Force
        Skip confirmation prompt.

    .PARAMETER NanerRoot
        Path to Naner root directory. Defaults to auto-detection.

    .EXAMPLE
        Remove-NanerEnvironment -Name "old-project"

    .EXAMPLE
        Remove-NanerEnvironment -Name "temp" -Force
    #>
    [CmdletBinding(SupportsShouldProcess)]
    param(
        [Parameter(Mandatory)]
        [string]$Name,

        [Parameter()]
        [switch]$Force,

        [Parameter()]
        [string]$NanerRoot = ""
    )

    # Validate environment name
    if ($Name -eq "default") {
        throw "Cannot remove the 'default' environment."
    }

    # Find Naner root
    if (-not $NanerRoot) {
        $commonModule = Join-Path $PSScriptRoot "Common.psm1"
        if (Test-Path $commonModule) {
            Import-Module $commonModule -Force
            $NanerRoot = Find-NanerRoot
        }
        else {
            throw "Cannot locate Naner root. Please specify -NanerRoot parameter."
        }
    }

    # Check if environment exists
    $envPath = Get-EnvironmentPath -EnvironmentName $Name -NanerRoot $NanerRoot
    if (-not (Test-Path $envPath)) {
        throw "Environment '$Name' not found at: $envPath"
    }

    # Check if environment is active
    $activeEnvFile = Get-ActiveEnvironmentPath -NanerRoot $NanerRoot
    $activeEnv = if (Test-Path $activeEnvFile) {
        (Get-Content $activeEnvFile -Raw).Trim()
    }
    else {
        "default"
    }

    if ($activeEnv -eq $Name) {
        throw "Cannot remove the currently active environment. Switch to another environment first using Use-NanerEnvironment."
    }

    # Confirm deletion
    if (-not $Force) {
        Write-Host "WARNING: This will permanently delete the environment '$Name' and all its files." -ForegroundColor Yellow
        Write-Host "Path: $envPath" -ForegroundColor Gray
        Write-Host ""

        $response = Read-Host "Are you sure you want to continue? (y/N)"

        if ($response -ne 'y' -and $response -ne 'Y') {
            Write-Host "Operation cancelled." -ForegroundColor Gray
            return
        }
    }

    # Remove environment
    if ($PSCmdlet.ShouldProcess($Name, "Remove environment")) {
        Write-Host "Removing environment: $Name" -ForegroundColor Yellow

        try {
            Remove-Item -Path $envPath -Recurse -Force
            Write-Host "Environment '$Name' removed successfully." -ForegroundColor Green
        }
        catch {
            throw "Failed to remove environment: $_"
        }
    }
}

function Get-ActiveNanerEnvironment {
    <#
    .SYNOPSIS
        Gets the name of the currently active environment.

    .PARAMETER NanerRoot
        Path to Naner root directory. Defaults to auto-detection.

    .EXAMPLE
        $activeEnv = Get-ActiveNanerEnvironment
    #>
    [CmdletBinding()]
    [OutputType([string])]
    param(
        [Parameter()]
        [string]$NanerRoot = ""
    )

    # Find Naner root
    if (-not $NanerRoot) {
        $commonModule = Join-Path $PSScriptRoot "Common.psm1"
        if (Test-Path $commonModule) {
            Import-Module $commonModule -Force
            $NanerRoot = Find-NanerRoot
        }
        else {
            throw "Cannot locate Naner root. Please specify -NanerRoot parameter."
        }
    }

    $activeEnvFile = Get-ActiveEnvironmentPath -NanerRoot $NanerRoot

    if (Test-Path $activeEnvFile) {
        return (Get-Content $activeEnvFile -Raw).Trim()
    }

    return "default"
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    'New-NanerEnvironment',
    'Use-NanerEnvironment',
    'Get-NanerEnvironment',
    'Remove-NanerEnvironment',
    'Get-ActiveNanerEnvironment'
)
