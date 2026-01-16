# Quick Commands for Unity Development
# Usage: .\quick.ps1 <command>
#
# Commands:
#   info     - Show environment status
#   compile  - Compile and check for errors
#   test     - Run unit tests
#   build    - Build the project
#   validate - Validate environment setup

param(
    [Parameter(Position=0)]
    [ValidateSet('info', 'compile', 'test', 'build', 'validate', 'help')]
    [string]$Command = 'help'
)

$scriptPath = Join-Path $PSScriptRoot "unity-cli.ps1"

switch ($Command) {
    'info'     { & $scriptPath -Action info }
    'compile'  { & $scriptPath -Action compile }
    'test'     { & $scriptPath -Action test }
    'build'    { & $scriptPath -Action build }
    'validate' { & $scriptPath -Action validate }
    'help' {
        Write-Host @"

Unity Quick Commands
====================

  .\quick.ps1 info      - Show environment status
  .\quick.ps1 compile   - Compile and check for errors  
  .\quick.ps1 test      - Run unit tests
  .\quick.ps1 build     - Build the project
  .\quick.ps1 validate  - Validate environment setup

For advanced options, use unity-cli.ps1 directly:
  .\unity-cli.ps1 -Action build -BuildTarget WebGL -ProjectPath "path\to\project"

"@
    }
}
