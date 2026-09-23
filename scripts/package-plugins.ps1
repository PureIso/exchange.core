[CmdletBinding()]
param(
    [ValidateSet('Debug', 'Release')]
    [string]$Configuration = 'Release',

    [string]$OutputRoot
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot 'artifacts\plugins'
}
elseif (-not [System.IO.Path]::IsPathRooted($OutputRoot)) {
    $OutputRoot = Join-Path $repositoryRoot $OutputRoot
}
$outputRootPath = [System.IO.Path]::GetFullPath($OutputRoot)
$targetFramework = 'net10.0'
$platform = 'x64'

$legacyArtifacts = @(
    [pscustomobject]@{ Project = 'exchange.binance'; Name = 'exchange.binance.dll' },
    [pscustomobject]@{ Project = 'exchange.binance'; Name = 'binance.config.ini' },
    [pscustomobject]@{ Project = 'exchange.coinbase'; Name = 'exchange.coinbase.dll' },
    [pscustomobject]@{ Project = 'exchange.coinbase'; Name = 'coinbase.config.ini' }
)
$advancedTradeArtifact =
    [pscustomobject]@{
        Project = 'exchange.coinbase.advancedtrade'
        Name = 'exchange.coinbase.advancedtrade.dll'
    }

function Get-SourcePath {
    param([pscustomobject]$Artifact)

    return Join-Path $repositoryRoot (Join-Path $Artifact.Project (
        'bin\{0}\{1}\{2}\{3}' -f
            $platform,
            $Configuration,
            $targetFramework,
            $Artifact.Name))
}

function Assert-SafeProfilePath {
    param([string]$ProfilePath)

    $rootWithSeparator = $outputRootPath.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) +
        [System.IO.Path]::DirectorySeparatorChar
    $fullProfilePath = [System.IO.Path]::GetFullPath($ProfilePath)
    if (-not $fullProfilePath.StartsWith(
            $rootWithSeparator,
            [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to package outside the output root: $fullProfilePath"
    }
}

function Write-Profile {
    param(
        [string]$Profile,
        [object[]]$Artifacts
    )

    $profilePath = Join-Path $outputRootPath $Profile
    Assert-SafeProfilePath $profilePath
    $stagingPath = Join-Path $outputRootPath ('.staging-{0}-{1}' -f $Profile, [guid]::NewGuid())
    Assert-SafeProfilePath $stagingPath
    New-Item -ItemType Directory -Path $stagingPath -Force | Out-Null
    try {
        foreach ($artifact in $Artifacts) {
            $source = Get-SourcePath $artifact
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) {
                throw "Required plugin artifact is missing. Build $($artifact.Project) for $platform/$Configuration/$targetFramework first."
            }
            Copy-Item -LiteralPath $source -Destination (Join-Path $stagingPath $artifact.Name)
        }

        $files = @(Get-ChildItem -LiteralPath $stagingPath -File |
            Sort-Object Name |
            ForEach-Object {
                [pscustomobject]@{
                    name = $_.Name
                    length = $_.Length
                    sha256 = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
                }
            })
        $manifest = [pscustomobject]@{
            schema_version = 1
            profile = $Profile
            platform = $platform
            configuration = $Configuration
            target_framework = $targetFramework
            files = $files
        }
        $manifest |
            ConvertTo-Json -Depth 4 |
            Set-Content -LiteralPath (Join-Path $stagingPath 'plugin-manifest.json') -Encoding UTF8

        if (Test-Path -LiteralPath $profilePath) {
            Remove-Item -LiteralPath $profilePath -Recurse -Force
        }
        Move-Item -LiteralPath $stagingPath -Destination $profilePath
        Write-Output "Packaged $Profile -> $profilePath"
    }
    finally {
        if (Test-Path -LiteralPath $stagingPath) {
            Remove-Item -LiteralPath $stagingPath -Recurse -Force
        }
    }
}

New-Item -ItemType Directory -Path $outputRootPath -Force | Out-Null
Write-Profile -Profile 'legacy-default' -Artifacts $legacyArtifacts
Write-Profile -Profile 'advanced-trade-acceptance' -Artifacts @(
    $legacyArtifacts + $advancedTradeArtifact)
