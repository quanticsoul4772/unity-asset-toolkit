# Cleanup script to remove manually created ProjectSettings files
# Unity will regenerate these when the project is first opened

$projectSettingsPath = "assets\EasyPath\ProjectSettings"

if (Test-Path $projectSettingsPath) {
    Write-Host "Removing manually created ProjectSettings files..." -ForegroundColor Yellow
    Remove-Item -Path $projectSettingsPath -Recurse -Force
    Write-Host "ProjectSettings removed. Unity will regenerate these when you open the project." -ForegroundColor Green
} else {
    Write-Host "ProjectSettings folder not found." -ForegroundColor Gray
}
