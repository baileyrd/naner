# Dynamic URL Fetching System

## Overview

The Naner vendor setup system uses **dynamic URL fetching** to automatically download the latest versions of all dependencies. This ensures users always get the most recent stable releases without requiring script updates.

## How It Works

### 1. GitHub API Integration

For PowerShell and Windows Terminal, the script uses the GitHub REST API:

```powershell
# Endpoint
https://api.github.com/repos/{owner}/{repo}/releases/latest

# PowerShell
https://api.github.com/repos/PowerShell/PowerShell/releases/latest

# Windows Terminal
https://api.github.com/repos/microsoft/terminal/releases/latest
```

**Process:**
1. Query the API for latest release
2. Parse JSON response to get version tag
3. Find matching asset (e.g., `*-win-x64.zip`)
4. Extract download URL
5. Use URL for download

**Advantages:**
- Always gets latest version
- Includes version number
- Gets exact file size
- Reliable official source

**API Limits:**
- Unauthenticated: 60 requests/hour per IP
- If exceeded, fallback URLs are used

### 2. Web Scraping for MSYS2

MSYS2 doesn't provide a formal API, so we scrape their repository:

```powershell
# Repository URL
https://repo.msys2.org/distrib/x86_64/

# Pattern
msys2-base-x86_64-YYYYMMDD.tar.xz
```

**Process:**
1. Fetch HTML directory listing
2. Use regex to find all base packages
3. Extract date from filename (YYYYMMDD format)
4. Sort by date (newest first)
5. Use most recent version

**Advantages:**
- Gets latest dated release
- No API dependencies
- Direct from official repository

### 3. Fallback System

Every dependency has a **fallback URL** in case dynamic fetching fails:

```powershell
$vendorConfig = @{
    PowerShell = @{
        GetLatestRelease = {
            $fallback = "https://github.com/.../PowerShell-7.4.6-win-x64.zip"
            return Get-LatestGitHubRelease ... -FallbackUrl $fallback
        }
    }
}
```

**When Fallbacks Are Used:**
- GitHub API rate limit exceeded
- Network connectivity issues
- API endpoint unavailable
- Regex pattern doesn't match

**Fallback Strategy:**
- Points to a recent stable version
- Guaranteed to work
- Users get a working installation
- Can manually update later

## Implementation Details

### GitHub Release Fetching

```powershell
function Get-LatestGitHubRelease {
    param(
        [string]$Repo,              # e.g., "PowerShell/PowerShell"
        [string]$AssetPattern,      # e.g., "*-win-x64.zip"
        [string]$FallbackUrl        # Fallback if API fails
    )
    
    try {
        # Query GitHub API
        $release = Invoke-RestMethod -Uri $apiUrl -Headers @{
            "User-Agent" = "Naner-Vendor-Setup"
            "Accept" = "application/vnd.github.v3+json"
        }
        
        # Find matching asset
        $asset = $release.assets | Where-Object { 
            $_.name -like $AssetPattern 
        } | Select-Object -First 1
        
        return @{
            Version = $release.tag_name
            Url = $asset.browser_download_url
            FileName = $asset.name
            Size = [math]::Round($asset.size / 1MB, 2)
        }
    }
    catch {
        # Use fallback
        return @{
            Version = "latest"
            Url = $FallbackUrl
            FileName = [System.IO.Path]::GetFileName($FallbackUrl)
        }
    }
}
```

### MSYS2 Scraping

```powershell
function Get-LatestMSYS2Release {
    param([string]$FallbackUrl)
    
    try {
        # Fetch directory listing
        $response = Invoke-WebRequest -Uri $baseUrl -UseBasicParsing
        
        # Regex pattern for base packages
        $pattern = 'href="(msys2-base-x86_64-(\d{8})\.tar\.xz)"'
        $matches = [regex]::Matches($response.Content, $pattern)
        
        # Get most recent by date
        $latest = $matches | 
            Sort-Object { $_.Groups[2].Value } -Descending | 
            Select-Object -First 1
        
        return @{
            Version = $latest.Groups[2].Value  # YYYYMMDD
            Url = $baseUrl + $latest.Groups[1].Value
            FileName = $latest.Groups[1].Value
        }
    }
    catch {
        # Use fallback
        return @{ ... }
    }
}
```

## Advantages of Dynamic Fetching

### For Users
1. **Always Current**: Get latest features and security fixes
2. **No Script Updates**: Script doesn't need version bumps
3. **Transparent**: See exactly what version is being installed
4. **Reliable**: Fallbacks ensure it always works

### For Maintainers
1. **Less Maintenance**: Don't need to update URLs
2. **Automatic**: New releases work immediately
3. **Flexible**: Easy to add new dependencies
4. **Robust**: Multiple failure modes handled

## Current Fallback Versions

These are used if dynamic fetching fails:

| Dependency | Fallback Version | Date |
|------------|------------------|------|
| PowerShell | 7.4.6 | Recent stable |
| Windows Terminal | 1.21.2361.0 | Recent stable |
| MSYS2 | 20240727 | July 2024 |

**Note**: Fallback versions are periodically updated to recent stable releases.

## Monitoring & Troubleshooting

### Check What Was Downloaded

After running `Setup-NanerVendor.ps1`, check the manifest:

```powershell
Get-Content vendor\vendor-manifest.json | ConvertFrom-Json
```

Output:
```json
{
  "Dependencies": {
    "PowerShell": {
      "Version": "v7.4.6",
      "DownloadUrl": "https://github.com/.../PowerShell-7.4.6-win-x64.zip",
      "InstalledDate": "2024-01-06 12:00:00"
    }
  }
}
```

### Verify Latest Versions Manually

**PowerShell:**
```powershell
Invoke-RestMethod https://api.github.com/repos/PowerShell/PowerShell/releases/latest | 
    Select-Object tag_name, published_at
```

**Windows Terminal:**
```powershell
Invoke-RestMethod https://api.github.com/repos/microsoft/terminal/releases/latest | 
    Select-Object tag_name, published_at
```

**MSYS2:**
```powershell
(Invoke-WebRequest https://repo.msys2.org/distrib/x86_64/).Links | 
    Where-Object href -like "msys2-base*" | 
    Select-Object href -First 5
```

### Force Fallback Usage (Testing)

To test fallback behavior:

```powershell
# Block GitHub API temporarily
Add-Content C:\Windows\System32\drivers\etc\hosts "127.0.0.1 api.github.com"

# Run setup
.\Setup-NanerVendor.ps1

# Remove block
# (Remove the line from hosts file)
```

## GitHub API Rate Limiting

### Understanding Rate Limits

**Unauthenticated requests:**
- Limit: 60 requests per hour per IP
- Reset: Every hour on the hour

**Checking your limit:**
```powershell
$response = Invoke-WebRequest -Uri "https://api.github.com/rate_limit"
$response.Content | ConvertFrom-Json | Select-Object -ExpandProperty rate
```

Output:
```json
{
  "limit": 60,
  "remaining": 58,
  "reset": 1704556800,
  "used": 2
}
```

### Avoiding Rate Limits

1. **Cache downloads**: Use `-SkipDownload` if already downloaded
2. **Run infrequently**: Setup is one-time per installation
3. **Use fallbacks**: Script automatically handles this
4. **Authenticate** (optional, for development):

```powershell
# Create personal access token (read-only, public repos)
$headers = @{
    "Authorization" = "token ghp_xxxxxxxxxxxx"
    "User-Agent" = "Naner-Vendor-Setup"
}

Invoke-RestMethod -Uri $apiUrl -Headers $headers
# Now you have 5000 requests/hour
```

## Adding New Dependencies

To add a new dependency with dynamic fetching:

```powershell
$vendorConfig = @{
    NewTool = @{
        Name = "New Tool"
        ExtractDir = "newtool"
        GetLatestRelease = {
            # Option 1: GitHub
            $fallback = "https://github.com/.../newtool-v1.0.0.zip"
            return Get-LatestGitHubRelease `
                -Repo "owner/repo" `
                -AssetPattern "*-win-x64.zip" `
                -FallbackUrl $fallback
            
            # Option 2: Custom logic
            try {
                # Your custom fetching logic
                $version = ...
                $url = ...
                return @{ Version = $version; Url = $url; ... }
            }
            catch {
                # Fallback
                return @{ Version = "latest"; Url = $fallback; ... }
            }
        }
        PostInstall = {
            param($extractPath)
            # Post-installation steps
        }
    }
}
```

## Best Practices

1. **Always provide fallbacks**: Ensure installation can succeed even if fetching fails
2. **Use stable versions for fallbacks**: Don't use pre-release or beta versions
3. **Test fallbacks regularly**: Verify they still work
4. **Update fallbacks periodically**: Keep them reasonably recent
5. **Handle errors gracefully**: Show warnings, not failures
6. **Log what's happening**: Help users troubleshoot

## Security Considerations

### Download Verification

**Current state**: Downloads are from official sources but not cryptographically verified

**Future enhancement**: Add checksum verification
```powershell
# PowerShell releases include SHA256 hashes
$expectedHash = "abc123..."
$actualHash = Get-FileHash $downloadPath -Algorithm SHA256
if ($actualHash.Hash -ne $expectedHash) {
    throw "Hash mismatch!"
}
```

### HTTPS Enforcement

All downloads use HTTPS:
- `https://github.com/...`
- `https://repo.msys2.org/...`

PowerShell's `Invoke-WebRequest` and `Invoke-RestMethod` validate certificates by default.

### Source Verification

- **PowerShell**: Official Microsoft repository
- **Windows Terminal**: Official Microsoft repository
- **MSYS2**: Official MSYS2 repository

No third-party mirrors or unofficial sources are used.

## Future Enhancements

1. **Checksum verification**: Verify downloads against published hashes
2. **Signature verification**: Validate Authenticode signatures (where available)
3. **Version caching**: Cache API responses to reduce rate limit usage
4. **Update notifications**: Check for newer versions of installed tools
5. **Multiple sources**: Support alternate download mirrors
6. **Proxy support**: Handle corporate proxies better

## Conclusion

The dynamic URL fetching system ensures Naner always uses the latest stable versions of its dependencies while maintaining reliability through comprehensive fallback mechanisms. Users benefit from automatic updates without maintainer intervention, and the system degrades gracefully when network or API issues occur.
