# Project Ideas 
 
Last Updated: January 15, 2026 
 
## Current Status: EasyPath & SwarmAI WORKING ✅ 
 
## Option 1: EasyPath ← COMPLETE ✅
**Simple A* pathfinding for beginners** 
 
- Price: $35 
- Target: Indie devs, first-time Unity users 
- Complexity: Medium (2-3 weeks) 
- Market gap: A* Pathfinding Project is $140 and complex 
- **Status:** ✅ Ready for Asset Store submission!
 
Features: 
- [x] Grid-based A* pathfinding 
- [x] Visual path debugging in editor (EasyPathDebugWindow) 
- [x] Custom inspectors for Grid and Agent 
- [x] Full source code with assembly definitions 
- [x] Demo scene generation (Basic, Multi-Agent, Stress Test) 
- [x] Click-to-move (all agents), obstacle spawning, multi-agent controls 
- [x] Obstacle layer auto-configuration (Fix Existing Demo Scenes menu)
- [x] Unit tests (PathNode, PriorityQueue, Pathfinding integration)
- [ ] User documentation for Asset Store 
 
## Option 2: SwarmAI ← CURRENT PROJECT
**Multi-agent coordination (like Battlecode rats)** 
 
- Price: $45 
- Target: RTS/colony sim developers 
- Complexity: Medium-High (3-4 weeks) 
- Unique selling point: Built from real competition experience 
- **Status:** ✅ Core features complete! Demos working!
- **Design Doc:** docs/SWARMAI-DESIGN.md
 
Features: 
- [x] Design document complete
- [x] Folder structure created
- [x] Assembly definitions configured
- [x] SwarmManager singleton (with proper cleanup)
- [x] SwarmAgent component
- [x] State machine system (Idle, Moving, Seeking, Fleeing, Gathering, Returning, Following)
- [x] Spatial partitioning (SpatialHash)
- [x] Inter-agent messaging (SwarmMessage with formation/group/resource types)
- [x] Steering behaviors (Seek, Flee, Arrive, Wander, Separation, Alignment, Cohesion, ObstacleAvoidance)
- [x] Formation system (Line, Column, Circle, Wedge, V, Box + FormationSlotBehavior)
- [x] Resource gathering (GatheringState, ReturningState, ResourceNode)
- [x] Group coordination (SwarmGroup, FollowLeaderBehavior)
- [x] Demo scenes (Flocking, Formation, Resource Gathering)
- [x] Documentation (README, GETTING-STARTED, BEHAVIORS, STATES, EXAMPLES, API-REFERENCE, TROUBLESHOOTING)
- [x] Unit tests (behaviors, formations, states, messages)
- [x] Custom editors and debug window
- [ ] Combat Formations demo
- [ ] Marketing materials for Asset Store 
 
## Option 3: NPCBrain ← CURRENT PROJECT
**All-in-one AI toolkit** 
 
- Price: $60 
- Target: Intermediate developers 
- Complexity: High (4-6 weeks) 
- Combines pathfinding + behaviors + sensing + decision making
- **Status:** 🔄 Starting Development!
- **Design Doc:** docs/NPCBRAIN-DESIGN.md (to be created)

Features:
- [ ] Design document
- [ ] Folder structure and assembly definitions
- [ ] Behavior Tree system (Selector, Sequence, Parallel, Decorator nodes)
- [ ] Utility AI system (action scoring, considerations)
- [ ] Perception system (sight, hearing, memory)
- [ ] Knowledge/Blackboard system
- [ ] Integration with EasyPath (pathfinding)
- [ ] Integration with SwarmAI (steering behaviors)
- [ ] Pre-built NPC archetypes (Guard, Patrol, Civilian, Enemy)
- [ ] Visual debugging tools
- [ ] Demo scenes
- [ ] Documentation
- [ ] Unit tests 
 
## Recommendation 
~~Start with EasyPath - lowest complexity, clear market demand.~~ 
 
**UPDATE (January 2026):** Both EasyPath and SwarmAI core features are complete!

### EasyPath Status
- ✅ Pathfinding tested and functional
- ✅ Demo scenes available via EasyPath menu
- ✅ Multi-agent click-to-move support
- ✅ Obstacle layer auto-configuration
- ✅ Unit tests added
- 📋 Ready for Asset Store submission (needs marketing materials)

### SwarmAI Status
- ✅ All steering behaviors working (Seek, Flee, Arrive, Wander, Flocking)
- ✅ Formation system with FormationSlotBehavior for stable formations
- ✅ Resource gathering with GatheringState/ReturningState
- ✅ Demo scenes working (Flocking, Formation, Resource Gathering)
- ✅ Comprehensive documentation
- ✅ Unit tests for all behaviors and components
- 🔄 Combat Formations demo in progress

**Next:** Start NPCBrain development - the all-in-one AI toolkit!
