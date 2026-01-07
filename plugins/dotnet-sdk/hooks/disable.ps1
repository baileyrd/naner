param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Disabling .NET SDK plugin..." -ForegroundColor Cyan
Write-Host ".NET environment variables will be removed on next shell launch" -ForegroundColor Yellow
