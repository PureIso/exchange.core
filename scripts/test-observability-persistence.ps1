[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$composeFile = Join-Path $repositoryRoot 'docker-compose.observability.acceptance.yml'
$graylogPasswordFile = Join-Path $repositoryRoot '.secrets/observability/graylog-root-password.txt'
$graylogPort = if ($env:GRAYLOG_HTTP_PORT) { $env:GRAYLOG_HTTP_PORT } else { '19000' }
$gelfPort = if ($env:GRAYLOG_GELF_PORT) { [int]$env:GRAYLOG_GELF_PORT } else { 12201 }

function Get-GraylogHeaders {
    $password = (Get-Content -Raw -LiteralPath $graylogPasswordFile).Trim()
    $token = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes("admin:$password"))
    return @{
        Authorization = "Basic $token"
        'X-Requested-By' = 'exchange-core-observability-acceptance'
        Accept = 'application/json'
    }
}

function Wait-ForHealthyStack {
    $deadline = (Get-Date).AddMinutes(3)
    do {
        Start-Sleep -Seconds 5
        $services = & docker compose --file $composeFile ps --format json | ConvertFrom-Json
        if ($LASTEXITCODE -ne 0) {
            throw 'Unable to inspect the observability stack.'
        }
        $healthy = @($services | Where-Object Health -eq 'healthy').Count
    } while ($healthy -lt 3 -and (Get-Date) -lt $deadline)

    if ($healthy -lt 3) {
        throw 'The observability stack did not become healthy after restart.'
    }
}

function Find-AcceptanceEvent {
    param(
        [Parameter(Mandatory)] [hashtable]$Headers,
        [Parameter(Mandatory)] [string]$EventId
    )

    $query = [Uri]::EscapeDataString("acceptance_id:$EventId")
    $uri = "http://127.0.0.1:$graylogPort/api/search/universal/relative?query=$query&range=3600&limit=10&offset=0&sort=timestamp%3Adesc&fields=message%2Cacceptance_id%2Ctimestamp&decorate=true"
    $response = Invoke-WebRequest -UseBasicParsing -Uri $uri -Headers $Headers -Method Get -TimeoutSec 30
    return ($response.Content | ConvertFrom-Json).total_results
}

if (-not (Test-Path -LiteralPath $graylogPasswordFile)) {
    throw 'The ignored Graylog admin password file is missing.'
}

$headers = Get-GraylogHeaders
$inputs = Invoke-RestMethod -Uri "http://127.0.0.1:$graylogPort/api/system/inputs" -Headers $headers -Method Get -TimeoutSec 15
$gelfInputs = @($inputs.inputs | Where-Object type -eq 'org.graylog2.inputs.gelf.udp.GELFUDPInput')
if ($gelfInputs.Count -lt 1) {
    throw 'No GELF UDP input is configured.'
}

$eventId = 'exchange-core-acceptance-' + [Guid]::NewGuid().ToString('N')
$message = @{
    version = '1.1'
    host = 'exchange-core-acceptance'
    short_message = 'Exchange Core observability persistence acceptance'
    timestamp = [DateTimeOffset]::UtcNow.ToUnixTimeMilliseconds() / 1000
    level = 6
    _acceptance_id = $eventId
    _component = 'observability-target'
} | ConvertTo-Json -Compress
$bytes = [Text.Encoding]::UTF8.GetBytes($message)
$udp = [Net.Sockets.UdpClient]::new()
try {
    [void]$udp.Send($bytes, $bytes.Length, '127.0.0.1', $gelfPort)
}
finally {
    $udp.Dispose()
}

$deadline = (Get-Date).AddSeconds(45)
do {
    Start-Sleep -Seconds 3
    $beforeRestart = Find-AcceptanceEvent -Headers $headers -EventId $eventId
} while ($beforeRestart -lt 1 -and (Get-Date) -lt $deadline)
if ($beforeRestart -lt 1) {
    throw 'The synthetic GELF event was not indexed before restart.'
}

& docker compose --file $composeFile restart
if ($LASTEXITCODE -ne 0) {
    throw 'The observability stack restart failed.'
}
Wait-ForHealthyStack

$headers = Get-GraylogHeaders
$inputsAfterRestart = Invoke-RestMethod -Uri "http://127.0.0.1:$graylogPort/api/system/inputs" -Headers $headers -Method Get -TimeoutSec 15
$gelfInputsAfterRestart = @($inputsAfterRestart.inputs | Where-Object type -eq 'org.graylog2.inputs.gelf.udp.GELFUDPInput')
$afterRestart = Find-AcceptanceEvent -Headers $headers -EventId $eventId
if ($gelfInputsAfterRestart.Count -lt 1 -or $afterRestart -lt 1) {
    throw 'The GELF input or synthetic event did not persist across restart.'
}

[pscustomobject]@{
    GelfUdpInputs = $gelfInputsAfterRestart.Count
    EventIndexedBeforeRestart = $beforeRestart -ge 1
    EventFoundAfterRestart = $afterRestart -ge 1
    ServicesHealthyAfterRestart = $true
}
