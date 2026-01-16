# Read Unity Editor.log and show compilation-related entries
$logPath = "$env:LOCALAPPDATA\Unity\Editor\Editor.log"

if (Test-Path $logPath) {
    Write-Host "=== Unity Editor.log Analysis ===" -ForegroundColor Cyan
    Write-Host "Log file: $logPath" -ForegroundColor Gray
    Write-Host ""
    
    # Get file info
    $logInfo = Get-Item $logPath
    Write-Host "Last modified: $($logInfo.LastWriteTime)" -ForegroundColor Gray
    Write-Host "Size: $([math]::Round($logInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host ""
    
    # Check for errors
    Write-Host "=== Checking for C# Errors ===" -ForegroundColor Yellow
    $errors = Select-String -Path $logPath -Pattern "error CS\d+" -AllMatches
    if ($errors) {
        Write-Host "ERRORS FOUND:" -ForegroundColor Red
        $errors | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    } else {
        Write-Host "No C# compilation errors found" -ForegroundColor Green
    }
    Write-Host ""
    
    # Check for script compilation messages
    Write-Host "=== Script Compilation Messages ===" -ForegroundColor Yellow
    $compileLines = Select-String -Path $logPath -Pattern "Compil|Script|Assembly" | Select-Object -Last 30
    if ($compileLines) {
        $compileLines | ForEach-Object { Write-Host $_.Line -ForegroundColor Gray }
    } else {
        Write-Host "No compilation messages found" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Show last 50 lines
    Write-Host "=== Last 50 Lines of Log ===" -ForegroundColor Yellow
    Get-Content $logPath -Tail 50
    
} else {
    Write-Host "Editor.log not found at: $logPath" -ForegroundColor Red
}
