# Naner Tests

This directory contains unit and integration tests for the Naner PowerShell modules.

## Structure

```
tests/
├── unit/                           # Unit tests for individual modules
│   ├── Common.Tests.ps1           # Tests for Common.psm1
│   ├── Naner.Vendors.Tests.ps1    # Tests for Naner.Vendors.psm1
│   └── Naner.Archives.Tests.ps1   # Tests for Naner.Archives.psm1
├── Run-Tests.ps1                  # Test runner script
└── README.md                      # This file
```

## Requirements

- **PowerShell 7.x** or higher
- **Pester 5.x** or higher (automatically installed if missing)

## Running Tests

### Run All Tests

```powershell
.\tests\Run-Tests.ps1
```

### Run Specific Test File

```powershell
.\tests\Run-Tests.ps1 -Path unit\Common.Tests.ps1
```

### Run with Code Coverage

```powershell
.\tests\Run-Tests.ps1 -CodeCoverage
```

### Run with Different Output Verbosity

```powershell
# Detailed output (default)
.\tests\Run-Tests.ps1 -Output Detailed

# Minimal output
.\tests\Run-Tests.ps1 -Output Minimal

# Normal output
.\tests\Run-Tests.ps1 -Output Normal
```

## Test Coverage

### Common.psm1 Tests

- **Logging Functions**: Write-Status, Write-Success, Write-Failure, Write-Info, Write-DebugInfo
- **Path Functions**: Find-NanerRoot, Get-NanerRootSimple, Expand-NanerPath
- **Configuration Functions**: Get-NanerConfig
- **GitHub API Functions**: Get-LatestGitHubRelease

### Naner.Vendors.psm1 Tests

- **Configuration Management**: Get-VendorConfiguration
- **Release Information**: Get-VendorRelease, Get-GitHubRelease, Get-WebScrapedRelease, Get-StaticRelease, Get-GoRelease
- **Vendor Installation**: Install-VendorPackage
- **PostInstall Functions**: Initialize-SevenZip, Initialize-PowerShell, Initialize-Go, etc.

### Naner.Archives.psm1 Tests

- **File Download**: Get-FileWithProgress (with retry logic)
- **7-Zip Detection**: Get-SevenZipPath
- **Archive Extraction**: Expand-ArchiveWith7Zip, Expand-VendorArchive

## Writing New Tests

### Test File Naming Convention

Test files should follow the pattern: `<ModuleName>.Tests.ps1`

### Test Structure

```powershell
BeforeAll {
    # Import module
    $modulePath = Join-Path $PSScriptRoot "..\..\src\powershell\ModuleName.psm1"
    Import-Module $modulePath -Force
}

Describe "Module Name - Feature Category" {

    Context "Function Name" {
        It "Should do something" {
            # Arrange
            $input = "test"

            # Act
            $result = Invoke-Function -Input $input

            # Assert
            $result | Should -Be "expected"
        }
    }
}
```

### Best Practices

1. **Isolation**: Each test should be independent
2. **Cleanup**: Use `BeforeAll`/`AfterAll` and `BeforeEach`/`AfterEach` for setup/teardown
3. **Temp Files**: Always use `[System.IO.Path]::GetTempPath()` for temporary files
4. **Mocking**: Mock external dependencies (network calls, file system operations)
5. **Assertions**: Use clear, descriptive assertion messages

## CI/CD Integration

Tests are automatically run on:
- Every push to main branch
- Every pull request
- Manual workflow dispatch

See `.github/workflows/test.yml` for CI configuration.

## Code Coverage Goals

Target coverage levels:
- **Minimum**: 60% overall coverage
- **Good**: 80% overall coverage
- **Excellent**: 90%+ overall coverage

## Troubleshooting

### Pester Not Found

If you see "Pester module not found":

```powershell
Install-Module -Name Pester -MinimumVersion 5.0.0 -Scope CurrentUser -Force
```

### Tests Failing Due to Network

Some tests (e.g., GitHub API tests) require internet connectivity. If running offline:

```powershell
# Skip specific tests using tags (future enhancement)
```

### Module Import Errors

If modules fail to import, ensure you're running from the project root:

```powershell
cd c:\Users\BAILEYRD\dev\naner
.\tests\Run-Tests.ps1
```

## Future Enhancements

- [ ] Integration tests for full vendor installation workflow
- [ ] Performance benchmarking tests
- [ ] Test tags for categorizing tests (unit, integration, slow, fast)
- [ ] Parallel test execution
- [ ] Test result publishing to CI dashboard
- [ ] Mutation testing for test quality assessment
