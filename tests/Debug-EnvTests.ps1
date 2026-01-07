$result = Invoke-Pester -Path "unit\Naner.Environments.Tests.ps1" -PassThru

Write-Host ""
Write-Host "=== Failed Tests ===" -ForegroundColor Red
$result.Failed | ForEach-Object {
    Write-Host ""
    Write-Host "Test: $($_.Name)" -ForegroundColor Yellow
    Write-Host "Error: $($_.ErrorRecord)" -ForegroundColor Gray
}
