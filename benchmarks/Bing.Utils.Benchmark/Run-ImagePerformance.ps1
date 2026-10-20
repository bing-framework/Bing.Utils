[CmdletBinding()]
param(
    [ValidateSet('Smoke', 'Calibrate', 'Verify')]
    [string]$Mode = 'Smoke',
    [string]$PolicyPath,
    [string]$OutputDirectory,
    [string]$InputDirectory,
    [string]$BenchmarkDll
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$baselineCommit = 'c6ff0a4bfd5802cbf28ccea72fee9f5f2d8d732e'
if (!$BenchmarkDll) { $BenchmarkDll = Join-Path $PSScriptRoot 'bin/Release/net8.0/Bing.Utils.Benchmark.dll' }
$BenchmarkDll = (Resolve-Path -LiteralPath $BenchmarkDll).Path
if (!$OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot ('bin/image-performance/' + $Mode + '-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
}
$OutputDirectory = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $OutputDirectory) { throw '输出目录必须不存在，以保留已有证据。' }
if ($Mode -ne 'Smoke' -and !$PolicyPath) { throw '校准与验收必须显式指定 PolicyPath。' }
if ($PolicyPath) { $PolicyPath = [IO.Path]::GetFullPath($PolicyPath) }
if ($Mode -eq 'Calibrate' -and (Test-Path -LiteralPath $PolicyPath)) { throw '策略已存在，不得重新校准覆盖。' }
if ($Mode -eq 'Verify' -and !(Test-Path -LiteralPath $PolicyPath -PathType Leaf)) { throw '缺少已冻结策略，先校准固定基线。' }
if (!$InputDirectory) {
    $InputDirectory = if ($Mode -eq 'Verify') {
        (Get-Content -LiteralPath $PolicyPath -Encoding utf8 -Raw | ConvertFrom-Json).inputDirectory
    } else { Join-Path $OutputDirectory 'inputs' }
}
$InputDirectory = [IO.Path]::GetFullPath($InputDirectory)

function Read-Json([string]$Path) {
    return Get-Content -LiteralPath $Path -Encoding utf8 -Raw | ConvertFrom-Json
}

function Write-Json([object]$Value, [string]$Path) {
    $Value | ConvertTo-Json -Depth 16 | Set-Content -LiteralPath $Path -Encoding utf8
}

function Get-HarnessIdentity {
    $result = [ordered]@{}
    foreach ($relative in @('Program.cs', 'Bing.Utils.Benchmark.csproj', 'Benchmarks/ImagePerformanceProbe.cs', 'Run-ImagePerformance.ps1')) {
        $result[$relative] = (Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $relative) -Algorithm SHA256).Hash
    }
    return $result
}

function Get-MachineIdentity {
    $cpu = Get-CimInstance -ClassName Win32_Processor
    $system = Get-CimInstance -ClassName Win32_ComputerSystem
    $power = & powercfg /getactivescheme
    if ($LASTEXITCODE -ne 0) { throw '无法记录电源模式，不能确认测量环境。' }
    return [ordered]@{
        machine = [Environment]::MachineName
        cpu = ($cpu.Name -join ';').Trim()
        logicalProcessors = [Environment]::ProcessorCount
        physicalMemoryBytes = [long]$system.TotalPhysicalMemory
        os = [Runtime.InteropServices.RuntimeInformation]::OSDescription
        architecture = [Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture.ToString()
        sdk = (& dotnet --version | Out-String).Trim()
        powerScheme = ($power | Out-String).Trim()
    }
}

function Assert-SameJson([object]$Expected, [object]$Actual, [string]$Name) {
    if (($Expected | ConvertTo-Json -Depth 16 -Compress) -cne ($Actual | ConvertTo-Json -Depth 16 -Compress)) {
        throw "$Name 不一致，不能使用当前策略比较；不得自动重建基线。"
    }
}

function Assert-Measure([object]$Measure) {
    $expected = foreach ($backend in @('ImageSharp', 'SkiaSharp')) {
        foreach ($size in @('512x512', '1920x1080')) {
            foreach ($format in @('Jpeg', 'Png')) { "$backend-$size-$format" }
        }
    }
    if ($Measure.schemaVersion -ne 1 -or $Measure.mode -ne 'measure' -or $Measure.rounds -ne 3 -or
        @($Measure.inputs).Count -ne 4 -or @($Measure.scenarios).Count -ne 8) {
        throw '测量头、输入或场景数量不完整。'
    }
    Assert-SameJson @($expected | Sort-Object) @($Measure.scenarios.id | Sort-Object) '八场景清单'
    foreach ($scenario in $Measure.scenarios) {
        if (@($scenario.roundResults).Count -ne 3) { throw "轮次不完整：$($scenario.id)" }
        Assert-SameJson @(1, 2, 3) @($scenario.roundResults.round | Sort-Object) '测量轮次'
        foreach ($round in $scenario.roundResults) {
            if (@($round.samples).Count -ne 5) { throw "样本不完整：$($scenario.id)" }
        }
        foreach ($row in @($scenario) + @($scenario.roundResults) + @($scenario.roundResults.samples)) {
            if ($null -eq $row.timeMs -or $null -eq $row.allocatedBytes -or
                ![double]::IsFinite([double]$row.timeMs) -or [double]$row.timeMs -le 0 -or
                ![double]::IsFinite([double]$row.allocatedBytes) -or [double]$row.allocatedBytes -lt 0) {
                throw "无效测量数值：$($scenario.id)"
            }
        }
    }
}

# 仅校准时要求生产构建输入与固定提交完全一致；不复制源码或创建额外工作树。
if ($Mode -eq 'Calibrate') {
    $scope = @('src/Bing.Utils', 'src/Bing.Utils.Drawing.Shared', 'src/Bing.Utils.Drawing.ImageSharp',
        'src/Bing.Utils.Drawing.SkiaSharp', 'asset', 'common.props', 'version.props', 'global.json',
        'Directory.Build.props', 'Directory.Build.targets', 'NuGet.Config')
    & git -C $repositoryRoot diff --quiet $baselineCommit -- @scope
    if ($LASTEXITCODE -ne 0) { throw '生产构建输入与固定基线不同，不能以当前程序集校准。' }
    $untracked = & git -C $repositoryRoot ls-files --others --exclude-standard -- @scope
    if ($LASTEXITCODE -ne 0 -or $untracked) { throw '生产构建范围存在未跟踪输入，不能校准。' }
}

$null = New-Item -ItemType Directory -Path $OutputDirectory
$harness = Get-HarnessIdentity
$environment = if ($Mode -eq 'Smoke') { $null } else { Get-MachineIdentity }
if ($Mode -ne 'Smoke' -and ($environment.sdk -ne '8.0.425' -or $environment.architecture -ne 'X64')) {
    throw '正式测量要求固定 SDK 8.0.425 与 x64 进程。'
}
if ($Mode -eq 'Verify') {
    $policy = Read-Json $PolicyPath
    if ($policy.schemaVersion -ne 1 -or $policy.baselineCommit -ne $baselineCommit -or
        $policy.policyId -ne 'image-local-net8-v1' -or @($policy.scenarios).Count -ne 8) {
        throw '策略版本、基线或场景不符合既定方案。'
    }
    Assert-SameJson $policy.environment $environment '机器环境'
    Assert-SameJson $policy.harness $harness '测量程序'
}

$resultPath = Join-Path $OutputDirectory 'results.json'
$probeMode = if ($Mode -eq 'Smoke') { 'smoke' } else { 'measure' }
$startInfo = [Diagnostics.ProcessStartInfo]::new('dotnet')
$startInfo.UseShellExecute = $false
foreach ($argument in @($BenchmarkDll, "--image-performance=$probeMode", "--image-inputs=$InputDirectory", "--image-output=$resultPath")) {
    $startInfo.ArgumentList.Add($argument)
}
$process = [Diagnostics.Process]::Start($startInfo)
try {
    if (!$process.WaitForExit(30 * 60 * 1000)) {
        $process.Kill($true)
        $process.WaitForExit()
        throw '测量达到预先规定的 30 分钟上限，结果未完成。'
    }
    if ($process.ExitCode -ne 0) { throw "图像探针失败，退出码 $($process.ExitCode)。" }
} finally { $process.Dispose() }
$measure = Read-Json $resultPath
if ($Mode -eq 'Smoke') {
    if ($measure.mode -ne 'smoke' -or @($measure.scenarios).Count -ne 8) { throw '八场景冒烟结果不完整。' }
    Write-Output "Smoke PASS: 8 scenarios; $resultPath"
    return
}
Assert-Measure $measure
$assemblyRoot = Split-Path -Parent $BenchmarkDll
$assemblies = [ordered]@{}
foreach ($name in @('Bing.Utils.Benchmark', 'Bing.Utils', 'Bing.Utils.Drawing.Shared', 'Bing.Utils.Drawing.ImageSharp', 'Bing.Utils.Drawing.SkiaSharp')) {
    $assemblies[$name] = (Get-FileHash -LiteralPath (Join-Path $assemblyRoot "$name.dll") -Algorithm SHA256).Hash
}
$candidateCommit = (& git -C $repositoryRoot rev-parse HEAD | Out-String).Trim()
if ($LASTEXITCODE -ne 0) { throw '无法记录当前提交。' }
Write-Json ([ordered]@{
    mode = $Mode; utc = [DateTime]::UtcNow.ToString('O'); gitCommit = $candidateCommit
    runtime = $measure.runtime; environment = $environment; harness = $harness; assemblies = $assemblies
}) (Join-Path $OutputDirectory 'identity.json')

if ($Mode -eq 'Calibrate') {
    $thresholds = foreach ($scenario in $measure.scenarios) {
        [ordered]@{
            id = $scenario.id
            maxTimeMs = [Math]::Max([double]$scenario.timeMs * 1.20, [double]$scenario.timeMs + 1)
            maxAllocatedBytes = [Math]::Max([Math]::Ceiling([double]$scenario.allocatedBytes * 1.10), [double]$scenario.allocatedBytes + 4096)
        }
    }
    $policy = [ordered]@{
        schemaVersion = 1; policyId = 'image-local-net8-v1'; baselineCommit = $baselineCommit
        environment = $environment; runtime = $measure.runtime; harness = $harness
        inputDirectory = $InputDirectory; inputs = $measure.inputs; scenarios = @($thresholds)
        baselineResults = $resultPath; baselineAssemblies = $assemblies
    }
    if (Test-Path -LiteralPath $PolicyPath) { throw '策略已存在，拒绝覆盖。' }
    Write-Json $policy $PolicyPath
    Write-Output "Calibration PASS: 8 scenarios; frozen policy: $PolicyPath"
} else {
    Assert-SameJson $policy.runtime $measure.runtime '实际运行时'
    Assert-SameJson $policy.inputs $measure.inputs '输入字节'
    Assert-SameJson @($policy.scenarios.id | Sort-Object) @($measure.scenarios.id | Sort-Object) '策略场景'
    $comparison = foreach ($scenario in $measure.scenarios) {
        $budget = @($policy.scenarios | Where-Object id -CEQ $scenario.id)
        if ($budget.Count -ne 1 -or $null -eq $budget[0].maxTimeMs -or $null -eq $budget[0].maxAllocatedBytes -or
            ![double]::IsFinite([double]$budget[0].maxTimeMs) -or [double]$budget[0].maxTimeMs -le 0 -or
            ![double]::IsFinite([double]$budget[0].maxAllocatedBytes) -or [double]$budget[0].maxAllocatedBytes -lt 0) {
            throw "无效策略预算：$($scenario.id)"
        }
        [ordered]@{
            id = $scenario.id; timeMs = $scenario.timeMs; maxTimeMs = $budget[0].maxTimeMs
            allocatedBytes = $scenario.allocatedBytes; maxAllocatedBytes = $budget[0].maxAllocatedBytes
            passed = ([double]$scenario.timeMs -le [double]$budget[0].maxTimeMs -and
                [double]$scenario.allocatedBytes -le [double]$budget[0].maxAllocatedBytes)
        }
    }
    $failed = @($comparison | Where-Object { !$_.passed })
    Write-Json ([ordered]@{ passed = $failed.Count -eq 0; scenarios = @($comparison) }) (Join-Path $OutputDirectory 'comparison.json')
    if ($failed.Count) { throw "预算未通过：$($failed.id -join ', ')；不自动放宽阈值。" }
    Write-Output "Verification PASS: 8 scenarios; $OutputDirectory"
}
