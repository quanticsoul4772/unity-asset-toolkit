# Read Unity Editor.log and show compilation-related entries
# Fixed to avoid environment variable stripping issues

$ErrorActionPreference = "Continue"

# Hardcode the path to avoid $env:LOCALAPPDATA being stripped by some shells
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$logPath = Join-Path $localAppData "Unity\Editor\Editor.log"

if (Test-Path $logPath) {
    Write-Host "=== Unity Editor.log Analysis ===" -ForegroundColor Cyan
    Write-Host "Log file: $logPath" -ForegroundColor Gray
    Write-Host ""
    
    # Get file info
    $logInfo = Get-Item $logPath
    Write-Host "Last modified: $($logInfo.LastWriteTime)" -ForegroundColor Gray
    Write-Host "Size: $([math]::Round($logInfo.Length / 1KB, 2)) KB" -ForegroundColor Gray
    Write-Host ""
    
    # Check for C# compilation errors
    Write-Host "=== C# Compilation Errors ===" -ForegroundColor Yellow
    $errors = Select-String -Path $logPath -Pattern "error CS\d+" -AllMatches
    if ($errors) {
        Write-Host "ERRORS FOUND ($($errors.Count)):" -ForegroundColor Red
        $errors | Select-Object -Last 20 | ForEach-Object { 
            Write-Host "  $($_.Line)" -ForegroundColor Red 
        }
    } else {
        Write-Host "No C# compilation errors found" -ForegroundColor Green
    }
    Write-Host ""
    
    # Check for C# warnings
    Write-Host "=== C# Compilation Warnings ===" -ForegroundColor Yellow
    $warnings = Select-String -Path $logPath -Pattern "warning CS\d+" -AllMatches
    if ($warnings) {
        Write-Host "WARNINGS FOUND ($($warnings.Count)):" -ForegroundColor Yellow
        $warnings | Select-Object -Last 10 | ForEach-Object { 
            Write-Host "  $($_.Line)" -ForegroundColor Yellow 
        }
    } else {
        Write-Host "No C# warnings found" -ForegroundColor Green
    }
    Write-Host ""
    
    # Check for assembly/asmdef issues
    Write-Host "=== Assembly/ASMDEF Issues ===" -ForegroundColor Yellow
    $asmdefIssues = Select-String -Path $logPath -Pattern "Assembly.*not found|asmdef|GUID.*not found|reference.*could not be resolved" -AllMatches
    if ($asmdefIssues) {
        Write-Host "ASMDEF ISSUES FOUND:" -ForegroundColor Red
        $asmdefIssues | Select-Object -Last 10 | ForEach-Object { 
            Write-Host "  $($_.Line)" -ForegroundColor Red 
        }
    } else {
        Write-Host "No assembly definition issues found" -ForegroundColor Green
    }
    Write-Host ""
    
    # Check for script compilation messages
    Write-Host "=== Recent Compilation Activity ===" -ForegroundColor Yellow
    $compileLines = Select-String -Path $logPath -Pattern "Compil|Script compilation|Refreshing native plugins|Domain Reload" | Select-Object -Last 15
    if ($compileLines) {
        $compileLines | ForEach-Object { Write-Host "  $($_.Line)" -ForegroundColor Gray }
    } else {
        Write-Host "No recent compilation messages found" -ForegroundColor Yellow
    }
    Write-Host ""
    
    # Show last 30 lines for context
    Write-Host "=== Last 30 Lines of Log ===" -ForegroundColor Yellow
    Get-Content $logPath -Tail 30 | ForEach-Object { Write-Host $_ }
    
} else {
    Write-Host "Editor.log not found at: $logPath" -ForegroundColor Red
    Write-Host "Make sure Unity has been opened at least once." -ForegroundColor Yellow
}
