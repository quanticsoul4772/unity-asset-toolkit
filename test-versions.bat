@echo off
REM SwarmAI Multi-Version Testing Batch File
REM Run from project root: test-versions.bat

echo.
echo ========================================
echo   SwarmAI Multi-Version Testing
echo ========================================
echo.
echo IMPORTANT: Close Unity Editor before running!
echo.
pause

REM Run deprecated API check first
echo.
echo [1/3] Checking for deprecated APIs...
echo ----------------------------------------
powershell -ExecutionPolicy Bypass -File scripts\check-deprecated-api.ps1

echo.
echo [2/3] Checking current compilation status...
echo ----------------------------------------
powershell -ExecutionPolicy Bypass -File scripts\check-compile.ps1

echo.
echo [3/3] Running multi-version tests...
echo ----------------------------------------
powershell -ExecutionPolicy Bypass -File scripts\test-unity-versions.ps1

echo.
echo ========================================
echo   Testing Complete!
echo ========================================
echo.
echo Results saved to: test-report.md
echo.
echo Next steps:
echo   1. Review test-report.md for results
echo   2. Install any missing Unity versions
echo   3. Manually test demos in each version
echo   4. Run unit tests via Test Runner
echo.
pause
