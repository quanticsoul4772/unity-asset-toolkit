# NPCBrain Scene Gizmos Design

**Version:** 1.0  
**Status:** Planning  
**Last Updated:** January 2026

## Overview

The scene gizmos system provides in-world visualization of NPC AI state in the Unity Scene view, complementing the NPCBrainDebugWindow. Gizmos use Unity's `Gizmos` API for wireframes and `Handles` API for filled shapes.

**Design Principles:**
- **Informative** - Show AI state at a glance without opening debug window
- **Non-cluttering** - Smart defaults, LOD system, toggle controls
- **Consistent** - Same color scheme as debug window
- **Performant** - LOD and culling for many NPCs

---

## Gizmo Types

### 1. Vision Cone

The most important gizmo - shows what the NPC can see.

#### Layers (drawn back to front)

```
                    ┌─────────────────────┐
                   /   Peripheral (180°)   \
                  /   ┌───────────────┐     \
                 /   /   Primary FOV   \     \
                /   /      (120°)       \     \
               /   /                     \     \
              /   /         ▲             \     \
             /   /          │ NPC          \     \
            └───┴───────────┴───────────────┴─────┘
```

| Layer | Angle | Style | Opacity |
|-------|-------|-------|---------|
| Peripheral Vision | 180° | Wireframe arc | 20% |
| Primary FOV | 120° | Solid filled arc | 40% |
| Distance Rings | - | Wireframe circles at 25%, 50%, 75%, 100% | 30% |

#### Color by Alert State

| State | Color | Hex | When |
|-------|-------|-----|------|
| **Idle/Patrol** | Green | #4CAF50 | No targets, normal operation |
| **Suspicious** | Yellow | #FFC107 | Heard sound, partial detection |
| **Alert/Detected** | Red | #F44336 | Target visible, in combat |
| **Searching** | Orange | #FF9800 | Lost target, actively searching |

#### Implementation

```csharp
// Primary FOV - filled arc
Handles.color = GetAlertColor(brain.AlertLevel).WithAlpha(0.4f);
Vector3 startDir = Quaternion.Euler(0, -fovAngle / 2, 0) * transform.forward;
Handles.DrawSolidArc(
    eyePosition,           // center
    Vector3.up,            // normal (Y-up for top-down arc)
    startDir,              // from direction
    fovAngle,              // angle in degrees
    viewDistance           // radius
);

// Peripheral - wireframe arc
Handles.color = GetAlertColor(brain.AlertLevel).WithAlpha(0.2f);
Vector3 periStart = Quaternion.Euler(0, -peripheralAngle / 2, 0) * transform.forward;
Handles.DrawWireArc(
    eyePosition,
    Vector3.up,
    periStart,
    peripheralAngle,
    viewDistance
);

// Distance rings
Gizmos.color = Color.white.WithAlpha(0.3f);
float[] ringDistances = { 0.25f, 0.5f, 0.75f, 1.0f };
foreach (float t in ringDistances)
{
    DrawWireArcAtDistance(eyePosition, fovAngle, viewDistance * t);
}
```

#### Visual Example

```
                            Normal (Green)
                         ╱‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾╲
                       ╱   ·  ·  ·  ·  ·   ╲
                     ╱   ·  ·  ·  ·  ·  ·   ╲
                   ╱   ·  ·  ·  ·  ·  ·  ·   ╲
                  │   ·  ·  ·  ▲  ·  ·  ·   │
                   ╲   ·  ·  ·NPC·  ·  ·   ╱
                     ╲   ·  ·  ·  ·  ·   ╱
                       ╲_______________╱

                            Alert (Red)
                         ╱‾‾‾‾‾‾‾‾‾‾‾‾‾‾‾╲
                       ╱   ▓  ▓  ▓  ▓  ▓   ╲
                     ╱   ▓  ▓  ▓  ▓  ▓  ▓   ╲
                   ╱   ▓  ▓  ▓  ▓  ▓  ▓  ▓   ╲
                  │   ▓  ▓  ▓  ▲  ▓  ▓  ▓   │
                   ╲   ▓  ▓  ▓NPC▓  ▓  ▓   ╱
                     ╲   ▓  ▓  ▓  ▓  ▓   ╱
                       ╲_______________╱
```

---

### 2. Hearing Range

Shows the area where the NPC can detect sounds.

#### Elements

| Element | Style | Color |
|---------|-------|-------|
| Outer Ring | Dashed circle (16-24 segments) | Cyan #00BCD4 @ 50% |
| Active Sound | Pulsing circle at sound position | Yellow #FFEB3B |
| Sound Line | Dashed line NPC → sound | Yellow @ 60% |

#### Implementation

```csharp
// Hearing radius - dashed circle
Handles.color = new Color(0, 0.74f, 0.83f, 0.5f); // Cyan
DrawDashedCircle(transform.position, hearingRange, 24, 0.5f);

// Recent sounds - pulsing indicators
foreach (var sound in recentSounds)
{
    float age = Time.time - sound.Time;
    float pulse = Mathf.Sin(age * 4) * 0.5f + 0.5f; // Pulsing effect
    float alpha = Mathf.Lerp(1f, 0f, age / 3f);     // Fade over 3 seconds
    
    Gizmos.color = new Color(1f, 0.92f, 0.23f, alpha);
    Gizmos.DrawWireSphere(sound.Position, 0.5f + pulse * 0.3f);
    
    // Line to sound
    DrawDashedLine(transform.position, sound.Position);
}
```

#### Visual Example

```
              ○ · · · · · · · · · · · · ○
            ·                             ·
          ·                                 ·
         ·                                   ·
        ·              ┌───┐                  ·
       ·               │NPC│                   ·
        ·              └───┘                  ·
         ·                  ╲                ·
          ·                  ╲  (sound)     ·
            ·                 ◎ ← pulsing  ·
              ○ · · · · · · · · · · · · ○
                    Hearing Range
```

---

### 3. Waypoint Path

Shows patrol routes and movement paths.

#### Elements

| Element | Style | Color |
|---------|-------|-------|
| Path Lines | Solid lines connecting waypoints | White @ 50% |
| Direction Arrows | Small triangles along path | White |
| Waypoint Markers | Wire spheres at each point | Blue #2196F3 |
| Current Waypoint | Larger filled sphere | Green #4CAF50 |
| Completed Waypoints | Smaller spheres | Gray #9E9E9E |
| Next Segment | Brighter/thicker line | White @ 80% |

#### Implementation

```csharp
// Draw path lines with direction arrows
for (int i = 0; i < waypoints.Length; i++)
{
    Vector3 current = waypoints[i];
    Vector3 next = waypoints[(i + 1) % waypoints.Length];
    
    // Line
    bool isNextSegment = (i == currentWaypointIndex);
    Gizmos.color = isNextSegment ? Color.white.WithAlpha(0.8f) : Color.white.WithAlpha(0.5f);
    Gizmos.DrawLine(current, next);
    
    // Direction arrow every 2 units
    DrawDirectionArrows(current, next, 2f);
    
    // Waypoint marker
    bool isCurrent = (i == currentWaypointIndex);
    bool isCompleted = (i < currentWaypointIndex);
    
    if (isCurrent)
    {
        Gizmos.color = new Color(0.3f, 0.69f, 0.31f, 1f); // Green
        Gizmos.DrawSphere(current, 0.5f);
    }
    else if (isCompleted)
    {
        Gizmos.color = new Color(0.62f, 0.62f, 0.62f, 0.8f); // Gray
        Gizmos.DrawWireSphere(current, 0.3f);
    }
    else
    {
        Gizmos.color = new Color(0.13f, 0.59f, 0.95f, 0.8f); // Blue
        Gizmos.DrawWireSphere(current, 0.4f);
    }
}
```

#### Visual Example

```
        ○ Gray (completed)
        │
        │  ◄── arrow
        │
        ● Green (current) ←── NPC is here
        │
        │  ◄── arrow
        │
        ◎ Blue (upcoming)
        │
        │  ◄── arrow
        │
        ◎ Blue (upcoming)
        │
        └──────────────────┐
                           │
                           ○ (loops back)
```

---

### 4. Target Indicators

Shows what the NPC is tracking.

#### Line Types

| Target State | Line Style | Opacity |
|--------------|------------|---------|
| Visible Target | Solid line | 80% |
| Memory Target | Dashed line | 40% × confidence |
| Current Target | Thick line + reticle | 100% |

#### Colors by Target Type

| Type | Color | Hex |
|------|-------|-----|
| Hostile | Red | #F44336 |
| Neutral | Amber | #FFC107 |
| Friendly | Green | #4CAF50 |

#### Implementation

```csharp
// Current target - thick line with reticle
if (brain.CurrentTarget != null)
{
    Vector3 targetPos = brain.CurrentTarget.transform.position;
    Handles.color = GetTargetColor(brain.CurrentTarget).WithAlpha(1f);
    Handles.DrawLine(eyePosition, targetPos, 3f); // Thick line
    
    // Target reticle
    DrawTargetReticle(targetPos, 1f);
}

// Memory targets - dashed, faded
foreach (var memory in brain.Memory.AllMemories)
{
    float alpha = memory.Confidence * 0.4f;
    Gizmos.color = GetTargetColor(memory.Target).WithAlpha(alpha);
    DrawDashedLine(eyePosition, memory.LastKnownPosition);
    
    // Question mark at position?
    DrawMemoryMarker(memory.LastKnownPosition, memory.Confidence);
}

void DrawTargetReticle(Vector3 position, float size)
{
    // Diamond shape
    Vector3[] points = {
        position + Vector3.forward * size,
        position + Vector3.right * size,
        position - Vector3.forward * size,
        position - Vector3.right * size,
        position + Vector3.forward * size
    };
    Handles.DrawPolyLine(points);
}
```

#### Visual Example

```
                    ◇ Target Reticle
                   ╱│
                  ╱ │
         Solid  ╱  │ Dashed (memory)
         line  ╱   │
              ╱    │
        ┌───┐      │
        │NPC│──────┼──────────────── ? Last known
        └───┘      │                   position
              ╲    │
               ╲   │
                ╲  │
                 ╲ ◎ Memory target (faded)
```

---

### 5. World-Space Debug Labels

Floating labels above NPCs showing key state info.

#### Layout

```
       ┌─────────────────┐
       │ Guard_01        │ ← NPC name
       │ State: Patrol   │ ← Current state  
       │ Action: MoveTo  │ ← Current action
       │ ████████░░ 80%  │ ← Health bar
       │ ● Balanced      │ ← Criticality indicator
       └─────────────────┘
              │
              │ 2m above head
              │
            ┌─┴─┐
            │NPC│
            └───┘
```

#### Detail Levels (LOD)

| Distance | Content |
|----------|---------|
| 0-15m | Full label (all info) |
| 15-30m | Compact label (name + state only) |
| 30-50m | Icon only (colored dot for state) |
| 50m+ | Hidden |

#### Implementation

```csharp
void DrawDebugLabel(NPCBrain brain)
{
    float distance = Vector3.Distance(brain.transform.position, SceneView.lastActiveSceneView.camera.transform.position);
    Vector3 labelPos = brain.transform.position + Vector3.up * 2.5f;
    
    if (distance > 50f) return; // Culled
    
    Handles.BeginGUI();
    
    Vector2 screenPos = HandleUtility.WorldToGUIPoint(labelPos);
    
    if (distance > 30f)
    {
        // Icon only
        DrawStateIcon(screenPos, brain.CurrentState);
    }
    else if (distance > 15f)
    {
        // Compact
        DrawCompactLabel(screenPos, brain.name, brain.CurrentState);
    }
    else
    {
        // Full
        DrawFullLabel(screenPos, brain);
    }
    
    Handles.EndGUI();
}

void DrawFullLabel(Vector2 pos, NPCBrain brain)
{
    GUIStyle style = new GUIStyle(GUI.skin.box);
    style.normal.background = MakeTexture(new Color(0, 0, 0, 0.8f));
    style.normal.textColor = Color.white;
    style.padding = new RectOffset(8, 8, 4, 4);
    
    string content = $"{brain.name}\n" +
                     $"State: {brain.CurrentState}\n" +
                     $"Action: {brain.CurrentAction}\n" +
                     $"Health: {brain.Health}%";
    
    GUI.Label(new Rect(pos.x - 60, pos.y - 50, 120, 70), content, style);
}
```

#### Criticality Indicator (in label)

| State | Icon | Color |
|-------|------|-------|
| Too Ordered | ● | Blue #2196F3 |
| Balanced | ● | Green #4CAF50 |
| Too Chaotic | ● | Red #F44336 |

---

### 6. Special Situation Gizmos

#### Investigation Point

When NPC is investigating a sound or sighting:

```csharp
// Question mark icon at investigation position
void DrawInvestigationPoint(Vector3 position)
{
    Gizmos.color = Color.yellow;
    
    // Draw "?" shape with lines
    // Or use Handles.Label with "?" character
    Handles.Label(position + Vector3.up, "?", investigationStyle);
    
    // Dashed path from NPC to point
    DrawDashedLine(transform.position, position);
    
    // Pulsing ring
    float pulse = Mathf.Sin(Time.time * 3) * 0.5f + 0.5f;
    Gizmos.DrawWireSphere(position, 0.5f + pulse * 0.2f);
}
```

```
                    ?  ← Question mark
                   ╱○╲ ← Pulsing ring
                  ╱   ╲
        Dashed   ╱     ╲
        path    ╱       ╲
               ╱         ╲
          ┌───┐
          │NPC│
          └───┘
```

#### Attack Range

When NPC is in combat:

```csharp
void DrawAttackRange(NPCBrain brain)
{
    if (!brain.IsInCombat) return;
    
    Handles.color = Color.red.WithAlpha(0.3f);
    
    // Attack range circle
    Handles.DrawWireDisc(brain.transform.position, Vector3.up, brain.AttackRange);
    
    // Attack cone if directional
    if (brain.HasDirectionalAttack)
    {
        Vector3 startDir = Quaternion.Euler(0, -brain.AttackAngle / 2, 0) * brain.transform.forward;
        Handles.DrawSolidArc(
            brain.transform.position,
            Vector3.up,
            startDir,
            brain.AttackAngle,
            brain.AttackRange
        );
    }
}
```

---

## Visibility Controls

### Per-NPC Settings (in NPCBrain component)

```csharp
[Header("Scene Gizmos")]
[Tooltip("Show vision cone in scene view")]
public bool ShowVisionCone = true;

[Tooltip("Show hearing range circle")]
public bool ShowHearingRange = true;

[Tooltip("Show waypoint path")]
public bool ShowWaypointPath = true;

[Tooltip("Show lines to targets")]
public bool ShowTargetLines = true;

[Tooltip("Show floating debug label")]
public bool ShowDebugLabel = true;

[Tooltip("Only show gizmos when this NPC is selected")]
public bool GizmosOnlyWhenSelected = false;
```

### Global Settings (in NPCBrainSettings ScriptableObject)

```csharp
[Header("Scene Gizmo Settings")]
[Tooltip("Master toggle for all scene gizmos")]
public bool EnableSceneGizmos = true;

[Tooltip("Distance beyond which gizmos are hidden")]
public float GizmoMaxDistance = 50f;

[Tooltip("Distance beyond which gizmos are simplified")]
public float GizmoLODDistance = 25f;

[Tooltip("Number of segments for arc drawing (higher = smoother)")]
[Range(8, 64)]
public int ArcSegments = 32;

[Tooltip("Show gizmos for all NPCs or only selected")]
public bool ShowAllNPCGizmos = true;
```

### Debug Window Integration

The NPCBrainDebugWindow header includes:

```
[☑ Scene Gizmos] [Vision ▼] [Hearing ▼] [Paths ▼] [Labels ▼]
```

Each dropdown allows toggling individual gizmo types.

---

## Implementation Structure

### File Organization

```
NPCBrain/
├── Runtime/
│   └── Core/
│       └── NPCBrainGizmoSettings.cs    # Serialized gizmo preferences
│
└── Editor/
    └── Gizmos/
        ├── NPCBrainGizmoDrawer.cs      # Main coordinator, OnDrawGizmos
        ├── VisionConeGizmo.cs          # Vision cone drawing utilities
        ├── HearingGizmo.cs             # Hearing range utilities
        ├── WaypointGizmo.cs            # Path visualization utilities
        ├── TargetGizmo.cs              # Target line utilities
        ├── DebugLabelGizmo.cs          # World-space label utilities
        └── GizmoColors.cs              # Centralized color definitions
```

### Main Coordinator

```csharp
// NPCBrainGizmoDrawer.cs
[CustomEditor(typeof(NPCBrain))]
public class NPCBrainGizmoDrawer : Editor
{
    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    static void DrawGizmos(NPCBrain brain, GizmoType gizmoType)
    {
        if (!NPCBrainSettings.Instance.EnableSceneGizmos) return;
        
        bool isSelected = (gizmoType & GizmoType.Selected) != 0;
        float distance = GetDistanceToSceneCamera(brain.transform.position);
        
        // Culling
        if (distance > NPCBrainSettings.Instance.GizmoMaxDistance) return;
        
        // LOD level
        int lod = distance > NPCBrainSettings.Instance.GizmoLODDistance ? 1 : 0;
        
        // Only show non-selected if allowed
        if (!isSelected && !NPCBrainSettings.Instance.ShowAllNPCGizmos) return;
        if (!isSelected && brain.GizmosOnlyWhenSelected) return;
        
        // Draw each gizmo type
        if (brain.ShowVisionCone)
            VisionConeGizmo.Draw(brain, isSelected, lod);
            
        if (brain.ShowHearingRange && isSelected) // Hearing only when selected
            HearingGizmo.Draw(brain, lod);
            
        if (brain.ShowWaypointPath)
            WaypointGizmo.Draw(brain, isSelected, lod);
            
        if (brain.ShowTargetLines && isSelected)
            TargetGizmo.Draw(brain);
            
        if (brain.ShowDebugLabel)
            DebugLabelGizmo.Draw(brain, lod);
    }
}
```

### OnDrawGizmos vs OnDrawGizmosSelected Strategy

| Gizmo Type | OnDrawGizmos (Always) | OnDrawGizmosSelected |
|------------|----------------------|---------------------|
| Vision Cone | Simplified (wireframe edge only) | Full (filled + peripheral + rings) |
| Hearing Range | ❌ Hidden | ✓ Full detail |
| Waypoint Path | ✓ Basic lines | ✓ + direction arrows + current highlight |
| Target Lines | ❌ Hidden | ✓ Full detail |
| Debug Label | ✓ Compact | ✓ Full detail |

---

## Color Reference

### Centralized Color Definitions

```csharp
// GizmoColors.cs
public static class GizmoColors
{
    // Alert States
    public static readonly Color Idle = new Color(0.30f, 0.69f, 0.31f);      // #4CAF50 Green
    public static readonly Color Suspicious = new Color(1.00f, 0.76f, 0.03f); // #FFC107 Yellow
    public static readonly Color Alert = new Color(0.96f, 0.26f, 0.21f);      // #F44336 Red
    public static readonly Color Searching = new Color(1.00f, 0.60f, 0.00f);  // #FF9800 Orange
    
    // Perception
    public static readonly Color Hearing = new Color(0.00f, 0.74f, 0.83f);    // #00BCD4 Cyan
    public static readonly Color Sound = new Color(1.00f, 0.92f, 0.23f);      // #FFEB3B Yellow
    
    // Waypoints
    public static readonly Color WaypointPath = Color.white;
    public static readonly Color WaypointCurrent = new Color(0.30f, 0.69f, 0.31f);  // Green
    public static readonly Color WaypointUpcoming = new Color(0.13f, 0.59f, 0.95f); // #2196F3 Blue
    public static readonly Color WaypointCompleted = new Color(0.62f, 0.62f, 0.62f);// Gray
    
    // Targets
    public static readonly Color TargetHostile = new Color(0.96f, 0.26f, 0.21f);    // Red
    public static readonly Color TargetNeutral = new Color(1.00f, 0.76f, 0.03f);    // Amber
    public static readonly Color TargetFriendly = new Color(0.30f, 0.69f, 0.31f);   // Green
    
    // Criticality
    public static readonly Color TooOrdered = new Color(0.13f, 0.59f, 0.95f);  // Blue
    public static readonly Color InBand = new Color(0.30f, 0.69f, 0.31f);      // Green
    public static readonly Color TooChaotic = new Color(0.96f, 0.26f, 0.21f);  // Red
    
    // UI
    public static readonly Color LabelBackground = new Color(0, 0, 0, 0.8f);
}
```

---

## Performance Considerations

### Budgets

| Scenario | Target |
|----------|--------|
| 1 selected NPC | < 0.1ms |
| 20 NPCs visible | < 0.5ms |
| 100 NPCs in scene | < 1.0ms (with LOD/culling) |

### Optimization Techniques

1. **Distance Culling**: Skip gizmos beyond `GizmoMaxDistance`
2. **LOD System**: Reduce arc segments and detail at distance
3. **Selection Priority**: Full detail only for selected NPCs
4. **Cached Calculations**: Cache arc points, reuse each frame
5. **Conditional Drawing**: Skip if system is disabled (no hearing? no hearing gizmo)

### Arc Segment Counts

| Distance | Segments |
|----------|----------|
| 0-15m | 32 |
| 15-30m | 16 |
| 30-50m | 8 |
| 50m+ | Hidden |

---

## Implementation Phases

### Phase 1 (Week 3) - Essential Gizmos
- [ ] Vision cone (filled arc + wireframe edge)
- [ ] Alert state colors
- [ ] Basic waypoint path lines
- [ ] Distance culling

### Phase 2 (Week 4) - Full Perception
- [ ] Hearing range circle
- [ ] Sound event indicators
- [ ] Target lines (visible + memory)
- [ ] Investigation point marker

### Phase 3 (Week 6) - Polish
- [ ] World-space debug labels with LOD
- [ ] Direction arrows on paths
- [ ] Criticality indicator
- [ ] Settings integration with debug window
- [ ] Performance optimization

---

## Success Criteria

1. ✅ **Informative** - Understand NPC perception at a glance
2. ✅ **Non-cluttering** - Smart defaults prevent scene clutter
3. ✅ **Consistent** - Colors match debug window exactly
4. ✅ **Performant** - < 1ms for 100 NPCs
5. ✅ **Toggleable** - Every gizmo type can be individually toggled
6. ✅ **LOD System** - Graceful degradation with distance

---

## References

- Unity Gizmos API documentation
- Unity Handles API documentation
- Stealth game vision cone implementations (Metal Gear, Hitman)
- Behavior Designer scene visualization
