[CmdletBinding()]
param(
    [switch]$RunDocker,
    [switch]$RunBrowser,
    [switch]$RunSandboxContract,
    [string]$ProductId = 'BTC-USD',
    [ValidateRange(5, 350)]
    [int]$CandleCount = 20,
    [string]$OutputDirectory = '.local-acceptance'
)

$ErrorActionPreference = 'Stop'
$ComposeFile = Join-Path $PSScriptRoot '..\docker-compose.acceptance.yml' | Resolve-Path
$Compose = Get-Command docker-compose.exe -ErrorAction SilentlyContinue

if ($RunBrowser -and -not $RunDocker) {
    throw '-RunBrowser requires -RunDocker because the browser probe consumes the disposable acceptance stack.'
}

if ($ProductId -notmatch '^[A-Za-z0-9]+-[A-Za-z0-9]+$') {
    throw "ProductId '$ProductId' must contain two alphanumeric symbols separated by a hyphen."
}

$ProductSlug = $ProductId.ToLowerInvariant()
$IndicatorFileName = "coinbase-$ProductSlug.csv"
$IndicatorDirectoryName = $IndicatorFileName.Replace('.csv', '').Replace('-', '_')
$NormalizerFileName = "coinbase-${ProductSlug}_normalizer.json"
$ModelFileName = "coinbase-${ProductSlug}_model.h5"

function Invoke-Compose {
    param([string[]]$Arguments)
    if ($null -eq $Compose) {
        throw 'docker-compose.exe is required for the Docker acceptance gate.'
    }
    & $Compose.Source '-f' $ComposeFile.Path @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose failed with exit code $LASTEXITCODE."
    }
}

function Assert-JsonShape {
    param(
        [object]$Value,
        [string[]]$RequiredProperties,
        [string]$Name
    )
    foreach ($Property in $RequiredProperties) {
        if ($null -eq $Value.PSObject.Properties[$Property]) {
            throw "$Name is missing required property '$Property'."
        }
    }
}

if (-not $RunDocker -and -not $RunSandboxContract) {
    Write-Host 'Local acceptance is opt-in. Re-run with -RunDocker and/or -RunSandboxContract.'
    exit 0
}

if ($RunSandboxContract) {
    $FixtureDirectory = Join-Path $PSScriptRoot '..\acceptance\fixtures\sandbox'
    $Accounts = Get-Content (Join-Path $FixtureDirectory 'accounts-response.json') -Raw | ConvertFrom-Json
    $Orders = Get-Content (Join-Path $FixtureDirectory 'orders-response.json') -Raw | ConvertFrom-Json
    Assert-JsonShape $Accounts @('accounts') 'Accounts sandbox contract fixture'
    Assert-JsonShape $Orders @('orders') 'Orders sandbox contract fixture'
    if ($Accounts.accounts.Count -gt 0) {
        Assert-JsonShape $Accounts.accounts[0] @('uuid', 'currency', 'available_balance') 'Accounts item'
    }
    if ($Orders.orders.Count -gt 0) {
        Assert-JsonShape $Orders.orders[0] @('order_id', 'product_id', 'status') 'Orders item'
    }
    Write-Host 'Sandbox Accounts/Orders fixtures satisfy the documented schema-only gate.'
    $SandboxBaseUri = 'https://api-sandbox.coinbase.com/api/v3/brokerage'
    foreach ($Endpoint in @('/accounts', '/orders/historical/batch')) {
        try {
            $SandboxResponse = Invoke-WebRequest -UseBasicParsing -Uri "$SandboxBaseUri$Endpoint" -Method Get -TimeoutSec 20
        }
        catch {
            $HttpResponse = $_.Exception.Response
            if ($null -eq $HttpResponse) {
                throw "Sandbox $Endpoint was unavailable; the live schema gate did not complete: $($_.Exception.Message)"
            }
            $SandboxResponse = [PSCustomObject]@{
                StatusCode = [int]$HttpResponse.StatusCode
                Content = ''
            }
        }
        if ($SandboxResponse.StatusCode -in @(401, 403)) {
            throw "Sandbox $Endpoint rejected the unauthenticated schema check with HTTP $($SandboxResponse.StatusCode)."
        }
        if ($SandboxResponse.StatusCode -ne 200) {
            throw "Sandbox $Endpoint returned unexpected HTTP $($SandboxResponse.StatusCode)."
        }
            $SandboxPayload = $SandboxResponse.Content | ConvertFrom-Json
            $RequiredProperty = if ($Endpoint -eq '/accounts') { 'accounts' } else { 'orders' }
            Assert-JsonShape $SandboxPayload @($RequiredProperty) "Sandbox $Endpoint response"
            $SandboxItems = @($SandboxPayload.$RequiredProperty)
            if ($SandboxItems.Count -gt 0) {
                $ItemProperties = if ($Endpoint -eq '/accounts') {
                    @('uuid', 'currency', 'available_balance')
                } else {
                    @('order_id', 'product_id', 'status')
                }
                Assert-JsonShape $SandboxItems[0] $ItemProperties "Sandbox $Endpoint item"
            }
    }
    Write-Host 'Only the official Advanced Trade sandbox Accounts/Orders GET paths are used; no Coinbase Exchange sandbox URL is used.'
}

if ($RunDocker) {
    Invoke-Compose @('config', '--quiet')
    [string]$OutputPath = $OutputDirectory
    if (-not [IO.Path]::IsPathRooted($OutputPath)) {
        $OutputPath = Join-Path (Get-Location).Path $OutputPath
    }
    $OutputPath = [IO.Path]::GetFullPath($OutputPath)
    New-Item -ItemType Directory -Force -Path $OutputPath | Out-Null

    $End = [DateTimeOffset]::UtcNow
    $Start = $End.AddMinutes(-1 * [Math]::Max($CandleCount, 5))
    $RestUri = "https://api.coinbase.com/api/v3/brokerage/market/products/$ProductId/candles?granularity=ONE_MINUTE&start=$($Start.ToUnixTimeSeconds())&end=$($End.ToUnixTimeSeconds())"
    $RestResponse = Invoke-WebRequest -UseBasicParsing -Uri $RestUri -Method Get -TimeoutSec 20
    if ($RestResponse.StatusCode -ne 200) {
        throw "Coinbase public market candle request returned HTTP $($RestResponse.StatusCode)."
    }
    $CandlePayload = $RestResponse.Content | ConvertFrom-Json
    Assert-JsonShape $CandlePayload @('candles') 'Coinbase candle response'
    $Candles = @($CandlePayload.candles | Select-Object -First $CandleCount)
    if ($Candles.Count -lt 5) {
        throw 'Coinbase candle response did not contain the five-close minimum.'
    }

    $CsvPath = Join-Path $OutputPath $IndicatorFileName
    $CsvRows = foreach ($Candle in $Candles | Sort-Object { [long]$_.start }) {
        [PSCustomObject]@{
            DateTime = [DateTimeOffset]::FromUnixTimeSeconds([long]$Candle.start).UtcDateTime.ToString('dd/MM/yyyy HH:mm:ss')
            Close = [decimal]::Parse($Candle.close, [Globalization.CultureInfo]::InvariantCulture)
        }
    }
    $CsvLines = @('DateTime,Close') + @(
        foreach ($CsvRow in $CsvRows) {
            $InvariantClose = $CsvRow.Close.ToString(
                'G',
                [Globalization.CultureInfo]::InvariantCulture)
            "$($CsvRow.DateTime),$InvariantClose"
        }
    )
    [IO.File]::WriteAllLines(
        $CsvPath,
        [string[]]$CsvLines,
        [Text.UTF8Encoding]::new($false))

    $WebSocket = [Net.WebSockets.ClientWebSocket]::new()
    $MessageBytes = [IO.MemoryStream]::new()
    try {
        [void]$WebSocket.ConnectAsync(
            [Uri]'wss://advanced-trade-ws.coinbase.com',
            [Threading.CancellationToken]::None).Wait([TimeSpan]::FromSeconds(20))
        if ($WebSocket.State -ne [Net.WebSockets.WebSocketState]::Open) {
            throw 'Coinbase public market WebSocket did not open within the bounded timeout.'
        }
        $Subscription = '{"type":"subscribe","product_ids":["' + $ProductId + '"],"channel":"ticker"}'
        $SubscriptionBytes = [Text.Encoding]::UTF8.GetBytes($Subscription)
        [void]$WebSocket.SendAsync(
            [ArraySegment[byte]]::new($SubscriptionBytes),
            [Net.WebSockets.WebSocketMessageType]::Text,
            $true,
        [Threading.CancellationToken]::None).Wait([TimeSpan]::FromSeconds(10))
        $ReceiveBuffer = [byte[]]::new(16384)
        $ObservedMarketEvent = $false
        $Deadline = [DateTimeOffset]::UtcNow.AddSeconds(20)
        while (-not $ObservedMarketEvent -and [DateTimeOffset]::UtcNow -lt $Deadline) {
            if ($WebSocket.State -in @(
                    [Net.WebSockets.WebSocketState]::Closed,
                    [Net.WebSockets.WebSocketState]::Aborted,
                    [Net.WebSockets.WebSocketState]::CloseSent)) {
                throw "Coinbase public market WebSocket entered state $($WebSocket.State) before a market event arrived."
            }
            $Remaining = $Deadline - [DateTimeOffset]::UtcNow
            if ($Remaining -le [TimeSpan]::Zero) { break }
            $Receive = $WebSocket.ReceiveAsync(
                [ArraySegment[byte]]::new($ReceiveBuffer),
                [Threading.CancellationToken]::None)
            if (-not $Receive.Wait($Remaining) -or
                $Receive.Result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
                if ($Receive.IsCompleted -and
                    $Receive.Result.MessageType -eq [Net.WebSockets.WebSocketMessageType]::Close) {
                    throw 'Coinbase public market WebSocket closed before a market-data event arrived.'
                }
                break
            }
            if ($Receive.Result.MessageType -ne [Net.WebSockets.WebSocketMessageType]::Text) {
                throw 'Coinbase public market WebSocket returned a non-text frame.'
            }
            $MessageBytes.Write($ReceiveBuffer, 0, $Receive.Result.Count)
            if (-not $Receive.Result.EndOfMessage) { continue }
            try {
                $Message = [Text.Encoding]::UTF8.GetString($MessageBytes.ToArray()) |
                    ConvertFrom-Json
                $ObservedMarketEvent =
                    ($Message.channel -in @('ticker', 'market_trades', 'level2')) -and
                    $null -ne $Message.events -and
                    @($Message.events).Count -gt 0
            }
            catch {
                $ObservedMarketEvent = $false
            }
            $MessageBytes.SetLength(0)
        }
        if (-not $ObservedMarketEvent) {
            throw 'Coinbase public market WebSocket did not return a parsed market-data event within the bounded timeout.'
        }
    }
    finally {
        if ($null -ne $MessageBytes) {
            $MessageBytes.Dispose()
        }
        $WebSocket.Dispose()
    }

    $ExistingBackendOutput = & $Compose.Source '-f' $ComposeFile.Path 'ps' '-q' 'backend'
    if ($LASTEXITCODE -ne 0) {
        throw "docker-compose ps -q backend failed with exit code $LASTEXITCODE."
    }
    $ExistingBackendContainer = (($ExistingBackendOutput -join "`n")).Trim()
    $StartedAcceptanceStack = [string]::IsNullOrWhiteSpace($ExistingBackendContainer)
    try {
        if ($StartedAcceptanceStack) {
            Invoke-Compose @('up', '-d', '--build')
        } else {
            Write-Host 'Using the already running acceptance stack; cleanup will leave it running.'
        }
        $Backend = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:5000/health' -TimeoutSec 90
        if ($Backend.StatusCode -ne 200) { throw 'Backend health check failed.' }
        $Gateway = Invoke-WebRequest -UseBasicParsing -Uri 'http://127.0.0.1:8088/' -TimeoutSec 90
        if ($Gateway.StatusCode -ne 200) { throw 'Gateway/browser root check failed.' }

        $ContainerOutput = & $Compose.Source '-f' $ComposeFile.Path 'ps' '-q' 'machinelearning'
        if ($LASTEXITCODE -ne 0) {
            throw "docker-compose ps -q machinelearning failed with exit code $LASTEXITCODE."
        }
        $ContainerName = (($ContainerOutput -join "`n")).Trim()
        if ([string]::IsNullOrWhiteSpace($ContainerName)) { throw 'Machine-learning container was not created.' }
        & docker cp $CsvPath "$ContainerName`:/usr/src/app/static/$IndicatorFileName"
        if ($LASTEXITCODE -ne 0) { throw 'Could not copy captured CSV into the disposable ML volume.' }
        & docker exec $ContainerName mkdir -p "/usr/src/app/static/$IndicatorDirectoryName"
        if ($LASTEXITCODE -ne 0) { throw 'Could not create the disposable ML fixture directory.' }

        $Normalizer = @{
            indicator_id = $IndicatorFileName
            min_close_value = [double](($CsvRows.Close | Measure-Object -Minimum).Minimum)
            max_close_value = [double](($CsvRows.Close | Measure-Object -Maximum).Maximum)
            epochs = 1
        } | ConvertTo-Json
        $NormalizerPath = Join-Path $OutputPath $NormalizerFileName
        [IO.File]::WriteAllText(
            $NormalizerPath,
            $Normalizer,
            [Text.UTF8Encoding]::new($false))
        & docker cp $NormalizerPath "$ContainerName`:/usr/src/app/static/$IndicatorDirectoryName/$NormalizerFileName"
        if ($LASTEXITCODE -ne 0) { throw 'Could not copy the temporary normalizer into the disposable ML volume.' }
        & docker exec -u 0 $ContainerName chown 10001:10001 "/usr/src/app/static/$IndicatorDirectoryName/$NormalizerFileName"
        if ($LASTEXITCODE -ne 0) { throw 'Could not assign the machine-learning worker ownership to the temporary normalizer.' }

        $ModelCommand = "import tensorflow as tf; m=tf.keras.Sequential([tf.keras.Input(shape=(5,1)),tf.keras.layers.Flatten(),tf.keras.layers.Dense(1,kernel_initializer='ones',bias_initializer='zeros')]); m.save('/usr/src/app/static/$IndicatorDirectoryName/$ModelFileName')"
        Invoke-Compose @('exec', '-T', 'machinelearning', 'python', '-c', $ModelCommand)

        $PredictionResults = @()
        foreach ($Run in 1..2) {
            $Prediction = Invoke-RestMethod -Uri 'http://127.0.0.1:8088/machinelearning/api/v1/predict/' -Method Post -ContentType 'application/json' -Body (@{ indicator_file = $IndicatorFileName } | ConvertTo-Json)
            $TaskId = $Prediction.task_id
            if ([string]::IsNullOrWhiteSpace($TaskId)) { throw 'Prediction API did not return a task ID.' }
            $Completed = $false
            for ($Attempt = 0; $Attempt -lt 30; $Attempt++) {
                Start-Sleep -Seconds 1
                $Status = Invoke-RestMethod -Uri 'http://127.0.0.1:8088/machinelearning/api/v1/taskstatus/' -Method Post -ContentType 'application/json' -Body (@{ task_id = $TaskId } | ConvertTo-Json)
                if ($Status.state -eq 'SUCCESS') {
                    $PredictionResults += ($Status.status.close_price_prediction | ConvertTo-Json -Depth 10 -Compress)
                    $Completed = $true
                    break
                }
                if ($Status.state -eq 'FAILURE') { throw "Prediction task $TaskId failed." }
            }
            if (-not $Completed) { throw "Prediction task $TaskId did not complete within the bounded timeout." }
        }
        if ($PredictionResults[0] -cne $PredictionResults[1]) {
            throw 'Identical replay input produced different prediction series.'
        }
        if ($RunBrowser) {
            $FrontendPackage = Join-Path $PSScriptRoot '..\exchange.signalR.client.web.frontend.angular\node_modules\@playwright\test\index.mjs'
            if (-not (Test-Path -LiteralPath $FrontendPackage)) {
                throw 'The optional browser gate requires the frontend npm install with Playwright.'
            }
            $env:LOCAL_ACCEPTANCE_PRODUCT_ID = $ProductId
            $env:LOCAL_ACCEPTANCE_INDICATOR_FILE = $IndicatorFileName
            & node (Join-Path $PSScriptRoot 'local-acceptance-browser.mjs')
            if ($LASTEXITCODE -ne 0) { throw 'Angular browser acceptance failed.' }
        }
        Write-Host 'Docker, backend health, gateway/browser root, ML API, Celery/Mongo, and deterministic replay checks passed.'
    }
    finally {
        if ($StartedAcceptanceStack) {
            Invoke-Compose @('down', '--remove-orphans')
        }
    }
}
