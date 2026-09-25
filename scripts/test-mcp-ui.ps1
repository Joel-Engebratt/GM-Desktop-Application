[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$previousOptIn = $env:GM_RUN_MCP_UI_TESTS
$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
$dotnetPath = if ($dotnetCommand) { $dotnetCommand.Source } else { Join-Path $env:ProgramFiles 'dotnet/dotnet.exe' }
try {
    $env:GM_RUN_MCP_UI_TESTS = '1'
    & $dotnetPath test (Join-Path $repoRoot 'tests/GM.Development.Mcp.Tests/GM.Development.Mcp.Tests.csproj') --configuration $Configuration --no-build --no-restore --filter 'TestCategory=DesktopMcp' --logger 'console;verbosity=normal'
    if ($LASTEXITCODE -ne 0) { throw 'MCP desktop smoke test failed.' }
} finally {
    $env:GM_RUN_MCP_UI_TESTS = $previousOptIn
}
