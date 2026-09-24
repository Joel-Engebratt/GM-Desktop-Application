[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release'
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($env:OS -ne 'Windows_NT') {
    throw 'This WPF solution requires Windows for verification.'
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$dotnetCommand = Get-Command dotnet -CommandType Application -ErrorAction SilentlyContinue
if ($dotnetCommand) {
    $dotnetPath = $dotnetCommand.Source
} else {
    $dotnetPath = Join-Path $env:ProgramFiles 'dotnet/dotnet.exe'
    if (-not (Test-Path -LiteralPath $dotnetPath)) {
        throw 'Install the .NET SDK specified in global.json and add dotnet to PATH.'
    }
}

function Invoke-Dotnet {
    param([string[]]$Arguments)
    & $dotnetPath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet $($Arguments -join ' ') failed with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    $solution = 'GM Desktop Application/GM Desktop Application.slnx'
    Invoke-Dotnet -Arguments @('--version')
    Invoke-Dotnet -Arguments @('restore', $solution)
    Invoke-Dotnet -Arguments @('build', $solution, '--configuration', $Configuration, '--no-restore')
    Invoke-Dotnet -Arguments @('test', $solution, '--configuration', $Configuration, '--no-build', '--no-restore', '--logger', 'trx', '--results-directory', (Join-Path $repoRoot 'TestResults'))
    Write-Host 'Verification completed. Review test output for actual coverage; perform UI smoke checks separately.'
} finally {
    Pop-Location
}
