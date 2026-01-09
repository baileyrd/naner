param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

# This hook is called during environment initialization
# to set up PostgreSQL-specific environment variables and PATH

$pgRoot = Join-Path $Context.NanerRoot "vendor\postgres\pgsql"

if (Test-Path $pgRoot) {
    # Add PostgreSQL bin to PATH
    $pgBin = Join-Path $pgRoot "bin"
    if ($env:PATH -notlike "*$pgBin*") {
        $env:PATH = "$pgBin;$env:PATH"
    }

    # Set PGDATA directory
    $env:PGDATA = Join-Path $Context.NanerRoot "home\.pgdata"

    # Set default user
    $env:PGUSER = $env:USERNAME

    # Set password file location
    $env:PGPASSFILE = Join-Path $Context.NanerRoot "home\.pgpass"
}
