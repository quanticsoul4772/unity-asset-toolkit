# Development Environment Setup 
 
## Current Environment (January 2026) 
- Unity 6 (6000.3.4f1) - INSTALLED ✅
- Visual Studio 2022 - INSTALLED ✅
- Git 2.45.1 - INSTALLED ✅
- Git LFS 3.5.1 - INSTALLED ✅
- Unity Hub - INSTALLED ✅
- Windows 11 

## Quick Start

```powershell
# Clone repository
git clone https://github.com/quanticsoul4772/unity-asset-toolkit.git
cd unity-asset-toolkit

# One-time setup (Git hooks + LFS)
.\scripts\setup-hooks.ps1

# Pre-development checks
.\scripts\preflight.ps1
```
 
## Unity Configuration 
1. Open Unity Hub 
2. Ensure Unity 6 LTS is set as default editor 
3. Add project from `assets/EasyPath` folder
4. Configure Visual Studio 2022 as external script editor: 
   - Edit > Preferences > External Tools 
   - Set External Script Editor to Visual Studio 2022 
 
## Visual Studio 2022 Setup 
1. Install "Game development with Unity" workload 
2. Enable Unity integration in VS settings 
3. Configure code formatting (optional): 
   - Tools > Options > Text Editor > C# > Code Style 

## VS Code Setup (Alternative)

Recommended extensions are auto-suggested via `.vscode/extensions.json`:
- C# Dev Kit
- Unity Tools
- GitLens
- PowerShell
- EditorConfig

Project settings in `.vscode/settings.json` and `.editorconfig`.
 
## Git Configuration 

### Git LFS (Already Configured)
Large binary files are tracked via `.gitattributes`:
- Textures: `.png`, `.jpg`, `.psd`, `.tga`, `.tif`, `.exr`, `.hdr`
- Audio: `.wav`, `.mp3`, `.ogg`, `.aif`
- Models: `.fbx`, `.obj`, `.blend`, `.max`, `.mb`
- Unity: `.unity`, `.prefab`, `.asset`, `.cubemap`

### Pre-commit Hooks (Run setup-hooks.ps1 once)
Validates before each commit:
- Missing Unity meta files
- Orphan meta files (no matching asset)
- Large files not tracked by LFS

## CI/CD Pipeline

GitHub Actions workflow at `.github/workflows/unity-ci.yml`:

| Job | Trigger | Description |
|-----|---------|-------------|
| preflight | All pushes | Validates asmdef files, checks deprecated APIs |
| test | All pushes | Runs EditMode and PlayMode tests |
| build-windows | Main branch | Creates Windows standalone build |
| build-webgl | Main branch | Creates WebGL build |

### GitHub Secrets (✅ Configured)
- `UNITY_LICENSE` - Unity license file content
- `UNITY_EMAIL` - Unity account email  
- `UNITY_PASSWORD` - Unity account password

See: https://game.ci/docs/github/activation

## Automation Scripts

| Script | Description |
|--------|-------------|
| `scripts/setup-hooks.ps1` | One-time Git hooks + LFS setup |
| `scripts/preflight.ps1` | Run all validators before opening Unity |
| `scripts/validate-asmdef.ps1` | Check assembly definitions for GUID refs |
| `scripts/check-deprecated-api.ps1` | Scan for deprecated Unity 6 APIs |
| `scripts/check-compile.ps1` | Check compilation status |
| `scripts/read-unity-log.ps1` | Read Unity Editor.log |
| `scripts/unity-cli.ps1` | Full CLI for builds, tests, compilation |
 
## Optional Tools 
- JetBrains Rider - alternative IDE (paid) 
- Unity Profiler - performance analysis 
- Asset Store Tools - for publishing

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Unity enters Safe Mode | Check console for errors, run `preflight.ps1` |
| Scripts not compiling | Validate asmdef references: `validate-asmdef.ps1` |
| Menu items missing | Assets → Reimport All or restart Unity |
| CI/CD fails | Check GitHub Actions logs, verify secrets |
