# Codebuff + Unity Integration Guide

This guide explains how Codebuff (AI assistant) integrates with your Unity development workflow for maximum automation.

## Overview

Codebuff can autonomously:
- ✅ Write and edit C# scripts
- ✅ Build Unity projects via command line
- ✅ Run unit tests headlessly
- ✅ Validate compilation and fix errors
- ✅ Create project structure and boilerplate
- ✅ Manage assembly definitions and project files

Codebuff requires you to:
- 🎮 Click Play to test in Unity Editor
- 🎨 Design scenes visually
- 📦 Import assets (textures, models, audio)
- 👁️ Review visual output

## Quick Reference

```powershell
# Check environment status
.\scripts\unity-cli.ps1 -Action info

# Validate everything is set up
.\scripts\unity-cli.ps1 -Action validate

# Create a new Unity project
.\scripts\unity-cli.ps1 -Action create-project -ProjectPath "MyProject"

# Compile and check for errors
.\scripts\unity-cli.ps1 -Action compile

# Run unit tests
.\scripts\unity-cli.ps1 -Action test

# Build the project
.\scripts\unity-cli.ps1 -Action build -BuildTarget Win64
```

## Workflow Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    UNITY DEVELOPMENT WORKFLOW                    │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│   ┌─────────────────┐         ┌─────────────────────────────┐   │
│   │   YOU (Human)   │         │      CODEBUFF (AI)          │   │
│   ├─────────────────┤         ├─────────────────────────────┤   │
│   │ • Open Unity    │ ──────► │ • Write C# scripts          │   │
│   │ • Play test     │         │ • Run CLI builds            │   │
│   │ • Visual design │ ◄────── │ • Run tests                 │   │
│   │ • Attach scripts│         │ • Fix compilation errors    │   │
│   │ • Review output │         │ • Parse logs for issues     │   │
│   └─────────────────┘         └─────────────────────────────┘   │
│           │                               │                      │
│           ▼                               ▼                      │
│   ┌─────────────────┐         ┌─────────────────────────────┐   │
│   │  Unity Editor   │         │     PowerShell CLI          │   │
│   │  (GUI)          │         │     unity-cli.ps1           │   │
│   └─────────────────┘         └─────────────────────────────┘   │
│           │                               │                      │
│           └───────────────┬───────────────┘                      │
│                           ▼                                      │
│                   ┌───────────────┐                              │
│                   │ Unity Project │                              │
│                   │ (assets/)     │                              │
│                   └───────────────┘                              │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

## Detailed Capabilities

### 1. Code Writing & Editing

Codebuff can write any C# code for Unity:

```csharp
// MonoBehaviours
public class PlayerController : MonoBehaviour { ... }

// ScriptableObjects
[CreateAssetMenu]
public class GameSettings : ScriptableObject { ... }

// Editor Scripts
[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor { ... }

// Unit Tests
[Test]
public void PathfindingReturnsValidPath() { ... }
```

**How to use:**
- Ask Codebuff to create/modify scripts
- Codebuff writes the file directly
- You refresh Unity Editor (Ctrl+R) or it auto-refreshes

### 2. Compilation Validation

Codebuff can compile your project and identify errors:

```powershell
# Compile and check for errors
.\scripts\unity-cli.ps1 -Action compile -ProjectPath "assets\EasyPath"
```

**Output:**
```
=== Compiling Unity Project ===
Project: C:\Projects\unity-asset-toolkit\assets\EasyPath
Log: C:\Projects\unity-asset-toolkit\logs\compile_20260116_143022.log

Compiling scripts...

Compilation SUCCEEDED
Warnings: 2
```

If errors occur, Codebuff sees them and can fix them automatically.

### 3. Running Tests

Codebuff can run Unity Test Framework tests headlessly:

```powershell
# Run EditMode tests (fast, no Play mode)
.\scripts\unity-cli.ps1 -Action test -TestPlatform EditMode

# Run PlayMode tests (requires Unity to enter Play mode)
.\scripts\unity-cli.ps1 -Action test -TestPlatform PlayMode
```

**Output:**
```
=== Running Unity Tests ===
Project: C:\Projects\unity-asset-toolkit\assets\EasyPath
Platform: EditMode

Running tests...

--- Test Results ---
Total: 15 | Passed: 15 | Failed: 0

All tests PASSED
```

### 4. Building Projects

Codebuff can build your Unity project for any platform:

```powershell
# Build for Windows 64-bit
.\scripts\unity-cli.ps1 -Action build -BuildTarget Win64

# Build for WebGL
.\scripts\unity-cli.ps1 -Action build -BuildTarget WebGL

# Build for Android
.\scripts\unity-cli.ps1 -Action build -BuildTarget Android
```

### 5. Project Creation

Codebuff can create new Unity projects with proper structure:

```powershell
.\scripts\unity-cli.ps1 -Action create-project -ProjectPath "EasyPath"
```

This creates:
```
assets/EasyPath/
├── Assets/
│   ├── Editor/
│   │   ├── BuildScript.cs      # CLI build automation
│   │   ├── CompileValidator.cs # Compilation checking
│   │   └── TestRunner.cs       # Test automation
│   ├── Scripts/
│   ├── Scenes/
│   └── Tests/
├── Packages/
└── ProjectSettings/
```

## Typical Development Session

### Example: Adding a New Feature

1. **You ask Codebuff:**
   > "Add a GridVisualizer component that draws the pathfinding grid in the Scene view"

2. **Codebuff:**
   - Reads existing code for context
   - Writes `GridVisualizer.cs` with Gizmos code
   - Runs compilation check: `.\scripts\unity-cli.ps1 -Action compile`
   - If errors, fixes them automatically
   - Reports success

3. **You:**
   - Open Unity Editor
   - Add GridVisualizer component to a GameObject
   - View the visualization in Scene view
   - Provide feedback

4. **Codebuff:**
   - Iterates based on your feedback

### Example: Fixing a Bug

1. **You report:**
   > "The pathfinding is returning null sometimes"

2. **Codebuff:**
   - Reads the pathfinding code
   - Identifies the bug
   - Writes a unit test that reproduces it
   - Runs tests: `.\scripts\unity-cli.ps1 -Action test`
   - Test fails (expected)
   - Fixes the bug
   - Runs tests again
   - Test passes
   - Reports the fix

## File Locations

| File | Purpose |
|------|----------|
| `scripts/unity-cli.ps1` | Main automation script |
| `scripts/templates/BuildScript.cs` | Build automation template |
| `scripts/templates/CompileValidator.cs` | Compilation validation template |
| `scripts/templates/TestRunner.cs` | Test runner template |
| `logs/` | Build/test/compile logs |

## Environment Requirements

| Tool | Status | Notes |
|------|--------|-------|
| Unity Hub | Required | Manages Unity versions |
| Unity Editor | Required | Any LTS version (6+, 7) |
| Visual Studio 2022 | Recommended | With Unity workload |
| PowerShell 5.1+ | Required | Ships with Windows |
| Git | Recommended | Version control |

## Limitations

### What Codebuff Cannot Do

1. **See visual output** - Can't view Scene/Game views
2. **Click Unity UI** - Can't interact with Editor GUI
3. **Import assets** - Unity must process imports
4. **Run Play mode interactively** - Can trigger, but can't see results

### Workarounds

| Limitation | Workaround |
|------------|------------|
| Can't see visuals | You describe what you see, Codebuff adjusts code |
| Can't click Play | You click Play, report behavior, Codebuff fixes |
| Can't import assets | You import, Codebuff writes code to use them |

## Troubleshooting

### Unity not found
```powershell
# Check Unity installation
.\scripts\unity-cli.ps1 -Action info
```
Install Unity via Unity Hub if not found.

### Compilation errors in CLI
```powershell
# View full log
Get-Content .\logs\compile_*.log -Tail 50
```

### Tests not running
- Ensure Unity Test Framework is installed (Package Manager)
- Check that test assemblies are configured

### Build fails
- Check that scenes are added to Build Settings
- Verify build target platform is installed

## Best Practices

1. **Always compile after code changes**
   ```powershell
   .\scripts\unity-cli.ps1 -Action compile
   ```

2. **Write tests for new features**
   - Codebuff can run them automatically
   - Catches regressions early

3. **Use descriptive feedback**
   - "The enemy moves too fast" ✓
   - "It's broken" ✗

4. **Keep Unity Editor open**
   - Faster iteration
   - Immediate visual feedback

5. **Commit working code frequently**
   - Easy to rollback if needed
