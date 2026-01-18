# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity Asset Store toolkit for AI/pathfinding tools. **Three products**:
1. **EasyPath** (Complete) - A* pathfinding for beginners ($35)
2. **SwarmAI** (Complete) - Multi-agent coordination with Jobs/Burst ($45)
3. **NPCBrain** (In Development) - All-in-one AI toolkit ($60)

**Current Focus**: NPCBrain - 4-week MVP (January 2026)

**Technology Stack**: C#, Unity 6 (6000.3.4f1), Visual Studio 2022, Windows 11, PowerShell automation

**Repository**: https://github.com/quanticsoul4772/unity-asset-toolkit

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

**EasyPath**:
- **Window → EasyPath → Debug Window** - Real-time pathfinding diagnostics
- **Tools → EasyPath → Check Compilation** - Verify assembly compilation
- **EasyPath → Create Demo Scene** - Generate Basic/Multi-Agent/Stress Test scenes
- **GameObject → EasyPath** - Create Grid or Agent components

**NPCBrain**:
- **NPCBrain → Create Demo Scene** - Generate PatrolDemo, GuardDemo, or UtilityDemo
- **GameObject → NPCBrain** - Create NPCBrainController, PatrolNPC, GuardNPC, UtilityNPC
- **Window → NPCBrain** - Debug windows for behavior tree visualization (future)

## Architecture

### Project Structure
```
unity-asset-toolkit/
├── assets/
│   ├── EasyPath/              # A* Pathfinding (Complete)
│   │   └── Assets/EasyPath/
│   │       ├── Runtime/       # EasyPath.Runtime.asmdef
│   │       ├── Editor/        # EasyPath.Editor.asmdef
│   │       └── Demo/          # EasyPath.Demo.asmdef
│   │
│   └── NPCBrain/              # AI Toolkit (In Development)
│       └── NPCBrain/Assets/NPCBrain/
│           ├── Runtime/       # NPCBrain.Runtime.asmdef
│           │   ├── BehaviorTree/      # BT nodes, composites, decorators
│           │   ├── UtilityAI/         # Scoring system with considerations
│           │   ├── Perception/        # SightSensor, Memory, TargetSelector
│           │   ├── Criticality/       # Adaptive behavior controller
│           │   ├── Core/              # NPCBrainController, Blackboard, WaypointPath
│           │   └── Archetypes/        # PatrolNPC, GuardNPC, UtilityNPC
│           ├── Editor/        # NPCBrain.Editor.asmdef
│           ├── Demo/          # NPCBrain.Demo.asmdef
│           ├── Tests/         # Runtime and Editor tests
│           └── Documentation/ # API.md, GETTING_STARTED.md
│
├── scripts/                   # PowerShell automation
├── docs/                      # Setup guides
└── guides/                    # Best practices documentation
```

### Assembly Definitions (Critical)
**Unity 6+ requires GUID-based references in .asmdef files, NOT name-based references.**

**EasyPath Assemblies**:
1. **EasyPath.Runtime** (`32e8732f4adef96408db2fc8a96644eb`) - Core pathfinding, no dependencies
2. **EasyPath.Editor** (`9485adf895dab1a4492c71fc321779b2`) - Editor tools, references Runtime, `includePlatforms: ["Editor"]`
3. **EasyPath.Demo** - Demo scripts, references Runtime

**NPCBrain Assemblies**:
1. **NPCBrain.Runtime** - Core AI systems, no dependencies
2. **NPCBrain.Editor** - Inspector customizations, references Runtime, `includePlatforms: ["Editor"]`
3. **NPCBrain.Demo** - Demo scenes and scripts, references Runtime
4. **NPCBrain.Tests.Runtime** - PlayMode tests, references Runtime
5. **NPCBrain.Tests.Editor** - EditMode tests, references Runtime and Editor
6. **NPCBrain.Tests.Shared** - Shared test utilities

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

### NPCBrain Core Architecture

**NPCBrain** combines four AI paradigms into a unified system:

**1. NPCBrainController** (`Core/NPCBrainController.cs`) - Central coordination hub
- **Blackboard**: Key-value store for cross-system state sharing (supports TTL)
- **Tick Sequence**: Blackboard cleanup → Perception update → Criticality adjustment → BehaviorTree execution
- **Events**: OnTargetAcquired, OnTargetLost, OnStateChanged, OnBrainPaused, OnBrainResumed
- **Lifecycle**: `Awake() → CreateBehaviorTree() → Update() → Tick()`

**2. Behavior Tree System** (`BehaviorTree/`)
- **Node Lifecycle**: `Execute() → OnEnter() → Tick() → OnExit()`
- **Status Flow**: Success/Failure/Running (running nodes continue next frame)
- **Composites**:
  - **Sequence**: AND logic, executes children in order (fails if any child fails)
  - **Selector**: OR logic, tries children until one succeeds (priority-based fallback)
  - **UtilitySelector**: Scores actions via Utility AI, selects via softmax with temperature
- **Decorators**: Cooldown (timing gates), Inverter (negate result), Repeater (loop child)
- **Actions**: MoveTo, Wait, SetBlackboard, ClearBlackboardKey, AdvanceWaypoint, CheckBlackboard, CheckDistance

**3. Utility AI System** (`UtilityAI/`)
- **UtilityAction**: Decision unit with base score and considerations
- **Consideration**: Factors that score actions (0-1 range)
  - **Types**: ConstantConsideration, BlackboardConsideration, DistanceConsideration, TimeConsideration
  - **Curves**: LinearCurve, ExponentialCurve, StepCurve (shape raw scores)
- **Scoring Algorithm**:
  1. Multiply all consideration scores
  2. Apply compensation factor (Dave Mark's make-up value) to avoid unfairly penalizing actions with more considerations
  3. Return 0-1 score
- **Integration**: UtilitySelector node bridges BT and Utility AI

**4. Perception System** (`Perception/SightSensor.cs`)
- **Vision Cone**: OverlapSphere + angle check + raycast line-of-sight
- **Target Tracking**: Maintains visible targets, fires events on acquire/lost
- **Performance**: maxRaycastsPerTick limits expensive raycasts
- **Data Flow**: SightSensor → Blackboard → Behavior Tree decisions

**5. Criticality Controller** (`Criticality/CriticalityController.cs`)
- **Adaptive Behavior**: Automatically adjusts exploration vs exploitation
- **Shannon Entropy**: Measures action variety from recent history
- **Temperature Control**: Adjusts softmax temperature based on entropy
  - Low entropy (repetitive) → Increase temperature (more random exploration)
  - High entropy (varied) → Decrease temperature (more deterministic)
- **Inertia**: Tendency to repeat actions (1 - normalized entropy)

**Archetype NPCs** (Pre-built examples):
- **PatrolNPC**: Simple waypoint following with random variation
- **GuardNPC**: Chase → Investigate → Return → Patrol (priority-based fallback with perception)
- **UtilityNPC**: Autonomous decision-making via scored actions (Wander/Rest/Patrol/SeekInterest)

**Data Flow Example** (Guard sees player):
```
SightSensor detects player
→ RaiseTargetAcquired event
→ GuardNPC.HandleTargetAcquired()
→ Blackboard.Set("target", player)
→ Next Tick: BehaviorTree executes
→ Selector tries Chase Sequence
→ CheckBlackboard("target") succeeds
→ MoveTo(player) executes
```

**Key Integration Pattern**: All systems communicate through the Blackboard
- Perception writes visible targets
- Behavior Tree reads/writes state
- Utility AI reads for scoring
- Criticality tracks action history

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

**EasyPath Demo**:
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

**NPCBrain GuardDemo**:
| Input | Action |
|-------|--------|
| WASD | Move player character |
| Mouse | Look around |
| Esc | Exit play mode |

**NPCBrain UtilityDemo**:
| Input | Action |
|-------|--------|
| Left-click | Spawn interest point at position |
| Space | Spawn interest point at random position |
| R | Remove all interest points |

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

**Project Guides** (`guides/`):
- **UNITY-CSHARP-BEST-PRACTICES.md** - C# coding standards, lifecycle, coroutines
- **AI-PATHFINDING-PATTERNS.md** - State machines, behavior trees, A* algorithm
- **ASSET-STORE-GUIDELINES.md** - Submission requirements, pricing
- **UNITY-PROJECT-STRUCTURE.md** - Folder organization, assembly definitions

**NPCBrain Documentation** (`assets/NPCBrain/NPCBrain/Assets/NPCBrain/Documentation/`):
- **GETTING_STARTED.md** - Quick start guide for NPCBrain (371 lines)
- **API.md** - Complete API reference for all components (691 lines)
- **README.md** - NPCBrain overview and feature list

**Project Planning** (`docs/`):
- **NPCBRAIN-MVP-SPEC.md** - Complete MVP specification (4-week plan)
- **NPCBRAIN-COMPLETION-REPORT.md** - Implementation status report (99% complete)
- **NPCBRAIN-PROFILING-GUIDE.md** - Performance profiling guide with Unity Profiler
- **CHECKLIST.md** - Environment setup checklist

## Important Notes for AI Assistants

### General Workflow
1. **ALWAYS run `.\scripts\preflight.ps1` before opening Unity** - catches issues early
2. **Use GUID references in .asmdef files** - Unity 6+ requirement
3. **Check compilation via CLI** before committing - `.\scripts\unity-cli.ps1 -Action compile`
4. **Unity 6+ API changes** - Use `FindFirstObjectByType<T>()` not `FindObjectOfType<T>()`
5. **PowerShell is primary CLI** - Not WSL/Bash
6. **Test in Play mode** - AI behavior requires runtime testing

### Working with NPCBrain
1. **Central Hub Pattern**: NPCBrainController coordinates all systems via Blackboard
2. **Blackboard is the communication layer**: All systems read/write through it
3. **Behavior Tree Lifecycle**: Always implement OnEnter/Tick/OnExit for custom actions
4. **Utility AI Scoring**: Use Dave Mark's make-up value compensation (already implemented)
5. **Criticality Auto-Adjusts**: Don't manually set temperature, let the system adapt
6. **Archetype Inheritance**: Custom NPCs should inherit from NPCBrainController and override CreateBehaviorTree()
7. **Event-Driven Architecture**: Subscribe to OnTargetAcquired/OnTargetLost for perception-reactive behavior
8. **TTL Pattern**: Use `Blackboard.SetWithTTL()` for temporary state that should expire

### NPCBrain Testing Strategy
1. **PatrolDemo**: Test basic waypoint following and movement
2. **GuardDemo**: Test perception, state transitions (Chase/Investigate/Return/Patrol)
3. **UtilityDemo**: Test autonomous decision-making and criticality adaptation
4. **Unit Tests**: EditMode for logic, PlayMode for MonoBehaviour integration

### Common NPCBrain Pitfalls
- **Missing Blackboard Keys**: CheckBlackboard will fail silently if key doesn't exist
- **NavMesh Required**: MoveTo actions require NavMeshAgent if useNavMesh=true
- **Perception Raycast Limits**: maxRaycastsPerTick prevents performance issues but may delay detection
- **Temperature Clamping**: CriticalityController clamps between minTemperature/maxTemperature
- **Action Recording**: UtilitySelector must call CriticalityController.RecordAction() for adaptation to work

### Performance Profiling
**Targets**: 100+ NPCs @ 60 FPS, <0.1ms per NPC tick cost

**Quick Start**:
1. **Unity Profiler**: Window → Analysis → Profiler (Ctrl+7)
2. **Performance Test Scene**: Use `PerformanceTest.cs` component
3. **Monitor**: CPU Usage, Scripts breakdown, GC allocations
4. **Optimize**: See `docs/NPCBRAIN-PROFILING-GUIDE.md` for detailed guide

**Common Bottlenecks**:
- Raycasts in SightSensor (limit with maxRaycastsPerTick)
- GC allocations from new lists/arrays (reuse cached collections)
- Too many active NPCs (implement LOD system for distant NPCs)

**Performance Test Usage**:
```
1. Create empty scene with ground plane
2. Add PerformanceTest component to GameObject
3. Assign NPC prefab (PatrolNPC, GuardNPC, or UtilityNPC)
4. Press Play - NPCs spawn automatically
5. Watch on-screen stats (FPS, per-NPC cost, pass/fail status)
6. Controls: Space=Spawn, C=Clear, +/-=Adjust count
```
