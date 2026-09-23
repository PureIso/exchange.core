[CmdletBinding()]
param(
    [switch]$Build
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.acceptance.yml'
$arguments = @('compose', '--file', $composeFile, 'up', '--detach', '--wait')

if ($Build) {
    $arguments += '--build'
}

& docker @arguments
if ($LASTEXITCODE -ne 0) {
    throw "Modern acceptance stack failed to start (exit $LASTEXITCODE)."
}

& docker compose --file $composeFile ps
if ($LASTEXITCODE -ne 0) {
    throw "Modern acceptance stack status failed (exit $LASTEXITCODE)."
}
