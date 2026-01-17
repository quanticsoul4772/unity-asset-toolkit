@echo off
echo ============================================
echo SwarmAI Multi-Version Testing
echo ============================================
echo.
echo NOTE: Please close Unity Editor before running!
echo.
echo Running tests...
echo.
powershell -ExecutionPolicy Bypass -File "%~dp0test-unity-versions.ps1"
echo.
echo Press any key to exit...
pause >nul
