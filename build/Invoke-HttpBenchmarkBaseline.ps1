[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BaselinePath,

    [string]$OutputPath = (Join-Path ([IO.Path]::GetTempPath()) ("bing-http-benchmark-compare-{0}.json" -f [Guid]::NewGuid().ToString("N"))),
    [ValidateSet("upload", "json", "json-stream", "metadata", "raw", "metadata-large", "raw-large", "json-large", "json-large-stream", "download-result", "download-progress", "retry-unobserved", "retry-observed", "interceptor", "observed")]
    [string]$Mode = "metadata-large",
    [ValidateRange(0, [int]::MaxValue)]
    [int]$SizeMiB = 1,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$Concurrency = 1,
    [ValidateRange(1, [int]::MaxValue)]
    [int]$Repetitions = 3,
    [ValidateRange(0, [double]::MaxValue)]
    [double]$FailOnRegression = 10,
    [ValidateSet("all", "time", "allocated", "request-allocated", "peak")]
    [string]$Metric = "all",
    [ValidateSet("p50", "p95")]
    [string]$Percentile,
    [switch]$NoBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "benchmarks/Bing.Utils.Http.Benchmark/Bing.Utils.Http.Benchmark.csproj"

function Resolve-PathValue([string]$path) {
    if ([IO.Path]::IsPathRooted($path)) { return [IO.Path]::GetFullPath($path) }
    return [IO.Path]::GetFullPath((Join-Path (Get-Location) $path))
}

$baselineFullPath = Resolve-PathValue $BaselinePath

function Write-Status([string]$status, [string]$message, [int]$exitCode) {
    [ordered]@{
        status = $status
        message = $message
        baseline = $baselineFullPath
        mode = $Mode
        targetFramework = "net8.0"
        os = [System.Runtime.InteropServices.RuntimeInformation]::OSDescription
        architecture = [System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture.ToString()
    } | ConvertTo-Json -Compress
    exit $exitCode
}

if (-not (Test-Path -LiteralPath $baselineFullPath -PathType Leaf)) {
    Write-Status "SKIPPED_BASELINE_MISSING" "Benchmark baseline was not provided; CI gate was skipped." 0
}

$outputFullPath = Resolve-PathValue $OutputPath
$currentPath = Join-Path ([IO.Path]::GetTempPath()) ("bing-http-benchmark-current-{0}.json" -f [Guid]::NewGuid().ToString("N"))
$runArguments = @("run", "--project", $project, "-c", "Release", "--no-restore")
if ($NoBuild) { $runArguments += "--no-build" }
$runArguments += @("--", $Mode, $SizeMiB, $Concurrency, $Repetitions, "--baseline", $currentPath)

try {
    & dotnet @runArguments
    if ($LASTEXITCODE -ne 0) {
        Write-Status "FAILED_BENCHMARK" "Benchmark execution failed with exit code $LASTEXITCODE." 1
    }

    $compareArguments = @("run", "--project", $project, "-c", "Release", "--no-restore")
    if ($NoBuild) { $compareArguments += "--no-build" }
    $compareArguments += @("--", "--compare", $baselineFullPath, $currentPath,
        "--output", $outputFullPath, "--fail-on-regression", $FailOnRegression,
        "--metric", $Metric)
    if ($Percentile) { $compareArguments += @("--percentile", $Percentile) }

    & dotnet @compareArguments
    if ($LASTEXITCODE -ne 0) {
        Write-Status "FAILED_REGRESSION_OR_ENVIRONMENT" "Benchmark comparison failed; inspect the comparison JSON for regression or environment mismatch." 1
    }

    [ordered]@{
        status = "PASSED"
        baseline = $baselineFullPath
        comparison = $outputFullPath
        mode = $Mode
        thresholdPercent = $FailOnRegression
        metric = $Metric
        percentile = $Percentile
    } | ConvertTo-Json -Compress
    exit 0
}
finally {
    if (Test-Path -LiteralPath $currentPath) { Remove-Item -LiteralPath $currentPath -Force }
}
