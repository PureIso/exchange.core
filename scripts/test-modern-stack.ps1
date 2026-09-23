[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
$backendPort = if ($env:BACKEND_ACCEPTANCE_PORT) { $env:BACKEND_ACCEPTANCE_PORT } else { '5000' }
$mlPort = if ($env:ML_ACCEPTANCE_PORT) { $env:ML_ACCEPTANCE_PORT } else { '5005' }
$frontendPort = if ($env:FRONTEND_ACCEPTANCE_PORT) { $env:FRONTEND_ACCEPTANCE_PORT } else { '9080' }
$gatewayPort = if ($env:GATEWAY_ACCEPTANCE_PORT) { $env:GATEWAY_ACCEPTANCE_PORT } else { '8088' }

function Assert-HttpStatus {
    param(
        [Parameter(Mandatory)] [string]$Uri,
        [int]$ExpectedStatus = 200
    )

    $response = Invoke-WebRequest -UseBasicParsing -Uri $Uri -TimeoutSec 15
    if ($response.StatusCode -ne $ExpectedStatus) {
        throw "$Uri returned $($response.StatusCode), expected $ExpectedStatus."
    }
    return $response
}

$backend = Assert-HttpStatus -Uri "http://127.0.0.1:$backendPort/health"
$ml = Assert-HttpStatus -Uri "http://127.0.0.1:$mlPort/api/v1/predict/"
$frontend = Assert-HttpStatus -Uri "http://127.0.0.1:$frontendPort/machine-learning"
$gatewayFrontend = Assert-HttpStatus -Uri "http://127.0.0.1:$gatewayPort/machine-learning"
$gatewayBackend = Assert-HttpStatus -Uri "http://127.0.0.1:$gatewayPort/backend/health"
$gatewayMl = Assert-HttpStatus -Uri "http://127.0.0.1:$gatewayPort/machinelearning/api/v1/predict/"
$runtime = Invoke-RestMethod -Uri "http://127.0.0.1:$gatewayPort/runtime-config.json" -TimeoutSec 15

if ($runtime.hubUrl -ne '/backend/hubs/exchange') {
    throw "Unexpected frontend hub URL: $($runtime.hubUrl)"
}
if ($runtime.mlApiUrl -ne '/machinelearning/api/v1/') {
    throw "Unexpected frontend ML API URL: $($runtime.mlApiUrl)"
}
if ($frontend.Content -notmatch '<app-root>' -or $gatewayFrontend.Content -notmatch '<app-root>') {
    throw 'Frontend SPA fallback did not return the Angular shell.'
}

[pscustomobject]@{
    BackendHealth = $backend.StatusCode
    MachineLearningApi = $ml.StatusCode
    FrontendRoute = $frontend.StatusCode
    GatewayFrontendRoute = $gatewayFrontend.StatusCode
    GatewayBackendHealth = $gatewayBackend.StatusCode
    GatewayMachineLearningApi = $gatewayMl.StatusCode
    HubUrl = $runtime.hubUrl
    MlApiUrl = $runtime.mlApiUrl
}
