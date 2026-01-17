# NPCBrain Demo Scenes

This folder contains demo scripts for testing and showcasing NPCBrain functionality.

## Demo Scripts

### TestNPC.cs
**Purpose:** Basic behavior tree demo with target chasing and waypoint patrol.

**Features:**
- Uses `Selector` to choose between chasing targets and patrolling
- Demonstrates `CheckBlackboard`, `MoveTo`, `Wait`, and `AdvanceWaypoint` nodes
- Configurable via Inspector (wait time, move speed, arrival distance)

### TestSceneSetup.cs
**Purpose:** Quickly generates a test scene with ground, waypoints, and a patrol NPC.

**Usage:**
1. Create an empty GameObject
2. Add `TestSceneSetup` component
3. Enable "Auto Generate" or use Context Menu → "Generate Test Scene"

### TestSceneController.cs
**Purpose:** Week 3 validation demo for Utility AI + Criticality.

**Features:**
- Spawns multiple `UtilityTestNPC` instances
- NPCs choose between Patrol, Wander, and Idle using `UtilitySelector`
- On-screen debug display shows Temperature, Entropy, and Inertia
- Visual indicators change color based on current action

## NPC Classes

| Class | Location | Purpose |
|-------|----------|--------|
| `TestNPC` | TestNPC.cs | Target chase + waypoint patrol |
| `PatrolNPC` | TestSceneSetup.cs | Simple waypoint patrol |
| `UtilityTestNPC` | TestSceneController.cs | Utility AI with Criticality |

## Related Editor Tools

- **NPCBrain → Create Test Scene:** Creates a new test scene via `TestSceneGenerator`
- **NPCBrain → Open Test Scene:** Opens an existing test scene
- **Window → NPCBrain → Debug Window:** Opens the debug window (Week 4)
