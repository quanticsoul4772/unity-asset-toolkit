# Preflight Check Script for Unity Development
# Run this before opening Unity to catch common issues early
#
# Usage: .\scripts\preflight.ps1 [-ProjectPath "path\to\project"] [-Fix]

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "assets\EasyPath",
    
    [Parameter(Mandatory=$false)]
    [switch]$Fix = $false
)

$ErrorActionPreference = "Continue"
$script:totalErrors = 0
$script:totalWarnings = 0

function Write-Section($title) {
    Write-Host ""
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host " $title" -ForegroundColor Cyan
    Write-Host "======================================" -ForegroundColor Cyan
    Write-Host ""
}

function Write-Result($success, $message) {
    if ($success) {
        Write-Host "  [PASS] $message" -ForegroundColor Green
    } else {
        Write-Host "  [FAIL] $message" -ForegroundColor Red
        $script:totalErrors++
    }
}

function Write-Warn($message) {
    Write-Host "  [WARN] $message" -ForegroundColor Yellow
    $script:totalWarnings++
}

Write-Host "=== Unity Development Preflight Check ===" -ForegroundColor Magenta
Write-Host "Project: $ProjectPath" -ForegroundColor Gray
Write-Host "Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Gray

# ============================================
# 1. Check Project Structure
# ============================================
Write-Section "1. Project Structure"

$assetsPath = Join-Path $ProjectPath "Assets"
$projectSettingsPath = Join-Path $ProjectPath "ProjectSettings"
$packagesPath = Join-Path $ProjectPath "Packages"

Write-Result (Test-Path $assetsPath) "Assets folder exists"
Write-Result (Test-Path $projectSettingsPath) "ProjectSettings folder exists"
Write-Result (Test-Path $packagesPath) "Packages folder exists"

# Check for Unity project indicator
$projectVersionPath = Join-Path $projectSettingsPath "ProjectVersion.txt"
if (Test-Path $projectVersionPath) {
    $versionContent = Get-Content $projectVersionPath -Raw
    if ($versionContent -match "m_EditorVersion:\s*([\d\.\w]+)") {
        Write-Host "  [INFO] Unity Version: $($Matches[1])" -ForegroundColor Cyan
    }
    Write-Result $true "Valid Unity project detected"
} else {
    Write-Result $false "ProjectVersion.txt not found - not a valid Unity project"
}

# ============================================
# 2. Assembly Definition Validation
# ============================================
Write-Section "2. Assembly Definitions (.asmdef)"

$asmdefScript = Join-Path $PSScriptRoot "validate-asmdef.ps1"
if (Test-Path $asmdefScript) {
    # Run the validator and capture output
    $asmdefOutput = & $asmdefScript -ProjectPath $ProjectPath 2>&1
    
    # Check for errors in output
    $hasAsmdefErrors = $asmdefOutput | Where-Object { $_ -match "\[ERROR\]" }
    $hasAsmdefWarnings = $asmdefOutput | Where-Object { $_ -match "\[WARN\]" }
    
    if ($hasAsmdefErrors) {
        Write-Result $false "Assembly definitions have errors (see above)"
    } elseif ($hasAsmdefWarnings) {
        Write-Warn "Assembly definitions have warnings"
    } else {
        Write-Result $true "Assembly definitions are valid"
    }
} else {
    Write-Host "  [SKIP] validate-asmdef.ps1 not found" -ForegroundColor Gray
}

# ============================================
# 3. Deprecated API Check
# ============================================
Write-Section "3. Deprecated API Check"

$deprecatedScript = Join-Path $PSScriptRoot "check-deprecated-api.ps1"
if (Test-Path $deprecatedScript) {
    # Run silently and check exit code
    $null = & $deprecatedScript -ProjectPath (Join-Path $ProjectPath "Assets") 2>&1
    $deprecatedExitCode = $LASTEXITCODE
    
    if ($deprecatedExitCode -eq 0) {
        Write-Result $true "No critical deprecated APIs found"
    } else {
        Write-Result $false "Deprecated APIs need attention"
    }
} else {
    Write-Host "  [SKIP] check-deprecated-api.ps1 not found" -ForegroundColor Gray
}

# ============================================
# 4. Unity Installation Check
# ============================================
Write-Section "4. Unity Installation"

$unityPaths = @(
    "C:\Program Files\Unity\Hub\Editor",
    "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor"
)

$unityFound = $false
$unityVersion = $null

foreach ($basePath in $unityPaths) {
    if (Test-Path $basePath) {
        $editors = Get-ChildItem $basePath -Directory | Sort-Object Name -Descending
        foreach ($editor in $editors) {
            $exePath = Join-Path $editor.FullName "Editor\Unity.exe"
            if (Test-Path $exePath) {
                $unityFound = $true
                $unityVersion = $editor.Name
                break
            }
        }
    }
    if ($unityFound) { break }
}

if ($unityFound) {
    Write-Result $true "Unity Editor found: $unityVersion"
} else {
    Write-Result $false "Unity Editor not found - install via Unity Hub"
}

# Check Unity Hub
$unityHubPath = "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe"
Write-Result (Test-Path $unityHubPath) "Unity Hub installed"

# ============================================
# 5. Git Status Check
# ============================================
Write-Section "5. Git Repository"

$isGitRepo = Test-Path ".git"
Write-Result $isGitRepo "Git repository initialized"

if ($isGitRepo) {
    # Check for uncommitted changes
    $gitStatus = git status --porcelain 2>&1
    $uncommittedCount = ($gitStatus | Measure-Object -Line).Lines
    
    if ($uncommittedCount -eq 0) {
        Write-Host "  [INFO] Working directory is clean" -ForegroundColor Green
    } else {
        Write-Warn "$uncommittedCount uncommitted change(s)"
    }
    
    # Check .gitignore has Unity patterns
    $gitignorePath = ".gitignore"
    if (Test-Path $gitignorePath) {
        $gitignoreContent = Get-Content $gitignorePath -Raw
        $hasLibrary = $gitignoreContent -match "Library/"
        $hasTemp = $gitignoreContent -match "Temp/"
        
        if ($hasLibrary -and $hasTemp) {
            Write-Result $true ".gitignore has Unity patterns"
        } else {
            Write-Warn ".gitignore may be missing Unity patterns (Library/, Temp/)"
        }
    } else {
        Write-Warn "No .gitignore file found"
    }
}

# ============================================
# 6. Library Folder Status
# ============================================
Write-Section "6. Unity Cache (Library Folder)"

$libraryPath = Join-Path $ProjectPath "Library"
if (Test-Path $libraryPath) {
    $scriptAssemblies = Join-Path $libraryPath "ScriptAssemblies"
    
    if (Test-Path $scriptAssemblies) {
        $dlls = Get-ChildItem $scriptAssemblies -Filter "*.dll" -ErrorAction SilentlyContinue
        Write-Host "  [INFO] Library exists with $($dlls.Count) compiled assemblies" -ForegroundColor Cyan
        
        # Check for EasyPath assemblies specifically
        $easyPathDlls = $dlls | Where-Object { $_.Name -match "EasyPath" }
        if ($easyPathDlls) {
            Write-Result $true "EasyPath assemblies compiled"
            $easyPathDlls | ForEach-Object { 
                Write-Host "           - $($_.Name) ($([math]::Round($_.Length / 1KB, 1)) KB)" -ForegroundColor Gray 
            }
        } else {
            Write-Warn "EasyPath assemblies not found - Unity may need to recompile"
        }
    } else {
        Write-Host "  [INFO] No ScriptAssemblies - Unity hasn't compiled yet" -ForegroundColor Yellow
    }
} else {
    Write-Host "  [INFO] Library folder doesn't exist - Unity will create it on first open" -ForegroundColor Cyan
}

# ============================================
# Summary
# ============================================
Write-Host ""
Write-Host "======================================" -ForegroundColor Magenta
Write-Host " PREFLIGHT SUMMARY" -ForegroundColor Magenta
Write-Host "======================================" -ForegroundColor Magenta
Write-Host ""

if ($script:totalErrors -eq 0 -and $script:totalWarnings -eq 0) {
    Write-Host "  All checks passed! Ready to open Unity." -ForegroundColor Green
    Write-Host ""
    Write-Host "  Next steps:" -ForegroundColor Cyan
    Write-Host "    1. Open Unity Hub" -ForegroundColor Gray
    Write-Host "    2. Add project from: $((Resolve-Path $ProjectPath).Path)" -ForegroundColor Gray
    Write-Host "    3. Open the project" -ForegroundColor Gray
    exit 0
} elseif ($script:totalErrors -eq 0) {
    Write-Host "  Passed with $($script:totalWarnings) warning(s)" -ForegroundColor Yellow
    Write-Host "  You can proceed, but consider addressing warnings." -ForegroundColor Yellow
    exit 0
} else {
    Write-Host "  FAILED: $($script:totalErrors) error(s), $($script:totalWarnings) warning(s)" -ForegroundColor Red
    Write-Host "  Fix the errors above before opening Unity." -ForegroundColor Red
    exit 1
}
