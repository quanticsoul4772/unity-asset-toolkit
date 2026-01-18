# Project Ideas 
 
Last Updated: January 15, 2026 
 
## Current Status: EasyPath and SwarmAI Complete 
 
## Option 1: EasyPath (Complete)
**Simple A* pathfinding for beginners** 
 
- Price: $35 
- Target: Indie devs, first-time Unity users 
- Complexity: Medium (2-3 weeks) 
- Market gap: A* Pathfinding Project is $140 and complex 
- **Status:** Ready for Asset Store submission
 
Features (complete):
- Grid-based A* pathfinding 
- Visual path debugging in editor (EasyPathDebugWindow) 
- Custom inspectors for Grid and Agent 
- Full source code with assembly definitions 
- Demo scene generation (Basic, Multi-Agent, Stress Test) 
- Click-to-move (all agents), obstacle spawning, multi-agent controls 
- Obstacle layer auto-configuration (Fix Existing Demo Scenes menu)
- Unit tests (PathNode, PriorityQueue, Pathfinding integration)

Pending:
- User documentation for Asset Store 
 
## Option 2: SwarmAI (Complete)
**Multi-agent coordination (like Battlecode rats)** 
 
- Price: $45 
- Target: RTS/colony sim developers 
- Complexity: Medium-High (3-4 weeks) 
- Unique selling point: Built from real competition experience 
- **Status:** Core features complete, demos working
- **Design Doc:** docs/SWARMAI-DESIGN.md
 
Features (complete):
- Design document complete
- Folder structure created
- Assembly definitions configured
- SwarmManager singleton (with proper cleanup)
- SwarmAgent component
- State machine system (Idle, Moving, Seeking, Fleeing, Gathering, Returning, Following)
- Spatial partitioning (SpatialHash)
- Inter-agent messaging (SwarmMessage with formation/group/resource types)
- Steering behaviors (Seek, Flee, Arrive, Wander, Separation, Alignment, Cohesion, ObstacleAvoidance)
- Formation system (Line, Column, Circle, Wedge, V, Box + FormationSlotBehavior)
- Resource gathering (GatheringState, ReturningState, ResourceNode)
- Group coordination (SwarmGroup, FollowLeaderBehavior)
- Demo scenes (Flocking, Formation, Resource Gathering)
- Documentation (README, GETTING-STARTED, BEHAVIORS, STATES, EXAMPLES, API-REFERENCE, TROUBLESHOOTING)
- Unit tests (behaviors, formations, states, messages)
- Custom editors and debug window

Pending:
- Combat Formations demo
- Marketing materials for Asset Store 
 
## Option 3: NPCBrain (In Development)
**All-in-one AI toolkit** 
 
- Price: $60 
- Target: Intermediate developers 
- Complexity: High (4-6 weeks) 
- Combines pathfinding + behaviors + sensing + decision making
- **Status:** In Development
- **Design Doc:** docs/NPCBRAIN-DESIGN.md (to be created)

Features (planned):
- Design document
- Folder structure and assembly definitions
- Behavior Tree system (Selector, Sequence, Parallel, Decorator nodes)
- Utility AI system (action scoring, considerations)
- Perception system (sight, hearing, memory)
- Knowledge/Blackboard system
- Integration with EasyPath (pathfinding)
- Integration with SwarmAI (steering behaviors)
- Pre-built NPC archetypes (Guard, Patrol, Civilian, Enemy)
- Visual debugging tools
- Demo scenes
- Documentation
- Unit tests 
 
## Recommendation 
~~Start with EasyPath - lowest complexity, clear market demand.~~ 
 
**UPDATE (January 2026):** Both EasyPath and SwarmAI core features are complete!

### EasyPath Status
- Pathfinding tested and functional
- Demo scenes available via EasyPath menu
- Multi-agent click-to-move support
- Obstacle layer auto-configuration
- Unit tests added
- Ready for Asset Store submission (needs marketing materials)

### SwarmAI Status
- All steering behaviors working (Seek, Flee, Arrive, Wander, Flocking)
- Formation system with FormationSlotBehavior for stable formations
- Resource gathering with GatheringState/ReturningState
- Demo scenes working (Flocking, Formation, Resource Gathering)
- Comprehensive documentation
- Unit tests for all behaviors and components
- Combat Formations demo in progress

**Next:** Complete NPCBrain development.
