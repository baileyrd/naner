# 7-Zip Bundling - Technical Details

## Why Bundle 7-Zip?

Making Naner **truly self-contained** means eliminating ALL external dependencies, including extraction tools. By bundling 7-Zip, Naner can bootstrap itself with zero prerequisites.

## The Bootstrap Chain

```
1. Download 7-Zip MSI     ← PowerShell can do this natively
2. Extract MSI            ← Windows msiexec (built-in)
3. Use 7-Zip to extract   ← Now have 7z.exe
   - PowerShell ZIP
   - Windows Terminal MSIXBUNDLE
   - MSYS2 tar.xz
```

## Implementation Details

### 7-Zip Extraction Method

We use the **MSI installer** because:
- Official, signed package from 7-zip.org
- Extractable using Windows built-in `msiexec`
- No external tools needed
- Reliable and consistent

**Extraction Process**:
```powershell
# Administrative install (extracts without installing)
msiexec /a "7z2408-x64.msi" /qn TARGETDIR="C:\temp\extract"

# Files are in: C:\temp\extract\Files\ProgramFiles\7-Zip\
# Copy 7z.exe and 7z.dll to vendor/7zip/
```

### Why Not Use Other Methods?

**Self-Extracting EXE**: 
- Requires running executable (security concerns)
- May trigger antivirus
- Less transparent

**7z-extra.7z Package**:
- Chicken-and-egg: needs 7-Zip to extract itself
- Not suitable for bootstrapping

**ZIP Package**:
- Not officially distributed
- Would require maintaining unofficial builds

**MSI is the perfect choice**: Official, extractable with built-in tools, reliable.

## Extraction Order

Order matters! Dependencies are extracted in this sequence:

```
[ordered]@{
    SevenZip        # MUST BE FIRST
    PowerShell      # Can use 7-Zip for .zip
    WindowsTerminal # Needs 7-Zip for MSIXBUNDLE
    MSYS2           # Needs 7-Zip for .tar.xz
}
```

PowerShell's `[ordered]@{}` hashtable ensures this order is maintained.

## Version Detection

7-Zip versions are scraped from the official download page:

```powershell
# URL: https://www.7-zip.org/download.html
# Pattern: 7z2408-x64.msi
# Version: 24.08 (from "2408")

$pattern = 'href="([^"]*7z(\d+)-x64\.msi)"'
# Groups[1]: 7z2408-x64.msi (filename)
# Groups[2]: 2408 (version code)

# Format: 24.08
$versionFormatted = "$($version.Substring(0,2)).$($version.Substring(2))"
```

## Benefits

### For Users
1. **Zero Prerequisites**: No manual installations
2. **Single Step**: One command downloads everything
3. **Portable**: Works on fresh Windows installs
4. **Reliable**: No "install 7-Zip first" errors

### For Distribution
1. **Self-Contained Package**: Everything in one archive
2. **No Instructions**: No "prerequisites" section needed
3. **Corporate Friendly**: Works behind firewalls (just needs internet)
4. **Support Simplification**: One less thing to troubleshoot

## Size Impact

- 7-Zip MSI: ~1.5MB download
- Extracted: ~2MB
- **Total overhead**: ~2MB

For a ~550MB package, this 0.4% increase is negligible for the benefits gained.

## Fallback Behavior

If 7-Zip extraction fails (extremely rare):

```
1. Try system-installed 7-Zip
   └─ Check: C:\Program Files\7-Zip\7z.exe
   
2. Try Windows tar (for .tar.xz)
   └─ May fail on some systems
   
3. User intervention required
   └─ Error message with clear instructions
```

The bundled approach makes fallbacks nearly unnecessary.

## Security Considerations

### MSI Verification
- Downloaded from official 7-zip.org (HTTPS)
- Can add checksum verification (future enhancement)
- MSI files are Authenticode signed by Igor Pavlov

### Execution Safety
- We don't run 7z installer
- We extract MSI contents only
- 7z.exe is used purely as extraction tool
- No elevated privileges required

## Testing 7-Zip Installation

```powershell
# Check if 7-Zip was extracted
Test-Path vendor\7zip\7z.exe

# Verify version
vendor\7zip\7z.exe

# Output:
# 7-Zip 24.08 (x64) : Copyright (c) 1999-2024 Igor Pavlov
```

## Comparison: Before vs After

### Before (External 7-Zip Required)

```
Prerequisites:
1. Install PowerShell 5.1+ ✓
2. Install 7-Zip          ✗ (user must do this)
3. Run Setup-NanerVendor.ps1

Issues:
- Users forget to install 7-Zip
- Different 7-Zip versions cause issues  
- Corporate environments may block installation
- Support burden: "Did you install 7-Zip?"
```

### After (Bundled 7-Zip)

```
Prerequisites:
1. PowerShell 5.1+ ✓ (built into Windows 10/11)

Run: .\Setup-NanerVendor.ps1

Issues:
- None! Everything just works
```

## Platform Support

Works on:
- ✓ Windows 10 (all versions)
- ✓ Windows 11
- ✓ Windows Server 2016+

Does NOT work on:
- ✗ Windows 7 (PowerShell too old)
- ✗ Windows 8/8.1 (update PowerShell first)

## Future Enhancements

1. **Checksum Verification**
   ```powershell
   $expectedHash = Get-Content "vendor-checksums.txt" | 
       Select-String "7z.*msi" | ...
   $actualHash = Get-FileHash $downloadPath
   if ($actualHash.Hash -ne $expectedHash) {
       throw "Checksum mismatch!"
   }
   ```

2. **Signature Verification**
   ```powershell
   $signature = Get-AuthenticodeSignature $downloadPath
   if ($signature.Status -ne "Valid") {
       Write-Warning "MSI signature invalid"
   }
   ```

3. **Minimal 7z.exe**
   - Currently ships full 7-Zip (~2MB)
   - Could use 7za.exe standalone (~1MB)
   - But MSI extraction is cleaner

## Troubleshooting

### MSI Extraction Fails

**Symptoms**: 
```
[✗] 7-Zip: MSI extraction failed with exit code: 1619
```

**Cause**: Windows Installer service issues

**Solution**:
```powershell
# Restart Windows Installer service
Restart-Service msiserver

# Re-run setup
.\Setup-NanerVendor.ps1 -ForceDownload
```

### 7z.exe Not Found After Extraction

**Symptoms**:
```
[✗] 7-Zip executable not found
```

**Cause**: MSI structure changed (unlikely)

**Solution**:
```powershell
# Check MSI contents manually
$temp = "C:\temp\7zip_extract"
msiexec /a "vendor\.downloads\7z2408-x64.msi" /qn TARGETDIR="$temp"

# Find 7z.exe location
Get-ChildItem $temp -Recurse -Filter "7z.exe"

# Update extraction logic if needed
```

## Development Notes

### Testing MSI Extraction

```powershell
# Test extraction without full setup
$msiPath = "7z2408-x64.msi"
$dest = "test_extract"

msiexec /a $msiPath /qn TARGETDIR="$dest"

# Verify files
Get-ChildItem $dest -Recurse
```

### Updating 7-Zip Version

When 7-Zip releases a new version:

1. Setup script automatically detects it
2. Scrapes latest version from download page
3. Downloads new MSI
4. No code changes needed!

Fallback URL should be updated periodically to a recent stable version.

## Conclusion

Bundling 7-Zip transforms Naner from "requires 7-Zip" to "requires nothing." This small addition (~2MB) eliminates a major friction point and makes Naner truly self-bootstrapping.

The MSI extraction approach is:
- ✓ Official
- ✓ Secure  
- ✓ Reliable
- ✓ Zero dependencies
- ✓ Automatic

Perfect for a portable, professional terminal environment.
