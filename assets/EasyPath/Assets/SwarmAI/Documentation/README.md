# SwarmAI - Multi-Agent Coordination System

**Version:** 1.0  
**Unity Compatibility:** Unity 2021.3+ (LTS), Unity 2022.x, Unity 6+  
**License:** Asset Store EULA

SwarmAI is a Unity asset for creating swarm behaviors, multi-agent coordination, and emergent AI systems. Built from MIT Battlecode 2026 experience, it provides patterns for RTS games, colony simulators, and AI-driven projects.

---

## Table of Contents

- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Core Concepts](#core-concepts)
- [Documentation](#documentation)
- [Demo Scenes](#demo-scenes)
- [Support](#support)

---

## Features

### Core Systems
- **SwarmManager** - Central coordinator with spatial partitioning for efficient neighbor queries
- **SwarmAgent** - Flexible agent component with state machine and behavior system
- **Spatial Hash** - O(1) neighbor queries supporting 100+ agents at 60 FPS

### Steering Behaviors
- **Seek** - Move toward a target position
- **Flee** - Move away from threats
- **Arrive** - Seek with smooth deceleration
- **Wander** - Random smooth movement for idle states
- **Obstacle Avoidance** - Raycast-based obstacle steering
- **Separation** - Avoid crowding neighbors
- **Alignment** - Match neighbor velocity
- **Cohesion** - Move toward group center
- **Follow Leader** - Smooth leader-follower patterns

### State Machine
- **Idle** - Default resting state
- **Moving** - Direct movement to position
- **Seeking** - Active pursuit of target
- **Fleeing** - Escape from threats
- **Gathering** - Resource collection
- **Returning** - Return to base with resources
- **Following** - Follow leader or formation

### Advanced Features
- **Formations** - Line, Column, Circle, Wedge, V, Box, Custom
- **Group Coordination** - SwarmGroup with leader-follower patterns
- **Resource Gathering** - Complete gather-return-deposit loop
- **Inter-Agent Messaging** - Type-safe message passing system
- **EasyPath Integration** - Optional pathfinding support

---

## Installation

### From Unity Asset Store
1. Purchase and download SwarmAI from the Asset Store
2. Import the package via **Assets → Import Package → Custom Package**
3. The SwarmAI folder will be created under `Assets/`

### From Package
1. Copy the `SwarmAI` folder to your project's `Assets/` directory
2. Unity will automatically compile the scripts

### Assembly Definitions
SwarmAI uses assembly definitions for clean compilation:
- `SwarmAI.Runtime` - Core runtime code
- `SwarmAI.Editor` - Editor tools and inspectors
- `SwarmAI.Demo` - Demo scene scripts
- `SwarmAI.Tests.Editor` - Edit-mode tests
- `SwarmAI.Tests.Runtime` - Play-mode tests

---

## Quick Start

### Step 1: Create a SwarmManager

```csharp
// The SwarmManager is created automatically as a singleton
// Or add it manually to a GameObject in your scene
var manager = SwarmManager.Instance;
```

### Step 2: Create Agent Prefab

Create a prefab with the `SwarmAgent` component:

```csharp
// Create agent GameObject
GameObject agentObj = new GameObject("Agent");
SwarmAgent agent = agentObj.AddComponent<SwarmAgent>();

// Agent automatically registers with SwarmManager on Enable
```

### Step 3: Add Behaviors

```csharp
// Add flocking behaviors
agent.AddBehavior(new SeparationBehavior(), 1.5f);  // Avoid crowding
agent.AddBehavior(new AlignmentBehavior(), 1.0f);   // Match velocity
agent.AddBehavior(new CohesionBehavior(), 1.0f);    // Stay together
agent.AddBehavior(new WanderBehavior(), 0.3f);      // Random movement
```

### Step 4: Control Agents

```csharp
// Move a single agent
agent.SetTarget(targetPosition);
agent.SetState(new SeekingState(targetPosition));

// Command all agents
SwarmManager.Instance.MoveAllTo(targetPosition);
SwarmManager.Instance.SeekAll(targetPosition);
SwarmManager.Instance.StopAll();

// Send messages
SwarmManager.Instance.BroadcastMessage(SwarmMessage.Seek(position));
```

### Complete Example

```csharp
using UnityEngine;
using SwarmAI;

public class SwarmDemo : MonoBehaviour
{
    public GameObject agentPrefab;
    public int agentCount = 20;
    
    void Start()
    {
        // Spawn agents with flocking behaviors
        for (int i = 0; i < agentCount; i++)
        {
            Vector3 pos = Random.insideUnitSphere * 5f;
            pos.y = 0;
            
            GameObject obj = Instantiate(agentPrefab, pos, Quaternion.identity);
            SwarmAgent agent = obj.GetComponent<SwarmAgent>();
            
            // Add flocking behaviors
            agent.AddBehavior(new SeparationBehavior(3f), 1.5f);
            agent.AddBehavior(new AlignmentBehavior(5f), 1.0f);
            agent.AddBehavior(new CohesionBehavior(5f), 1.0f);
            agent.AddBehavior(new WanderBehavior(), 0.5f);
            agent.AddBehavior(new ObstacleAvoidanceBehavior(), 2.0f);
        }
    }
    
    void Update()
    {
        // Click to set flock target
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                SwarmManager.Instance.SeekAll(hit.point);
            }
        }
    }
}
```

---

## Core Concepts

### SwarmManager
The central coordinator that manages all agents. It provides:
- Agent registration and lookup
- Spatial partitioning for efficient neighbor queries
- Message routing between agents
- Global commands (MoveAllTo, StopAll, etc.)

### SwarmAgent
The component attached to each agent. Features:
- Finite State Machine (FSM) for behavior control
- Weighted steering behavior system
- Automatic neighbor detection
- Message handling

### Steering Behaviors
Modular forces that influence agent movement:
- Behaviors are weighted and combined
- Can be enabled/disabled at runtime
- Easy to create custom behaviors

### Agent States
FSM states that control agent logic:
- Each state has Enter, Execute, Exit methods
- States can trigger transitions
- States can handle messages

---

## Documentation

| Document | Description |
|----------|-------------|
| [Getting Started](GETTING-STARTED.md) | Step-by-step tutorials |
| [API Reference](API-REFERENCE.md) | Complete class documentation |
| [Behaviors](BEHAVIORS.md) | Steering behavior guide |
| [States](STATES.md) | Agent state documentation |
| [Examples](EXAMPLES.md) | Code samples and patterns |

---

## Demo Scenes

SwarmAI includes three demo scenes showcasing key features:

### Flocking Demo
**Menu:** SwarmAI → Create Demo Scene → Flocking Demo

Demonstrates boids-style flocking with:
- Separation, Alignment, Cohesion
- Wander behavior
- Obstacle avoidance
- Click to set flock target

**Controls:**
- `1-5` - Toggle behaviors
- `Space` - Scatter flock
- `G` - Gather at center
- `Left Click` - Set target

### Formation Demo
**Menu:** SwarmAI → Create Demo Scene → Formation Demo

Showcases formation system with:
- Line, Column, Circle, Wedge, V, Box formations
- Leader-follower movement
- WASD leader control

**Controls:**
- `1-6` - Change formation type
- `+/-` - Adjust spacing
- `WASD` - Move leader
- `Left Click` - Move formation to point

### Resource Gathering Demo
**Menu:** SwarmAI → Create Demo Scene → Resource Gathering Demo

RTS-style resource collection with:
- Multiple resource nodes
- Gather and return states
- Worker assignment

**Controls:**
- `G` - Send all to gather
- `H` - Send all home
- `N` - Spawn new resource
- `Left Click` - Assign worker to resource

---

## Configuration

### SwarmSettings (ScriptableObject)

Create custom settings via **Assets → Create → SwarmAI → Swarm Settings**

| Setting | Default | Description |
|---------|---------|-------------|
| `SpatialHashCellSize` | 5.0 | Cell size for spatial partitioning |
| `MaxAgentsPerFrame` | 50 | Max messages processed per frame |
| `NeighborRadius` | 5.0 | Default neighbor detection radius |
| `WanderRadius` | 2.0 | Wander circle radius |
| `WanderDistance` | 4.0 | Wander circle distance |
| `WanderJitter` | 80.0 | Wander randomness |
| `ObstacleAvoidanceDistance` | 5.0 | Raycast distance for obstacles |
| `EnableDebugVisualization` | false | Show debug gizmos |

---

## Performance Tips

1. **Use appropriate cell size** - Set `SpatialHashCellSize` to ~2x agent radius
2. **Limit behaviors** - Use only necessary behaviors per agent
3. **Adjust neighbor radius** - Smaller radius = faster queries
4. **Use LOD** - Disable behaviors for distant agents
5. **Batch messages** - Use BroadcastMessage instead of individual sends

### Performance Targets
- **100+ agents** at 60 FPS (standalone)
- **50+ agents** at 60 FPS (mobile)
- **O(1)** neighbor queries via spatial hash

---

## Support

- **Documentation:** See the `/Documentation` folder
- **Demo Scenes:** SwarmAI → Create Demo Scene
- **Email:** support@example.com
- **Discord:** discord.gg/example

---

## Changelog

### Version 1.0.0
- Initial release
- Core systems: SwarmManager, SwarmAgent, SpatialHash
- 9 steering behaviors
- 7 agent states
- Formation system with 6 formation types
- Resource gathering system
- 3 demo scenes
- Comprehensive documentation

---


