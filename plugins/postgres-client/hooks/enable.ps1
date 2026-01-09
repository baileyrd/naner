param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Enabling PostgreSQL Client plugin..." -ForegroundColor Cyan

$pgRoot = Join-Path $Context.NanerRoot "vendor\postgres\pgsql"

# Check if PostgreSQL client is installed
if (-not (Test-Path $pgRoot)) {
    Write-Warning "PostgreSQL client not found in vendor directory."
    Write-Host "Please run: Setup-NanerVendor.ps1 -VendorId postgres-client" -ForegroundColor Yellow
    return
}

# Verify psql installation
$psqlExe = Join-Path $pgRoot "bin\psql.exe"
if (Test-Path $psqlExe) {
    $version = & $psqlExe --version 2>&1
    Write-Host "PostgreSQL client enabled: $version" -ForegroundColor Green
}

# Create .pgdata directory
$pgDataDir = Join-Path $Context.NanerRoot "home\.pgdata"
if (-not (Test-Path $pgDataDir)) {
    New-Item -ItemType Directory -Path $pgDataDir -Force | Out-Null
}

# Create .pgpass file if it doesn't exist
$pgPassFile = Join-Path $Context.NanerRoot "home\.pgpass"
if (-not (Test-Path $pgPassFile)) {
    @"
# PostgreSQL password file
# Format: hostname:port:database:username:password
# Example: localhost:5432:mydb:myuser:mypassword
"@ | Set-Content $pgPassFile -Force
}

Write-Host "PostgreSQL environment variables will be set on next shell launch" -ForegroundColor Green
Write-Host "Tip: Edit home\.pgpass to store connection credentials" -ForegroundColor Yellow
