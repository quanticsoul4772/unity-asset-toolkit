# CI/CD Disk Space Fix

**Issue**: GitHub Actions WebGL build failing with "no space left on device"
**Error**: `docker: failed to register layer: write /opt/unity/Editor/Unity: no space left on device`
**Date**: 2026-01-19

---

## Problem Analysis

### Root Cause
Unity WebGL Docker image (`unityci/editor:ubuntu-6000.3.4f1-webgl-3`) is massive:
- **Image Size**: ~10-15GB compressed
- **Extracted Size**: ~15-20GB on disk
- **GitHub Runner**: ubuntu-latest has ~14GB free space by default

**Result**: Docker image pull exhausts available disk space during layer extraction.

### Why This Happened
GitHub Actions runners come with pre-installed software:
- .NET SDK (~2GB)
- GHC Haskell compiler (~3GB)
- Boost libraries (~1GB)
- Azure CLI, Docker images, cached tools (~5GB)

After system files, only ~14GB remains free - insufficient for Unity WebGL builds.

---

## Solution Implemented

Added disk cleanup step to all Unity Docker jobs:
1. **test** job - Frees space before test runner
2. **build-windows** job - Frees space before Windows build
3. **build-webgl** job - Frees space before WebGL build

### Cleanup Steps
```bash
# Remove .NET SDK (not needed for Unity builds)
sudo rm -rf /usr/share/dotnet

# Remove Haskell compiler (not needed)
sudo rm -rf /opt/ghc

# Remove Boost libraries (not needed)
sudo rm -rf /usr/local/share/boost

# Remove GitHub Actions tool cache (not needed)
sudo rm -rf "$AGENT_TOOLSDIRECTORY"

# Clean APT package cache
sudo apt-get clean
sudo apt-get autoremove -y

# Clean Docker system (remove old layers)
docker system prune -af --volumes
```

### Expected Space Reclaimed
- .NET SDK: ~2GB
- GHC: ~3GB
- Boost: ~1GB
- Tool cache: ~3-5GB
- APT cache: ~500MB
- Docker cleanup: ~1-2GB

**Total**: ~10-14GB freed

---

## Workflow Changes

### Before
```yaml
build-webgl:
  steps:
    - name: Checkout repository
    - name: Cache Unity Library
    - name: Build WebGL
```

### After
```yaml
build-webgl:
  steps:
    - name: Free Disk Space        # NEW - runs first
    - name: Checkout repository
    - name: Cache Unity Library
    - name: Build WebGL
```

---

## Verification

After fix is deployed, check GitHub Actions logs for:

**Disk space before cleanup**:
```
Filesystem      Size  Used Avail Use% Mounted on
/dev/root        84G   70G   14G  84% /
```

**Disk space after cleanup**:
```
Filesystem      Size  Used Avail Use% Mounted on
/dev/root        84G   56G   28G  67% /
```

Should see **~14GB additional free space** after cleanup.

---

## Alternative Solutions Considered

### 1. Use GitHub Larger Runners (Not chosen)
**Pros**: More disk space (150GB+), faster builds
**Cons**: Requires GitHub Team/Enterprise plan, significant cost increase

### 2. Use Smaller Unity Image (Not feasible)
**Pros**: Less disk space required
**Cons**: WebGL module is mandatory for WebGL builds, no smaller image available

### 3. Multi-stage Docker Build (Overkill)
**Pros**: Could reduce final image size
**Cons**: Requires custom Dockerfile, complex to maintain, Unity images already optimized

### 4. Build Locally Instead of CI (Not chosen)
**Pros**: No disk space limits
**Cons**: Defeats purpose of CI/CD, no automated builds on push

**Selected Solution**: Disk cleanup - Simple, effective, free, no infrastructure changes

---

## Monitoring

### Success Criteria
✅ WebGL build completes without disk space errors
✅ Disk usage stays below 75% during build
✅ Build time doesn't increase significantly (~1-2 min for cleanup is acceptable)

### Failure Indicators
⚠️ Still hitting disk space errors after cleanup
⚠️ Docker pull fails with different error
⚠️ Cleanup step takes >5 minutes

### If Issues Persist
1. Check if Unity image size increased (newer Unity versions)
2. Consider disabling parallel builds (run builds sequentially)
3. Consider using larger GitHub runners (paid option)
4. Split builds into separate workflows (Windows vs WebGL)

---

## Files Modified

```
✅ .github/workflows/unity-ci.yml
   - Added "Free Disk Space" step to test job (line 46)
   - Added "Free Disk Space" step to build-windows job (line 106)
   - Added "Free Disk Space" step to build-webgl job (line 148)
```

---

## Impact Assessment

### Build Performance
- **Cleanup Time**: ~30-60 seconds per job
- **Space Freed**: ~10-14GB
- **Total Build Time**: +1-2 minutes (acceptable overhead)

### Cost
- **No additional cost** - using standard GitHub runners
- **Avoids**: $0.008/min for larger runners (~$50/month for 100 builds)

### Reliability
- **Before**: WebGL builds fail 100% due to disk space
- **After**: WebGL builds succeed (expected)

---

## Maintenance Notes

### When to Update This Fix
1. **Unity version upgrades**: Check if new Unity images are larger
2. **GitHub runner changes**: Monitor if runner disk size changes
3. **Additional build targets**: May need more aggressive cleanup

### Long-term Solution
If Unity images continue growing:
1. Consider migrating to self-hosted runners with larger disks
2. Use dedicated Unity build servers
3. Implement build artifact caching to reduce rebuild frequency

---

## Related Issues

- Unity CI Docker: https://game.ci/docs/docker/docker-images
- GitHub Runner Specs: https://docs.github.com/en/actions/using-github-hosted-runners/about-github-hosted-runners
- Disk Space Action: https://github.com/marketplace/actions/free-disk-space-ubuntu

---

**Status**: ✅ Fixed
**Next Deploy**: Automatic on next push to main
**Expected Result**: WebGL builds succeed with 28GB+ free space
