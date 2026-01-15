# Project Knowledge - Unity Asset Toolkit 
 
Unity Asset Store project for AI/pathfinding tools. C#, Unity 7 LTS, Visual Studio 2022, Windows 11. 
 
## Quickstart 
 
```bash 
# Open Unity Hub and add project from assets/ folder 
# Open in Visual Studio 2022 
# Build: Ctrl+Shift+B 
# Play: Click Play button in Unity Editor 
``` 
 
## Project Status 
 
**Phase:** Planning/Setup (January 2026) 
**Next Step:** Create first Unity project and choose which asset to build 
 
## Development Environment 
 
- **Unity 7 LTS** - Installed via Unity Hub 
- **Visual Studio 2022** - Primary IDE 
- **Git 2.45.1** - Version control 
- **Windows 11** - OS 
 
## Architecture 
 
``` 
unity-asset-toolkit/ 
+-- assets/           # Unity project(s) go here 
+-- docs/             # Setup guides, checklists 
+-- notes/            # Planning, research, learnings 
+-- prototypes/       # Quick C# experiments 
+-- scripts/          # Build and automation tools 
+-- knowledge.md      # This file (Codebuff context) 
+-- README.md         # Project overview 
``` 
 
## Potential Products 
 
| Asset | Description | Price | Complexity | 
|-------|-------------|-------|------------| 
| EasyPath | Simple A* pathfinding for beginners | $35 | 2-3 weeks | 
| SwarmAI | Multi-agent coordination system | $45 | 3-4 weeks | 
| NPCBrain | All-in-one AI toolkit | $60 | 4-6 weeks | 
 
**Recommendation:** Start with EasyPath - lowest complexity, clear market demand. 
 
## Conventions 
 
- **C# naming:** PascalCase (classes, methods), camelCase (local variables) 
- **Unity 7 LTS** target version 
- **Visual Studio 2022** as IDE 
- **Asset Store submission guidelines** must be followed 
- **Full source code** included in all assets 
- **Demo scenes** required for each feature 
 
## Skills from Battlecode 2026 
 
This project leverages skills learned from MIT Battlecode 2026: 
- State machine design for AI agents 
- A* and Bug2 pathfinding algorithms 
- Multi-agent coordination 
- Resource/economy management 
- Performance optimization 
- Debug and logging systems 
 
## Key Patterns to Implement 
 
From Battlecode experience: 
- tryMove() helper - prefer efficient movement 
- State enums (IDLE, MOVING, ATTACKING, etc.) 
- Distance-based decision making 
- Threat avoidance algorithms 
- ID-based agent differentiation 
 
## Key Links 
 
- Unity Asset Store: https://assetstore.unity.com 
- Publisher Portal: https://publisher.unity.com 
- Unity Docs: https://docs.unity3d.com 
- C# Docs: https://docs.microsoft.com/en-us/dotnet/csharp/ 
 
## File Descriptions 
 
- **docs/SETUP.md** - Development environment setup guide 
- **docs/CHECKLIST.md** - Environment setup checklist with status 
- **notes/project-ideas.md** - Detailed comparison of asset options 
- **notes/battlecode-learnings.md** - Skills and patterns from competition
