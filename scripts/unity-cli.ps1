# Unity CLI Automation Script for Codebuff Integration
# This script provides command-line automation for Unity development
# Usage: .\unity-cli.ps1 -Action <build|test|compile|validate|info>

param(
    [Parameter(Mandatory=$true)]
    [ValidateSet('build', 'test', 'compile', 'validate', 'info', 'create-project')]
    [string]$Action,
    
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = "",
    
    [Parameter(Mandatory=$false)]
    [string]$BuildTarget = "Win64",
    
    [Parameter(Mandatory=$false)]
    [string]$OutputPath = "Builds",
    
    [Parameter(Mandatory=$false)]
    [string]$TestPlatform = "EditMode"
)

# Configuration - Update these paths after Unity installation
$script:Config = @{
    UnityHubPath = "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe"
    UnityEditorPaths = @(
        "C:\Program Files\Unity\Hub\Editor",
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor"
    )
    VSPath = "C:\Program Files\Microsoft Visual Studio\2022\Community"
    DefaultProjectRoot = "$PSScriptRoot\..\assets"
    LogPath = "$PSScriptRoot\..\logs"
}

# Find Unity Editor installation
function Find-UnityEditor {
    foreach ($basePath in $script:Config.UnityEditorPaths) {
        if (Test-Path $basePath) {
            $editors = Get-ChildItem $basePath -Directory | Sort-Object Name -Descending
            foreach ($editor in $editors) {
                $exePath = Join-Path $editor.FullName "Editor\Unity.exe"
                if (Test-Path $exePath) {
                    return $exePath
                }
            }
        }
    }
    return $null
}

# Get project path (auto-detect or use provided)
function Get-ProjectPath {
    param([string]$Provided)
    
    if ($Provided -and (Test-Path $Provided)) {
        return (Resolve-Path $Provided).Path
    }
    
    # Auto-detect: look in assets/ folder
    $assetsDir = Join-Path $PSScriptRoot "..\assets"
    if (Test-Path $assetsDir) {
        $projects = Get-ChildItem $assetsDir -Directory | Where-Object {
            Test-Path (Join-Path $_.FullName "Assets")
        }
        if ($projects.Count -eq 1) {
            return $projects[0].FullName
        } elseif ($projects.Count -gt 1) {
            Write-Host "Multiple Unity projects found. Please specify -ProjectPath:" -ForegroundColor Yellow
            $projects | ForEach-Object { Write-Host "  - $($_.FullName)" }
            return $null
        }
    }
    
    Write-Host "No Unity project found. Create one with: .\unity-cli.ps1 -Action create-project" -ForegroundColor Yellow
    return $null
}

# Ensure log directory exists
function Ensure-LogDir {
    if (-not (Test-Path $script:Config.LogPath)) {
        New-Item -ItemType Directory -Path $script:Config.LogPath -Force | Out-Null
    }
}

# Action: Show environment info
function Show-Info {
    Write-Host "`n=== Unity Development Environment ==="  -ForegroundColor Cyan
    
    # Unity Hub
    $hubExists = Test-Path $script:Config.UnityHubPath
    Write-Host "`nUnity Hub: " -NoNewline
    if ($hubExists) {
        Write-Host "INSTALLED" -ForegroundColor Green
        Write-Host "  Path: $($script:Config.UnityHubPath)"
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
        Write-Host "  Expected: $($script:Config.UnityHubPath)"
    }
    
    # Unity Editor
    $unityExe = Find-UnityEditor
    Write-Host "`nUnity Editor: " -NoNewline
    if ($unityExe) {
        Write-Host "INSTALLED" -ForegroundColor Green
        Write-Host "  Path: $unityExe"
        # Get version from path
        $version = ($unityExe -split '\\Editor\\')[0] | Split-Path -Leaf
        Write-Host "  Version: $version"
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
        Write-Host "  Install via Unity Hub"
    }
    
    # Visual Studio
    $vsExists = Test-Path $script:Config.VSPath
    Write-Host "`nVisual Studio 2022: " -NoNewline
    if ($vsExists) {
        Write-Host "INSTALLED" -ForegroundColor Green
        Write-Host "  Path: $($script:Config.VSPath)"
        
        # Check Unity workload
        $unityTools = Test-Path "$($script:Config.VSPath)\Common7\IDE\Extensions\Microsoft\Visual Studio Tools for Unity"
        Write-Host "  Unity Tools: " -NoNewline
        if ($unityTools) {
            Write-Host "INSTALLED" -ForegroundColor Green
        } else {
            Write-Host "NOT INSTALLED" -ForegroundColor Yellow
            Write-Host "    Install via: Tools > Get Tools and Features > Game development with Unity"
        }
    } else {
        Write-Host "NOT FOUND" -ForegroundColor Red
    }
    
    # Git
    Write-Host "`nGit: " -NoNewline
    try {
        $gitVersion = git --version 2>&1
        Write-Host "INSTALLED" -ForegroundColor Green
        Write-Host "  Version: $gitVersion"
    } catch {
        Write-Host "NOT FOUND" -ForegroundColor Red
    }
    
    # .NET SDK
    Write-Host "`n.NET SDK: " -NoNewline
    try {
        $dotnetVersion = dotnet --version 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-Host "INSTALLED" -ForegroundColor Green
            Write-Host "  Version: $dotnetVersion"
        } else {
            Write-Host "NOT FOUND" -ForegroundColor Yellow
            Write-Host "  (Optional - Unity has built-in compiler)"
        }
    } catch {
        Write-Host "NOT FOUND" -ForegroundColor Yellow
        Write-Host "  (Optional - Unity has built-in compiler)"
    }
    
    # Unity Projects
    Write-Host "`n=== Unity Projects ==="  -ForegroundColor Cyan
    $projectPath = Get-ProjectPath -Provided $ProjectPath
    if ($projectPath) {
        Write-Host "Found: $projectPath" -ForegroundColor Green
    } else {
        Write-Host "No projects in assets/ folder" -ForegroundColor Yellow
    }
    
    Write-Host ""
}

# Action: Build Unity project
function Invoke-Build {
    $unityExe = Find-UnityEditor
    if (-not $unityExe) {
        Write-Host "ERROR: Unity Editor not found. Install via Unity Hub." -ForegroundColor Red
        return $false
    }
    
    $project = Get-ProjectPath -Provided $ProjectPath
    if (-not $project) {
        return $false
    }
    
    Ensure-LogDir
    $logFile = Join-Path $script:Config.LogPath "build_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    $buildOutput = Join-Path $project $OutputPath
    
    Write-Host "`n=== Building Unity Project ==="  -ForegroundColor Cyan
    Write-Host "Project: $project"
    Write-Host "Target: $BuildTarget"
    Write-Host "Output: $buildOutput"
    Write-Host "Log: $logFile"
    Write-Host ""
    
    # Ensure BuildScript exists
    $buildScriptPath = Join-Path $project "Assets\Editor\BuildScript.cs"
    if (-not (Test-Path $buildScriptPath)) {
        Write-Host "WARNING: BuildScript.cs not found. Creating it..." -ForegroundColor Yellow
        $editorDir = Join-Path $project "Assets\Editor"
        if (-not (Test-Path $editorDir)) {
            New-Item -ItemType Directory -Path $editorDir -Force | Out-Null
        }
        Copy-Item "$PSScriptRoot\templates\BuildScript.cs" $buildScriptPath -Force
    }
    
    $arguments = @(
        "-batchmode",
        "-projectPath", "`"$project`"",
        "-buildTarget", $BuildTarget,
        "-executeMethod", "BuildScript.BuildFromCLI",
        "-logFile", "`"$logFile`"",
        "-quit"
    )
    
    Write-Host "Starting build..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    if ($process.ExitCode -eq 0) {
        Write-Host "`nBuild SUCCEEDED" -ForegroundColor Green
        return $true
    } else {
        Write-Host "`nBuild FAILED (Exit code: $($process.ExitCode))" -ForegroundColor Red
        Write-Host "Check log file: $logFile"
        
        # Show last 20 lines of log
        if (Test-Path $logFile) {
            Write-Host "`n--- Last 20 lines of log ---" -ForegroundColor Yellow
            Get-Content $logFile -Tail 20
        }
        return $false
    }
}

# Action: Run Unity tests
function Invoke-Tests {
    $unityExe = Find-UnityEditor
    if (-not $unityExe) {
        Write-Host "ERROR: Unity Editor not found." -ForegroundColor Red
        return $false
    }
    
    $project = Get-ProjectPath -Provided $ProjectPath
    if (-not $project) {
        return $false
    }
    
    Ensure-LogDir
    $logFile = Join-Path $script:Config.LogPath "test_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    $resultsFile = Join-Path $script:Config.LogPath "test_results_$(Get-Date -Format 'yyyyMMdd_HHmmss').xml"
    
    Write-Host "`n=== Running Unity Tests ==="  -ForegroundColor Cyan
    Write-Host "Project: $project"
    Write-Host "Platform: $TestPlatform"
    Write-Host "Log: $logFile"
    Write-Host "Results: $resultsFile"
    Write-Host ""
    
    $arguments = @(
        "-batchmode",
        "-projectPath", "`"$project`"",
        "-runTests",
        "-testPlatform", $TestPlatform,
        "-testResults", "`"$resultsFile`"",
        "-logFile", "`"$logFile`"",
        "-quit"
    )
    
    Write-Host "Running tests..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    # Parse results
    if (Test-Path $resultsFile) {
        [xml]$results = Get-Content $resultsFile
        $total = $results.'test-run'.total
        $passed = $results.'test-run'.passed
        $failed = $results.'test-run'.failed
        
        Write-Host "`n--- Test Results ---" -ForegroundColor Cyan
        Write-Host "Total: $total | Passed: $passed | Failed: $failed"
        
        if ([int]$failed -gt 0) {
            Write-Host "`nFailed tests:" -ForegroundColor Red
            $results.SelectNodes("//test-case[@result='Failed']") | ForEach-Object {
                Write-Host "  - $($_.name)" -ForegroundColor Red
            }
            return $false
        } else {
            Write-Host "`nAll tests PASSED" -ForegroundColor Green
            return $true
        }
    } else {
        Write-Host "No test results found. Check log: $logFile" -ForegroundColor Yellow
        return $false
    }
}

# Action: Compile/validate C# code
function Invoke-Compile {
    $unityExe = Find-UnityEditor
    if (-not $unityExe) {
        Write-Host "ERROR: Unity Editor not found." -ForegroundColor Red
        return $false
    }
    
    $project = Get-ProjectPath -Provided $ProjectPath
    if (-not $project) {
        return $false
    }
    
    Ensure-LogDir
    $logFile = Join-Path $script:Config.LogPath "compile_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    
    Write-Host "`n=== Compiling Unity Project ==="  -ForegroundColor Cyan
    Write-Host "Project: $project"
    Write-Host "Log: $logFile"
    Write-Host ""
    
    # Unity compiles on project open in batchmode
    $arguments = @(
        "-batchmode",
        "-projectPath", "`"$project`"",
        "-logFile", "`"$logFile`"",
        "-quit"
    )
    
    Write-Host "Compiling scripts..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    # Check log for compilation errors
    if (Test-Path $logFile) {
        $errors = Select-String -Path $logFile -Pattern "error CS\d+" -AllMatches
        $warnings = Select-String -Path $logFile -Pattern "warning CS\d+" -AllMatches
        
        if ($errors.Count -gt 0) {
            Write-Host "`nCompilation FAILED" -ForegroundColor Red
            Write-Host "Errors found: $($errors.Count)" -ForegroundColor Red
            Write-Host "`n--- Errors ---" -ForegroundColor Red
            $errors | ForEach-Object { Write-Host $_.Line }
            return $false
        } else {
            Write-Host "`nCompilation SUCCEEDED" -ForegroundColor Green
            if ($warnings.Count -gt 0) {
                Write-Host "Warnings: $($warnings.Count)" -ForegroundColor Yellow
            }
            return $true
        }
    } else {
        Write-Host "Log file not created. Unity may have failed to start." -ForegroundColor Red
        return $false
    }
}

# Action: Validate environment and project
function Invoke-Validate {
    Write-Host "`n=== Validating Environment ==="  -ForegroundColor Cyan
    
    $allGood = $true
    
    # Check Unity
    $unityExe = Find-UnityEditor
    if ($unityExe) {
        Write-Host "[OK] Unity Editor found" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Unity Editor not found" -ForegroundColor Red
        $allGood = $false
    }
    
    # Check VS
    if (Test-Path $script:Config.VSPath) {
        Write-Host "[OK] Visual Studio 2022 found" -ForegroundColor Green
    } else {
        Write-Host "[FAIL] Visual Studio 2022 not found" -ForegroundColor Red
        $allGood = $false
    }
    
    # Check project
    $project = Get-ProjectPath -Provided $ProjectPath
    if ($project) {
        Write-Host "[OK] Unity project found: $project" -ForegroundColor Green
        
        # Check for required files
        $requiredDirs = @("Assets", "ProjectSettings")
        foreach ($dir in $requiredDirs) {
            $path = Join-Path $project $dir
            if (Test-Path $path) {
                Write-Host "  [OK] $dir folder exists" -ForegroundColor Green
            } else {
                Write-Host "  [WARN] $dir folder missing" -ForegroundColor Yellow
            }
        }
        
        # Check for Editor scripts
        $editorPath = Join-Path $project "Assets\Editor"
        if (Test-Path $editorPath) {
            Write-Host "  [OK] Editor scripts folder exists" -ForegroundColor Green
        } else {
            Write-Host "  [INFO] No Editor scripts folder yet" -ForegroundColor Cyan
        }
    } else {
        Write-Host "[INFO] No Unity project found (create one first)" -ForegroundColor Cyan
    }
    
    Write-Host ""
    if ($allGood) {
        Write-Host "Environment is ready for Unity development!" -ForegroundColor Green
    } else {
        Write-Host "Some components are missing. Install them before proceeding." -ForegroundColor Yellow
    }
    
    return $allGood
}

# Action: Create new Unity project
function Invoke-CreateProject {
    $unityExe = Find-UnityEditor
    if (-not $unityExe) {
        Write-Host "ERROR: Unity Editor not found. Install via Unity Hub first." -ForegroundColor Red
        return $false
    }
    
    $projectName = if ($ProjectPath) { Split-Path $ProjectPath -Leaf } else { "EasyPath" }
    $projectDir = Join-Path (Join-Path $PSScriptRoot "..\assets") $projectName
    
    if (Test-Path $projectDir) {
        Write-Host "ERROR: Project already exists at $projectDir" -ForegroundColor Red
        return $false
    }
    
    Ensure-LogDir
    $logFile = Join-Path $script:Config.LogPath "create_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
    
    Write-Host "`n=== Creating Unity Project ==="  -ForegroundColor Cyan
    Write-Host "Name: $projectName"
    Write-Host "Path: $projectDir"
    Write-Host "Log: $logFile"
    Write-Host ""
    
    $arguments = @(
        "-batchmode",
        "-createProject", "`"$projectDir`"",
        "-logFile", "`"$logFile`"",
        "-quit"
    )
    
    Write-Host "Creating project (this may take a minute)..." -ForegroundColor Yellow
    $process = Start-Process -FilePath $unityExe -ArgumentList $arguments -Wait -PassThru -NoNewWindow
    
    if ((Test-Path $projectDir) -and (Test-Path (Join-Path $projectDir "Assets"))) {
        Write-Host "`nProject created SUCCESSFULLY" -ForegroundColor Green
        Write-Host "Location: $projectDir"
        
        # Create Editor folder and BuildScript
        $editorDir = Join-Path $projectDir "Assets\Editor"
        New-Item -ItemType Directory -Path $editorDir -Force | Out-Null
        
        $templatesDir = Join-Path $PSScriptRoot "templates"
        if (Test-Path $templatesDir) {
            Copy-Item "$templatesDir\*.cs" $editorDir -Force
            Write-Host "Added automation scripts to Assets\Editor"
        }
        
        return $true
    } else {
        Write-Host "`nProject creation FAILED" -ForegroundColor Red
        Write-Host "Check log: $logFile"
        return $false
    }
}

# Main execution
switch ($Action) {
    'info'           { Show-Info }
    'build'          { Invoke-Build }
    'test'           { Invoke-Tests }
    'compile'        { Invoke-Compile }
    'validate'       { Invoke-Validate }
    'create-project' { Invoke-CreateProject }
}
