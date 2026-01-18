# NPCBrain Demo Scenes

This folder contains polished demo scenes showcasing NPCBrain's archetype NPCs.

## Cops and Robbers Demo (NEW!)

**File:** `Scenes/CopsAndRobbersDemo.unity`

A unified demo showcasing ALL NPC archetypes working together in an interactive heist scenario.

### Features
- **CopNPC** - Police officers that patrol, investigate sounds/alarms, chase and arrest robbers
- **RobberNPC** - Thieves that steal loot, evade cops, hide, and escape
- **LootPoints** - Stealable objectives that trigger alarms when stolen
- **EscapeZone** - Victory condition for robbers carrying loot
- **CoverPoints** - Hiding spots for robbers
- Full Utility AI with Criticality system for all NPCs
- Scoring system tracking arrests vs escapes
- Real-time UI showing all NPC states and Criticality values

### How It Works
1. Robbers spawn near the escape zone and seek out loot
2. Cops patrol the area, listening for alarms and footsteps
3. When a robber steals loot, an alarm sounds alerting nearby cops
4. Robbers must evade cops and reach the escape zone with their loot
5. Cops win by arresting robbers; Robbers win by escaping with loot

### Behaviors

**Cops (CopNPC)**
- Arrest - Capture robbers when very close
- Chase - Pursue visible robbers
- InvestigateAlarm - Rush to alarm locations
- InvestigateSound - Check suspicious footsteps
- Return - Go back to patrol area
- Patrol - Follow waypoints

**Robbers (RobberNPC)**
- Flee - Run away from visible cops
- CarryToEscape - Head to escape zone with loot
- StealLoot - Pick up loot when safe
- Hide - Take cover when pursued
- Sneak - Move carefully when cops are nearby
- Scout - Look for loot opportunities

---

## Creating Demo Scenes

Use the Unity menu to create demo scenes:

- **NPCBrain → Create Guard Demo Scene** - Creates `GuardDemo.unity`
- **NPCBrain → Create Patrol Demo Scene** - Creates `PatrolDemo.unity`
- **NPCBrain → Create All Demo Scenes** - Creates both at once

## Opening Demo Scenes

- **NPCBrain → Open Guard Demo** - Opens the Guard demo (creates if needed)
- **NPCBrain → Open Patrol Demo** - Opens the Patrol demo (creates if needed)

---

## Guard Demo

**File:** `Scenes/GuardDemo.unity`

Demonstrates the **GuardNPC** archetype with chase, investigate, and patrol behaviors.

### Features
- Player-controlled character (WASD + Shift to sprint)
- Multiple guards with sight sensors
- Chase behavior when player is spotted
- Investigation of last known position
- Return to post after losing target
- Normal patrol when idle

### Controls
| Key | Action |
|-----|--------|
| W/↑ | Move forward |
| S/↓ | Move backward |
| A/← | Move left |
| D/→ | Move right |
| Shift | Sprint |

### Guard Behavior Priority
1. **Chase** - If target is visible and in range
2. **Investigate** - Go to last known position if target lost
3. **Return** - Return to patrol area if far from home
4. **Patrol** - Walk between waypoints when idle

---

## Patrol Demo

**File:** `Scenes/PatrolDemo.unity`

Demonstrates the **PatrolNPC** archetype with simple waypoint following.

### Features
- Multiple patrol NPCs with different routes
- Color-coded patrollers and waypoints
- Different patrol patterns (square, diamond, circle, line)
- Random variation in wait times and speed
- Visual waypoint markers

### Patrol Patterns
- **Square** - 4 waypoints in a square pattern
- **Diamond** - 4 waypoints in a rotated square
- **Circle** - 6 waypoints in a circular pattern
- **Line** - 2 waypoints for back-and-forth patrol

---

## Debug Tools

While running any demo, you can use NPCBrain's debug tools:

1. **Window → NPCBrain → Debug Window** - Inspect any NPC's state
2. **Scene Gizmos** - Vision cones and waypoint paths are drawn in Scene view

### Debug Window Features
- NPC selector dropdown
- Current state display
- Blackboard key viewer
- Criticality stats (Temperature, Entropy, Inertia)

**Note:** To see Criticality values change, use the **Utility Demo** scene which uses `UtilitySelector`. The Guard and Patrol demos use regular BT nodes which don't record actions to the Criticality system.
- Pause/Step/Resume controls

---

## Customization

Both demo setups are MonoBehaviour scripts with serialized fields you can tweak:

### GuardDemoSetup
- `_guardCount` - Number of guards (1-4)
- `_arenaSize` - Size of the play area
- `_groundColor`, `_guardColor`, etc. - Visual customization

### PatrolDemoSetup
- `_patrollerCount` - Number of patrol NPCs (1-4)
- `_arenaSize` - Size of the play area
- `_patrollerColors` - Colors for each patroller

---

## Creating Your Own NPCs

Use these demos as reference for creating custom NPCs:

```csharp
// Simple patrol NPC
public class MyPatroller : PatrolNPC
{
    // PatrolNPC handles everything!
    // Just assign a WaypointPath in the inspector
}

// Custom guard with extended behavior
public class MyGuard : GuardNPC
{
    protected override void Awake()
    {
        base.Awake();
        // Add custom initialization
    }
}
```

See the `Runtime/Archetypes/` folder for the full source code of each archetype.
