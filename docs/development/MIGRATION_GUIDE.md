# PowerShell to C# Migration Guide

## Overview

This guide explains how to port the PowerShell prototype to C# for production use.

## Why Port to C#?

### Benefits of C# Version
- **Performance**: 3-5x faster startup time
- **Size**: Smaller executable (500KB vs 1-5MB)
- **Distribution**: Single .exe, easier to sign and distribute
- **Maintenance**: Better IDE support, type safety, refactoring tools
- **Professionalism**: More trusted by users and antivirus software

### When to Port
Port when:
- ✅ All features are tested and working in PowerShell
- ✅ User feedback has been incorporated
- ✅ Feature set is stable
- ✅ Ready for wider distribution

## Migration Strategy

### Phase 1: Setup (1 day)
1. Install .NET 6 SDK
2. Test build system
3. Create project structure
4. Set up version control

### Phase 2: Core Features (2-3 days)
1. Port environment initialization
2. Port Windows Terminal detection
3. Port launch logic
4. Port command-line parsing

### Phase 3: Advanced Features (2-3 days)
1. Port shell integration (registry operations)
2. Port profile management
3. Port configuration handling
4. Add error handling

### Phase 4: Polish (1-2 days)
1. Add unit tests
2. Performance optimization
3. Documentation
4. Create installer

## Function Mapping

### PowerShell → C# Translation

| PowerShell Function | C# Equivalent | Notes |
|-------------------|---------------|-------|
| `Initialize-CmderEnvironment` | `InitializeCmderEnvironment()` | Static method |
| `Find-WindowsTerminal` | `FindWindowsTerminal()` | Return `string?` |
| `Start-WindowsTerminal` | `LaunchWindowsTerminal()` | Use `Process.Start()` |
| `Register-CmderShellIntegration` | `RegisterShellIntegration()` | Use `Microsoft.Win32.Registry` |
| `Test-Path` | `File.Exists()` / `Directory.Exists()` | Built-in .NET |
| `Get-Content` / `ConvertFrom-Json` | `JsonSerializer.Deserialize()` | System.Text.Json |
| `Write-Log` | `Console.WriteLine()` + colors | Or use logging framework |

## Code Conversion Examples

### Example 1: Path Checking

**PowerShell:**
```powershell
if (Test-Path $path) {
    Write-Log "Found: $path"
}
```

**C#:**
```csharp
if (File.Exists(path) || Directory.Exists(path))
{
    Console.WriteLine($"Found: {path}");
}
```

### Example 2: Environment Variables

**PowerShell:**
```powershell
$env:CMDER_ROOT = $cmderRoot
```

**C#:**
```csharp
Environment.SetEnvironmentVariable("CMDER_ROOT", cmderRoot);
```

### Example 3: Process Launch

**PowerShell:**
```powershell
Start-Process -FilePath $wtPath -ArgumentList $wtArgs
```

**C#:**
```csharp
var psi = new ProcessStartInfo
{
    FileName = wtPath,
    Arguments = string.Join(" ", wtArgs),
    UseShellExecute = false
};
Process.Start(psi);
```

### Example 4: JSON Handling

**PowerShell:**
```powershell
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json
$settings.profiles.list += $newProfile
$settings | ConvertTo-Json -Depth 10 | Set-Content $settingsPath
```

**C#:**
```csharp
var json = await File.ReadAllTextAsync(settingsPath);
var settings = JsonSerializer.Deserialize<WindowsTerminalSettings>(json);
settings.Profiles.List.Add(newProfile);
var updatedJson = JsonSerializer.Serialize(settings, new JsonSerializerOptions 
{ 
    WriteIndented = true 
});
await File.WriteAllTextAsync(settingsPath, updatedJson);
```

### Example 5: Registry Operations

**PowerShell:**
```powershell
New-Item -Path "Registry::$path" -Force
Set-ItemProperty -Path "Registry::$path" -Name $name -Value $value
```

**C#:**
```csharp
using Microsoft.Win32;

var key = Registry.CurrentUser.CreateSubKey(path);
key.SetValue(name, value);
key.Close();
```

## Key Differences

### 1. Error Handling

**PowerShell:**
```powershell
try {
    # Code
} catch {
    Write-Log "Error: $_" -Level Error
}
```

**C#:**
```csharp
try
{
    // Code
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

### 2. Null Handling

**PowerShell:**
```powershell
if ($value) {
    # Use value
}
```

**C#:**
```csharp
if (!string.IsNullOrEmpty(value))
{
    // Use value
}

// Or with nullable reference types:
if (value is not null)
{
    // Use value
}
```

### 3. Switch Statements

**PowerShell:**
```powershell
switch ($level) {
    "Info"    { $color = "Cyan" }
    "Success" { $color = "Green" }
    "Error"   { $color = "Red" }
}
```

**C#:**
```csharp
var color = level switch
{
    "Info" => ConsoleColor.Cyan,
    "Success" => ConsoleColor.Green,
    "Error" => ConsoleColor.Red,
    _ => ConsoleColor.White
};
```

### 4. Collections

**PowerShell:**
```powershell
$list = @()
$list += $item
```

**C#:**
```csharp
var list = new List<string>();
list.Add(item);
```

## Building the C# Version

### Development Build

```bash
cd src/csharp
dotnet build
```

### Release Build (Framework-Dependent)

```bash
dotnet publish -c Release -r win-x64 --self-contained false
```

Output: `bin/Release/net6.0-windows/win-x64/publish/Cmder.exe` (~200KB)

### Release Build (Self-Contained, Single File)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true
```

Output: `bin/Release/net6.0-windows/win-x64/publish/Cmder.exe` (~60MB)

### Release Build (Trimmed, Single File)

```bash
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:PublishTrimmed=true -p:TrimMode=link
```

Output: `bin/Release/net6.0-windows/win-x64/publish/Cmder.exe` (~15-20MB)

## Testing Strategy

### Unit Tests

Create `CmderLauncher.Tests` project:

```bash
dotnet new xunit -n CmderLauncher.Tests
cd CmderLauncher.Tests
dotnet add reference ../CmderLauncher.csproj
```

Test coverage priorities:
1. ✅ Windows Terminal detection
2. ✅ Environment variable setup
3. ✅ Command-line parsing
4. ✅ Path resolution
5. ✅ Registry operations (mock)

### Integration Tests

Test against actual Windows Terminal:
1. Launch with different profiles
2. Launch in different directories
3. Single vs new window modes
4. Shell integration registration

## Performance Targets

| Metric | PowerShell | C# Target |
|--------|-----------|-----------|
| Cold start | 300-500ms | <100ms |
| Warm start | 200-300ms | <50ms |
| Memory usage | 30-50MB | <20MB |
| File size | 1-5MB | <1MB |

## Distribution

### Installer Options

1. **MSI Installer** - Using WiX Toolset
2. **Chocolatey Package** - For automated installation
3. **Portable ZIP** - No installation required
4. **Microsoft Store** - Wide distribution (requires MSIX)

### Code Signing

```bash
# Sign the executable
signtool sign /f certificate.pfx /p password /t http://timestamp.digicert.com Cmder.exe
```

## Migration Checklist

### Before Starting
- [ ] All PowerShell features tested and working
- [ ] User feedback collected and addressed
- [ ] .NET 6 SDK installed
- [ ] Development environment set up

### Core Migration
- [ ] Project structure created
- [ ] Command-line parsing implemented
- [ ] Environment initialization ported
- [ ] Windows Terminal detection ported
- [ ] Launch logic ported
- [ ] Error handling added

### Advanced Features
- [ ] Shell integration (registry) ported
- [ ] Profile management ported
- [ ] Configuration handling ported
- [ ] Logging system implemented

### Quality Assurance
- [ ] Unit tests written
- [ ] Integration tests passed
- [ ] Performance benchmarks met
- [ ] Memory usage optimized
- [ ] Error scenarios handled

### Distribution
- [ ] Build scripts created
- [ ] Installer created
- [ ] Documentation updated
- [ ] Code signed (optional)
- [ ] Release notes prepared

## Maintenance Strategy

### Dual Maintenance Phase

During transition, maintain both versions:

1. **PowerShell Version** (v1.x)
   - Bug fixes only
   - Keep as "simple/lightweight" option
   - No new features

2. **C# Version** (v2.x)
   - Active development
   - New features
   - Performance improvements

### Long-term

Eventually:
- C# becomes primary version
- PowerShell archived as legacy
- Clear migration path for users

## Common Pitfalls

### 1. Path Separators
Windows uses `\` but strings need escaping: `"C:\\Path\\To\\File"`
Better: Use `Path.Combine()` or verbatim strings `@"C:\Path\To\File"`

### 2. Async Operations
Don't block on async: Use `await` properly or `.GetAwaiter().GetResult()`

### 3. Registry Permissions
Always check for admin rights before HKLM operations

### 4. JSON Serialization
Windows Terminal settings may have extra fields - use flexible deserialization

### 5. Process Launch
Set `UseShellExecute = false` to capture output/errors properly

## Resources

### Documentation
- [.NET 6 Documentation](https://docs.microsoft.com/en-us/dotnet/core/whats-new/dotnet-6)
- [System.CommandLine](https://github.com/dotnet/command-line-api)
- [Windows Terminal Settings Schema](https://aka.ms/terminal-documentation)

### Tools
- Visual Studio 2022
- Visual Studio Code + C# extension
- JetBrains Rider
- dnSpy (for debugging)

### Libraries
- System.CommandLine - CLI parsing
- System.Text.Json - JSON handling
- Microsoft.Win32.Registry - Registry operations

## Next Steps

1. **Read** the PowerShell script thoroughly
2. **Understand** each function's purpose
3. **Port** incrementally, testing as you go
4. **Compare** outputs with PowerShell version
5. **Optimize** once feature-complete

---

**Remember**: The PowerShell version is your specification. When in doubt, refer back to it!
