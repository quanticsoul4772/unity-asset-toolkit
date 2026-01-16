# Check Unity EasyPath project compilation status
$ErrorActionPreference = "Continue"

Write-Host "=== Unity EasyPath Compilation Check ===" -ForegroundColor Cyan
Write-Host ""

# Check for compiled EasyPath assemblies
$assembliesPath = "assets\EasyPath\Library\ScriptAssemblies"
Write-Host "Checking for compiled assemblies..." -ForegroundColor Yellow

if (Test-Path $assembliesPath) {
    $easyPathDlls = Get-ChildItem -Path $assembliesPath -Filter "*EasyPath*" -ErrorAction SilentlyContinue
    if ($easyPathDlls) {
        Write-Host "SUCCESS: EasyPath assemblies found:" -ForegroundColor Green
        $easyPathDlls | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Green }
    } else {
        Write-Host "WARNING: No EasyPath assemblies found yet" -ForegroundColor Yellow
        Write-Host "Available assemblies:" -ForegroundColor Gray
        Get-ChildItem -Path $assembliesPath -Filter "*.dll" | ForEach-Object { Write-Host "  - $($_.Name)" -ForegroundColor Gray }
    }
} else {
    Write-Host "ERROR: ScriptAssemblies folder not found" -ForegroundColor Red
}

Write-Host ""

# Check Unity Editor.log for errors
$editorLog = Join-Path $env:LOCALAPPDATA "Unity\Editor\Editor.log"
Write-Host "Checking Unity Editor.log for errors..." -ForegroundColor Yellow

if (Test-Path $editorLog) {
    $errors = Select-String -Path $editorLog -Pattern "error CS\d+" -AllMatches
    if ($errors) {
        Write-Host "ERRORS FOUND:" -ForegroundColor Red
        $errors | Select-Object -Last 20 | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    } else {
        Write-Host "No C# compilation errors found in Editor.log" -ForegroundColor Green
    }
} else {
    Write-Host "Editor.log not found at: $editorLog" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Check Complete ===" -ForegroundColor Cyan
