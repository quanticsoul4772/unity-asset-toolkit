# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Asset Store toolkit for AI/pathfinding tools. Current focus: **EasyPath** - A* pathfinding asset for Unity 7 LTS.

**Technology Stack**: C#, Unity 6 (6000.x), Visual Studio 2022, Windows 11, PowerShell automation

## Quick Commands

### Development Workflow
```powershell
# Pre-flight validation (run before opening Unity)
.\scripts\preflight.ps1

# Check environment and Unity installation
.\scripts\unity-cli.ps1 -Action info

# Validate project setup
.\scripts\unity-cli.ps1 -Action validate

# Compile and check for errors (headless)
.\scripts\unity-cli.ps1 -Action compile

# Run tests (EditMode and PlayMode)
.\scripts\unity-cli.ps1 -Action test

# Build for Windows
.\scripts\unity-cli.ps1 -Action build -BuildTarget Win64
```

### Validation Scripts
```powershell
# Check assembly definitions for GUID references (Unity 6+ requirement)
.\scripts\validate-asmdef.ps1

# Scan for deprecated Unity APIs
.\scripts\check-deprecated-api.ps1

# Check compilation status
.\scripts\check-compile.ps1

# Read Unity Editor log
.\scripts\read-unity-log.ps1

# Setup Git hooks (run once)
.\scripts\setup-hooks.ps1
```

### In Unity Editor
- **Window → EasyPath → Debug Window** - Real-time pathfinding diagnostics
- **Tools → EasyPath → Check Compilation** - Verify assembly compilation
- **EasyPath → Create Demo Scene** - Generate Basic/Multi-Agent/Stress Test scenes
- **GameObject → EasyPath** - Create Grid or Agent components

## Architecture

### Project Structure
```
unity-asset-toolkit/
├── assets/EasyPath/           # Main Unity project (Unity 7 LTS)
│   ├── Assets/EasyPath/
│   │   ├── Runtime/           # Core pathfinding logic (EasyPath.Runtime.asmdef)
│   │   │   ├── Components/    # EasyPathGrid, EasyPathAgent
│   │   │   ├── Core/          # AStarPathfinder, PathNode, PriorityQueue
│   │   │   └── Data/          # EasyPathSettings
│   │   ├── Editor/            # Editor tools (EasyPath.Editor.asmdef)
│   │   │   ├── Inspectors/    # Custom inspectors
│   │   │   └── Windows/       # EasyPathDebugWindow
│   │   └── Demo/              # Demo scripts (EasyPath.Demo.asmdef)
│   │       └── Scripts/       # ClickToMove, DemoController
├── scripts/                   # PowerShell automation
│   ├── unity-cli.ps1         # Main CLI automation
│   ├── preflight.ps1         # Pre-development validation
│   └── templates/            # BuildScript.cs, TestRunner.cs
├── docs/                      # Setup guides
└── guides/                    # Best practices documentation
```

### Assembly Definitions (Critical)
**Unity 6+ requires GUID-based references in .asmdef files, NOT name-based references.**

Three assemblies:
1. **EasyPath.Runtime** (`32e8732f4adef96408db2fc8a96644eb`) - Core pathfinding, no external dependencies
2. **EasyPath.Editor** (`9485adf895dab1a4492c71fc321779b2`) - Editor tools, references Runtime, `includePlatforms: ["Editor"]`
3. **EasyPath.Demo** - Demo scripts, references Runtime

### EasyPath Core Architecture

**Grid System** (`EasyPathGrid.cs`):
- 2D grid overlayed on 3D world space
- Obstacle detection using Physics.CheckSphere at elevated height (`_obstacleCheckHeight`)
- Runtime diagnostics warn about misconfiguration (<10% walkable cells, "Everything" obstacle layer)
- `BuildGrid()` creates PathNode[,] array and initializes AStarPathfinder

**Pathfinding** (`AStarPathfinder.cs`):
- Classic A* with diagonal movement support (STRAIGHT_COST=10, DIAGONAL_COST=14)
- PriorityQueue for open set, HashSet for closed set
- Falls back to nearest walkable node if target blocked
- Returns List<Vector3> of world positions

**Agent System** (`EasyPathAgent.cs`):
- MonoBehaviour that moves along calculated paths
- Uses `FindFirstObjectByType<EasyPathGrid>()` (Unity 6+ API)
- Movement handled in Update() with configurable speed

## Critical Unity 6/7 Compatibility Notes

1. **Use `FindFirstObjectByType<T>()` instead of deprecated `FindObjectOfType<T>()`**
2. **Assembly definitions MUST use GUID references**: `"GUID:xxx"` not name strings
3. **Editor assemblies require**: `"includePlatforms": ["Editor"]` in .asmdef
4. **Grid obstacle detection**: Uses `_obstacleCheckHeight` (default 0.5f) to check above ground plane, avoiding false positives from the ground itself

## Coding Conventions

### C# Naming
- **Classes, Methods, Properties**: PascalCase
- **Private fields**: _camelCaseWithUnderscore
- **Local variables, parameters**: camelCase
- **Namespaces**: Match assembly name (e.g., `namespace EasyPath`)

### Unity Patterns
- **MonoBehaviour lifecycle**: Awake → Start → Update → OnDestroy
- **Serialized fields**: Use `[SerializeField]` for private inspector fields
- **Headers**: `[Header("Section Name")]` for Inspector organization
- **Tooltips**: `[Tooltip("Description")]` for user guidance

### Performance
- **Cache GetComponent calls** in Awake/Start
- **Use object pooling** for frequently instantiated objects
- **Avoid allocations in Update/FixedUpdate**
- **Physics.OverlapSphere** is expensive - cache results when possible

## Common Issues and Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Editor scripts not compiling | Name-based asmdef references | Use GUID references in .asmdef |
| Unity Safe Mode on startup | Missing assembly reference | Add dependency to .asmdef, or use `validate-asmdef.ps1` |
| All pathfinding fails | Grid detecting ground as obstacle | Configure obstacle layer properly, increase `_obstacleCheckHeight` |
| Menu items not appearing | Domain reload needed | Reimport All (Ctrl+R) or restart Unity |
| PowerShell scripts fail | Wrong execution policy | Run: `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` |

## Demo Scene Controls (Play Mode)

| Input | Action |
|-------|--------|
| Left-click | Move all agents to clicked position |
| Right-click | Spawn obstacle at position |
| Middle-click | Remove obstacle |
| Space | Send agents to random positions |
| W | Start wandering mode |
| S | Stop all agents |
| G | Gather agents to center |
| X | Scatter agents to corners |
| R | Rebuild pathfinding grid |

## Git Workflow

### Pre-commit Hooks
Installed via `.\scripts\setup-hooks.ps1`. Validates:
- Missing .meta files for Unity assets
- Orphan .meta files (no matching asset)
- Large files not tracked by Git LFS

### Git LFS Configuration
Binary assets (textures, audio, models) tracked via `.gitattributes`:
```
*.png filter=lfs diff=lfs merge=lfs -text
*.jpg filter=lfs diff=lfs merge=lfs -text
*.fbx filter=lfs diff=lfs merge=lfs -text
*.wav filter=lfs diff=lfs merge=lfs -text
```

### Unity .gitignore Patterns
Critical patterns in `.gitignore`:
- `[Ll]ibrary/` - Unity cache (regenerated on project open)
- `[Tt]emp/` - Temporary Unity files
- `[Oo]bj/` - Build artifacts
- `[Bb]uild/` - Build output
- `*.csproj`, `*.sln` - Generated by Unity, don't commit

## CI/CD (GitHub Actions)

Workflow: `.github/workflows/unity-ci.yml`
- **Pre-flight**: Validates asmdef files, checks for deprecated APIs
- **Tests**: Runs EditMode and PlayMode tests
- **Builds**: Windows and WebGL builds on main branch

Requires GitHub secrets:
- `UNITY_LICENSE` - Unity license file content
- `UNITY_EMAIL` - Unity account email
- `UNITY_PASSWORD` - Unity account password

Setup: https://game.ci/docs/github/activation

## VS Code Integration

Recommended extensions (`.vscode/extensions.json`):
- C# Dev Kit
- Unity Tools
- GitLens
- PowerShell

Settings configured in `.vscode/settings.json` and `.editorconfig`.

## Documentation References

Project guides in `guides/`:
- **UNITY-CSHARP-BEST-PRACTICES.md** - C# coding standards, lifecycle, coroutines
- **AI-PATHFINDING-PATTERNS.md** - State machines, behavior trees, A* algorithm
- **ASSET-STORE-GUIDELINES.md** - Submission requirements, pricing
- **UNITY-PROJECT-STRUCTURE.md** - Folder organization, assembly definitions

## Important Notes for AI Assistants

1. **ALWAYS run `.\scripts\preflight.ps1` before opening Unity** - catches issues early
2. **Use GUID references in .asmdef files** - Unity 6+ requirement
3. **Test in Play mode** - pathfinding requires runtime testing
4. **Check compilation via CLI** before committing - `.\scripts\unity-cli.ps1 -Action compile`
5. **Unity 6+ API changes** - Use `FindFirstObjectByType<T>()` not `FindObjectOfType<T>()`
6. **PowerShell is primary CLI** - Not WSL/Bash
7. **Grid diagnostics are automatic** - EasyPathGrid logs warnings about misconfiguration at runtime
