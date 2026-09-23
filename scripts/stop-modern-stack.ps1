[CmdletBinding()]
param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.acceptance.yml'
$arguments = @('compose', '--file', $composeFile, 'down')

if ($RemoveVolumes) {
    $arguments += '--volumes'
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Modern acceptance stack failed to stop (exit $LASTEXITCODE)."
}
