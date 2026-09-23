[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.observability.acceptance.yml'
$secretFile = Join-Path $repositoryRoot '.secrets/observability/graylog-password-secret.txt'

if (-not (Test-Path -LiteralPath $secretFile)) {
    throw 'Observability secrets are missing. Run ./scripts/initialize-observability-secrets.ps1 first.'
}

& docker compose --file $composeFile up --detach
if ($LASTEXITCODE -ne 0) {
    throw "Observability stack failed to start (exit $LASTEXITCODE)."
}

& docker compose --file $composeFile ps
if ($LASTEXITCODE -ne 0) {
    throw "Observability stack status failed (exit $LASTEXITCODE)."
}

Write-Output 'Complete Graylog Data Node certificate preflight at http://127.0.0.1:19000/ before configuring inputs.'
