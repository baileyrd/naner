param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Disabling PostgreSQL Client plugin..." -ForegroundColor Cyan
Write-Host "PostgreSQL environment variables will be removed on next shell launch" -ForegroundColor Yellow
