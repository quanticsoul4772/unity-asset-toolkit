# Check Unity EasyPath project compilation status
# Fixed to avoid environment variable stripping issues

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
        $easyPathDlls | ForEach-Object { 
            Write-Host "  - $($_.Name) ($([math]::Round($_.Length / 1KB, 1)) KB)" -ForegroundColor Green 
        }
    } else {
        Write-Host "WARNING: No EasyPath assemblies found yet" -ForegroundColor Yellow
        Write-Host "Available assemblies:" -ForegroundColor Gray
        Get-ChildItem -Path $assembliesPath -Filter "*.dll" | ForEach-Object { 
            Write-Host "  - $($_.Name)" -ForegroundColor Gray 
        }
    }
} else {
    Write-Host "ERROR: ScriptAssemblies folder not found" -ForegroundColor Red
    Write-Host "Unity may not have been opened yet, or the Library folder was deleted." -ForegroundColor Yellow
}

Write-Host ""

# Check Unity Editor.log for errors (using safer method)
$localAppData = [Environment]::GetFolderPath('LocalApplicationData')
$editorLog = Join-Path $localAppData "Unity\Editor\Editor.log"

Write-Host "Checking Unity Editor.log for errors..." -ForegroundColor Yellow

if (Test-Path $editorLog) {
    # Check for C# compilation errors
    $errors = Select-String -Path $editorLog -Pattern "error CS\d+" -AllMatches
    if ($errors) {
        Write-Host "COMPILATION ERRORS FOUND:" -ForegroundColor Red
        $errors | Select-Object -Last 20 | ForEach-Object { 
            Write-Host "  $($_.Line)" -ForegroundColor Red 
        }
    } else {
        Write-Host "No C# compilation errors found in Editor.log" -ForegroundColor Green
    }
    
    # Also check for assembly/asmdef issues
    Write-Host ""
    Write-Host "Checking for assembly definition issues..." -ForegroundColor Yellow
    $asmdefErrors = Select-String -Path $editorLog -Pattern "Assembly.*not found|GUID.*could not be resolved" -AllMatches | Select-Object -Last 10
    if ($asmdefErrors) {
        Write-Host "ASSEMBLY DEFINITION ISSUES:" -ForegroundColor Red
        $asmdefErrors | ForEach-Object { 
            Write-Host "  $($_.Line)" -ForegroundColor Red 
        }
    } else {
        Write-Host "No assembly definition issues found" -ForegroundColor Green
    }
} else {
    Write-Host "Editor.log not found at: $editorLog" -ForegroundColor Yellow
    Write-Host "Make sure Unity has been opened at least once." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Check Complete ===" -ForegroundColor Cyan
