# Unity Asset Toolkit - Pre-commit Hook (PowerShell)
# Validates Unity meta files before committing
# Run via: powershell -ExecutionPolicy Bypass -File .githooks/pre-commit.ps1

Write-Host "Running Unity pre-commit checks..." -ForegroundColor Cyan

$ErrorCount = 0
$WarningCount = 0

# Get staged files
$StagedFiles = git diff --cached --name-only --diff-filter=ACM

# ============================================
# Check 1: Meta files for Unity assets
# ============================================
Write-Host "Checking for missing meta files..." -ForegroundColor Yellow

foreach ($file in $StagedFiles) {
    # Skip if it's a meta file itself
    if ($file -like "*.meta") { continue }
    
    # Skip if not in Unity project
    if ($file -notlike "assets/*") { continue }
    
    # Skip Library, Temp, obj folders
    if ($file -like "*/Library/*" -or $file -like "*/Temp/*" -or $file -like "*/obj/*") { continue }
    
    # Check if meta file exists for this asset
    $MetaFile = "$file.meta"
    if ((Test-Path $file) -and (-not (Test-Path $MetaFile))) {
        Write-Host "  [ERROR] Missing meta file: $MetaFile" -ForegroundColor Red
        $ErrorCount++
    }
}

# ============================================
# Check 2: Orphan meta files
# ============================================
Write-Host "Checking for orphan meta files..." -ForegroundColor Yellow

foreach ($file in $StagedFiles) {
    # Only check meta files
    if ($file -notlike "*.meta") { continue }
    
    # Get the asset file (remove .meta extension)
    $AssetFile = $file -replace '\.meta$', ''
    
    # Check if asset exists
    if ((-not (Test-Path $AssetFile)) -and (-not (Test-Path $AssetFile -PathType Container))) {
        # Check if asset is also being staged for deletion
        $DeletedFiles = git diff --cached --name-only --diff-filter=D
        if ($DeletedFiles -notcontains $AssetFile) {
            Write-Host "  [WARN] Orphan meta file (no matching asset): $file" -ForegroundColor Yellow
            $WarningCount++
        }
    }
}

# ============================================
# Check 3: Large files not tracked by LFS
# ============================================
Write-Host "Checking for large files not tracked by LFS..." -ForegroundColor Yellow

$MaxSizeKB = 1024  # 1MB

foreach ($file in $StagedFiles) {
    if (Test-Path $file) {
        $Size = (Get-Item $file).Length / 1KB
        if ($Size -gt $MaxSizeKB) {
            # Check if tracked by LFS
            $LfsCheck = git check-attr filter $file 2>$null | Select-String "lfs"
            if (-not $LfsCheck) {
                Write-Host "  [WARN] Large file not tracked by LFS: $file ($([math]::Round($Size))KB)" -ForegroundColor Yellow
                $WarningCount++
            }
        }
    }
}

# ============================================
# Summary
# ============================================
Write-Host ""
Write-Host "=== Pre-commit Check Summary ===" -ForegroundColor Cyan

if ($ErrorCount -eq 0 -and $WarningCount -eq 0) {
    Write-Host "All checks passed!" -ForegroundColor Green
    exit 0
} elseif ($ErrorCount -eq 0) {
    Write-Host "Passed with $WarningCount warning(s)" -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "FAILED: $ErrorCount error(s), $WarningCount warning(s)" -ForegroundColor Red
    Write-Host ""
    Write-Host "To fix missing meta files:"
    Write-Host "  1. Open Unity and let it generate meta files"
    Write-Host "  2. Stage the new meta files: git add *.meta"
    Write-Host ""
    Write-Host "To bypass this check (not recommended):"
    Write-Host "  git commit --no-verify"
    exit 1
}
