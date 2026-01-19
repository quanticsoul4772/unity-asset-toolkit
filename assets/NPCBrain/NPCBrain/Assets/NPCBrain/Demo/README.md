# NPCBrain Demo Scenes

Demo scenes for NPCBrain archetypes.

## Cops and Robbers Demo

**File:** `Scenes/CopsAndRobbersDemo.unity`

Demo showcasing NPC archetypes working together in a heist scenario.

### Features
- CopNPC - Patrol, investigate, chase, arrest
- RobberNPC - Steal, evade, hide, escape
- LootPoints - Stealable objectives that trigger alarms
- EscapeZone - Victory condition for robbers
- CoverPoints - Hiding spots
- Utility AI with Criticality system
- Scoring system (arrests vs escapes)
- Real-time UI showing NPC states

### How It Works
1. Robbers spawn near the escape zone and seek out loot
2. Cops patrol the area, listening for alarms and footsteps
3. When a robber steals loot, an alarm sounds alerting nearby cops
4. Robbers must evade cops and reach the escape zone with their loot
5. Cops win by arresting robbers; Robbers win by escaping with loot

### Behaviors

Cops (CopNPC):
- Arrest, Chase, InvestigateAlarm, InvestigateSound, Return, Patrol

Robbers (RobberNPC):
- Flee, CarryToEscape, StealLoot, Hide, Sneak, Scout

## Creating Demo Scenes

Use the Unity menu to create demo scenes:

- **NPCBrain → Create Guard Demo Scene** - Creates `GuardDemo.unity`
- **NPCBrain → Create Patrol Demo Scene** - Creates `PatrolDemo.unity`
- **NPCBrain → Create All Demo Scenes** - Creates both at once

## Opening Demo Scenes

- **NPCBrain → Open Guard Demo** - Opens the Guard demo (creates if needed)
- **NPCBrain → Open Patrol Demo** - Opens the Patrol demo (creates if needed)

## Guard Demo

**File:** `Scenes/GuardDemo.unity`

Demonstrates GuardNPC with chase, investigate, and patrol behaviors.

### Features
- Player-controlled character (WASD + Shift to sprint)
- Guards with sight sensors
- Chase when spotted, investigate last known position
- Return to post after losing target

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

## Patrol Demo

**File:** `Scenes/PatrolDemo.unity`

Demonstrates PatrolNPC with waypoint following.

### Features
- Multiple patrol NPCs with different routes
- Color-coded patrollers and waypoints
- Patterns: Square, Diamond, Circle, Line

## Debug Tools

While running any demo, you can use NPCBrain's debug tools:

1. **Window → NPCBrain → Debug Window** - Inspect any NPC's state
2. **Scene Gizmos** - Vision cones and waypoint paths are drawn in Scene view

### Debug Window Features
- NPC selector dropdown
- Current state display
- Blackboard key viewer
- Criticality stats (Temperature, Entropy, Inertia)

Note: To see Criticality values change, use the Utility Demo scene which uses UtilitySelector. The Guard and Patrol demos use regular BT nodes which don't record actions to the Criticality system.

## Customization

Demo setups have serialized fields:

- GuardDemoSetup: `_guardCount`, `_arenaSize`, colors
- PatrolDemoSetup: `_patrollerCount`, `_arenaSize`, `_patrollerColors`

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
