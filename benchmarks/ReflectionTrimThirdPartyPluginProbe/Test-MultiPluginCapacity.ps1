param(
    [Parameter(Mandatory = $true)]
    [string]$ProbePath,

    [Parameter(Mandatory = $true)]
    [ValidateCount(9, 9)]
    [string[]]$ValidPluginDirectories,

    [ValidateRange(1, 20)]
    [int]$Rounds = 1
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$probe = [System.IO.Path]::GetFullPath($ProbePath)
if (!(Test-Path -LiteralPath $probe -PathType Leaf)) {
    throw "Reflection probe was not found: $probe"
}

$maxContexts = 32
$requestedContexts = $maxContexts + 1
[string]$roundText = $Rounds.ToString([System.Globalization.CultureInfo]::InvariantCulture)
[string[]]$normalizedValidDirectories = @(
    $ValidPluginDirectories | ForEach-Object {
        $directory = [System.IO.Path]::GetFullPath($_)
        if (!(Test-Path -LiteralPath $directory -PathType Container)) {
            throw "A valid multi-plugin fixture directory was not found: $directory"
        }

        $directory
    }
)

$positiveDirectories = [System.Collections.Generic.List[string]]::new()
$positiveDirectories.AddRange($normalizedValidDirectories)
while ($positiveDirectories.Count -lt $maxContexts) {
    $positiveDirectories.Add($normalizedValidDirectories[0])
}

$directories = @(
    1..$requestedContexts | ForEach-Object {
        Join-Path ([System.IO.Path]::GetTempPath()) "reflection-third-party-plugin-capacity-$($_)"
    }
)

$expectedMarker = "reflection-third-party-plugin-boundary:multi-plugin:resource-rejected:contexts:$requestedContexts>$maxContexts"

foreach ($mode in @('multi-plugin', 'multi-plugin-rounds')) {
    $modeArguments = if ($mode -eq 'multi-plugin') {
        @('--multi-plugin') + $directories
    }
    else {
        @('--multi-plugin-rounds', '1') + $directories
    }
    $arguments = @($probe) + $modeArguments
    $output = (& dotnet @arguments 2>&1 | Out-String).Trim()
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 2) {
        throw "Expected resource rejection exit code 2, received $exitCode. Output: $output"
    }

    if (!$output.Contains($expectedMarker, [System.StringComparison]::Ordinal)) {
        throw "Expected resource rejection marker '$expectedMarker'. Output: $output"
    }
}

$positiveModes = [System.Collections.Generic.List[string[]]]::new()
$positiveModes.Add([string[]](@('--multi-plugin') + $positiveDirectories.ToArray()))
$positiveModes.Add([string[]](@('--multi-plugin-rounds', $roundText) + $positiveDirectories.ToArray()))
$expectedPositiveMarkers = [System.Collections.Generic.List[string[]]]::new()
$expectedPositiveMarkers.Add([string[]]@(
    'reflection-third-party-plugin-boundary:multi-plugin:path:metadata-preserved',
    'reflection-third-party-plugin-boundary:multi-plugin:resolver:metadata-preserved',
    'reflection-third-party-plugin-boundary:multi-plugin:bytes:metadata-preserved'
))
$expectedPositiveMarkers.Add([string[]]@(
    foreach ($round in 1..$Rounds) {
        foreach ($mode in @('path', 'resolver', 'bytes')) {
            "reflection-third-party-plugin-boundary:multi-plugin:round:$round`:$mode`:metadata-preserved"
        }
    }
))

for ($index = 0; $index -lt $positiveModes.Count; $index++) {
    $arguments = @($probe) + $positiveModes[$index]
    $output = (& dotnet @arguments 2>&1 | Out-String).Trim()
    $exitCode = $LASTEXITCODE
    if ($exitCode -ne 0) {
        throw "Expected $maxContexts-context positive admission to succeed, received $exitCode. Output: $output"
    }

    $markerLines = @(
        $output -split '\r?\n' |
            Where-Object {
                ($_ -match '^reflection-third-party-plugin-boundary:multi-plugin(?::round:\d+)?:') -and
                    ($_ -match ':(path|resolver|bytes):metadata-preserved$')
            }
    )
    $expectedMarkerCount = if ($index -eq 0) { 3 } else { $Rounds * 3 }
    if ($markerLines.Count -ne $expectedMarkerCount) {
        throw "Expected $expectedMarkerCount positive admission markers, received $($markerLines.Count). Output: $output"
    }

    foreach ($marker in $expectedPositiveMarkers[$index]) {
        if (!$output.Contains($marker, [System.StringComparison]::Ordinal)) {
            throw "Expected positive admission marker '$marker'. Output: $output"
        }
    }
}

Write-Output "reflection-third-party-plugin-capacity:positive-admission:verified:contexts=$maxContexts"
Write-Output "reflection-third-party-plugin-capacity:positive-admission:verified:contexts=${maxContexts}:rounds=${Rounds}:markers=$($Rounds * 3)"
Write-Output "reflection-third-party-plugin-capacity:verified:max-contexts=$maxContexts"
exit 0
