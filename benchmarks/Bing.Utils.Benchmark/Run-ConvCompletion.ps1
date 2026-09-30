param(
    [ValidateSet('net6.0', 'net7.0', 'net8.0')]
    [string]$Framework = 'net8.0',
    [ValidateSet('Matrix', 'Representative', 'FirstCall', 'Jit', 'Steady', 'SustainedList')]
    [string]$Mode = 'Matrix',
    [string]$OutputDirectory = '',
    [string[]]$Filter = @(),
    [int]$InvocationCount = 20000
)

# 使用已构建的 Release 程序；不在测量进程中还原或构建项目。
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
$evidenceRoot = $env:CONV_EVIDENCE_ROOT
if ([string]::IsNullOrWhiteSpace($evidenceRoot)) {
    $evidenceRoot = Join-Path $repositoryRoot 'artifacts/conv-evidence'
}
$evidenceRoot = [System.IO.Path]::GetFullPath($evidenceRoot)
$benchmarkDll = Join-Path $PSScriptRoot "bin/Release/$Framework/Bing.Utils.Benchmark.dll"
if (!(Test-Path -LiteralPath $benchmarkDll)) {
    throw "请先构建 Release/$Framework 基准项目。"
}
if (!$OutputDirectory) {
    $runId = [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff')
    $OutputDirectory = Join-Path $evidenceRoot "conv-completion/$Framework-$Mode-$runId"
}
$null = New-Item -ItemType Directory -Path $OutputDirectory -Force
$OutputDirectory = (Resolve-Path -LiteralPath $OutputDirectory).Path
if (!$Filter.Count -and $Mode -in @('Matrix', 'Representative')) {
    $Filter = if ($Mode -eq 'Representative') {
        @('*ConvTypedScalarBenchmarks*', '*ConvReferenceBenchmarks*', '*ConvScalarBenchmarks<Int64>*')
    } else {
        @('*ConvTypedScalarBenchmarks*', '*ConvCrossNumericBenchmarks*',
          '*ConvDedicatedIntegerBenchmarks*', '*ConvDateParsingBenchmarks*', '*ConvReferenceBenchmarks*')
    }
}

function Invoke-RecordedDotNet {
    param([string[]]$Arguments, [string]$LogName)
    & dotnet $benchmarkDll @Arguments 2>&1 |
        Out-File -LiteralPath (Join-Path $OutputDirectory $LogName) -Encoding utf8
    if ($LASTEXITCODE -ne 0) {
        throw "测量失败，详见 $LogName，退出码 $LASTEXITCODE。"
    }
}

Push-Location $repositoryRoot
$previousEvidenceRoot = [Environment]::GetEnvironmentVariable('CONV_EVIDENCE_ROOT', 'Process')
[Environment]::SetEnvironmentVariable('CONV_EVIDENCE_ROOT', $evidenceRoot, 'Process')
try {
    $identity = [ordered]@{
        Framework = $Framework
        Mode = $Mode
        Utc = [DateTime]::UtcNow.ToString('O')
        SourceSha256 = (Get-FileHash -LiteralPath 'src/Bing.Utils/Bing/Helpers/Conv.cs' -Algorithm SHA256).Hash
        ProductionDllSha256 = (Get-FileHash -LiteralPath (Join-Path (Split-Path $benchmarkDll) 'Bing.Utils.dll') -Algorithm SHA256).Hash
        HarnessDllSha256 = (Get-FileHash -LiteralPath $benchmarkDll -Algorithm SHA256).Hash
        Filter = $Filter
        InvocationCount = $InvocationCount
        DedicatedSuccessOnly = $env:CONV_DEDICATED_CONFIRM
        ScalarSuccessOnly = $env:CONV_SCALAR_SUCCESS_ONLY
        WarmupCount = if ($Mode -in @('Matrix', 'Representative')) { 3 } else { $null }
        IterationCount = if ($Mode -in @('Matrix', 'Representative')) { 5 } else { $null }
        Toolchain = if ($Mode -in @('Matrix', 'Representative')) { 'InProcessEmitToolchain' } else { 'DirectProcess' }
    }
    $identity | ConvertTo-Json -Depth 4 |
        Out-File -LiteralPath (Join-Path $OutputDirectory 'identity.json') -Encoding utf8

    if ($Mode -eq 'Steady') {
        $steadyEnvironment = @{}
        foreach ($name in @('CONV_STEADY_EVIDENCE_DIR', 'CONV_CANDIDATE_ID', 'CONV_SOURCE_SHA256',
                           'CONV_PRODUCTION_DLL_SHA256', 'CONV_HARNESS_DLL_SHA256')) {
            $steadyEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        }
        $env:CONV_STEADY_EVIDENCE_DIR = $OutputDirectory
        $env:CONV_CANDIDATE_ID = 'conv-next-20260929'
        $env:CONV_SOURCE_SHA256 = $identity.SourceSha256
        $env:CONV_PRODUCTION_DLL_SHA256 = $identity.ProductionDllSha256
        $env:CONV_HARNESS_DLL_SHA256 = $identity.HarnessDllSha256
        try {
            Invoke-RecordedDotNet -Arguments @('--conv-steady') -LogName 'console.txt'
        } finally {
            foreach ($name in $steadyEnvironment.Keys) {
                [Environment]::SetEnvironmentVariable($name, $steadyEnvironment[$name], 'Process')
            }
        }
    } elseif ($Mode -eq 'SustainedList') {
        Invoke-RecordedDotNet -Arguments @('--conv-sustained-gc-list') -LogName 'console.txt'
    } elseif ($Mode -eq 'FirstCall') {
        # 每个场景使用新进程，日志保留 JIT 及共享运行时初始化成本。
        foreach ($scenario in @('JsonConversion', 'GenericLongConversion', 'NumericConversion',
                               'TypedSameTypeInt', 'CustomConverterConversion', 'DictionaryConversion', 'ListConversion')) {
            Invoke-RecordedDotNet -Arguments @("--conv-first-call=$scenario") -LogName "$scenario.txt"
        }
    } elseif ($Mode -eq 'Jit') {
        $savedEnvironment = @{}
        foreach ($name in @('DOTNET_JitDisasm', 'DOTNET_TieredCompilation', 'DOTNET_ReadyToRun')) {
            $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
        }
        try {
            $env:DOTNET_JitDisasm = '*To* *TryConvert*'
            $env:DOTNET_TieredCompilation = '0'
            $env:DOTNET_ReadyToRun = '0'
            Invoke-RecordedDotNet -Arguments @('--conv-baseline') -LogName 'jit.txt'
        } finally {
            foreach ($name in $savedEnvironment.Keys) {
                [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process')
            }
        }
    } else {
        $arguments = @('--filter') + $Filter + @('--inProcess', '--warmupCount', '3', '--iterationCount', '5',
            '--invocationCount', "$InvocationCount", '--unrollFactor', '1', '--artifacts', $OutputDirectory)
        Invoke-RecordedDotNet -Arguments $arguments -LogName 'console.txt'
        $log = Get-Content -LiteralPath (Join-Path $OutputDirectory 'console.txt') -Encoding utf8 -Raw
        if ($log -notmatch 'Global total time:.*executed benchmarks: [1-9]' -or
            $log -match 'There are not any results runs|Benchmarks with issues:|No benchmarks to choose from') {
            throw '基准未完整产出结果，请检查 console.txt。'
        }
    }
} finally {
    [Environment]::SetEnvironmentVariable('CONV_EVIDENCE_ROOT', $previousEvidenceRoot, 'Process')
    Pop-Location
}
