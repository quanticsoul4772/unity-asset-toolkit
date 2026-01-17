# NPCBrain Debug Window Design

**Version:** 1.0  
**Status:** Planning  
**Last Updated:** January 2026

## Overview

The NPCBrainDebugWindow is a professional multi-panel editor window using **UI Toolkit** that provides comprehensive runtime visualization of all NPCBrain systems. It follows Unity's native editor aesthetics while offering glanceable status and drill-down detail.

**Key Design Principles:**
- **Glanceable** - See NPC status at a glance without digging
- **Drill-down** - Click anything to see more detail
- **Non-intrusive** - Minimal performance overhead
- **Professional** - Matches Unity's native look and feel

---

## Window Layout

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ NPCBrain Debug ──────────────────────────────────────────────────────────── │
│ NPC: [Guard_01 ▼] [☑ Auto-Follow] [⏸ Pause] [▶ Step] │ 12 NPCs @ 0.3ms    │
├────────────────┬────────────────────────────────────────┬───────────────────┤
│ OVERVIEW       │ MAIN VIEW                              │ DETAILS           │
│                │ [BT] [Utility] [Perception]            │                   │
│ ▼ Status       │ [Criticality] [Blackboard]             │ Selected: Patrol  │
│ State: Patrol  │                                        │                   │
│ Health: ████░░ │    ┌─────────────────────┐            │ Type: Action      │
│ Uptime: 45.2s  │    │      Selector       │            │ Status: Running   │
│                │    └──────────┬──────────┘            │ Duration: 3.2s    │
│ ▼ Behavior Tree│    ┌──────────┼──────────┐            │                   │
│ 🟢 Patrol      │    │          │          │            │ Properties:       │
│ Path: Root→    │ ┌──┴──┐  ┌────┴───┐ ┌────┴───┐       │ • WaypointIdx: 2  │
│   Sel→Patrol   │ │Chase│  │Investig│ │ Patrol │       │ • Speed: 2.5      │
│                │ │ ⚪  │  │   ⚪   │ │  🟢    │       │                   │
│ ▼ Utility AI   │ └─────┘  └────────┘ └────────┘       │ ─── History ───   │
│ Patrol    0.72 │                                        │ 12:03:45 Started  │
│ Idle      0.45 │                                        │ 12:03:42 WP 1→2   │
│ Chase     0.00 │                                        │ 12:03:38 Started  │
│                │                                        │                   │
│ ▼ Perception   │                                        │                   │
│ 👁 Targets: 0  │                                        │                   │
│ 👂 Sounds: 0   │                                        │                   │
│ 🧠 Memory: 2   │                                        │                   │
│                │                                        │                   │
│ ▼ Criticality  │                                        │                   │
│ Chaos: 0.47 ✓  │                                        │                   │
│ ████████░░     │                                        │                   │
│ Temp: 1.2      │                                        │                   │
│ Inertia: 0.35  │                                        │                   │
├────────────────┴────────────────────────────────────────┴───────────────────┤
│ TIMELINE ───────────────────────────────────────────────── [−60s ────▼── 0] │
│ States:  │▓▓▓▓ Idle ▓▓│▓▓▓▓▓▓▓▓▓ Patrol ▓▓▓▓▓▓▓▓▓│▓▓ Investigate ▓▓│      │
│ Actions: ○────────○──────────────○────────────────○────────────────○       │
│ Events:          👁              👂                   ❓                     │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## Panel Breakdown

### 1. Header Bar

| Element | Description |
|---------|-------------|
| **NPC Dropdown** | Lists all NPCBrain instances in scene, searchable |
| **Auto-Follow Checkbox** | Syncs selection with Unity Hierarchy |
| **Pause Button** | Freezes all NPCBrain ticks for inspection |
| **Step Button** | Advances one tick while paused |
| **Performance Display** | "X NPCs @ Y.Yms" total overhead indicator |

### 2. Left Panel: Overview (Always Visible)

Collapsible foldout sections showing summary of each system at a glance.

#### Status Section
```
▼ Status
State: Patrol        ← Current high-level state
Health: ████░░ 80%   ← Health bar with percentage
Target: None         ← Current target (or "None")
Uptime: 45.2s        ← Time since NPC spawned
```

#### Behavior Tree Section
```
▼ Behavior Tree
🟢 Patrol            ← Currently executing node with status icon
Path: Root → Selector → Patrol   ← Full path to active node
Ticks: 142           ← Total tick count
```

#### Utility AI Section
```
▼ Utility AI
Patrol    ████████░░ 0.72   ← Top 3-5 actions with score bars
Idle      ████░░░░░░ 0.45   ← Selected action highlighted
Chase     ░░░░░░░░░░ 0.00   ← Zero scores grayed out
```

#### Perception Section
```
▼ Perception
👁 Visible: 0        ← Targets currently in sight
👂 Sounds: 1         ← Recent sounds heard
🧠 Memory: 2         ← Remembered targets
⚠️ Threat: Low       ← Aggregate threat level
```

#### Criticality Section
```
▼ Adaptive Behavior
Balance: 0.47 ✓      ← Chaos index with status icon
████████░░           ← Gauge showing position in band
                       (Blue = too ordered, Green = in band, Red = too chaotic)
Exploration: 1.2     ← Temperature (friendly name)
Commitment: 0.35     ← Inertia (friendly name)
```

### 3. Center Panel: Main View (Tabbed)

Five tabs for detailed views of each system.

---

#### Tab 1: Behavior Tree

Visual tree representation of the behavior tree structure.

**Node Shapes by Type:**
| Type | Shape | Description |
|------|-------|-------------|
| Composite | Hexagon | Selector, Sequence, Parallel |
| Decorator | Diamond | Inverter, Repeater, Cooldown |
| Action | Rectangle | MoveTo, Wait, Attack |
| Condition | Circle | CheckDistance, CheckHealth |

**Node Colors by Status:**
| Status | Color | Icon |
|--------|-------|------|
| Running | Green (#4CAF50) | 🟢 |
| Success | Light Green (#8BC34A) | ✓ |
| Failure | Red (#F44336) | ✗ |
| Inactive | Gray (#9E9E9E) | ⚪ |
| Aborted | Amber (#FFC107) | ⚠ |

**Interactions:**
- Click node → Select (shows details in right panel)
- Double-click node → Set breakpoint (red dot appears)
- Right-click → Context menu (Go to Code, Disable, Copy Path)
- Zoom/pan with mouse wheel and drag

**Example View:**
```
                    ┌───────────────┐
                    │   Selector    │
                    │      🟢       │
                    └───────┬───────┘
           ┌────────────────┼────────────────┐
           │                │                │
    ┌──────┴──────┐  ┌──────┴──────┐  ┌──────┴──────┐
    │   Sequence  │  │   Sequence  │  │   Patrol    │
    │     ⚪      │  │     ⚪      │  │     🟢      │
    └──────┬──────┘  └──────┬──────┘  └─────────────┘
           │                │
    ┌──────┴──────┐  ┌──────┴──────┐
    │ CheckTarget │  │ CheckSound  │
    │   ◯ ✗      │  │   ◯ ✗      │
    └─────────────┘  └─────────────┘
```

---

#### Tab 2: Utility AI

Horizontal bar chart visualization of action scoring.

**Bar Colors:**
| Status | Color | Meaning |
|--------|-------|---------|
| Selected | Green | Currently executing |
| Viable | Blue | Above threshold, could be picked |
| Disqualified | Gray | Zero score (failed consideration) |
| Cooldown | Red stripe | On cooldown, temporarily unavailable |

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│ Action Scores                          Temperature: 1.2 │
├─────────────────────────────────────────────────────────┤
│ ▶ Patrol     ████████████████████████████░░░░  0.72    │ ← Selected (▶)
│   Idle       ███████████████░░░░░░░░░░░░░░░░░  0.45    │
│   Investigate████████████░░░░░░░░░░░░░░░░░░░░  0.38    │
│   Chase      ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  0.00 ⊘  │ ← Disqualified (⊘)
│   Attack     ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  0.00 🕐 │ ← On cooldown (🕐)
│   Flee       ░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░  0.00    │
├─────────────────────────────────────────────────────────┤
│ Selection Probability (after softmax):                  │
│ Patrol: 62%  Idle: 28%  Investigate: 10%               │
└─────────────────────────────────────────────────────────┘
```

**Click Action to Expand Considerations:**
```
▼ Patrol (0.72)
  ├── HasWaypoints      1.00  ████████████████████
  ├── NoThreat          0.95  ███████████████████░
  ├── NotTired          0.80  ████████████████░░░░
  └── TimeOnTask        0.95  ███████████████████░
      Compensation: +0.05
      Final: 0.72
```

**Response Curve Preview:**
When hovering over a consideration, show mini-graph of the response curve with current input marked.

---

#### Tab 3: Perception

Top-down radar view centered on the NPC.

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│ Perception Radar                    Range: 20m  FOV: 120°│
├─────────────────────────────────────────────────────────┤
│                                                          │
│                        · · ·                             │
│                    · ·       · ·                         │
│                  ·               ·     🔴 Enemy (12m)    │
│                ·        ▲         ·                      │
│               ·         │          ·                     │
│              ·    ◄─────┼─────►    ·                     │
│               ·         │          ·                     │
│                ·        ▼         ·     🟡 Memory (8m)   │
│                  ·               ·       (faded)         │
│                    · ·       · ·                         │
│                        · · ·                             │
│     ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○ ○  ← Hearing radius │
│                                                          │
├─────────────────────────────────────────────────────────┤
│ Visible Targets:                                         │
│   (none)                                                 │
│                                                          │
│ Recent Sounds:                                           │
│   🔊 Footstep @ 8m NE (2.3s ago) - Investigating        │
│                                                          │
│ Memory:                                                  │
│   🧠 Player @ (12, 0, 8) - 15s ago - 40% confidence     │
│   🧠 Guard_02 @ (5, 0, 3) - 8s ago - 80% confidence     │
└─────────────────────────────────────────────────────────┘
```

**Elements:**
- Vision cone (filled semi-transparent)
- Hearing radius (dashed circle)
- Targets as colored dots with distance labels
- Memory targets faded based on decay
- Click target for full details in right panel

---

#### Tab 4: Criticality (Adaptive Behavior)

Detailed view of all criticality metrics and controls.

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│ Adaptive Behavior                                        │
├─────────────────────────────────────────────────────────┤
│                                                          │
│              ┌─────────────────────────┐                │
│              │     BEHAVIOR BALANCE    │                │
│              │                         │                │
│              │    ◄── 0.47 ──►        │                │
│              │  ░░░░████████░░░░░░    │                │
│              │  0.0  ↑    ↑  1.0      │                │
│              │      0.40  0.55        │                │
│              │      (target band)      │                │
│              │                         │                │
│              │      ✓ IN BAND         │                │
│              └─────────────────────────┘                │
│                                                          │
├─────────────────────────────────────────────────────────┤
│ Order Parameters                                         │
│                                                          │
│ Action Variety   ████████░░  0.52  [0.35 ─────── 0.55] │
│ Decision Stable  ████░░░░░░  0.24  [0.15 ─────── 0.30] │
│ Surprise         ██████░░░░  0.38  [0.20 ─────── 0.40] │
│ State Stability  ███░░░░░░░  0.18  [0.10 ─────── 0.25] │
│                                                          │
│ Sparkline History (last 60s):                           │
│ Variety:   ▁▂▃▄▅▆▇█▇▆▅▄▃▄▅▆▇▆▅▄▃▄▅▆▅▄▃▂▃▄▅            │
│                                                          │
├─────────────────────────────────────────────────────────┤
│ Control Knobs (Auto-Adjusted)                           │
│                                                          │
│ Exploration:     ████████████░░░░  1.20  [0.1 - 2.0]   │
│ Commitment:      ██████░░░░░░░░░░  0.35  [0.0 - 1.0]   │
│ Attention:       ████████░░░░░░░░  6     [3 - 12]      │
│ Group Alignment: ██████████░░░░░░  0.50  [0.0 - 1.0]   │
│                                                          │
└─────────────────────────────────────────────────────────┘
```

**Color Coding for Chaos Index:**
- Blue (#2196F3): Too ordered (below 0.40) - "Predictable"
- Green (#4CAF50): In band (0.40-0.55) - "Balanced"
- Red (#F44336): Too chaotic (above 0.55) - "Erratic"

---

#### Tab 5: Blackboard

Table view of all blackboard key-value pairs.

**Layout:**
```
┌─────────────────────────────────────────────────────────┐
│ Blackboard                          🔍 [Search...     ] │
├─────────────────────────────────────────────────────────┤
│ Key                │ Value              │ Type │ Age   │
├────────────────────┼────────────────────┼──────┼───────┤
│ target             │ (null)             │ GO   │ -     │
│ lastKnownPosition  │ (12.0, 0.0, 8.5)  │ V3   │ 15.2s │
│ alertLevel         │ 1                  │ int  │ 3.1s  │
│ homePosition       │ (0.0, 0.0, 0.0)   │ V3   │ -     │
│ currentWaypoint    │ 2                  │ int  │ 1.2s  │
│ patrolDirection    │ 1                  │ int  │ 45.0s │
│ health             │ 80                 │ int  │ 0.1s  │
│ lastSoundPosition  │ (8.0, 0.0, 5.0)   │ V3   │ 2.3s  │
└─────────────────────────────────────────────────────────┘

[+ Add Key]  [🗑 Clear All]
```

**Features:**
- Sort by clicking column headers
- Search/filter bar
- Type indicators (GO=GameObject, V3=Vector3, int, float, bool, string)
- Age column shows time since last modification
- Double-click value to edit (debug mode only)
- Highlight recently changed values (flash yellow)

---

### 4. Right Panel: Details Inspector

Context-sensitive detail view that changes based on what's selected.

**When BT Node Selected:**
```
┌─────────────────────┐
│ Patrol (Action)     │
├─────────────────────┤
│ Status: Running     │
│ Duration: 3.2s      │
│ Tick Count: 47      │
│ Last Result: Running│
│                     │
│ ── Properties ──    │
│ WaypointIndex: 2    │
│ Speed: 2.5          │
│ WaitAtWaypoint: 1.0 │
│                     │
│ ── Node Path ──     │
│ Root                │
│ └─ Selector         │
│    └─ Patrol ◀      │
│                     │
│ ── Event Log ──     │
│ 12:03:45 OnEnter    │
│ 12:03:42 WP 1 → 2   │
│ 12:03:38 OnEnter    │
│ 12:03:35 OnExit     │
└─────────────────────┘
```

**When Utility Action Selected:**
```
┌─────────────────────┐
│ Patrol Action       │
├─────────────────────┤
│ Score: 0.72         │
│ Probability: 62%    │
│ Times Selected: 8   │
│ Cooldown: None      │
│                     │
│ ── Considerations ──│
│                     │
│ HasWaypoints   1.00 │
│ ████████████████████│
│ Input: true → 1.0   │
│ Curve: Step         │
│                     │
│ NoThreat       0.95 │
│ ███████████████████░│
│ Input: 0.05 → 0.95  │
│ Curve: Inverse Lin. │
│                     │
│ [View Response Curve]│
└─────────────────────┘
```

**When Perception Target Selected:**
```
┌─────────────────────┐
│ Target: Player      │
├─────────────────────┤
│ Type: Hostile       │
│ Distance: 12.4m     │
│ Direction: NE       │
│ Visibility: 0%      │
│ In Memory: Yes      │
│                     │
│ ── Memory ──        │
│ Last Seen: 15.2s ago│
│ Last Position:      │
│   (12.0, 0.0, 8.5)  │
│ Confidence: 40%     │
│ Decay: ████░░░░░░   │
│                     │
│ ── Threat Score ──  │
│ Distance:  0.6      │
│ Visibility: 0.0     │
│ Hostility: 1.0      │
│ Total: 0.32         │
└─────────────────────┘
```

---

### 5. Bottom Panel: Timeline

Horizontal scrolling history view (last 60 seconds by default).

**Layout:**
```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Timeline                                              [-60s ────────▼─── 0] │
├─────────────────────────────────────────────────────────────────────────────┤
│ -60s        -45s        -30s        -15s        Now                         │
│  │           │           │           │           │                          │
│                                                                              │
│ States:                                                                      │
│ ▓▓▓▓▓▓ Idle ▓▓▓▓│▓▓▓▓▓▓▓▓▓▓▓▓ Patrol ▓▓▓▓▓▓▓▓▓▓▓│▓▓▓▓ Investigate ▓▓▓▓│    │
│                                                                              │
│ Actions:                                                                     │
│ ────○────────○──────────────○────────────────○────────────────○────         │
│     Idle     Patrol         WP1→2            Heard Sound      Look          │
│                                                                              │
│ Events:                                                                      │
│              👁                👂               ❓                            │
│           Spotted           Heard           Lost Target                      │
│                                                                              │
│ Criticality:                                                                 │
│ ▁▂▃▄▅▆▇█▇▆▅▄▃▄▅▆▇▆▅▄▃▄▅▆▅▄▃▂▃▄▅▆▅▄▃▂▁▂▃▄▅▄▃▂▃▄▅▆▅▄▅▆▇▆▅▄▃▄             │
│ ══════════════════════════════════════════════════════════════(target band) │
│                                                                              │
│ [◀◀] [◀] [▶] [▶▶]  │  🔴 Recording  │  [Export...]                         │
└─────────────────────────────────────────────────────────────────────────────┘
```

**Features:**
- Scrubber to jump to any point in history
- Click event to see details at that moment
- Zoom in/out on time range
- Export to file for post-mortem analysis
- Recording indicator

**Event Icons:**
| Icon | Event |
|------|-------|
| 👁 | Target spotted |
| 👂 | Sound heard |
| ❓ | Target lost |
| ⚔️ | Attack started |
| 💔 | Damage taken |
| 🏃 | Fled |
| ⚠️ | Criticality left band |

---

## Color Scheme

### Status Colors
| Status | Color | Hex | Usage |
|--------|-------|-----|-------|
| Running/Active | Green | #4CAF50 | Active nodes, selected actions |
| Success | Light Green | #8BC34A | Completed nodes |
| Failure | Red | #F44336 | Failed nodes, errors |
| Inactive | Gray | #9E9E9E | Unexecuted nodes |
| Aborted | Amber | #FFC107 | Interrupted nodes |

### Criticality Colors
| State | Color | Hex |
|-------|-------|-----|
| Too Ordered | Blue | #2196F3 |
| In Band | Green | #4CAF50 |
| Too Chaotic | Red | #F44336 |

### Target Colors
| Type | Color | Hex |
|------|-------|-----|
| Hostile | Red | #F44336 |
| Neutral | Amber | #FFC107 |
| Friendly | Green | #4CAF50 |
| Memory (faded) | 50% opacity | - |

### UI Colors
| Element | Color | Hex |
|---------|-------|-----|
| Background | Dark Gray | #2D2D2D |
| Panel | Slightly Lighter | #383838 |
| Text | Light Gray | #E0E0E0 |
| Accent | Unity Blue | #3E8EDE |
| Highlight | Yellow | #FFEB3B |

---

## Interactive Features

### 1. Breakpoints
- Double-click BT node to toggle breakpoint
- Red dot indicator on node
- Pauses NPC tick when breakpoint hit
- "Step" button to advance one tick

### 2. Force Actions
- Right-click action in Utility tab → "Force Select"
- Overrides normal selection for N ticks
- Orange highlight indicates forced state

### 3. Inject Events
- Button to simulate perception events
- "Inject Sound" → Pick position on radar
- "Inject Sighting" → Pick target from list
- Useful for testing without player

### 4. Value Override
- Double-click blackboard values to edit
- Slider for numeric values
- Checkbox for bools
- Object picker for GameObjects

### 5. Recording & Playback
- Record button captures all NPC data
- Playback mode scrubs through history
- Export to JSON for post-mortem analysis
- Import recordings for offline review

### 6. Comparison Mode
- "Add NPC" button creates split view
- Side-by-side comparison of two NPCs
- Useful for debugging group behavior

### 7. Scene Gizmos Toggle
- Checkbox to show/hide scene view overlays
- Vision cones, hearing spheres, paths
- Synced with debug window selection

---

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Space | Pause/Resume |
| S | Step (when paused) |
| 1-5 | Switch tabs |
| F | Focus selected NPC in Scene |
| R | Toggle recording |
| Ctrl+C | Copy selected node path |
| Delete | Clear breakpoints |

---

## Performance Considerations

| Feature | Update Rate | Notes |
|---------|-------------|-------|
| Overview panel | 10 Hz | Sufficient for glanceable info |
| BT tree view | On change | Only redraw when tree structure changes |
| Utility bars | 10 Hz | Match NPC tick rate |
| Perception radar | 10 Hz | Interpolate target positions |
| Timeline | 60 Hz | Smooth scrolling required |
| Blackboard | On change | Event-driven updates |

**Overhead Target:** < 0.5ms when window is open, 0ms when closed.

---

## Implementation Phases

### MVP (Week 2) - Essential Debugging
- [ ] EditorWindow skeleton with UI Toolkit
- [ ] NPC selector dropdown with auto-follow
- [ ] Overview panel (status, current node, current action)
- [ ] Basic BT tree view with status colors
- [ ] Simple blackboard table
- [ ] Pause/Step controls

### Phase 2 (Week 4) - Full Visualization
- [ ] Complete BT tree visualization with all shapes
- [ ] Utility AI tab with score bars
- [ ] Consideration breakdown view
- [ ] Perception radar view
- [ ] Right panel details inspector

### Phase 3 (Week 6) - Polish & Advanced Features
- [ ] Criticality tab with gauges
- [ ] Timeline with history
- [ ] Breakpoints
- [ ] Recording/playback
- [ ] Scene gizmo integration
- [ ] Keyboard shortcuts
- [ ] Performance optimization

---

## File Structure

```
NPCBrain/
└── Editor/
    └── Windows/
        ├── NPCBrainDebugWindow.cs       # Main window class
        ├── NPCBrainDebugWindow.uss      # Stylesheet
        ├── NPCBrainDebugWindow.uxml     # Layout template
        ├── Panels/
        │   ├── OverviewPanel.cs         # Left panel
        │   ├── BehaviorTreePanel.cs     # BT visualization
        │   ├── UtilityAIPanel.cs        # Utility scores
        │   ├── PerceptionPanel.cs       # Radar view
        │   ├── CriticalityPanel.cs      # Adaptive behavior
        │   ├── BlackboardPanel.cs       # Key-value table
        │   ├── DetailsPanel.cs          # Right panel
        │   └── TimelinePanel.cs         # Bottom history
        └── Components/
            ├── BTNodeElement.cs         # Individual tree node
            ├── ScoreBar.cs              # Horizontal bar
            ├── RadarView.cs             # Top-down perception
            ├── Gauge.cs                 # Vertical gauge
            ├── Sparkline.cs             # Mini history graph
            └── TimelineTrack.cs         # Timeline row
```

---

## Success Criteria

1. ✅ **Glanceable** - Understand NPC state in < 2 seconds
2. ✅ **Complete** - All systems visible (BT, Utility, Perception, Criticality, Blackboard)
3. ✅ **Interactive** - Click anything to see more detail
4. ✅ **Performant** - < 0.5ms overhead
5. ✅ **Professional** - Matches Unity's native editor style
6. ✅ **Debuggable** - Breakpoints, step, force actions work

---

## References

- Behavior Designer runtime debugger
- NodeCanvas visual debugging
- Unreal Engine 5 AI debugging tools
- Unity UI Toolkit documentation
- Unity Graph Toolkit (for future visual BT editor)
