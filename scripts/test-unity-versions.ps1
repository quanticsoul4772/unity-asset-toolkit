# SwarmAI Unity Version Compatibility Testing Script
# Run from project root: powershell -ExecutionPolicy Bypass -File scripts/test-unity-versions.ps1

param(
    [switch]$SkipBuild,
    [switch]$Verbose,
    [string]$UnityHubPath = "C:\Program Files\Unity Hub\Unity Hub.exe"
)

$ErrorActionPreference = "Continue"
$ProjectPath = (Get-Location).Path
$AssetsPath = Join-Path $ProjectPath "assets\EasyPath"
$ReportPath = Join-Path $ProjectPath "test-report.md"

# Check if Unity is running
function Test-UnityRunning {
    $unityProcesses = Get-Process -Name "Unity" -ErrorAction SilentlyContinue
    return $null -ne $unityProcesses -and $unityProcesses.Count -gt 0
}

if (Test-UnityRunning) {
    Write-Host ""
    Write-Host "ERROR: Unity Editor is currently running!" -ForegroundColor Red
    Write-Host "" -ForegroundColor Red
    Write-Host "Please close ALL Unity Editor instances before running this test." -ForegroundColor Yellow
    Write-Host "The batch mode tests cannot run while Unity is open." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Steps to fix:" -ForegroundColor Cyan
    Write-Host "  1. Save your work in Unity" -ForegroundColor White
    Write-Host "  2. Close the Unity Editor" -ForegroundColor White
    Write-Host "  3. Run this script again" -ForegroundColor White
    Write-Host ""
    exit 1
}

# Target Unity versions to test
$TargetVersions = @(
    @{ Pattern = "2021.3"; Name = "Unity 2021.3 LTS"; Required = $true },
    @{ Pattern = "2022.3"; Name = "Unity 2022.3 LTS"; Required = $true },
    @{ Pattern = "6000";   Name = "Unity 6";          Required = $true }
)

function Write-Header {
    param([string]$Text)
    Write-Host ""
    Write-Host "=" * 60 -ForegroundColor Cyan
    Write-Host $Text -ForegroundColor Cyan
    Write-Host "=" * 60 -ForegroundColor Cyan
}

function Write-Status {
    param([string]$Text, [string]$Status, [string]$Color = "White")
    Write-Host "  $Text" -NoNewline
    Write-Host " [$Status]" -ForegroundColor $Color
}

function Find-UnityInstallations {
    Write-Header "Detecting Unity Installations"
    
    $installations = @()
    
    # Common Unity installation paths
    $searchPaths = @(
        "C:\Program Files\Unity\Hub\Editor",
        "C:\Program Files\Unity",
        "$env:USERPROFILE\Unity\Hub\Editor",
        "$env:LOCALAPPDATA\Unity\Hub\Editor"
    )
    
    foreach ($searchPath in $searchPaths) {
        if (Test-Path $searchPath) {
            $editors = Get-ChildItem -Path $searchPath -Directory -ErrorAction SilentlyContinue
            foreach ($editor in $editors) {
                $unityExe = Join-Path $editor.FullName "Editor\Unity.exe"
                if (Test-Path $unityExe) {
                    $installations += @{
                        Version = $editor.Name
                        Path = $unityExe
                    }
                    Write-Status $editor.Name "Found at $searchPath" "Green"
                }
            }
        }
    }
    
    if ($installations.Count -eq 0) {
        Write-Host "  No Unity installations found!" -ForegroundColor Red
        Write-Host "  Please install Unity via Unity Hub" -ForegroundColor Yellow
    }
    
    return $installations
}

function Test-UnityVersion {
    param(
        [string]$UnityPath,
        [string]$VersionName,
        [string]$ProjectPath
    )
    
    $result = @{
        Version = $VersionName
        Compilation = "Unknown"
        Errors = @()
        Warnings = @()
        Duration = 0
    }
    
    Write-Host "  Testing compilation..." -NoNewline
    
    $logFile = Join-Path $env:TEMP "unity-compile-$([guid]::NewGuid()).log"
    
    $startTime = Get-Date
    
    # Run Unity in batch mode to check compilation
    $process = Start-Process -FilePath $UnityPath -ArgumentList @(
        "-batchmode",
        "-quit",
        "-projectPath", $ProjectPath,
        "-logFile", $logFile,
        "-nographics"
    ) -PassThru -Wait -NoNewWindow
    
    $result.Duration = ((Get-Date) - $startTime).TotalSeconds
    
    # Parse log file
    if (Test-Path $logFile) {
        $logContent = Get-Content $logFile -Raw
        
        # Check for compilation errors
        $errors = [regex]::Matches($logContent, "error CS\d+:.*")
        $warnings = [regex]::Matches($logContent, "warning CS\d+:.*")
        
        $result.Errors = $errors | ForEach-Object { $_.Value }
        $result.Warnings = $warnings | ForEach-Object { $_.Value }
        
        if ($process.ExitCode -eq 0 -and $result.Errors.Count -eq 0) {
            $result.Compilation = "Pass"
            Write-Host " [PASS]" -ForegroundColor Green
        } else {
            $result.Compilation = "Fail"
            Write-Host " [FAIL]" -ForegroundColor Red
        }
        
        # Cleanup
        Remove-Item $logFile -Force -ErrorAction SilentlyContinue
    } else {
        $result.Compilation = "Error"
        Write-Host " [ERROR - No log file]" -ForegroundColor Red
    }
    
    return $result
}

function Test-SwarmAIAssemblies {
    param([string]$ProjectPath)
    
    Write-Header "Checking SwarmAI Assemblies"
    
    $libraryPath = Join-Path $ProjectPath "Library\ScriptAssemblies"
    $requiredAssemblies = @(
        "SwarmAI.Runtime.dll",
        "SwarmAI.Editor.dll",
        "SwarmAI.Demo.dll"
    )
    
    $results = @()
    
    foreach ($assembly in $requiredAssemblies) {
        $assemblyPath = Join-Path $libraryPath $assembly
        if (Test-Path $assemblyPath) {
            $fileInfo = Get-Item $assemblyPath
            $size = [math]::Round($fileInfo.Length / 1KB, 1)
            Write-Status $assembly "OK ($size KB)" "Green"
            $results += @{ Name = $assembly; Status = "Found"; Size = $size }
        } else {
            Write-Status $assembly "MISSING" "Red"
            $results += @{ Name = $assembly; Status = "Missing"; Size = 0 }
        }
    }
    
    return $results
}

function Test-DemoScenes {
    param([string]$ProjectPath)
    
    Write-Header "Checking Demo Scenes"
    
    $scenesPath = Join-Path $ProjectPath "Assets\EasyPath\Assets\SwarmAI\Demo\Scenes"
    $expectedScenes = @(
        "SwarmAI_FlockingDemo.unity",
        "SwarmAI_FormationDemo.unity",
        "SwarmAI_ResourceGatheringDemo.unity",
        "SwarmAI_CombatFormationsDemo.unity"
    )
    
    $results = @()
    
    foreach ($scene in $expectedScenes) {
        $scenePath = Join-Path $scenesPath $scene
        if (Test-Path $scenePath) {
            Write-Status $scene "Found" "Green"
            $results += @{ Name = $scene; Status = "Found" }
        } else {
            Write-Status $scene "Not Created" "Yellow"
            $results += @{ Name = $scene; Status = "Not Created" }
        }
    }
    
    if ($results | Where-Object { $_.Status -eq "Not Created" }) {
        Write-Host ""
        Write-Host "  Note: Demo scenes need to be created in Unity Editor" -ForegroundColor Yellow
        Write-Host "  Use menu: SwarmAI > Create Demo Scene > Create All Demo Scenes" -ForegroundColor Yellow
    }
    
    return $results
}

function Generate-Report {
    param(
        [array]$VersionResults,
        [array]$AssemblyResults,
        [array]$SceneResults,
        [string]$OutputPath
    )
    
    $report = @"
# SwarmAI Compatibility Test Report

**Generated:** $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")
**Project:** $ProjectPath

## Unity Version Compatibility

| Version | Compilation | Errors | Warnings | Duration |
|---------|-------------|--------|----------|----------|
"@
    
    foreach ($result in $VersionResults) {
        $status = if ($result.Compilation -eq "Pass") { "Pass" } else { "**FAIL**" }
        $report += "`n| $($result.Version) | $status | $($result.Errors.Count) | $($result.Warnings.Count) | $([math]::Round($result.Duration, 1))s |"
    }
    
    $report += @"


## Assembly Status

| Assembly | Status | Size |
|----------|--------|------|
"@
    
    foreach ($assembly in $AssemblyResults) {
        $status = if ($assembly.Status -eq "Found") { "Found" } else { "**Missing**" }
        $report += "`n| $($assembly.Name) | $status | $($assembly.Size) KB |"
    }
    
    $report += @"


## Demo Scenes

| Scene | Status |
|-------|--------|
"@
    
    foreach ($scene in $SceneResults) {
        $status = if ($scene.Status -eq "Found") { "Found" } else { "Not Created" }
        $report += "`n| $($scene.Name) | $status |"
    }
    
    $report += @"


## Notes

- Demo scenes must be created manually in Unity Editor
- Use menu: **SwarmAI > Create Demo Scene > Create All Demo Scenes**
- After creating scenes: **SwarmAI > Add Demo Scenes to Build Settings**

## Next Steps

1. Open project in each Unity version listed
2. Create demo scenes via editor menu
3. Test each demo (see TESTING-GUIDE.md)
4. Build and test standalone builds
5. Complete Asset Store submission
"@
    
    $report | Out-File -FilePath $OutputPath -Encoding UTF8
    Write-Host ""
    Write-Host "Report saved to: $OutputPath" -ForegroundColor Green
}

# Main execution
Write-Host ""
Write-Host "SwarmAI Unity Compatibility Tester" -ForegroundColor Cyan
Write-Host "===================================" -ForegroundColor Cyan

# Find Unity installations
$installations = Find-UnityInstallations

# Check assemblies in current project
$assemblyResults = Test-SwarmAIAssemblies -ProjectPath $AssetsPath

# Check demo scenes
$sceneResults = Test-DemoScenes -ProjectPath $AssetsPath

# Test each target version if found
$versionResults = @()

Write-Header "Testing Unity Versions"

foreach ($target in $TargetVersions) {
    $found = $installations | Where-Object { $_.Version -like "*$($target.Pattern)*" } | Select-Object -First 1
    
    if ($found) {
        Write-Host "  $($target.Name) ($($found.Version))" -ForegroundColor White
        $result = Test-UnityVersion -UnityPath $found.Path -VersionName $target.Name -ProjectPath $AssetsPath
        $versionResults += $result
    } else {
        Write-Host "  $($target.Name)" -NoNewline
        Write-Host " [NOT INSTALLED]" -ForegroundColor Yellow
        $versionResults += @{
            Version = $target.Name
            Compilation = "Not Tested"
            Errors = @()
            Warnings = @()
            Duration = 0
        }
    }
}

# Generate report
Generate-Report -VersionResults $versionResults -AssemblyResults $assemblyResults -SceneResults $sceneResults -OutputPath $ReportPath

# Summary
Write-Header "Summary"

$passCount = ($versionResults | Where-Object { $_.Compilation -eq "Pass" }).Count
$totalTested = ($versionResults | Where-Object { $_.Compilation -ne "Not Tested" }).Count

if ($totalTested -gt 0) {
    Write-Host "  Versions tested: $totalTested" -ForegroundColor White
    Write-Host "  Passed: $passCount" -ForegroundColor $(if ($passCount -eq $totalTested) { "Green" } else { "Yellow" })
} else {
    Write-Host "  No Unity versions tested (none installed)" -ForegroundColor Yellow
}

$missingAssemblies = ($assemblyResults | Where-Object { $_.Status -eq "Missing" }).Count
if ($missingAssemblies -gt 0) {
    Write-Host "  Missing assemblies: $missingAssemblies" -ForegroundColor Red
} else {
    Write-Host "  All assemblies present" -ForegroundColor Green
}

$missingScenes = ($sceneResults | Where-Object { $_.Status -eq "Not Created" }).Count
if ($missingScenes -gt 0) {
    Write-Host "  Demo scenes to create: $missingScenes" -ForegroundColor Yellow
} else {
    Write-Host "  All demo scenes created" -ForegroundColor Green
}

Write-Host ""
Write-Host "Done! See test-report.md for full details." -ForegroundColor Cyan
Write-Host ""
