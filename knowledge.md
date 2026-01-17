# Project Knowledge - Unity Asset Toolkit

**Last Updated:** January 2026 
 
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
**Current Project:** NPCBrain - All-in-One AI Toolkit

### EasyPath Status
**Status:** ✅ Complete! Ready for Asset Store submission
- A* pathfinding working and tested
- Multi-agent click-to-move support
- Demo scenes: Basic, Multi-Agent, Stress Test
- Obstacle layer auto-configuration with "Fix Existing Demo Scenes" menu

### SwarmAI Status  
**Status:** ✅ Phase 1-4 Complete! All core features implemented.
- ✅ Phase 1: Core Framework (SwarmManager, SwarmAgent, States, Spatial Hash)
- ✅ Phase 2: Steering Behaviors (Seek, Flee, Arrive, Wander, Flocking, Obstacle Avoidance)
- ✅ Phase 3: Advanced Features (Formations, Resource Gathering, Groups, Messaging)
- ✅ Phase 4: Demo Scenes (Flocking, Formation, Resource Gathering)
- 🔄 Phase 5: Combat Formations Demo (in progress)

**Next Step:** Create Combat Formations demo, then prepare for Asset Store submission 
 
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
 
## Completed Products

| Asset | Description | Price | Status |
|-------|-------------|-------|--------|
| **EasyPath** | A* pathfinding for Unity | $35 | ✅ Complete |
| **SwarmAI** | Multi-agent coordination | $45 | ✅ Complete |

### EasyPath Features
- Grid-based A* pathfinding
- Custom inspectors
- Demo scenes: BasicDemo, MultiAgentDemo
- Click-to-move with multi-agent support
- Obstacle layer configuration

### SwarmAI Features
- 10+ steering behaviors (Seek, Flee, Arrive, Wander, Separation, Alignment, Cohesion, etc.)
- Formation system (Line, Column, Circle, Wedge, V, Box)
- State machine (Idle, Moving, Seeking, Fleeing, Gathering, Returning, Following)
- Resource gathering system
- Inter-agent messaging
- Spatial hash for efficient neighbor queries
- 4 demo scenes (Flocking, Formation, Resource Gathering, Combat Formations) 
 
## Conventions 

- **C# naming:** PascalCase (classes, methods), camelCase (local variables)
- **Unity 6+** target version (compatible with 2021.3+)
- **Visual Studio 2022** as IDE
- **Asset Store submission guidelines** followed
- **Full source code** included in all assets
- **Demo scenes** for each major feature
- **Unit tests** for core systems (NUnit + Unity Test Framework)
- **XML documentation** for all public APIs 
 
## Skills from Battlecode 2026 
 
This project leverages skills learned from MIT Battlecode 2026: 
- State machine design for AI agents 
- A* and Bug2 pathfinding algorithms 
- Multi-agent coordination 
- Resource/economy management 
- Performance optimization 
- Debug and logging systems 
 
## SwarmAI Key Patterns (Implemented)

From Battlecode experience:
- **State machine** - AgentState FSM (Idle, Moving, Seeking, Fleeing, Gathering, Returning, Following)
- **Steering behaviors** - IBehavior interface with weighted blending
- **Spatial partitioning** - SpatialHash<T> for O(1) neighbor queries
- **ID-based coordination** - AgentId for messaging and targeting
- **Formation system** - SwarmFormation with FormationSlotBehavior for stable formations
- **Per-frame caching** - Neighbor queries cached per frame for performance

## Performance Optimizations

- **Squared distances** - All behaviors use sqrMagnitude instead of magnitude
- **Neighbor query caching** - SwarmManager caches queries per frame
- **Spatial hash** - O(1) lookups for neighbor queries
- **Behavior weight blending** - Efficient weighted sum of steering forces 
 
## Test Coverage

### SwarmAI Tests (Editor Mode)
- `SwarmManagerTests.cs` - Singleton, settings, messages, states
- `SwarmAgentTests.cs` - Behaviors, properties, state machine
- `ResourceNodeTests.cs` - Harvesting, depletion, respawn logic
- `SpatialHashTests.cs` - Insert, remove, query, update
- `SteeringBehaviorTests.cs` - All steering behaviors
- `FormationTests.cs` - Formation patterns
- `FormationSlotBehaviorTests.cs` - Formation slot behavior

### SwarmAI Tests (PlayMode)
- `SwarmManagerPlayModeTests.cs` - Registration, messaging, spatial queries
- `SwarmAgentPlayModeTests.cs` - Movement, neighbors, state changes
- `ResourceNodePlayModeTests.cs` - Harvesting, events, static helpers
- `FlockingIntegrationTests.cs` - Full flocking behavior integration

## Key Links 

- Unity Asset Store: https://assetstore.unity.com
- Publisher Portal: https://publisher.unity.com
- Unity Docs: https://docs.unity3d.com
- C# Docs: https://docs.microsoft.com/en-us/dotnet/csharp/

## File Descriptions 

- **docs/SETUP.md** - Development environment setup guide
- **docs/CHECKLIST.md** - Environment setup checklist with status
- **docs/SWARMAI-DESIGN.md** - SwarmAI architecture and feature breakdown
- **notes/project-ideas.md** - Asset comparison and status
- **notes/battlecode-learnings.md** - Skills and patterns from competition

## SwarmAI Documentation

Located in `assets/EasyPath/Assets/SwarmAI/Documentation/`:
- **README.md** - Overview, features, quick start
- **GETTING-STARTED.md** - Step-by-step tutorials
- **BEHAVIORS.md** - All steering behaviors with diagrams
- **STATES.md** - Agent states and transitions
- **EXAMPLES.md** - Code samples
- **API-REFERENCE.md** - Complete API documentation
- **TROUBLESHOOTING.md** - Common issues and solutions

## Common Issues & Solutions

| Issue | Solution |
|-------|----------|
| Input Manager deprecation warning | Change Project Settings → Player → Active Input Handling to "Input System Package (New)" |
| SwarmManager cleanup warning | Fixed - uses HasInstance check and _applicationQuitting flag |
| Formation oscillation | Use FormationSlotBehavior instead of FollowLeaderBehavior |
| Multi-agent click moves one | Fixed - ClickToMove now moves all agents |
| EasyPath obstacle layer warning | Run EasyPath → Fix Existing Demo Scenes |

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
|   +-- Scripts/          # ClickToMove, DemoController
+-- Tests/                # Unit tests
    +-- Editor/           # EditMode tests (PathNodeTests, PriorityQueueTests, PerformanceBenchmarks)
    +-- Runtime/          # PlayMode tests (PathfindingIntegrationTests)
```

## SwarmAI Asset Structure

```
assets/EasyPath/Assets/SwarmAI/
+-- Runtime/              # Core swarm system (SwarmAI.Runtime.dll)
|   +-- Core/             # SwarmManager, AgentState, SwarmMessage
|   +-- Components/       # SwarmAgent, SwarmSensor, SwarmFormation
|   +-- Behaviors/        # IBehavior, Seek, Flee, Flock, Gather, etc.
+-- Editor/               # Editor tools (SwarmAI.Editor.dll)
|   +-- Inspectors/       # Custom inspectors
|   +-- Windows/          # SwarmDebugWindow
+-- Demo/                 # Demo scripts (SwarmAI.Demo.dll)
|   +-- Scripts/          # Demo controllers
|   +-- Scenes/           # BasicSwarm, ResourceGathering, CombatFormations
+-- Documentation/        # README, quickstart guide
```

**Unity Menu Items:**
- Window → EasyPath → Debug Window
- Tools → EasyPath → Check Compilation
- EasyPath → Create Demo Scene → Basic/Multi-Agent/Stress Test
- GameObject → EasyPath → Create Grid / Create Agent
- Build → Quick Build / Release Build

**Demo Scene Controls (during Play mode):**

### EasyPath Demo Controls
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

### SwarmAI Flocking Demo Controls
| Input | Action |
|-------|--------|
| Left-click | Set flock target position |
| 1-5 | Toggle behaviors (Separation, Alignment, Cohesion, Wander, Obstacle Avoidance) |
| 6 | Toggle Seek behavior |
| Space | Scatter flock |
| G | Gather at center |

### SwarmAI Formation Demo Controls
| Input | Action |
|-------|--------|
| WASD | Move leader manually |
| Left-click | Move formation to position |
| 1-6 | Change formation (Line, Column, Circle, Wedge, V, Box) |

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
| EasyPath obstacle layer warning | Obstacle layer set to "Everything" | Run EasyPath → Fix Existing Demo Scenes |
| SwarmManager cleanup warning | Singleton created during scene teardown | Use SwarmManager.HasInstance instead of Instance |
| Formation agents oscillating | Conflicting behaviors | Use FormationSlotBehavior instead of FollowLeaderBehavior |
| Input Manager deprecation warning | Legacy Input API detected | Set Active Input Handling to "Input System Package (New)" |

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

GitHub secrets (✅ configured):
- `UNITY_LICENSE` - Unity license file content
- `UNITY_EMAIL` - Unity account email
- `UNITY_PASSWORD` - Unity account password

See: https://game.ci/docs/github/activation for license setup.

## GitHub Repository

**URL:** https://github.com/quanticsoul4772/unity-asset-toolkit

**Commits:**
- `8cc4d6c` - Add EasyPath pathfinding asset with working demo
- `f6637c6` - Add development environment - CI/CD, Git LFS, pre-commit hooks, VS Code config
- `440aa31` - Remove base64 decode step - license is already raw XML
- `f074d68` - Fix CI/CD build - add demo scenes to EditorBuildSettings

## Unit Testing

### Test Assemblies
- **EasyPath.Tests.Editor** - EditMode unit tests (PathNode, PriorityQueue, benchmarks)
- **EasyPath.Tests.Runtime** - PlayMode integration tests (pathfinding scenarios)
- **SwarmAI.Tests.Editor** - EditMode unit tests (behaviors, formations, states, messages)
- **SwarmAI.Tests.Runtime** - PlayMode integration tests (flocking, steering behaviors)

### Running Tests
```powershell
# Run all tests via CLI
.\scripts\unity-cli.ps1 -Action test

# Or in Unity Editor: Window → General → Test Runner
```

### Performance Benchmarks
Benchmark tests are in `EasyPath.Tests.Editor.PerformanceBenchmarks`:
- Small grid (20x20) pathfinding: target < 5ms
- PriorityQueue operations: target < 500ms for 10k ops
- Memory allocation per pathfind: target < 1MB for 100 paths

## VS Code Integration

Recommended extensions in `.vscode/extensions.json`:
- C# Dev Kit
- Unity Tools
- GitLens
- PowerShell

Project settings in `.vscode/settings.json` and `.editorconfig`.
