# Check for deprecated Unity APIs that need to be updated for Unity 6
# Scans C# files for common deprecated patterns

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "assets\EasyPath\Assets"
)

$ErrorActionPreference = "Continue"
$script:issues = @()

Write-Host "=== Unity 6 Deprecated API Scanner ===" -ForegroundColor Cyan
Write-Host "Scanning: $ProjectPath" -ForegroundColor Gray
Write-Host ""

# Define deprecated patterns and their replacements
$deprecatedPatterns = @(
    @{
        Pattern = 'FindObjectOfType\s*<'
        Replacement = 'FindFirstObjectByType<T>() or FindAnyObjectByType<T>()'
        Severity = 'Warning'
        Description = 'FindObjectOfType is deprecated in Unity 2023+'
    },
    @{
        Pattern = 'FindObjectsOfType\s*<'
        Replacement = 'FindObjectsByType<T>()'
        Severity = 'Warning'
        Description = 'FindObjectsOfType is deprecated in Unity 2023+'
    },
    @{
        Pattern = 'FindObjectOfType\s*\('
        Replacement = 'FindFirstObjectByType(type) or FindAnyObjectByType(type)'
        Severity = 'Warning'
        Description = 'FindObjectOfType is deprecated in Unity 2023+'
    },
    @{
        Pattern = 'Application\.isPlaying'
        Replacement = 'Application.isPlaying (still valid but consider EditorApplication.isPlaying in Editor code)'
        Severity = 'Info'
        Description = 'Consider using EditorApplication.isPlaying in Editor scripts'
    },
    @{
        Pattern = '\.material\s*=' 
        Replacement = '.material or .sharedMaterial (be aware of material instance creation)'
        Severity = 'Info'
        Description = 'Direct .material access creates instances; use .sharedMaterial when appropriate'
    },
    @{
        Pattern = 'WWW\s'
        Replacement = 'UnityWebRequest'
        Severity = 'Error'
        Description = 'WWW class is obsolete, use UnityWebRequest'
    },
    @{
        Pattern = 'Application\.LoadLevel'
        Replacement = 'SceneManager.LoadScene()'
        Severity = 'Error'
        Description = 'Application.LoadLevel is obsolete'
    },
    @{
        Pattern = 'Application\.LoadLevelAsync'
        Replacement = 'SceneManager.LoadSceneAsync()'
        Severity = 'Error'
        Description = 'Application.LoadLevelAsync is obsolete'
    },
    @{
        Pattern = 'GUIText|GUITexture'
        Replacement = 'UI.Text or TextMeshPro'
        Severity = 'Error'
        Description = 'GUIText/GUITexture components are removed'
    },
    @{
        Pattern = 'Network\.'
        Replacement = 'Netcode for GameObjects or other networking solution'
        Severity = 'Warning'
        Description = 'Legacy networking (UNet) is deprecated'
    },
    @{
        Pattern = 'MovieTexture'
        Replacement = 'VideoPlayer'
        Severity = 'Error'
        Description = 'MovieTexture is obsolete, use VideoPlayer'
    },
    @{
        Pattern = 'UnityEngine\.Random\.seed'
        Replacement = 'UnityEngine.Random.InitState()'
        Severity = 'Warning'
        Description = 'Random.seed is deprecated, use Random.InitState()'
    },
    @{
        Pattern = 'ParticleSystem\.startColor'
        Replacement = 'ParticleSystem.main.startColor'
        Severity = 'Warning'
        Description = 'Direct ParticleSystem properties are deprecated, use .main module'
    },
    @{
        Pattern = 'EditorStyles\.whiteLabel'
        Replacement = 'EditorStyles alternatives or custom GUIStyle'
        Severity = 'Info'
        Description = 'Some EditorStyles may be removed in future versions'
    },
    @{
        Pattern = 'AssetDatabase\.LoadAssetAtPath\s*\('
        Replacement = 'AssetDatabase.LoadAssetAtPath<T>() with generic type'
        Severity = 'Info'
        Description = 'Prefer generic version for type safety'
    }
)

# Find all C# files
$csFiles = Get-ChildItem -Path $ProjectPath -Filter "*.cs" -Recurse -ErrorAction SilentlyContinue

if (-not $csFiles) {
    Write-Host "No C# files found in $ProjectPath" -ForegroundColor Yellow
    exit 0
}

Write-Host "Scanning $($csFiles.Count) C# files..." -ForegroundColor Gray
Write-Host ""

$errorCount = 0
$warningCount = 0
$infoCount = 0

foreach ($file in $csFiles) {
    $content = Get-Content $file.FullName -Raw
    $relativePath = $file.FullName.Replace((Get-Location).Path + "\", "")
    $fileHasIssues = $false
    
    foreach ($pattern in $deprecatedPatterns) {
        $matches = [regex]::Matches($content, $pattern.Pattern)
        
        if ($matches.Count -gt 0) {
            if (-not $fileHasIssues) {
                Write-Host "$relativePath" -ForegroundColor White
                $fileHasIssues = $true
            }
            
            # Find line numbers
            $lines = $content -split "`n"
            $lineNum = 1
            foreach ($line in $lines) {
                if ($line -match $pattern.Pattern) {
                    $severityColor = switch ($pattern.Severity) {
                        'Error' { 'Red'; $errorCount++ }
                        'Warning' { 'Yellow'; $warningCount++ }
                        'Info' { 'Cyan'; $infoCount++ }
                    }
                    
                    Write-Host "  [$($pattern.Severity)] Line $lineNum`: $($pattern.Description)" -ForegroundColor $severityColor
                    Write-Host "           Replace with: $($pattern.Replacement)" -ForegroundColor Gray
                    
                    $script:issues += @{
                        File = $relativePath
                        Line = $lineNum
                        Severity = $pattern.Severity
                        Description = $pattern.Description
                        Replacement = $pattern.Replacement
                    }
                }
                $lineNum++
            }
        }
    }
    
    if ($fileHasIssues) {
        Write-Host ""
    }
}

# Summary
Write-Host "=== Summary ===" -ForegroundColor Cyan
Write-Host "Files scanned: $($csFiles.Count)" -ForegroundColor Gray
Write-Host "Errors: $errorCount" -ForegroundColor $(if ($errorCount -gt 0) { "Red" } else { "Green" })
Write-Host "Warnings: $warningCount" -ForegroundColor $(if ($warningCount -gt 0) { "Yellow" } else { "Green" })
Write-Host "Info: $infoCount" -ForegroundColor $(if ($infoCount -gt 0) { "Cyan" } else { "Green" })

if ($script:issues.Count -eq 0) {
    Write-Host ""
    Write-Host "No deprecated APIs found! Code is Unity 6 compatible." -ForegroundColor Green
    exit 0
} else {
    Write-Host ""
    Write-Host "Found $($script:issues.Count) potential issue(s) to review." -ForegroundColor Yellow
    
    if ($errorCount -gt 0) {
        Write-Host "Errors should be fixed before building." -ForegroundColor Red
        exit 1
    }
    exit 0
}
