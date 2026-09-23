[CmdletBinding()]
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
$secretDirectory = Join-Path $repositoryRoot '.secrets/observability'

function New-RandomHex {
    param([Parameter(Mandatory)] [int]$Bytes)

    $buffer = [byte[]]::new($Bytes)
    $generator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $generator.GetBytes($buffer)
    }
    finally {
        $generator.Dispose()
    }
    return ([BitConverter]::ToString($buffer) -replace '-', '').ToLowerInvariant()
}

function Write-SecretFile {
    param(
        [Parameter(Mandatory)] [string]$Name,
        [Parameter(Mandatory)] [string]$Value
    )

    $path = Join-Path $secretDirectory $Name
    if ((Test-Path -LiteralPath $path) -and -not $Force) {
        throw "Secret already exists: $path. Use -Force only when intentionally rotating an unused stack."
    }
    [System.IO.File]::WriteAllText($path, $Value, [System.Text.UTF8Encoding]::new($false))
}

New-Item -ItemType Directory -Path $secretDirectory -Force | Out-Null

$mongoRootPassword = New-RandomHex -Bytes 32
$graylogMongoPassword = New-RandomHex -Bytes 32
$graylogPasswordSecret = New-RandomHex -Bytes 48
$graylogRootPassword = New-RandomHex -Bytes 18
$hasher = [System.Security.Cryptography.SHA256]::Create()
try {
    $graylogRootHashBytes = $hasher.ComputeHash([System.Text.Encoding]::UTF8.GetBytes($graylogRootPassword))
}
finally {
    $hasher.Dispose()
}
$graylogRootHash = ([BitConverter]::ToString($graylogRootHashBytes) -replace '-', '').ToLowerInvariant()

Write-SecretFile -Name 'mongo-root-username.txt' -Value 'root'
Write-SecretFile -Name 'mongo-root-password.txt' -Value $mongoRootPassword
Write-SecretFile -Name 'graylog-mongodb-password.txt' -Value $graylogMongoPassword
Write-SecretFile -Name 'graylog-mongodb-uri.txt' -Value "mongodb://graylog:$graylogMongoPassword@mongodb:27017/graylog?authSource=graylog"
Write-SecretFile -Name 'graylog-password-secret.txt' -Value $graylogPasswordSecret
Write-SecretFile -Name 'graylog-root-password.txt' -Value $graylogRootPassword
Write-SecretFile -Name 'graylog-root-password-sha2.txt' -Value $graylogRootHash

Write-Output "Created ignored observability secrets in $secretDirectory"
Write-Output 'The Graylog admin password is stored in graylog-root-password.txt; it is not mounted into a container.'
