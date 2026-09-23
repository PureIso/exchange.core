[CmdletBinding()]
param(
    [switch]$RemoveVolumes
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.observability.acceptance.yml'
$arguments = @('compose', '--file', $composeFile, 'down', '--remove-orphans')

if ($RemoveVolumes) {
    $arguments += '--volumes'
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Observability stack failed to stop (exit $LASTEXITCODE)."
}
