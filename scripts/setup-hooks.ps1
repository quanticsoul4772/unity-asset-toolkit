# Setup Git Hooks for Unity Development
# Run this once to configure Git hooks

Write-Host "=== Setting up Git Hooks ===" -ForegroundColor Cyan
Write-Host ""

# Check if we're in a git repository
if (-not (Test-Path ".git")) {
    Write-Host "ERROR: Not in a git repository" -ForegroundColor Red
    exit 1
}

# Configure Git to use our hooks directory
Write-Host "Configuring Git to use custom hooks..." -ForegroundColor Yellow
git config core.hooksPath .githooks

# Verify
$hooksPath = git config --get core.hooksPath
if ($hooksPath -eq ".githooks") {
    Write-Host "SUCCESS: Git hooks configured" -ForegroundColor Green
    Write-Host "  Hooks directory: .githooks" -ForegroundColor Gray
} else {
    Write-Host "WARNING: Could not configure hooks path" -ForegroundColor Yellow
}

# Make hooks executable (for Unix systems when using WSL/Git Bash)
Write-Host ""
Write-Host "Setting hook permissions..." -ForegroundColor Yellow

$hookFiles = @(
    ".githooks/pre-commit"
)

foreach ($hook in $hookFiles) {
    if (Test-Path $hook) {
        # Try to set executable permission (may fail on Windows but works in Git Bash)
        try {
            git update-index --chmod=+x $hook 2>$null
            Write-Host "  Set executable: $hook" -ForegroundColor Green
        } catch {
            Write-Host "  Skipped (Windows): $hook" -ForegroundColor Gray
        }
    }
}

# Initialize Git LFS
Write-Host ""
Write-Host "Initializing Git LFS..." -ForegroundColor Yellow

$lfsVersion = git lfs version 2>$null
if ($lfsVersion) {
    git lfs install
    Write-Host "SUCCESS: Git LFS initialized" -ForegroundColor Green
    Write-Host "  Version: $lfsVersion" -ForegroundColor Gray
} else {
    Write-Host "WARNING: Git LFS not installed" -ForegroundColor Yellow
    Write-Host "  Install from: https://git-lfs.github.com/" -ForegroundColor Gray
}

# Summary
Write-Host ""
Write-Host "=== Setup Complete ==="  -ForegroundColor Cyan
Write-Host ""
Write-Host "Git hooks are now active. They will run automatically on commit." -ForegroundColor Gray
Write-Host ""
Write-Host "Hooks installed:" -ForegroundColor White
Write-Host "  - pre-commit: Validates Unity meta files" -ForegroundColor Gray
Write-Host ""
Write-Host "To test the pre-commit hook:" -ForegroundColor White
Write-Host "  1. Make a change to a file in assets/" -ForegroundColor Gray
Write-Host "  2. Stage and commit: git add . && git commit -m 'test'" -ForegroundColor Gray
Write-Host ""
Write-Host "To bypass hooks (not recommended):" -ForegroundColor White
Write-Host "  git commit --no-verify" -ForegroundColor Gray
