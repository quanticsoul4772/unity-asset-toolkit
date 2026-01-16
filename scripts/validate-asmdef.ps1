# Validate Assembly Definition (.asmdef) files for Unity 6 compatibility
# Checks for common issues like name-based references that should be GUIDs

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "assets\EasyPath"
)

$ErrorActionPreference = "Continue"
$script:errors = 0
$script:warnings = 0

Write-Host "=== Assembly Definition Validator ===" -ForegroundColor Cyan
Write-Host "Project: $ProjectPath" -ForegroundColor Gray
Write-Host ""

# Find all .asmdef files
$asmdefFiles = Get-ChildItem -Path $ProjectPath -Filter "*.asmdef" -Recurse -ErrorAction SilentlyContinue

if (-not $asmdefFiles) {
    Write-Host "No .asmdef files found in $ProjectPath" -ForegroundColor Yellow
    exit 0
}

Write-Host "Found $($asmdefFiles.Count) assembly definition file(s)" -ForegroundColor Gray
Write-Host ""

# Build GUID lookup from .asmdef.meta files
$guidLookup = @{}
foreach ($asmdef in $asmdefFiles) {
    $metaFile = "$($asmdef.FullName).meta"
    if (Test-Path $metaFile) {
        $metaContent = Get-Content $metaFile -Raw
        if ($metaContent -match "guid:\s*([a-f0-9]+)") {
            $guid = $Matches[1]
            $asmdefContent = Get-Content $asmdef.FullName -Raw | ConvertFrom-Json
            $name = $asmdefContent.name
            $guidLookup[$name] = $guid
            $guidLookup[$guid] = $name
        }
    }
}

Write-Host "GUID Lookup Table:" -ForegroundColor Gray
foreach ($key in $guidLookup.Keys | Where-Object { $_ -notmatch "^[a-f0-9]{32}$" }) {
    Write-Host "  $key -> GUID:$($guidLookup[$key])" -ForegroundColor Gray
}
Write-Host ""

# Validate each .asmdef file
foreach ($asmdef in $asmdefFiles) {
    Write-Host "Checking: $($asmdef.Name)" -ForegroundColor White
    
    try {
        $content = Get-Content $asmdef.FullName -Raw | ConvertFrom-Json
    } catch {
        Write-Host "  [ERROR] Invalid JSON in $($asmdef.Name)" -ForegroundColor Red
        $script:errors++
        continue
    }
    
    # Check for required fields
    if (-not $content.name) {
        Write-Host "  [ERROR] Missing 'name' field" -ForegroundColor Red
        $script:errors++
    } else {
        Write-Host "  Name: $($content.name)" -ForegroundColor Gray
    }
    
    # Check references
    if ($content.references) {
        foreach ($ref in $content.references) {
            if ($ref -match "^GUID:([a-f0-9]+)$") {
                # GUID reference - good!
                $refGuid = $Matches[1]
                $refName = $guidLookup[$refGuid]
                if ($refName) {
                    Write-Host "  [OK] Reference: $refName (GUID)" -ForegroundColor Green
                } else {
                    Write-Host "  [WARN] Reference: Unknown GUID $refGuid" -ForegroundColor Yellow
                    $script:warnings++
                }
            } elseif ($ref -notmatch "Unity\.|UnityEngine\.|UnityEditor\.") {
                # Name-based reference to non-Unity assembly - should be GUID!
                Write-Host "  [ERROR] Name-based reference: '$ref' - should use GUID format!" -ForegroundColor Red
                
                # Suggest the correct GUID if we know it
                if ($guidLookup[$ref]) {
                    Write-Host "           Suggestion: Use 'GUID:$($guidLookup[$ref])' instead" -ForegroundColor Cyan
                }
                $script:errors++
            } else {
                # Unity built-in reference
                Write-Host "  [OK] Reference: $ref (Unity built-in)" -ForegroundColor Green
            }
        }
    }
    
    # Check includePlatforms for Editor assemblies
    $isEditorFolder = $asmdef.DirectoryName -match "\\Editor($|\\)"
    if ($isEditorFolder) {
        if (-not $content.includePlatforms -or $content.includePlatforms -notcontains "Editor") {
            Write-Host "  [WARN] Editor assembly should have 'includePlatforms': ['Editor']" -ForegroundColor Yellow
            $script:warnings++
        } else {
            Write-Host "  [OK] Editor-only platform restriction" -ForegroundColor Green
        }
    }
    
    # Check for rootNamespace
    if (-not $content.rootNamespace) {
        Write-Host "  [INFO] No rootNamespace defined (optional)" -ForegroundColor Gray
    }
    
    Write-Host ""
}

# Summary
Write-Host "=== Summary ===" -ForegroundColor Cyan
if ($script:errors -eq 0 -and $script:warnings -eq 0) {
    Write-Host "All assembly definitions are valid!" -ForegroundColor Green
    exit 0
} else {
    Write-Host "Errors: $($script:errors)" -ForegroundColor $(if ($script:errors -gt 0) { "Red" } else { "Green" })
    Write-Host "Warnings: $($script:warnings)" -ForegroundColor $(if ($script:warnings -gt 0) { "Yellow" } else { "Green" })
    
    if ($script:errors -gt 0) {
        Write-Host ""
        Write-Host "To fix GUID reference issues:" -ForegroundColor Cyan
        Write-Host "1. Open the .asmdef.meta file of the target assembly" -ForegroundColor Gray
        Write-Host "2. Copy the 'guid' value" -ForegroundColor Gray
        Write-Host "3. Replace name reference with 'GUID:<guid-value>'" -ForegroundColor Gray
        exit 1
    }
    exit 0
}
