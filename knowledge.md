# Project Knowledge - Unity Asset Toolkit 
 
Unity Asset Store project for AI/pathfinding tools. C#, Unity 7 LTS, Visual Studio 2022, Windows 11. 
 
## Quickstart 

```powershell
# Check environment status
.\scripts\unity-cli.ps1 -Action info

# Validate setup
.\scripts\unity-cli.ps1 -Action validate

# Compile and check for errors
.\scripts\unity-cli.ps1 -Action compile

# Run unit tests
.\scripts\unity-cli.ps1 -Action test

# Build project
.\scripts\unity-cli.ps1 -Action build -BuildTarget Win64
```

**Manual Steps (in Unity Editor):**
- Open Unity Hub and add project from assets/ folder
- Click Play to test in editor
- Add scenes to Build Settings 
 
## Project Status 
 
**Phase:** Development (January 2026) 
**Current Project:** EasyPath - A* Pathfinding Asset 
**Status:** ✅ Working! Pathfinding tested and functional in Unity 6 
**Next Step:** Create more demo scenes, add unit tests, write documentation 
 
## Development Environment 

- **Unity 7 LTS** - Installed via Unity Hub 
- **Visual Studio 2022** - Primary IDE (C:\Program Files\Microsoft Visual Studio\2022\Community)
- **Git 2.45.1** - Version control 
- **Git LFS 3.5.1** - Large file storage for binary assets
- **Windows 11** - OS
- **PowerShell** - CLI automation (not WSL)

## Codebuff CLI Integration

Codebuff can autonomously build, test, and validate via command line:

| Command | Description |
|---------|-------------|
| `unity-cli.ps1 -Action info` | Show environment status |
| `unity-cli.ps1 -Action compile` | Compile and check errors |
| `unity-cli.ps1 -Action test` | Run unit tests headlessly |
| `unity-cli.ps1 -Action build` | Build for target platform |
| `unity-cli.ps1 -Action validate` | Validate environment setup |
| `unity-cli.ps1 -Action create-project` | Create new Unity project |

See `guides/CODEBUFF-UNITY-INTEGRATION.md` for full documentation. 
 
## Architecture 
 
``` 
unity-asset-toolkit/ 
+-- assets/           # Unity project(s) 
|   +-- EasyPath/     # A* Pathfinding Asset (COMPILING ✅) 
+-- docs/             # Setup guides, checklists 
+-- notes/            # Planning, research, learnings 
+-- prototypes/       # Quick C# experiments 
+-- scripts/          # Build and automation tools 
+-- knowledge.md      # This file (Codebuff context) 
+-- README.md         # Project overview 
``` 
 
## Potential Products 
 
| Asset | Description | Price | Complexity | 
|-------|-------------|-------|------------| 
| EasyPath | Simple A* pathfinding for beginners | $35 | 2-3 weeks | 
| SwarmAI | Multi-agent coordination system | $45 | 3-4 weeks | 
| NPCBrain | All-in-one AI toolkit | $60 | 4-6 weeks | 
 
**Recommendation:** Start with EasyPath - lowest complexity, clear market demand. 
 
## Conventions 
 
- **C# naming:** PascalCase (classes, methods), camelCase (local variables) 
- **Unity 7 LTS** target version 
- **Visual Studio 2022** as IDE 
- **Asset Store submission guidelines** must be followed 
- **Full source code** included in all assets 
- **Demo scenes** required for each feature 
 
## Skills from Battlecode 2026 
 
This project leverages skills learned from MIT Battlecode 2026: 
- State machine design for AI agents 
- A* and Bug2 pathfinding algorithms 
- Multi-agent coordination 
- Resource/economy management 
- Performance optimization 
- Debug and logging systems 
 
## Key Patterns to Implement 
 
From Battlecode experience: 
- tryMove() helper - prefer efficient movement 
- State enums (IDLE, MOVING, ATTACKING, etc.) 
- Distance-based decision making 
- Threat avoidance algorithms 
- ID-based agent differentiation 
 
## Key Links 
 
- Unity Asset Store: https://assetstore.unity.com 
- Publisher Portal: https://publisher.unity.com 
- Unity Docs: https://docs.unity3d.com 
- C# Docs: https://docs.microsoft.com/en-us/dotnet/csharp/ 
 
## File Descriptions 

- **docs/SETUP.md** - Development environment setup guide 
- **docs/CHECKLIST.md** - Environment setup checklist with status 
- **notes/project-ideas.md** - Detailed comparison of asset options 
- **notes/battlecode-learnings.md** - Skills and patterns from competition

## Automation Scripts

### Core Scripts
- **scripts/unity-cli.ps1** - Main CLI automation script for builds, tests, compilation
- **scripts/quick.ps1** - Quick shortcut commands
- **scripts/preflight.ps1** - Pre-development validation (run before opening Unity)

### Validation Scripts
- **scripts/validate-asmdef.ps1** - Check assembly definitions for GUID references (Unity 6 requirement)
- **scripts/check-deprecated-api.ps1** - Scan for deprecated Unity APIs
- **scripts/check-compile.ps1** - Check compilation status and errors
- **scripts/read-unity-log.ps1** - Read and analyze Unity Editor.log

### Template Scripts (copied to new projects)
- **scripts/templates/BuildScript.cs** - CLI build automation
- **scripts/templates/CompileValidator.cs** - Compilation validation
- **scripts/templates/TestRunner.cs** - Test automation

### Quick Commands
```powershell
# Before opening Unity - run all validators
.\scripts\preflight.ps1

# Check assembly definitions for issues
.\scripts\validate-asmdef.ps1

# Scan for deprecated APIs
.\scripts\check-deprecated-api.ps1

# Check compilation status
.\scripts\check-compile.ps1

# Read Unity log for errors
.\scripts\read-unity-log.ps1

# Setup Git hooks (run once)
.\scripts\setup-hooks.ps1
```

## Reference Guides

- **guides/UNITY-CSHARP-BEST-PRACTICES.md** - C# coding standards, MonoBehaviour lifecycle, coroutines, memory management, performance optimization
- **guides/AI-PATHFINDING-PATTERNS.md** - State machines, behavior trees, A* algorithm, steering behaviors, multi-agent coordination
- **guides/ASSET-STORE-GUIDELINES.md** - Submission requirements, documentation standards, pricing strategies, review process
- **guides/UNITY-PROJECT-STRUCTURE.md** - Folder organization, naming conventions, assembly definitions, editor scripting

## EasyPath Asset Structure

```
assets/EasyPath/Assets/EasyPath/
+-- Runtime/              # Core pathfinding (EasyPath.Runtime.dll)
|   +-- Components/       # EasyPathGrid, EasyPathAgent
|   +-- Core/             # AStarPathfinder, PathNode, PriorityQueue
|   +-- Data/             # EasyPathSettings
+-- Editor/               # Editor tools (EasyPath.Editor.dll)
|   +-- Inspectors/       # Custom inspectors for Grid/Agent
|   +-- Windows/          # EasyPathDebugWindow
+-- Demo/                 # Demo scripts (EasyPath.Demo.dll)
    +-- Scripts/          # ClickToMove, DemoController
```

**Unity Menu Items:**
- Window → EasyPath → Debug Window
- Tools → EasyPath → Check Compilation
- EasyPath → Create Demo Scene → Basic/Multi-Agent/Stress Test
- GameObject → EasyPath → Create Grid / Create Agent
- Build → Quick Build / Release Build

**Demo Scene Controls (during Play mode):**
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

## Unity 6 Compatibility Notes

- Use `FindFirstObjectByType<T>()` instead of deprecated `FindObjectOfType<T>()`
- Assembly definitions should use GUID references, not name references
- Editor assemblies need `"includePlatforms": ["Editor"]` in .asmdef
- Grid obstacle detection uses `_obstacleCheckHeight` (default 0.5f) to check above ground plane

## Common Issues & Solutions

| Issue | Cause | Solution |
|-------|-------|----------|
| Editor scripts not compiling | Name-based asmdef references | Use GUID references: `"GUID:xxx"` |
| Unity enters Safe Mode | Missing assembly reference | Add dependency to .asmdef file |
| All pathfinding fails | Grid detecting ground as obstacle | Set obstacle layer, increase check height |
| Menu items not appearing | Domain reload needed | Reimport All or restart Unity |
| PowerShell log reading fails | Environment variable stripping | Scripts now use `[Environment]::GetFolderPath()` |

## Runtime Diagnostics

EasyPathGrid now includes automatic diagnostics:
- Warns if <10% of cells are walkable (configuration issue)
- Warns if obstacle layer is set to "Everything"
- Warns if no obstacle layer is set
- Logs grid build statistics

## Git & CI/CD

### Git LFS
Large binary files (textures, audio, models) are tracked with Git LFS via `.gitattributes`.

### Pre-commit Hooks
Installed via `.\scripts\setup-hooks.ps1`. Validates:
- Missing meta files for Unity assets
- Orphan meta files (no matching asset)
- Large files not tracked by LFS

### GitHub Actions CI/CD
Workflow at `.github/workflows/unity-ci.yml`:
- **Pre-flight checks**: Validates asmdef files, checks for deprecated APIs
- **Tests**: Runs EditMode and PlayMode tests
- **Builds**: Windows and WebGL builds on main branch

Requires GitHub secrets:
- `UNITY_LICENSE` - Unity license file content
- `UNITY_EMAIL` - Unity account email
- `UNITY_PASSWORD` - Unity account password

See: https://game.ci/docs/github/activation for license setup.

## VS Code Integration

Recommended extensions in `.vscode/extensions.json`:
- C# Dev Kit
- Unity Tools
- GitLens
- PowerShell

Project settings in `.vscode/settings.json` and `.editorconfig`.
