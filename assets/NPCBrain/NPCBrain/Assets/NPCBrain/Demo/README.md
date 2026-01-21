# NPCBrain Demo Scenes

This folder contains demo scenes showcasing NPCBrain's AI capabilities.

## Demo Scenes

### 1. Stealth Infiltration Demo (`StealthDemo.unity`) ⭐ NEW
A spy/infiltrator must sneak through a facility guarded by patrols, collect intel, and reach extraction.

**Features Showcased:**
- SightSensor vision cones (visible in debug - red cones)
- HearingSensor footstep detection
- Guard alert levels and investigation behavior
- Patrol routes with WaypointPath
- Player sneaking mechanics (sprint = louder)
- Intel collection system
- Extraction zone

**Controls:**
- WASD - Move
- Shift - Sprint (makes more noise!)
- E - Collect intel when near
- F1 - Toggle UI
- R - Restart mission

**Objective:** Collect the primary intel (red) and any secondary intel (blue), then reach the extraction zone (cyan helipad) without being detected!

---

### 2. Cops and Robbers Demo (`CopsAndRobbersDemo.unity`)
Robbers try to steal loot and escape while cops patrol and arrest them.

**Features Showcased:**
- Utility AI decision-making
- Multi-agent coordination (cops share robber sightings)
- Smart flee algorithm (robber evades multiple cops)
- Opportunistic loot grabbing
- Fear/urgency system

**Controls:**
- F1 - Toggle UI
- R - Restart game

---

### 3. Guard Demo (`GuardDemo.unity`)
A player-controlled character avoids guards with sight-based detection.

**Features Showcased:**
- GuardNPC + Utility AI
- Criticality system (behavioral variety)
- Chase, Investigate, Return, Patrol behaviors

**Controls:**
- WASD - Move player
- Shift - Sprint

---

### 4. Patrol Demo (`PatrolDemo.unity`)
Simple patrol route demonstration.

---

### 5. Hearing Demo (`HearingDemo.unity`)
Demonstrates HearingSensor and sound propagation.

---

### 6. Utility Demo (`UtilityDemo.unity`)
Raw Utility AI scoring visualization.

---

## Quick Start

1. Open any demo scene in Unity
2. Press Play
3. The scene will auto-generate (if `Auto Generate` is enabled)
4. Use the on-screen controls

## Tips

- In Stealth Demo, watch the guard vision cones (red) - stay out of their sight!
- Guards will investigate sounds, so sprint carefully
- The UI shows real-time guard states and your detection status
