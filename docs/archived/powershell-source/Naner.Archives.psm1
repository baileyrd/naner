<#
.SYNOPSIS
    Archive extraction utilities for Naner vendor setup.

.DESCRIPTION
    Provides functions for extracting various archive formats including:
    - ZIP files (using built-in Expand-Archive)
    - MSI files (using msiexec)
    - .tar.xz files (using 7-Zip or tar)
    - Other formats supported by 7-Zip

.NOTES
    This module is part of the Naner PowerShell framework.
    Requires: Common.psm1 for Write-* logging functions
#>

# Import Common module for logging
$commonModule = Join-Path $PSScriptRoot "Common.psm1"
if (Test-Path $commonModule) {
    Import-Module $commonModule -Force
}

function Get-FileWithProgress {
    <#
    .SYNOPSIS
        Downloads a file with progress indication and retry logic.

    .DESCRIPTION
        Downloads a file using .NET WebClient with visual progress bar and
        automatic retry on failure. Includes User-Agent header for compatibility.

    .PARAMETER Url
        URL of the file to download.

    .PARAMETER OutFile
        Destination path for the downloaded file.

    .PARAMETER MaxRetries
        Maximum number of download attempts. Default is 3.

    .OUTPUTS
        Boolean indicating whether the download was successful.

    .EXAMPLE
        $success = Get-FileWithProgress -Url "https://example.com/file.zip" -OutFile "C:\Downloads\file.zip"
        if ($success) {
            Write-Host "Download completed successfully"
        }

    .NOTES
        Uses approved PowerShell verb 'Get' instead of 'Download'.
        Includes Chrome User-Agent header for better compatibility.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$Url,

        [Parameter(Mandatory)]
        [string]$OutFile,

        [Parameter()]
        [int]$MaxRetries = 3
    )

    $attempt = 0
    $success = $false

    while ($attempt -lt $MaxRetries -and -not $success) {
        $attempt++

        try {
            if ($attempt -gt 1) {
                if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                    Write-Info "Retry attempt $attempt of $MaxRetries..."
                }
                Start-Sleep -Seconds 2
            }

            # Use .NET WebClient for better progress support
            $webClient = New-Object System.Net.WebClient
            $webClient.Headers.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/108.0.0.0 Safari/537.36")

            # Register progress event
            $progressEventHandler = {
                param($sender, $e)
                $percent = [int]($e.BytesReceived / $e.TotalBytesToReceive * 100)
                Write-Progress -Activity "Downloading" -Status "$percent% Complete" -PercentComplete $percent
            }

            Register-ObjectEvent -InputObject $webClient -EventName DownloadProgressChanged -SourceIdentifier WebClient.DownloadProgressChanged -Action $progressEventHandler | Out-Null

            # Start download
            $downloadTask = $webClient.DownloadFileTaskAsync($Url, $OutFile)
            $downloadTask.Wait()

            # Cleanup
            Unregister-Event -SourceIdentifier WebClient.DownloadProgressChanged -ErrorAction SilentlyContinue
            $webClient.Dispose()
            Write-Progress -Activity "Downloading" -Completed

            $success = $true
        }
        catch {
            Write-Warning "Download attempt $attempt failed: $($_.Exception.Message)"

            if ($attempt -lt $MaxRetries) {
                if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                    Write-Info "Retrying..."
                }
            }
            else {
                if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                    Write-Failure "Download failed after $MaxRetries attempts: $_"
                }
            }

            # Cleanup on error
            Unregister-Event -SourceIdentifier WebClient.DownloadProgressChanged -ErrorAction SilentlyContinue

            # Remove partial download
            if (Test-Path $OutFile) {
                Remove-Item $OutFile -Force -ErrorAction SilentlyContinue
            }
        }
    }

    return $success
}

function Get-SevenZipPath {
    <#
    .SYNOPSIS
        Locates an available 7-Zip executable.

    .DESCRIPTION
        Searches for 7-Zip in the following order:
        1. Vendored 7-Zip in vendor/7zip directory
        2. System-installed 7-Zip in Program Files
        3. Chocolatey-installed 7-Zip

    .PARAMETER VendorDir
        Path to the vendor directory to check for vendored 7-Zip.

    .OUTPUTS
        Hashtable with Path and Source properties, or $null if not found.

    .EXAMPLE
        $sevenZip = Get-SevenZipPath -VendorDir "C:\Naner\vendor"
        if ($sevenZip) {
            Write-Host "Found 7-Zip: $($sevenZip.Path) (source: $($sevenZip.Source))"
        }
    #>
    [CmdletBinding()]
    param(
        [Parameter()]
        [string]$VendorDir = ""
    )

    # First, check if we have vendored 7-Zip
    $vendoredSevenZip = if ($VendorDir) { Join-Path $VendorDir "7zip\7z.exe" } else { "" }

    if ($vendoredSevenZip -and (Test-Path $vendoredSevenZip)) {
        return @{
            Path = $vendoredSevenZip
            Source = "vendored"
        }
    }

    # Try system 7-Zip
    $sevenZipPaths = @(
        "${env:ProgramFiles}\7-Zip\7z.exe",
        "${env:ProgramFiles(x86)}\7-Zip\7z.exe",
        "$env:ProgramData\chocolatey\bin\7z.exe"
    )

    $sevenZip = $sevenZipPaths | Where-Object { Test-Path $_ } | Select-Object -First 1

    if ($sevenZip) {
        return @{
            Path = $sevenZip
            Source = "system"
        }
    }

    return $null
}

function Expand-ArchiveWith7Zip {
    <#
    .SYNOPSIS
        Extracts archives using 7-Zip.

    .DESCRIPTION
        Uses 7-Zip to extract various archive formats. Handles .tar.xz files
        with two-stage extraction (decompress .xz, then extract .tar).

    .PARAMETER ArchivePath
        Path to the archive file to extract.

    .PARAMETER DestinationPath
        Directory where files should be extracted.

    .PARAMETER VendorDir
        Path to vendor directory to locate vendored 7-Zip.

    .OUTPUTS
        Boolean indicating success or failure.

    .EXAMPLE
        $success = Expand-ArchiveWith7Zip -ArchivePath "C:\Downloads\file.tar.xz" -DestinationPath "C:\Extract"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ArchivePath,

        [Parameter(Mandatory)]
        [string]$DestinationPath,

        [Parameter()]
        [string]$VendorDir = ""
    )

    $sevenZipInfo = Get-SevenZipPath -VendorDir $VendorDir

    if (-not $sevenZipInfo) {
        return $false
    }

    if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
        Write-Info "Using $($sevenZipInfo.Source) 7-Zip for extraction..."
    }

    $sevenZip = $sevenZipInfo.Path
    $extension = [System.IO.Path]::GetExtension($ArchivePath).ToLower()

    if ($extension -eq ".xz") {
        # Extract .xz to get .tar
        $tarPath = $ArchivePath -replace '\.xz$', ''
        & $sevenZip x "$ArchivePath" -o"$(Split-Path $ArchivePath)" -y | Out-Null

        if ($LASTEXITCODE -ne 0) {
            if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                Write-Failure "Failed to decompress .xz file"
            }
            return $false
        }

        # Extract .tar to destination
        & $sevenZip x "$tarPath" -o"$DestinationPath" -y | Out-Null

        if ($LASTEXITCODE -ne 0) {
            if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                Write-Failure "Failed to extract .tar file"
            }
            Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
            return $false
        }

        # Cleanup intermediate .tar file
        Remove-Item $tarPath -Force -ErrorAction SilentlyContinue
    }
    else {
        # Standard extraction for other formats
        & $sevenZip x "$ArchivePath" -o"$DestinationPath" -y | Out-Null

        if ($LASTEXITCODE -ne 0) {
            if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                Write-Failure "7-Zip extraction failed"
            }
            return $false
        }
    }

    return $true
}

function Expand-VendorArchive {
    <#
    .SYNOPSIS
        Extracts vendor dependency archives of various formats.

    .DESCRIPTION
        Intelligently extracts archives based on file extension:
        - .zip: Uses PowerShell's built-in Expand-Archive
        - .msi: Uses msiexec for administrative installation
        - .tar.xz: Uses 7-Zip or falls back to tar command
        - Other formats: Attempts extraction with 7-Zip

    .PARAMETER ArchivePath
        Path to the archive file to extract.

    .PARAMETER DestinationPath
        Directory where files should be extracted.

    .PARAMETER VendorDir
        Path to vendor directory (used to locate vendored 7-Zip).

    .OUTPUTS
        Boolean indicating success or failure.

    .EXAMPLE
        $success = Expand-VendorArchive -ArchivePath "C:\Downloads\app.zip" -DestinationPath "C:\Apps\MyApp"

    .EXAMPLE
        $success = Expand-VendorArchive -ArchivePath "C:\Downloads\tool.msi" -DestinationPath "C:\Tools\MyTool" -VendorDir "C:\Naner\vendor"
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)]
        [string]$ArchivePath,

        [Parameter(Mandatory)]
        [string]$DestinationPath,

        [Parameter()]
        [string]$VendorDir = ""
    )

    $extension = [System.IO.Path]::GetExtension($ArchivePath).ToLower()

    if ($extension -eq ".zip") {
        # Use built-in PowerShell cmdlet for ZIP files
        Expand-Archive -Path $ArchivePath -DestinationPath $DestinationPath -Force
    }
    elseif ($extension -eq ".msi") {
        # Extract MSI using msiexec (Windows built-in)
        if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
            Write-Info "Extracting MSI using msiexec..."
        }

        # Create temp directory for extraction
        $tempExtract = Join-Path ([System.IO.Path]::GetTempPath()) "naner_msi_$([Guid]::NewGuid())"
        New-Item -Path $tempExtract -ItemType Directory -Force | Out-Null

        # Use msiexec to extract files (administrative install)
        $msiArgs = @(
            "/a",
            "`"$ArchivePath`"",
            "/qn",
            "TARGETDIR=`"$tempExtract`""
        )

        $process = Start-Process -FilePath "msiexec.exe" -ArgumentList $msiArgs -Wait -PassThru -NoNewWindow

        if ($process.ExitCode -eq 0) {
            # Find the Files directory (MSI extracts to Files/ProgramFiles/7-Zip)
            $filesDir = Get-ChildItem $tempExtract -Recurse -Directory |
                Where-Object { $_.Name -eq "7-Zip" } |
                Select-Object -First 1

            if ($filesDir) {
                # Copy extracted files to destination
                Copy-Item "$($filesDir.FullName)\*" -Destination $DestinationPath -Recurse -Force
            }
            else {
                # Fallback: copy everything from temp
                Copy-Item "$tempExtract\*" -Destination $DestinationPath -Recurse -Force
            }

            # Cleanup temp directory
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
        }
        else {
            if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                Write-Failure "MSI extraction failed with exit code: $($process.ExitCode)"
            }
            Remove-Item $tempExtract -Recurse -Force -ErrorAction SilentlyContinue
            return $false
        }
    }
    elseif ($extension -eq ".xz") {
        # For .tar.xz files, try 7-Zip first, then fallback to tar
        $success = Expand-ArchiveWith7Zip -ArchivePath $ArchivePath -DestinationPath $DestinationPath -VendorDir $VendorDir

        if (-not $success) {
            # Try tar with Unix-style path (last resort)
            if (Get-Command tar -ErrorAction SilentlyContinue) {
                if (Get-Command Write-Info -ErrorAction SilentlyContinue) {
                    Write-Info "Using tar for extraction..."
                }
                Write-Warning "This may fail on some systems. Consider letting setup complete 7-Zip installation first."

                # Create destination directory first
                New-Item -Path $DestinationPath -ItemType Directory -Force | Out-Null

                # Change to destination directory and extract
                $currentDir = Get-Location
                Set-Location $DestinationPath

                & tar -xf "$ArchivePath" 2>&1 | Out-Null
                $tarExitCode = $LASTEXITCODE

                Set-Location $currentDir

                if ($tarExitCode -ne 0) {
                    if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                        Write-Failure "tar extraction failed"
                    }
                    return $false
                }
            }
            else {
                if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
                    Write-Failure "Cannot extract .tar.xz files - 7-Zip not available"
                }
                Write-Host ""
                Write-Host "This shouldn't happen as 7-Zip is downloaded first." -ForegroundColor Yellow
                Write-Host "Try re-running the setup script." -ForegroundColor Yellow
                return $false
            }
        }
    }
    else {
        if (Get-Command Write-Failure -ErrorAction SilentlyContinue) {
            Write-Failure "Unsupported archive format: $extension"
        }
        return $false
    }

    return $true
}

# Export module functions
Export-ModuleMember -Function @(
    'Get-FileWithProgress',
    'Get-SevenZipPath',
    'Expand-ArchiveWith7Zip',
    'Expand-VendorArchive'
)
