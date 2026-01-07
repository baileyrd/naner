param(
    [Parameter(Mandatory)]
    [hashtable]$Context
)

Write-Host "Disabling Java JDK plugin..." -ForegroundColor Cyan
Write-Host "Java environment variables will be removed on next shell launch" -ForegroundColor Yellow
