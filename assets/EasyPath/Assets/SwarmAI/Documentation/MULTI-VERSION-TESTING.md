# SwarmAI Multi-Version Testing Guide

This document provides a checklist for testing SwarmAI across different Unity versions before Asset Store submission.

## Supported Unity Versions

| Version | Support Level | Status |
|---------|--------------|--------|
| Unity 2021.3 LTS | Minimum Supported | ⬜ Not Tested |
| Unity 2022.3 LTS | Recommended | ⬜ Not Tested |
| Unity 6 (6000.x) | Latest | ✅ Tested |

---

## Prerequisites

### Install Required Unity Versions

1. Open **Unity Hub**
2. Go to **Installs** tab
3. Click **Install Editor**
4. Select these versions:
   - Unity 2021.3.x (LTS) - for maximum compatibility
   - Unity 2022.3.x (LTS) - current LTS
   - Unity 6000.x - latest release
5. Ensure **Windows Build Support** is included

### Prepare Test Environment

- [ ] Close all Unity Editor instances
- [ ] Backup the project (or ensure Git is clean)
- [ ] Delete `Library` folder to test fresh import (optional but recommended)

---

## Testing Checklist Per Unity Version

Repeat this checklist for each Unity version:

### 1. Project Import

- [ ] Open project in Unity Hub with target version
- [ ] Wait for full import to complete
- [ ] Check Console for compilation errors
- [ ] Verify no missing script references

### 2. Assembly Verification

Check that these assemblies compile without errors:

- [ ] `SwarmAI.Runtime.dll` - Core runtime scripts
- [ ] `SwarmAI.Editor.dll` - Editor extensions
- [ ] `SwarmAI.Demo.dll` - Demo scene scripts
- [ ] `SwarmAI.Tests.Editor.dll` - Editor unit tests
- [ ] `SwarmAI.Tests.Runtime.dll` - Runtime unit tests

### 3. Demo Scene Testing

Create and test each demo scene:

#### Flocking Demo
- [ ] Menu: **SwarmAI > Create Demo Scene > Flocking Demo (30 Agents)**
- [ ] Press Play
- [ ] Verify: Agents flock together
- [ ] Test: Click to set target position
- [ ] Test: Number keys 1-5 toggle behaviors
- [ ] Test: Space to scatter, G to gather

#### Formation Demo
- [ ] Menu: **SwarmAI > Create Demo Scene > Formation Demo (10 Agents)**
- [ ] Press Play
- [ ] Verify: Leader (yellow) and followers visible
- [ ] Test: WASD moves leader
- [ ] Test: Number keys 1-6 change formations
- [ ] Test: Click to set formation destination

#### Resource Gathering Demo
- [ ] Menu: **SwarmAI > Create Demo Scene > Resource Gathering Demo (15 Agents)**
- [ ] Press Play
- [ ] Verify: Agents gather from green resource nodes
- [ ] Verify: Agents return to blue base when carrying resources
- [ ] Test: G sends all to gather, H sends all home
- [ ] Test: Click resource to assign nearest worker

#### Combat Formations Demo
- [ ] Menu: **SwarmAI > Create Demo Scene > Combat Formations Demo (10 Agents)**
- [ ] Press Play
- [ ] Verify: Two teams visible (blue and red)
- [ ] Test: Space to switch selected team
- [ ] Test: G for attack, R for retreat, H for hold
- [ ] Test: Number keys 1-6 change formations

### 4. Editor Tools Testing

- [ ] Select a SwarmAgent - custom inspector displays
- [ ] Select SwarmManager - shows agent list
- [ ] Open **Window > SwarmAI > Debug Window**
- [ ] Verify all tabs work (Agents, Visualization, Commands, Stats)
- [ ] Test **SwarmAI > Validate Package** menu item

### 5. Unit Tests

- [ ] Open **Window > General > Test Runner**
- [ ] Run **EditMode** tests - all should pass
- [ ] Run **PlayMode** tests - all should pass
- [ ] Note any failures with Unity version

### 6. Build Testing

- [ ] Add demo scenes to Build Settings (**SwarmAI > Add Demo Scenes to Build Settings**)
- [ ] Build for Windows Standalone (**File > Build Settings > Build**)
- [ ] Run the built executable
- [ ] Verify all demos work in built player
- [ ] Test basic controls in each demo

---

## Version-Specific Notes

### Unity 2021.3 LTS

**Known Considerations:**
- Uses older `FindObjectOfType<T>()` API (still works, just deprecated)
- Input System may need manual installation
- Some EditorGUI APIs may have slight differences

**Expected Warnings:**
- Deprecation warnings for `FindObjectOfType` are acceptable
- These don't affect functionality

### Unity 2022.3 LTS

**Known Considerations:**
- `FindObjectOfType` shows deprecation warnings
- SwarmAI uses `FindFirstObjectByType` with preprocessor fallback
- Should compile without issues

### Unity 6 (6000.x)

**Known Considerations:**
- New Input System may be default
- Demo scripts support both legacy and new Input System
- Render pipeline may differ (materials adjust automatically)

---

## Automated Testing

### Running the Test Script

```bash
# From project root (close Unity first!)
powershell -ExecutionPolicy Bypass -File scripts/test-unity-versions.ps1

# Or use the batch file
test-versions.bat
```

### What the Script Checks

1. **Finds installed Unity versions** in common locations
2. **Opens project in batch mode** for each version
3. **Checks compilation** for errors
4. **Generates report** at `test-report.md`

### Interpreting Results

| Result | Meaning |
|--------|--------|
| **Pass** | Compiles without errors |
| **Fail** | Compilation errors found |
| **Not Tested** | Unity version not installed |
| **Error** | Script couldn't run Unity |

---

## Troubleshooting

### "Another Unity instance is running"

**Solution:** Close all Unity Editor windows before running the test script.

### Compilation Errors

**Check:**
1. Console window for specific error messages
2. Assembly definition files (`.asmdef`) for missing references
3. Missing packages in Package Manager

### Missing Script References

**Solution:**
1. Delete `Library` folder
2. Reimport project
3. Recreate demo scenes

### Materials Look Pink

**Cause:** Shader not compatible with render pipeline.

**Solution:** SwarmAI auto-detects and uses appropriate shaders. If pink, check:
- URP: Uses `Universal Render Pipeline/Lit`
- HDRP: Uses `HDRP/Lit`
- Built-in: Uses `Standard`

---

## Results Log

Record your testing results here:

### Unity 2021.3 LTS
- **Date Tested:** _____________
- **Version:** 2021.3.___
- **Compilation:** ⬜ Pass / ⬜ Fail
- **Demos Work:** ⬜ Yes / ⬜ Partial / ⬜ No
- **Tests Pass:** ⬜ Yes / ⬜ Partial / ⬜ No
- **Build Works:** ⬜ Yes / ⬜ No
- **Notes:** _____________

### Unity 2022.3 LTS
- **Date Tested:** _____________
- **Version:** 2022.3.___
- **Compilation:** ⬜ Pass / ⬜ Fail
- **Demos Work:** ⬜ Yes / ⬜ Partial / ⬜ No
- **Tests Pass:** ⬜ Yes / ⬜ Partial / ⬜ No
- **Build Works:** ⬜ Yes / ⬜ No
- **Notes:** _____________

### Unity 6
- **Date Tested:** January 2026
- **Version:** 6000.3.4f1
- **Compilation:** ✅ Pass
- **Demos Work:** ✅ Yes
- **Tests Pass:** ✅ Yes
- **Build Works:** ⬜ Not Tested
- **Notes:** Primary development version, all features verified

---

## Submission Readiness

Before Asset Store submission, ensure:

- [ ] Tested on at least 2 Unity LTS versions
- [ ] All demos work without errors
- [ ] All unit tests pass
- [ ] Standalone build works
- [ ] No critical console warnings
- [ ] Documentation is accurate
- [ ] Package validation passes
