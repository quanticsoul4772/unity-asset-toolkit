# Changelog

All notable changes to SwarmAI will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - <RELEASE_DATE>

<!-- NOTE: Replace <RELEASE_DATE> with actual release date in YYYY-MM-DD format before submission -->

### Added

#### Core Systems
- SwarmManager singleton with agent registry and spatial partitioning
- SwarmAgent component with finite state machine and behavior system
- SpatialHash for O(1) neighbor queries supporting 100+ agents at 60 FPS
- SwarmSettings ScriptableObject for centralized configuration
- Inter-agent messaging system with type-safe message passing

#### Steering Behaviors (9 behaviors)
- SeekBehavior - Move toward target position
- FleeBehavior - Move away from threats
- ArriveBehavior - Seek with smooth deceleration
- WanderBehavior - Random smooth movement for idle states
- ObstacleAvoidanceBehavior - Raycast-based obstacle steering
- SeparationBehavior - Avoid crowding neighbors
- AlignmentBehavior - Match neighbor velocity
- CohesionBehavior - Move toward group center
- FollowLeaderBehavior - Smooth leader-follower patterns

#### Agent States (8 states)
- IdleState - Default resting state
- MovingState - Direct movement to position
- SeekingState - Active pursuit of target
- FleeingState - Escape from threats
- GatheringState - Resource collection with stuck detection
- ReturningState - Return to base with resources
- FollowingState - Follow leader or formation slot
- Custom state support via AgentState base class

#### Formation System
- SwarmFormation component with 7 formation types
- Line, Column, Circle, Wedge, V, Box, Custom formations
- FormationSlot assignment and tracking
- Leader-based formation movement
- SwarmGroup for coordinating multiple agents

#### Resource Gathering System
- ResourceNode component for harvestable resources
- Configurable harvest rate, capacity, and respawn
- Worker tracking with harvester limits
- Visual feedback with scale and color changes

#### Editor Tools
- SwarmAgentEditor - Custom inspector with runtime info and scene handles
- SwarmManagerEditor - Custom inspector with agent list and commands
- SwarmFormationEditor - Custom inspector with formation preview
- ResourceNodeEditor - Custom inspector with progress bar and status
- SwarmSettingsEditor - Organized foldout sections for all settings
- SwarmDebugWindow - EditorWindow with agents, visualization, commands, stats tabs
- SwarmAIDemoSceneSetup - Menu items to create demo scenes

#### Demo Scenes
- FlockingDemo - Separation, Alignment, Cohesion, Wander, Obstacle Avoidance
- FormationDemo - All formation types with leader control
- ResourceGatheringDemo - Complete gather-return-deposit workflow

#### Documentation
- README.md - Overview, features, installation, quick start
- GETTING-STARTED.md - Step-by-step tutorials
- API-REFERENCE.md - Complete class and method documentation
- BEHAVIORS.md - Steering behavior guide with examples
- STATES.md - Agent state documentation with transitions
- EXAMPLES.md - Code samples for common use cases

### Technical Details
- Unity 2021.3+ LTS compatibility
- Assembly definitions for clean compilation
- Full source code included
- XML documentation on all public APIs
- Edit-mode and Play-mode tests

---

## [Unreleased]

### Planned
- Performance profiler integration
- Jobs/Burst optimization option
- NavMesh integration
- Additional formation types
- Combat behavior examples
