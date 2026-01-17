# Unity Asset Toolkit 
 
A collection of AI and pathfinding tools for the Unity Asset Store.

**Repository:** https://github.com/quanticsoul4772/unity-asset-toolkit
 
## Project Status 
**NPCBrain - Ready to Build** (January 2026)

- EasyPath ✅ Complete - A* pathfinding working and tested
- SwarmAI ✅ Complete - Multi-agent coordination with Jobs/Burst
- NPCBrain 🔄 Ready to Build - All-in-one AI toolkit (4-week MVP)  
 
## Development Environment 
- Unity 6 (6000.3.4f1) 
- Visual Studio 2022 
- Windows 11 
- Git 2.45.1 + Git LFS 3.5.1
- VS Code (optional, with recommended extensions)
 
## Products 
| Asset | Description | Price | Status |
|-------|-------------|-------|--------|
| EasyPath | Simple A* pathfinding for beginners | $35 | ✅ Complete |
| SwarmAI | Multi-agent coordination system | $45 | ✅ Complete |
| NPCBrain | All-in-one AI toolkit | $60 | 🔄 In Development |
 
## Project Structure 
```
unity-asset-toolkit/
├── assets/           # Unity project files
│   └── EasyPath/     # A* Pathfinding Asset
├── docs/             # Documentation and guides
├── guides/           # Best practices guides
├── notes/            # Planning and research
├── scripts/          # Build and automation tools
├── .github/          # CI/CD workflows
├── .githooks/        # Pre-commit validation
└── .vscode/          # VS Code configuration
```
 
## Getting Started 

### First Time Setup
```powershell
# Clone the repository
git clone https://github.com/quanticsoul4772/unity-asset-toolkit.git
cd unity-asset-toolkit

# Set up Git hooks (run once)
.\scripts\setup-hooks.ps1

# Run preflight checks
.\scripts\preflight.ps1
```

### Open in Unity
1. Open Unity Hub 
2. Add project from `assets/EasyPath` folder 
3. In Unity: **EasyPath → Create Demo Scene → Multi-Agent Demo (5 Agents)**
4. Press **Play** and left-click to move agents!

## Automation Scripts

| Script | Description |
|--------|-------------|
| `scripts/preflight.ps1` | Run all validators before opening Unity |
| `scripts/validate-asmdef.ps1` | Check assembly definitions |
| `scripts/check-deprecated-api.ps1` | Scan for deprecated Unity APIs |
| `scripts/setup-hooks.ps1` | Install Git hooks (run once) |
| `scripts/unity-cli.ps1` | Full CLI for builds, tests, compilation |

## CI/CD Pipeline

GitHub Actions automatically runs on every push:
- **Preflight checks** - Validates asmdef files, checks deprecated APIs
- **Unit tests** - Runs EditMode and PlayMode tests
- **Builds** - Creates Windows and WebGL builds (main branch only)

## Contributing

Pre-commit hooks validate:
- Missing/orphan Unity meta files
- Large files not tracked by Git LFS
- Deprecated API usage

## License

Proprietary - Unity Asset Store
