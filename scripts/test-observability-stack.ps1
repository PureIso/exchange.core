[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.observability.acceptance.yml'
$graylogPasswordFile = Join-Path $repositoryRoot '.secrets/observability/graylog-root-password.txt'
$graylogPort = if ($env:GRAYLOG_HTTP_PORT) { $env:GRAYLOG_HTTP_PORT } else { '19000' }
$datanodePort = if ($env:GRAYLOG_DATANODE_PORT) { $env:GRAYLOG_DATANODE_PORT } else { '18999' }

function Get-HttpStatus {
    param([Parameter(Mandatory)] [string]$Uri)

    $lastError = $null
    foreach ($attempt in 1..5) {
        try {
            return (Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 15).StatusCode
        }
        catch {
            if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
                return [int]$_.Exception.Response.StatusCode
            }
            $lastError = $_
            if ($attempt -lt 5) {
                Start-Sleep -Seconds 2
            }
        }
    }
    throw $lastError
}

function Get-LocalHttpsStatus {
    param([Parameter(Mandatory)] [string]$Uri)

    $status = & curl.exe --insecure --silent --output NUL --write-out '%{http_code}' $Uri
    if ($LASTEXITCODE -ne 0 -or -not $status) {
        throw "Unable to query the local TLS endpoint: $Uri"
    }
    return [int]$status
}

$services = & docker compose --file $composeFile ps --format json | ConvertFrom-Json
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to inspect the observability stack.'
}

$mongo = $services | Where-Object Service -eq 'mongodb'
if (-not $mongo -or $mongo.Health -ne 'healthy') {
    throw "MongoDB is not healthy. Current state: $($mongo.State)/$($mongo.Health)"
}

$graylogStatus = Get-HttpStatus -Uri "http://127.0.0.1:$graylogPort/"
$datanodeStatus = Get-LocalHttpsStatus -Uri "https://127.0.0.1:$datanodePort/"

if ($graylogStatus -notin 200, 401) {
    throw "Unexpected Graylog status: $graylogStatus"
}
if ($datanodeStatus -notin 200, 401) {
    throw "Unexpected Data Node status: $datanodeStatus"
}

$authenticationBoundary = & docker compose --file $composeFile exec -T mongodb /usr/local/bin/verify-auth-boundary
if ($LASTEXITCODE -ne 0 -or $authenticationBoundary.Trim() -ne 'graylog-authenticated;anonymous-denied') {
    throw 'The MongoDB authentication boundary did not pass.'
}

if (-not (Test-Path -LiteralPath $graylogPasswordFile)) {
    throw 'The ignored Graylog admin password file is missing.'
}

$graylogPassword = (Get-Content -Raw -LiteralPath $graylogPasswordFile).Trim()
$basicToken = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:$graylogPassword"))
$graylogHeaders = @{
    Authorization = "Basic $basicToken"
    'X-Requested-By' = 'exchange-core-observability-acceptance'
}
$configuredInputs = Invoke-RestMethod -Uri "http://127.0.0.1:$graylogPort/api/system/inputs" -Headers $graylogHeaders -Method Get -TimeoutSec 15
$gelfInputs = @($configuredInputs.inputs | Where-Object type -eq 'org.graylog2.inputs.gelf.udp.GELFUDPInput')
if ($gelfInputs.Count -lt 1) {
    throw 'No GELF UDP input is configured.'
}

[pscustomobject]@{
    MongoHealth = $mongo.Health
    GraylogHttp = $graylogStatus
    DataNodeHttp = $datanodeStatus
    DataNodeScheme = 'https'
    MongoGraylogUser = 'graylog'
    AnonymousMongoAccess = 'denied'
    PreflightRequired = $false
    GelfUdpInputs = $gelfInputs.Count
}
