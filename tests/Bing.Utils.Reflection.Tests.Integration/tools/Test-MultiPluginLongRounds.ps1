param(
    [Parameter(Mandatory = $true)]
    [string]$ProbePath,

    [Parameter(Mandatory = $true)]
    [ValidateCount(9, 9)]
    [string[]]$PluginDirectories,

    [ValidateRange(1, 20)]
    [int]$Rounds = 20
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

[string[]]$normalizedDirectories = @(
    $PluginDirectories | ForEach-Object {
        $directory = [System.IO.Path]::GetFullPath($_)
        if (!(Test-Path -LiteralPath $directory -PathType Container)) {
            throw "A multi-plugin long-round fixture directory was not found: $directory"
        }

        $directory
    }
)

$roundText = $Rounds.ToString([System.Globalization.CultureInfo]::InvariantCulture)
$arguments = @($ProbePath, '--multi-plugin-rounds', $roundText) + $normalizedDirectories
$output = (& dotnet @arguments 2>&1 | Out-String).Trim()
$exitCode = $LASTEXITCODE
if ($exitCode -ne 0) {
    throw "Expected $Rounds multi-plugin rounds to succeed, received $exitCode. Output: $output"
}

$markerLines = @(
    $output -split '\r?\n' |
        Where-Object {
            $_ -match '^reflection-third-party-plugin-boundary:multi-plugin:round:\d+:(path|resolver|bytes):metadata-preserved$'
        }
)
$expectedMarkerCount = $Rounds * 3
if ($markerLines.Count -ne $expectedMarkerCount) {
    throw "Expected $expectedMarkerCount metadata-preserved markers, received $($markerLines.Count). Output: $output"
}

foreach ($round in 1..$Rounds) {
    foreach ($mode in @('path', 'resolver', 'bytes')) {
        $marker = "reflection-third-party-plugin-boundary:multi-plugin:round:$round`:$mode`:metadata-preserved"
        if (!$output.Contains($marker, [System.StringComparison]::Ordinal)) {
            throw "Missing long-round marker '$marker'. Output: $output"
        }
    }
}

Write-Output "reflection-third-party-plugin-rounds:verified:rounds=${Rounds}:contexts=$($normalizedDirectories.Count):markers=$expectedMarkerCount"
exit 0
