# Development Environment Setup 
 
## Current Environment (January 2026) 
- Unity 7 LTS - INSTALLED 
- Visual Studio 2022 - INSTALLED 
- Git 2.45.1 - INSTALLED 
- Unity Hub - INSTALLED 
- Windows 11 
 
## Unity Configuration 
1. Open Unity Hub 
2. Ensure Unity 7 LTS is set as default editor 
3. Configure Visual Studio 2022 as external script editor: 
   - Edit > Preferences > External Tools 
   - Set External Script Editor to Visual Studio 2022 
 
## Visual Studio 2022 Setup 
1. Install \"Game development with Unity\" workload 
2. Enable Unity integration in VS settings 
3. Configure code formatting (optional): 
   - Tools > Options > Text Editor > C# > Code Style 
 
## Git Configuration 
Git LFS is recommended for large binary assets: 
``` 
git lfs install 
git lfs track \"*.psd\" 
git lfs track \"*.fbx\" 
git lfs track \"*.png\" 
``` 
 
## Optional Tools 
- JetBrains Rider - alternative IDE (paid) 
- Unity Profiler - performance analysis 
- Asset Store Tools - for publishing
