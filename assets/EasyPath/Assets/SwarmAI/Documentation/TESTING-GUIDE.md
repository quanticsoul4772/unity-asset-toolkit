# SwarmAI Testing Guide

This document provides step-by-step instructions for creating demo scenes, testing SwarmAI functionality, and validating compatibility across Unity versions.

## Table of Contents

1. [Creating Demo Scenes](#creating-demo-scenes)
2. [Testing Demo Scenes](#testing-demo-scenes)
3. [Multi-Version Testing](#multi-version-testing)
4. [Validation Checklist](#validation-checklist)
5. [Troubleshooting](#troubleshooting)

---

## Creating Demo Scenes

### Prerequisites

- Unity Editor open with the SwarmAI project
- Project compiles without errors
- SwarmAI assemblies visible in Project window

### Method 1: Create All Scenes at Once (Recommended)

1. Open Unity Editor
2. Go to menu: **SwarmAI > Create Demo Scene > Create All Demo Scenes**
3. Wait for all three scenes to be created
4. Scenes are saved to: `Assets/EasyPath/Assets/SwarmAI/Demo/Scenes/`

### Method 2: Create Individual Scenes

#### Flocking Demo
1. Go to menu: **SwarmAI > Create Demo Scene > Flocking Demo (30 Agents)**
2. Scene creates with:
   - 30 agents with flocking behaviors
   - Obstacles for avoidance
   - Camera with controller
   - FlockingDemo controller script

#### Formation Demo
1. Go to menu: **SwarmAI > Create Demo Scene > Formation Demo (10 Agents)**
2. Scene creates with:
   - 10 agents (1 leader, 9 followers)
   - SwarmFormation component
   - FormationDemo controller script

#### Resource Gathering Demo
1. Go to menu: **SwarmAI > Create Demo Scene > Resource Gathering Demo (15 Agents)**
2. Scene creates with:
   - 15 worker agents
   - 3 resource nodes
   - Home base
   - ResourceGatheringDemo controller script

### Adding Scenes to Build Settings

1. After creating scenes, go to: **SwarmAI > Add Demo Scenes to Build Settings**
2. This adds all SwarmAI demo scenes to the build configuration

---

## Testing Demo Scenes

### Flocking Demo Testing

| Test | Steps | Expected Result |
|------|-------|----------------|
| Basic flocking | Press Play, observe agents | Agents move together in a cohesive flock |
| Click target | Left-click on ground | Flock moves toward clicked position |
| Toggle Separation | Press 1 | Agents spread apart when ON, cluster when OFF |
| Toggle Alignment | Press 2 | Agents align velocities when ON |
| Toggle Cohesion | Press 3 | Agents group together when ON |
| Toggle Wander | Press 4 | Agents move randomly when ON |
| Toggle Obstacle Avoidance | Press 5 | Agents avoid cubes when ON |
| Scatter | Press Space | Flock disperses |
| Gather | Press G | Flock regroups at center |

### Formation Demo Testing

| Test | Steps | Expected Result |
|------|-------|----------------|
| Leader movement | Press WASD | Yellow leader agent moves, followers follow |
| Click to move | Left-click on ground | Formation moves to position |
| Line formation | Press 1 | Agents arrange in horizontal line |
| Column formation | Press 2 | Agents arrange in vertical column |
| Circle formation | Press 3 | Agents arrange in circle around leader |
| Wedge formation | Press 4 | Agents arrange in wedge/arrow shape |
| V formation | Press 5 | Agents arrange in V shape |
| Box formation | Press 6 | Agents arrange in box/square |
| Adjust spacing | Press +/- | Formation spacing increases/decreases |
| Follow leader | Press F | Followers resume following leader |
| Stop all | Press X | All agents stop moving |

### Resource Gathering Demo Testing

| Test | Steps | Expected Result |
|------|-------|----------------|
| Auto-gather | Press Play, observe | Workers automatically gather from nearest resource |
| Send to gather | Press G | All workers seek resources |
| Send home | Press H | All workers return to base |
| Click assign | Click on resource node | Nearest worker assigned to that resource |
| Spawn resource | Press N | New resource node appears |
| Resource depletion | Wait for workers | Resources deplete over time |
| Resource respawn | Wait after depletion | Resources respawn (if enabled) |

### Camera Controls (All Demos)

| Control | Action |
|---------|--------|
| WASD / Arrow Keys | Pan camera |
| Q / E | Rotate camera |
| Mouse Scroll | Zoom in/out |
| Middle Mouse + Drag | Pan camera |

---

## Multi-Version Testing

> **See also:** [MULTI-VERSION-TESTING.md](MULTI-VERSION-TESTING.md) for detailed checklists and automation.

### Supported Unity Versions

| Version | Status | Notes |
|---------|--------|-------|
| Unity 2021.3 LTS | Required | Minimum supported version |
| Unity 2022.3 LTS | Required | Current LTS |
| Unity 6 (6000.x) | Required | Latest version |

### Testing Procedure for Each Version

#### Step 1: Open Project

1. Open Unity Hub
2. Add project if not already added
3. Select target Unity version from dropdown
4. Click "Open"
5. Wait for import/recompile

#### Step 2: Check Compilation

1. Open Console window (Window > General > Console)
2. Verify no compilation errors (warnings are acceptable)
3. Check that all SwarmAI assemblies appear in:
   - `Library/ScriptAssemblies/SwarmAI.Runtime.dll`
   - `Library/ScriptAssemblies/SwarmAI.Editor.dll`
   - `Library/ScriptAssemblies/SwarmAI.Demo.dll`

#### Step 3: Create Demo Scenes

1. Go to: **SwarmAI > Create Demo Scene > Create All Demo Scenes**
2. Verify all three scenes created without errors
3. Check scenes appear in Project window

#### Step 4: Test Each Demo

1. Open each demo scene
2. Press Play
3. Run through the testing checklist above
4. Note any issues

#### Step 5: Test Editor Tools

1. Select a SwarmAgent in scene
2. Verify custom inspector displays correctly
3. Open SwarmAI Debug Window (Window > SwarmAI > Debug Window)
4. Verify all tabs function correctly
5. Test menu items (GameObject > SwarmAI > ...)

#### Step 6: Test Build

1. Add demo scenes to build settings
2. Build for target platform (File > Build Settings > Build)
3. Run built executable
4. Verify demos work in standalone build

### Version-Specific Notes

#### Unity 2021.3 LTS
- Uses `FindObjectOfType<T>()` (deprecated in newer versions but functional)
- Some editor APIs may have slight differences

#### Unity 2022.3 LTS
- `FindObjectOfType` replaced with `FindFirstObjectByType`
- SwarmAI already uses the newer API

#### Unity 6 (6000.x)
- New Input System may be active by default
- Demo scripts use legacy Input class (compatible with both)
- Render pipeline may affect material appearance

---

## Validation Checklist

Use this checklist before Asset Store submission:

### Compilation
- [ ] No compiler errors in Unity 2021.3 LTS
- [ ] No compiler errors in Unity 2022.3 LTS
- [ ] No compiler errors in Unity 6
- [ ] No critical warnings (some deprecation warnings acceptable)

### Demo Scenes
- [ ] All 3 demo scenes created successfully
- [ ] Flocking Demo runs without errors
- [ ] Formation Demo runs without errors
- [ ] Resource Gathering Demo runs without errors
- [ ] All demo controls work as documented

### Editor Tools
- [ ] SwarmAgentEditor displays correctly
- [ ] SwarmManagerEditor displays correctly
- [ ] SwarmFormationEditor displays correctly
- [ ] ResourceNodeEditor displays correctly
- [ ] SwarmSettingsEditor displays correctly
- [ ] SwarmDebugWindow opens and functions
- [ ] All menu items work (SwarmAI menu, GameObject menu)

### Documentation
- [ ] README.md is accurate
- [ ] API Reference matches actual code
- [ ] All demo controls documented
- [ ] Installation instructions work

### Build
- [ ] Standalone Windows build succeeds
- [ ] Standalone macOS build succeeds (if applicable)
- [ ] Built demos function correctly

### Package
- [ ] package.json is valid
- [ ] All source files included
- [ ] No unnecessary files (Temp, Library, etc.)
- [ ] LICENSE.txt present
- [ ] CHANGELOG.md up to date

---

## Troubleshooting

### Scene Creation Fails

**Problem:** Menu item does nothing or throws error

**Solutions:**
1. Check Console for error messages
2. Verify SwarmAI.Editor assembly compiled
3. Ensure Demo/Scenes folder exists
4. Try creating folder manually: `Assets/EasyPath/Assets/SwarmAI/Demo/Scenes`

### Agents Don't Move

**Problem:** Agents spawn but don't exhibit behaviors

**Solutions:**
1. Check that SwarmManager exists in scene
2. Verify agents are registered (check Debug Window)
3. Ensure behaviors are added and active
4. Check Time.timeScale is not 0

### Formation Doesn't Work

**Problem:** Followers don't follow leader

**Solutions:**
1. Verify leader is assigned (yellow color in demo)
2. Check SwarmFormation component is attached
3. Ensure followers are in FollowingState
4. Check formation slots are generated

### Resources Not Gathering

**Problem:** Workers don't gather from resources

**Solutions:**
1. Verify ResourceNode components are active
2. Check resource has remaining amount (not depleted)
3. Ensure workers are in GatheringState
4. Verify base position is set correctly

### Materials Look Wrong

**Problem:** Materials appear pink or incorrect

**Solutions:**
1. This happens when Standard shader isn't available
2. Install Built-in Render Pipeline if using URP/HDRP
3. Or modify demo setup to use appropriate shaders

### Console Spam

**Problem:** Many debug messages in console

**Solutions:**
1. Select SwarmManager in scene
2. Uncheck "Show Debug Info" in Inspector
3. Or disable debug visualization in SwarmSettings

---

## Automated Testing Script

A PowerShell script is provided for automated validation:

```powershell
# Run from project root (close Unity first!)
powershell -ExecutionPolicy Bypass -File scripts/test-unity-versions.ps1

# Or use the convenient batch file:
test-versions.bat
```

This script:
1. Detects installed Unity versions
2. Opens project in each version
3. Runs compilation check
4. Generates compatibility report

### Quick Version Check

In Unity Editor, use **SwarmAI > Show Unity Version Info** to see:
- Current Unity version
- Compatibility status
- Render pipeline in use
- Input system status

See `scripts/test-unity-versions.ps1` for details.
