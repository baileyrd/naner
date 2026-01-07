<#
.SYNOPSIS
    Structured error code system for Naner.

.DESCRIPTION
    Provides consistent error codes, messages, and handling across all Naner scripts.
    Error codes are grouped by category for easy identification and troubleshooting.

.NOTES
    Error Code Format: NANER-XXXX
    - 1000-1999: General errors
    - 2000-2999: Configuration errors
    - 3000-3999: Network/Download errors
    - 4000-4999: Vendor installation errors
    - 5000-5999: File system errors
    - 6000-6999: Validation errors
#>

#region Error Code Definitions

# Error code catalog
$script:ErrorCatalog = @{
    # General Errors (1000-1999)
    'NANER-1001' = @{
        Message = 'Failed to locate Naner root directory'
        Category = 'General'
        Severity = 'Error'
        Resolution = 'Ensure you are running the script from within the Naner directory structure. The script looks for bin/, vendor/, and config/ directories.'
    }
    'NANER-1002' = @{
        Message = 'Invalid parameter combination'
        Category = 'General'
        Severity = 'Error'
        Resolution = 'Check the script usage and provide valid parameter combinations.'
    }
    'NANER-1003' = @{
        Message = 'Operation cancelled by user'
        Category = 'General'
        Severity = 'Warning'
        Resolution = 'Operation was cancelled. No changes were made.'
    }
    'NANER-1004' = @{
        Message = 'Insufficient permissions'
        Category = 'General'
        Severity = 'Error'
        Resolution = 'Run PowerShell with elevated permissions or adjust file/directory permissions.'
    }

    # Configuration Errors (2000-2999)
    'NANER-2001' = @{
        Message = 'Configuration file not found'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Ensure config/naner.json or config/vendors.json exists in the Naner directory.'
    }
    'NANER-2002' = @{
        Message = 'Invalid JSON in configuration file'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Validate JSON syntax using a JSON validator or editor with syntax highlighting.'
    }
    'NANER-2003' = @{
        Message = 'Missing required configuration field'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Check the configuration file against the schema (vendors-schema.json) for required fields.'
    }
    'NANER-2004' = @{
        Message = 'Invalid vendor configuration'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Verify the vendor configuration in vendors.json matches the expected schema.'
    }
    'NANER-2005' = @{
        Message = 'Vendor not found in configuration'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Check that the vendor ID exists in config/vendors.json.'
    }
    'NANER-2006' = @{
        Message = 'Lock file validation failed'
        Category = 'Configuration'
        Severity = 'Error'
        Resolution = 'Regenerate the lock file or verify its integrity.'
    }

    # Network/Download Errors (3000-3999)
    'NANER-3001' = @{
        Message = 'Network connection failed'
        Category = 'Network'
        Severity = 'Error'
        Resolution = 'Check internet connection and proxy settings. Verify URLs are accessible.'
    }
    'NANER-3002' = @{
        Message = 'Download failed after maximum retries'
        Category = 'Network'
        Severity = 'Error'
        Resolution = 'Check network connectivity. If problem persists, try manually downloading the file.'
    }
    'NANER-3003' = @{
        Message = 'GitHub API rate limit exceeded'
        Category = 'Network'
        Severity = 'Warning'
        Resolution = 'Wait for rate limit to reset (typically 1 hour) or use a GitHub API token.'
    }
    'NANER-3004' = @{
        Message = 'Invalid or unreachable URL'
        Category = 'Network'
        Severity = 'Error'
        Resolution = 'Verify the URL is correct and accessible. Check for typos or network restrictions.'
    }
    'NANER-3005' = @{
        Message = 'File hash mismatch'
        Category = 'Network'
        Severity = 'Error'
        Resolution = 'Delete the corrupted download and retry. If problem persists, the file source may be compromised.'
    }

    # Vendor Installation Errors (4000-4999)
    'NANER-4001' = @{
        Message = 'Vendor installation failed'
        Category = 'Installation'
        Severity = 'Error'
        Resolution = 'Check the installation logs for specific error details. Ensure sufficient disk space.'
    }
    'NANER-4002' = @{
        Message = 'Archive extraction failed'
        Category = 'Installation'
        Severity = 'Error'
        Resolution = 'Verify archive file is not corrupted. Ensure 7-Zip is properly installed.'
    }
    'NANER-4003' = @{
        Message = 'PostInstall function failed'
        Category = 'Installation'
        Severity = 'Warning'
        Resolution = 'Installation completed but post-installation configuration failed. Check vendor documentation.'
    }
    'NANER-4004' = @{
        Message = 'Vendor dependency not satisfied'
        Category = 'Installation'
        Severity = 'Error'
        Resolution = 'Install required dependencies first. Check vendor configuration for dependency list.'
    }
    'NANER-4005' = @{
        Message = 'Vendor already installed'
        Category = 'Installation'
        Severity = 'Info'
        Resolution = 'Use -ForceDownload to reinstall, or skip this vendor.'
    }
    'NANER-4006' = @{
        Message = 'Unsupported archive format'
        Category = 'Installation'
        Severity = 'Error'
        Resolution = 'Supported formats: .zip, .msi, .tar.xz, .7z. Contact maintainers for support of other formats.'
    }

    # File System Errors (5000-5999)
    'NANER-5001' = @{
        Message = 'Insufficient disk space'
        Category = 'FileSystem'
        Severity = 'Error'
        Resolution = 'Free up disk space. Vendor installations require several GB of storage.'
    }
    'NANER-5002' = @{
        Message = 'File or directory not found'
        Category = 'FileSystem'
        Severity = 'Error'
        Resolution = 'Verify the path exists and is accessible. Check for typos in paths.'
    }
    'NANER-5003' = @{
        Message = 'File access denied'
        Category = 'FileSystem'
        Severity = 'Error'
        Resolution = 'Check file permissions. Close any applications using the file.'
    }
    'NANER-5004' = @{
        Message = 'Directory creation failed'
        Category = 'FileSystem'
        Severity = 'Error'
        Resolution = 'Verify parent directory exists and you have write permissions.'
    }
    'NANER-5005' = @{
        Message = 'File deletion failed'
        Category = 'FileSystem'
        Severity = 'Warning'
        Resolution = 'File may be in use. Close applications and retry.'
    }

    # Validation Errors (6000-6999)
    'NANER-6001' = @{
        Message = 'Vendor path validation failed'
        Category = 'Validation'
        Severity = 'Warning'
        Resolution = 'One or more vendor paths do not exist. Run Setup-NanerVendor.ps1 to install missing vendors.'
    }
    'NANER-6002' = @{
        Message = 'Installation verification failed'
        Category = 'Validation'
        Severity = 'Error'
        Resolution = 'Expected files not found after installation. Retry installation or check for errors.'
    }
    'NANER-6003' = @{
        Message = 'Version mismatch detected'
        Category = 'Validation'
        Severity = 'Warning'
        Resolution = 'Installed version differs from expected. Update lock file or reinstall.'
    }
    'NANER-6004' = @{
        Message = 'SHA256 hash validation failed'
        Category = 'Validation'
        Severity = 'Error'
        Resolution = 'Downloaded file hash does not match expected value. Delete and re-download.'
    }
}

#endregion

#region Error Functions

function Get-NanerError {
    <#
    .SYNOPSIS
        Retrieves error information by error code.

    .PARAMETER ErrorCode
        The Naner error code (e.g., "NANER-2001").

    .OUTPUTS
        PSCustomObject with error details.

    .EXAMPLE
        $error = Get-NanerError -ErrorCode "NANER-2001"
        Write-Host $error.Message
    #>
    [CmdletBinding()]
    [OutputType([PSCustomObject])]
    param(
        [Parameter(Mandatory)]
        [string]$ErrorCode
    )

    if ($script:ErrorCatalog.ContainsKey($ErrorCode)) {
        $errorInfo = $script:ErrorCatalog[$ErrorCode]

        return [PSCustomObject]@{
            Code = $ErrorCode
            Message = $errorInfo.Message
            Category = $errorInfo.Category
            Severity = $errorInfo.Severity
            Resolution = $errorInfo.Resolution
        }
    }

    return [PSCustomObject]@{
        Code = 'NANER-0000'
        Message = 'Unknown error'
        Category = 'Unknown'
        Severity = 'Error'
        Resolution = 'Check logs for more information.'
    }
}

function Write-NanerError {
    <#
    .SYNOPSIS
        Writes a structured error message.

    .PARAMETER ErrorCode
        The Naner error code.

    .PARAMETER AdditionalInfo
        Optional additional context or details.

    .PARAMETER Exception
        Optional exception object for technical details.

    .PARAMETER ExitScript
        If true, exits the script with error code.

    .EXAMPLE
        Write-NanerError -ErrorCode "NANER-2001" -AdditionalInfo "Path: C:\config\naner.json"

    .EXAMPLE
        Write-NanerError -ErrorCode "NANER-3002" -ExitScript
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ErrorCode,

        [Parameter()]
        [string]$AdditionalInfo = "",

        [Parameter()]
        [System.Exception]$Exception = $null,

        [Parameter()]
        [switch]$ExitScript
    )

    $error = Get-NanerError -ErrorCode $ErrorCode

    # Write error header
    Write-Host ""
    Write-Host "ERROR $($error.Code): $($error.Message)" -ForegroundColor Red

    # Write additional info if provided
    if ($AdditionalInfo) {
        Write-Host "Details: $AdditionalInfo" -ForegroundColor Gray
    }

    # Write exception details if provided
    if ($Exception) {
        Write-Host "Exception: $($Exception.Message)" -ForegroundColor Gray

        if ($Exception.InnerException) {
            Write-Host "Inner Exception: $($Exception.InnerException.Message)" -ForegroundColor DarkGray
        }
    }

    # Write resolution
    Write-Host ""
    Write-Host "Resolution:" -ForegroundColor Yellow
    Write-Host "  $($error.Resolution)" -ForegroundColor Gray

    # Write documentation reference
    Write-Host ""
    Write-Host "For more help:" -ForegroundColor Cyan
    Write-Host "  Category: $($error.Category)" -ForegroundColor Gray
    Write-Host "  See: docs/ERROR-CODES.md#$($ErrorCode.ToLower())" -ForegroundColor Gray
    Write-Host ""

    # Exit if requested
    if ($ExitScript) {
        exit 1
    }
}

function Write-NanerWarning {
    <#
    .SYNOPSIS
        Writes a structured warning message.

    .PARAMETER ErrorCode
        The Naner error/warning code.

    .PARAMETER AdditionalInfo
        Optional additional context.

    .EXAMPLE
        Write-NanerWarning -ErrorCode "NANER-4005" -AdditionalInfo "Vendor: PowerShell"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ErrorCode,

        [Parameter()]
        [string]$AdditionalInfo = ""
    )

    $error = Get-NanerError -ErrorCode $ErrorCode

    Write-Host ""
    Write-Host "WARNING $($error.Code): $($error.Message)" -ForegroundColor Yellow

    if ($AdditionalInfo) {
        Write-Host "Details: $AdditionalInfo" -ForegroundColor Gray
    }

    Write-Host "Resolution: $($error.Resolution)" -ForegroundColor Gray
    Write-Host ""
}

function Get-AllNanerErrors {
    <#
    .SYNOPSIS
        Gets all defined error codes and their information.

    .OUTPUTS
        Array of error code information.

    .EXAMPLE
        Get-AllNanerErrors | Format-Table Code, Category, Severity
    #>
    [CmdletBinding()]
    param()

    $errors = @()

    foreach ($code in $script:ErrorCatalog.Keys | Sort-Object) {
        $errorInfo = $script:ErrorCatalog[$code]

        $errors += [PSCustomObject]@{
            Code = $code
            Message = $errorInfo.Message
            Category = $errorInfo.Category
            Severity = $errorInfo.Severity
            Resolution = $errorInfo.Resolution
        }
    }

    return $errors
}

#endregion

# Export functions
Export-ModuleMember -Function @(
    'Get-NanerError',
    'Write-NanerError',
    'Write-NanerWarning',
    'Get-AllNanerErrors'
)
