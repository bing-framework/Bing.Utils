[CmdletBinding()]
param(
    [Parameter(ParameterSetName = 'Path', Mandatory = $true)][string]$BaselineRoot,
    [Parameter(ParameterSetName = 'Ref', Mandatory = $true)][string]$BaselineRef,
    [ValidateRange(1, 20)][int]$Rounds = 5,
    [string]$OutputDirectory,
    [double]$ColdStartMaxTimeNs,
    [double]$ColdStartMaxBytes,
    [double]$SteadyStateMaxTimeNs,
    [double]$SteadyStateMaxBytes,
    [string]$PerformancePolicyPath
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
$candidateRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$machineSnapshot = [ordered]@{
    osDescription = [string][System.Runtime.InteropServices.RuntimeInformation]::OSDescription
    osArchitecture = [string][System.Runtime.InteropServices.RuntimeInformation]::OSArchitecture
    processArchitecture = [string][System.Runtime.InteropServices.RuntimeInformation]::ProcessArchitecture
    scriptHostFrameworkDescription = [string][System.Runtime.InteropServices.RuntimeInformation]::FrameworkDescription
    processorCount = [int][Environment]::ProcessorCount
    is64BitProcess = [bool][Environment]::Is64BitProcess
}
$probeRuntimeDescriptions = [Collections.Generic.HashSet[string]]::new()
$probeRuntimeLineCount = 0
$candidateCommit = (& git -C $candidateRoot rev-parse --verify --end-of-options HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($candidateCommit)) {
    throw 'Cannot resolve candidate Git revision.'
}
$comparisonTargetFramework = 'net8.0'
$performancePolicy = $null
$performancePolicyFullPath = $null
$performancePolicySnapshotPath = $null
$performancePolicyId = $null
$performancePolicyEnabled = $false
$performancePolicyThresholdNames = @(
    'ColdStartMaxTimeNs',
    'ColdStartMaxBytes',
    'SteadyStateMaxTimeNs',
    'SteadyStateMaxBytes')

function ConvertTo-PolicyNumber([object]$Value, [string]$Name) {
    if ($null -eq $Value) { return $null }
    if ($Value -is [string] -or $Value -is [bool] -or $Value -is [Array]) {
        throw "性能策略字段 $Name 必须是有限的非负数。"
    }
    try {
        $number = [double]$Value
    } catch {
        throw "性能策略字段 $Name 必须是有限的非负数。"
    }
    if ([double]::IsNaN($number) -or [double]::IsInfinity($number) -or $number -lt 0) {
        throw "性能策略字段 $Name 必须是有限的非负数。"
    }
    return $number
}

function Get-PolicyProperty([object]$Policy, [string]$Name) {
    if ($null -eq $Policy) { return $null }
    $property = $Policy.PSObject.Properties[$Name]
    if ($null -eq $property) { return $null }
    return $property.Value
}

if ($PSBoundParameters.ContainsKey('PerformancePolicyPath')) {
    if ([string]::IsNullOrWhiteSpace($PerformancePolicyPath)) {
        throw 'PerformancePolicyPath must not be empty.'
    }
    $performancePolicyFullPath = (Resolve-Path -LiteralPath $PerformancePolicyPath -ErrorAction Stop).Path
    if (!(Test-Path -LiteralPath $performancePolicyFullPath -PathType Leaf)) {
        throw "性能策略文件不是普通文件：$performancePolicyFullPath"
    }
    try {
        $performancePolicy = Get-Content -LiteralPath $performancePolicyFullPath -Raw -Encoding utf8 |
            ConvertFrom-Json -ErrorAction Stop
    } catch {
        throw "无法读取性能策略 JSON：$performancePolicyFullPath；$($_.Exception.Message)"
    }
    if ($null -eq $performancePolicy -or $performancePolicy -is [Array] -or $performancePolicy -is [ValueType]) {
        throw '性能策略 JSON 根节点必须是对象。'
    }
    $allowedPolicyProperties = @(
        'schemaVersion',
        'policyId',
        'targetFramework',
        'rounds',
        'baselineCommit',
        'coldStartMaxTimeNs',
        'coldStartMaxBytes',
        'steadyStateMaxTimeNs',
        'steadyStateMaxBytes')
    foreach ($property in @($performancePolicy.PSObject.Properties)) {
        if ($allowedPolicyProperties -notcontains $property.Name) {
            throw "性能策略包含未知字段：$($property.Name)"
        }
    }
    $schemaVersion = Get-PolicyProperty $performancePolicy 'schemaVersion'
    if ($schemaVersion -ne 1) {
        throw '性能策略 schemaVersion 必须为 1。'
    }
    $policyIdValue = Get-PolicyProperty $performancePolicy 'policyId'
    if ($policyIdValue -isnot [string]) {
        throw '性能策略 policyId 必须是字符串。'
    }
    $performancePolicyId = $policyIdValue.Trim()
    if ([string]::IsNullOrWhiteSpace($performancePolicyId)) {
        throw '性能策略 policyId 不能为空。'
    }
    $targetFrameworkValue = Get-PolicyProperty $performancePolicy 'targetFramework'
    if ($targetFrameworkValue -isnot [string]) {
        throw '性能策略 targetFramework 必须是字符串。'
    }
    $targetFramework = $targetFrameworkValue.Trim()
    if ($targetFramework -ne $comparisonTargetFramework) {
        throw "性能策略 targetFramework 必须为 $comparisonTargetFramework。"
    }
    $policyRoundsValue = Get-PolicyProperty $performancePolicy 'rounds'
    if ($null -ne $policyRoundsValue) {
        $policyRoundsNumber = ConvertTo-PolicyNumber $policyRoundsValue 'rounds'
        if ($policyRoundsNumber -ne [math]::Floor($policyRoundsNumber)) {
            throw '性能策略 rounds 必须为 1 到 20 的整数。'
        }
        $policyRounds = [int]$policyRoundsNumber
        if ($policyRounds -lt 1 -or $policyRounds -gt 20 -or $policyRounds -ne $Rounds) {
            throw "性能策略 rounds 必须与本次比较一致（当前为 $Rounds）。"
        }
    }
    $policyThresholdCount = 0
    foreach ($budgetParameter in $performancePolicyThresholdNames) {
        $propertyName = switch ($budgetParameter) {
            'ColdStartMaxTimeNs' { 'coldStartMaxTimeNs' }
            'ColdStartMaxBytes' { 'coldStartMaxBytes' }
            'SteadyStateMaxTimeNs' { 'steadyStateMaxTimeNs' }
            'SteadyStateMaxBytes' { 'steadyStateMaxBytes' }
        }
        $property = $performancePolicy.PSObject.Properties[$propertyName]
        if ($null -eq $property) { continue }
        $value = ConvertTo-PolicyNumber $property.Value $propertyName
        if ($null -eq $value) { continue }
        $policyThresholdCount++
        if ($PSBoundParameters.ContainsKey($budgetParameter)) {
            $explicitValue = [double]$PSBoundParameters[$budgetParameter]
            if ($explicitValue -ne $value) {
                throw "命令行预算 $budgetParameter 与性能策略字段 $propertyName 不一致。"
            }
        } else {
            Set-Variable -Name $budgetParameter -Value $value -Scope 0
        }
    }
    if ($policyThresholdCount -eq 0) {
        throw '性能策略至少需要一个非空预算字段。'
    }
    $performancePolicyEnabled = $true
}
$baselineTempRoot = $null
$buildTempRoot = $null
function Remove-TemporaryRoots {
    foreach ($temporaryRoot in @($baselineTempRoot, $buildTempRoot)) {
        if ($temporaryRoot -and (Test-Path -LiteralPath $temporaryRoot)) {
            Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}
trap {
    if (Get-Command Remove-TemporaryRoots -CommandType Function -ErrorAction SilentlyContinue) {
        Remove-TemporaryRoots
    }
    break
}
foreach ($budgetParameter in @('ColdStartMaxTimeNs', 'ColdStartMaxBytes', 'SteadyStateMaxTimeNs', 'SteadyStateMaxBytes')) {
    if ($PSBoundParameters.ContainsKey($budgetParameter)) {
        $budgetValue = [double]$PSBoundParameters[$budgetParameter]
        if ([double]::IsNaN($budgetValue) -or [double]::IsInfinity($budgetValue) -or $budgetValue -lt 0) {
            throw "$budgetParameter 必须是有限的非负数。"
        }
    }
}
$hasPolicyColdStartMaxTimeNs = $performancePolicyEnabled -and $null -ne (Get-PolicyProperty $performancePolicy 'coldStartMaxTimeNs')
$hasPolicyColdStartMaxBytes = $performancePolicyEnabled -and $null -ne (Get-PolicyProperty $performancePolicy 'coldStartMaxBytes')
$hasPolicySteadyStateMaxTimeNs = $performancePolicyEnabled -and $null -ne (Get-PolicyProperty $performancePolicy 'steadyStateMaxTimeNs')
$hasPolicySteadyStateMaxBytes = $performancePolicyEnabled -and $null -ne (Get-PolicyProperty $performancePolicy 'steadyStateMaxBytes')
$thresholdEnabled = $performancePolicyEnabled -or
    $PSBoundParameters.ContainsKey('ColdStartMaxTimeNs') -or
    $PSBoundParameters.ContainsKey('ColdStartMaxBytes') -or
    $PSBoundParameters.ContainsKey('SteadyStateMaxTimeNs') -or
    $PSBoundParameters.ContainsKey('SteadyStateMaxBytes')
$thresholdSource = if ($performancePolicyEnabled) { 'policy' } elseif ($thresholdEnabled) { 'command-line' } else { 'disabled' }
if ($PSBoundParameters.ContainsKey('BaselineRef')) {
    $BaselineRef = $BaselineRef.Trim()
    if ([string]::IsNullOrWhiteSpace($BaselineRef)) {
        throw 'BaselineRef must not be empty.'
    }
}
if ($BaselineRef) {
    $baselineCommit = (& git -C $candidateRoot rev-parse --verify --end-of-options "$BaselineRef^{commit}").Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($baselineCommit)) {
        throw "Cannot resolve baseline Git revision: $BaselineRef"
    }

    $baselineTempRoot = Join-Path ([IO.Path]::GetTempPath()) ('bing-utils-reflection-baseline-' + [guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $baselineTempRoot
    $baselineArchive = Join-Path $baselineTempRoot 'source.zip'
    & git -C $candidateRoot archive --format=zip "--output=$baselineArchive" $baselineCommit
    if ($LASTEXITCODE -ne 0) { throw "Cannot archive baseline Git revision: $baselineCommit" }
    $baselinePath = Join-Path $baselineTempRoot 'source'
    Expand-Archive -LiteralPath $baselineArchive -DestinationPath $baselinePath
} else {
    $baselinePath = (Resolve-Path -LiteralPath $BaselineRoot).Path
    $baselineCommit = (& git -c "safe.directory=$baselinePath" -C $baselinePath rev-parse --verify --end-of-options HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($baselineCommit)) { throw 'Cannot resolve baseline Git revision.' }
}
$policyBaselineValue = Get-PolicyProperty $performancePolicy 'baselineCommit'
if ($null -ne $policyBaselineValue -and $policyBaselineValue -isnot [string]) {
    throw '性能策略 baselineCommit 必须是字符串。'
}
$policyBaselineCommit = [string]$policyBaselineValue
if ($performancePolicyEnabled -and ![string]::IsNullOrWhiteSpace($policyBaselineCommit) -and
    ![string]::Equals($policyBaselineCommit.Trim(), $baselineCommit, [StringComparison]::OrdinalIgnoreCase)) {
    throw "性能策略 baselineCommit 与实际基线不一致：$baselineCommit"
}
if (!$OutputDirectory) {
    $OutputDirectory = Join-Path $PSScriptRoot ('bin/evidence/' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fff'))
}
$evidenceRoot = [IO.Path]::GetFullPath($OutputDirectory)
if (Test-Path -LiteralPath $evidenceRoot) {
    throw 'OutputDirectory must be a new directory to preserve previous evidence.'
}
$null = New-Item -ItemType Directory -Path $evidenceRoot
if ($performancePolicyEnabled) {
    $performancePolicySnapshotPath = Join-Path $evidenceRoot 'performance-policy-input.json'
    if (![string]::Equals(
            [IO.Path]::GetFullPath($performancePolicyFullPath),
            [IO.Path]::GetFullPath($performancePolicySnapshotPath),
            [StringComparison]::OrdinalIgnoreCase)) {
        Copy-Item -LiteralPath $performancePolicyFullPath -Destination $performancePolicySnapshotPath
    }
}

function Invoke-CheckedDotnet([string[]]$Arguments) {
    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) { throw "dotnet failed: $Arguments" }
}

# 保存两个版本的完整库源码和共享构建输入；不依赖候选提交状态，也不复制 bin/obj。
function Save-SourceSnapshot([string]$Root, [string]$Label) {
    $inputs = @('src/Bing.Utils', 'src/Bing.Utils.Reflection', 'asset', 'common.props', 'version.props', 'LICENSE')
    $files = foreach ($input in $inputs) {
        $target = Join-Path $Root $input
        if (Test-Path -LiteralPath $target -PathType Container) {
            Get-ChildItem -LiteralPath $target -Recurse -File |
                Where-Object { $_.FullName -notmatch '[\\/](bin|obj)[\\/]' }
        } else {
            Get-Item -LiteralPath $target
        }
    }
    $archivePath = Join-Path $evidenceRoot "$Label-source.zip"
    $archive = [IO.Compression.ZipFile]::Open($archivePath, [IO.Compression.ZipArchiveMode]::Create)
    try {
        $manifest = foreach ($file in ($files | Sort-Object FullName)) {
            $relative = [IO.Path]::GetRelativePath($Root, $file.FullName).Replace('\', '/')
            $null = [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive, $file.FullName, $relative)
            [pscustomobject]@{ path = $relative; sha256 = (Get-FileHash -LiteralPath $file.FullName -Algorithm SHA256).Hash }
        }
    } finally { $archive.Dispose() }
    $manifest | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceRoot "$Label-source-manifest.json") -Encoding utf8
    $snapshotRoot = Join-Path $evidenceRoot "$Label-source"
    [IO.Compression.ZipFile]::ExtractToDirectory($archivePath, $snapshotRoot)
    return $snapshotRoot
}

if (!$BaselineRef) {
    $baselineChanges = & git -c "safe.directory=$baselinePath" -C $baselinePath status --porcelain -- src/Bing.Utils src/Bing.Utils.Reflection asset common.props version.props
    if ($LASTEXITCODE -ne 0 -or $baselineChanges) { throw 'Baseline build inputs must be clean.' }
}
$baselineSnapshot = Save-SourceSnapshot $baselinePath 'baseline'
$candidateSnapshot = Save-SourceSnapshot $candidateRoot 'candidate'
$buildTempRoot = Join-Path ([IO.Path]::GetTempPath()) ('bing-utils-reflection-build-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $buildTempRoot
$cacheOutput = & dotnet nuget locals global-packages --list
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve NuGet package cache.' }
$packageCache = ($cacheOutput | Select-Object -Last 1) -replace '^.*?:\s*', ''

# 在快照中离线还原、同配置构建，确保测量输入不会被工作区后续修改影响。
foreach ($variantInput in @(
        [pscustomobject]@{ label = 'baseline'; snapshot = $baselineSnapshot },
        [pscustomobject]@{ label = 'candidate'; snapshot = $candidateSnapshot })) {
    $snapshot = $variantInput.snapshot
    $project = Join-Path $snapshot 'src/Bing.Utils.Reflection/Bing.Utils.Reflection.csproj'
    $intermediatePath = Join-Path $buildTempRoot "$($variantInput.label)/obj/"
    $buildProperties = @(
        "-p:BaseIntermediateOutputPath=$intermediatePath",
        '-p:UseSharedCompilation=false',
        '-p:BuildInParallel=false')
    Invoke-CheckedDotnet -Arguments (@('restore', $project, "-p:TargetFrameworks=$comparisonTargetFramework") + $buildProperties + @('--source', $packageCache, '--verbosity', 'quiet'))
    Invoke-CheckedDotnet -Arguments (@('build', $project, '--no-restore', '-c', 'Release', '-f', $comparisonTargetFramework, "-p:TargetFrameworks=$comparisonTargetFramework") + $buildProperties + @('-p:CopyLocalLockFileAssemblies=true', '-p:GenerateDocumentationFile=false', '-p:WarningLevel=0', '--verbosity', 'quiet'))
}
$probeProject = Join-Path $PSScriptRoot 'ReflectionComparisonProbe.csproj'
Invoke-CheckedDotnet -Arguments @('build', $probeProject, '-c', 'Release', '-m:1', '-p:UseSharedCompilation=false', '-p:BuildInParallel=false', '--verbosity', 'quiet')
$probePath = Join-Path $PSScriptRoot "bin/Release/$comparisonTargetFramework/ReflectionComparisonProbe.dll"
$variants = @(
    [pscustomobject]@{ label = 'baseline'; root = $baselineSnapshot },
    [pscustomobject]@{ label = 'candidate'; root = $candidateSnapshot }
)
$rows = [Collections.Generic.List[object]]::new()
$metricRows = [Collections.Generic.List[object]]::new()
$modes = @('--steady', '--getter-only', '--path-write-only', '--cold=constructor', '--cold=getter', '--cold=setter', '--cold=signature', '--cold=path-read', '--cold=path-write', '--cold=dynamic')
$expectedMetricScopes = @{
    '--steady' = @(
        'constructor',
        'property getter',
        'property setter',
        'method signature',
        'alternating method signatures',
        'member path read',
        'member path write',
        'member path read explicit options',
        'member path write explicit options',
        'dynamic schema hit',
        'equivalent fresh schema')
    '--getter-only' = @('direct property getter', 'isolated property getter')
    '--path-write-only' = @('isolated member path write', 'isolated member path write explicit options')
    '--cold=constructor' = @('cold constructor')
    '--cold=getter' = @('cold getter')
    '--cold=setter' = @('cold setter')
    '--cold=signature' = @('cold signature')
    '--cold=path-read' = @('cold path-read')
    '--cold=path-write' = @('cold path-write')
    '--cold=dynamic' = @('cold dynamic')
}
$expectedResultScopes = @{}
foreach ($mode in $expectedMetricScopes.Keys) {
    $expectedResultScopes[$mode] = @($expectedMetricScopes[$mode])
}
# 测量总窗口固定为 30 分钟，不包含上面的还原和构建；超时终止当前进程树。
$measurementClock = [Diagnostics.Stopwatch]::StartNew()
function Invoke-ReflectionProbe([string[]]$Arguments) {
    $remainingMs = [int][math]::Max(0, 1800000 - $measurementClock.ElapsedMilliseconds)
    $startInfo = [Diagnostics.ProcessStartInfo]::new('dotnet')
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    foreach ($argument in $Arguments) { $startInfo.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    try {
        if ($remainingMs -gt 0) {
            $null = $process.Start()
            $stdout = $process.StandardOutput.ReadToEndAsync()
            $stderr = $process.StandardError.ReadToEndAsync()
            if ($process.WaitForExit($remainingMs)) {
                $output = $stdout.GetAwaiter().GetResult()
                $errorOutput = $stderr.GetAwaiter().GetResult()
                if ($process.ExitCode -ne 0) { throw "Probe failed: $Arguments; $errorOutput" }
                return @($output -split '\r?\n' | Where-Object { $_.Length -gt 0 })
            }
            $process.Kill($true)
            $process.WaitForExit()
        }
        [ordered]@{ status = 'TIMEOUT'; measurementLimitSeconds = 1800;
            elapsedSeconds = $measurementClock.Elapsed.TotalSeconds; arguments = $Arguments;
            baselineCommit = $baselineCommit; candidateCommit = $candidateCommit } |
            ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'measurement-timeout.json') -Encoding utf8
        throw '反射测量已达到固定 30 分钟窗口，停止且不生成未完成的统计。'
    } finally { $process.Dispose() }
}
foreach ($mode in $modes) {
    for ($round = 1; $round -le $Rounds; $round++) {
        foreach ($variant in $variants) {
            $dllRoot = Join-Path $variant.root "output/release/$comparisonTargetFramework"
            $probeOutput = Invoke-ReflectionProbe -Arguments @($probePath, (Join-Path $dllRoot 'Bing.Utils.dll'), (Join-Path $dllRoot 'Bing.Utils.Reflection.dll'), $mode)
            $probeOutput | Add-Content -LiteralPath (Join-Path $evidenceRoot 'raw.txt') -Encoding utf8
            foreach ($line in $probeOutput) {
                if ($line -match '^# runtime: (?<runtime>[^;]+);') {
                    $probeRuntimeLineCount++
                    $null = $probeRuntimeDescriptions.Add($Matches.runtime.Trim())
                }
                if ($line -match '^(?<scenario>[^,]+),(?<time>[0-9.]+) (?<unit>ns|us)/op,(?<bytes>[0-9.]+) B/op$') {
                    $rows.Add([pscustomobject]@{ round = $round; variant = $variant.label; mode = $mode;
                        scenario = $Matches.scenario; time = $Matches.time; unit = $Matches.unit; bytes = $Matches.bytes })
                }
                if ($line -match '^cache-metrics,(?<payload>\{.*\})$') {
                    try {
                        $metric = $Matches.payload | ConvertFrom-Json -ErrorAction Stop
                    } catch {
                        throw "Probe returned invalid cache metrics JSON: $line"
                    }
                    $metricRows.Add([pscustomobject]@{
                        round = $round; variant = $variant.label; mode = $mode
                        scope = [string]$metric.scope
                        available = [bool]$metric.available
                        coreHits = if ($metric.available) { [long]$metric.coreHits } else { $null }
                        coreMisses = if ($metric.available) { [long]$metric.coreMisses } else { $null }
                        corePending = if ($metric.available) { [long]$metric.corePending } else { $null }
                        coreBypasses = if ($metric.available) { [long]$metric.coreBypasses } else { $null }
                        coreFailures = if ($metric.available) { [long]$metric.coreFailures } else { $null }
                        reflectionHits = if ($metric.available) { [long]$metric.reflectionHits } else { $null }
                        reflectionMisses = if ($metric.available) { [long]$metric.reflectionMisses } else { $null }
                        reflectionPending = if ($metric.available) { [long]$metric.reflectionPending } else { $null }
                        reflectionBypasses = if ($metric.available) { [long]$metric.reflectionBypasses } else { $null }
                        reflectionFailures = if ($metric.available) { [long]$metric.reflectionFailures } else { $null }
                        reflectionStructuralBypasses = if ($metric.available) { [long]$metric.reflectionStructuralBypasses } else { $null }
                        constructorInvokerBuilds = if ($metric.available) { [long]$metric.constructorInvokerBuilds } else { $null }
                        dynamicTypeBuilds = if ($metric.available) { [long]$metric.dynamicTypeBuilds } else { $null }
                        reason = [string]$metric.reason
                    })
                }
            }
        }
    }
}
$rows | Export-Csv -LiteralPath (Join-Path $evidenceRoot 'results.csv') -Encoding utf8 -NoTypeInformation

$expectedProbeRuntimeLines = $Rounds * $variants.Count * $modes.Count
$runtimeContractIssues = [Collections.Generic.List[object]]::new()
if ($probeRuntimeLineCount -ne $expectedProbeRuntimeLines) {
    $runtimeContractIssues.Add([pscustomobject]@{
            expectedLines = $expectedProbeRuntimeLines
            actualLines = $probeRuntimeLineCount
            issue = 'probe runtime header count does not match invocation count'
        })
}
if ($probeRuntimeDescriptions.Count -ne 1) {
    $runtimeContractIssues.Add([pscustomobject]@{
            expectedDescriptions = 1
            actualDescriptions = $probeRuntimeDescriptions.Count
            descriptions = @($probeRuntimeDescriptions | Sort-Object)
            issue = 'baseline and candidate did not use one shared probe runtime description'
        })
}

$resultCompletenessIssues = [Collections.Generic.List[object]]::new()
foreach ($variant in $variants) {
    foreach ($round in 1..$Rounds) {
        foreach ($mode in $modes) {
            $expectedScopes = @($expectedResultScopes[$mode])
            $modeRows = @($rows | Where-Object {
                    $_.variant -eq $variant.label -and $_.round -eq $round -and $_.mode -eq $mode
                })
            foreach ($scenario in $expectedScopes) {
                $matches = @($modeRows | Where-Object { $_.scenario -eq $scenario })
                if ($matches.Count -ne 1) {
                    $resultCompletenessIssues.Add([pscustomobject]@{
                            variant = $variant.label; round = $round; mode = $mode
                            scenario = $scenario; actualRows = $matches.Count
                        })
                }
            }
            foreach ($scenario in @($modeRows | ForEach-Object { [string]$_.scenario } | Sort-Object -Unique)) {
                if ($expectedScopes -notcontains $scenario) {
                    $resultCompletenessIssues.Add([pscustomobject]@{
                            variant = $variant.label; round = $round; mode = $mode
                            scenario = $scenario; actualRows = @($modeRows | Where-Object { $_.scenario -eq $scenario }).Count
                            issue = 'unexpected scenario'
                        })
                }
            }
        }
    }
}
$resultCompletenessManifest = [ordered]@{
    expectedRows = @($expectedResultScopes.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum * $Rounds * $variants.Count
    actualRows = $rows.Count
    complete = $resultCompletenessIssues.Count -eq 0
    completenessIssues = @($resultCompletenessIssues)
}
$resultCompletenessManifest | ConvertTo-Json -Depth 6 |
    Set-Content -LiteralPath (Join-Path $evidenceRoot 'results-completeness.json') -Encoding utf8
if ($runtimeContractIssues.Count -gt 0) {
    throw "性能比较 runtime 契约不完整或不一致；详情见 manifest 相关字段。"
}
if ($resultCompletenessIssues.Count -gt 0) {
    throw "性能比较场景结果不完整或包含未知场景，共 $($resultCompletenessIssues.Count) 项；详情见 results-completeness.json。"
}

function Get-Median([double[]]$values) {
    $ordered = @($values | Sort-Object)
    if ($ordered.Count -eq 0) { return $null }
    $middle = [int]($ordered.Count / 2)
    if (($ordered.Count % 2) -eq 1) { return [double]$ordered[$middle] }
    return ([double]$ordered[$middle - 1] + [double]$ordered[$middle]) / 2
}

# 将逐轮样本压缩为稳定的场景摘要，供 CI 或后续报告直接比较；这里不设置未经批准的性能阈值。
$summaryRows = [Collections.Generic.List[object]]::new()
foreach ($group in ($rows | Group-Object -Property variant, mode, scenario)) {
    $first = @($group.Group)[0]
    $timeValues = @($group.Group | ForEach-Object {
            $value = [double]$_.time
            if ($_.unit -eq 'us') { $value = $value * 1000 }
            $value
        })
    $byteValues = @($group.Group | ForEach-Object { [double]$_.bytes })
    $timeStats = $timeValues | Measure-Object -Minimum -Maximum
    $byteStats = $byteValues | Measure-Object -Minimum -Maximum
    $summaryRows.Add([pscustomobject]@{
            variant = [string]$first.variant
            mode = [string]$first.mode
            scenario = [string]$first.scenario
            rounds = $group.Count
            timeMedianNs = Get-Median $timeValues
            timeMinNs = [double]$timeStats.Minimum
            timeMaxNs = [double]$timeStats.Maximum
            bytesMedian = Get-Median $byteValues
            bytesMin = [double]$byteStats.Minimum
            bytesMax = [double]$byteStats.Maximum
        })
}
$summaryRows = [Collections.Generic.List[object]](@($summaryRows | Sort-Object variant, mode, scenario))
$comparisonRows = [Collections.Generic.List[object]]::new()
foreach ($group in ($summaryRows | Group-Object -Property mode, scenario)) {
    $baseline = @($group.Group | Where-Object { $_.variant -eq 'baseline' })
    $candidate = @($group.Group | Where-Object { $_.variant -eq 'candidate' })
    if ($baseline.Count -ne 1 -or $candidate.Count -ne 1) {
        throw "多轮摘要缺少 baseline/candidate 场景：$($group.Name)"
    }
    $baselineRow = $baseline[0]
    $candidateRow = $candidate[0]
    $timeDelta = [double]$candidateRow.timeMedianNs - [double]$baselineRow.timeMedianNs
    $bytesDelta = [double]$candidateRow.bytesMedian - [double]$baselineRow.bytesMedian
    $timePercent = $null
    if ([double]$baselineRow.timeMedianNs -ne 0) {
        $timePercent = $timeDelta / [double]$baselineRow.timeMedianNs * 100
    }
    $bytesPercent = $null
    if ([double]$baselineRow.bytesMedian -ne 0) {
        $bytesPercent = $bytesDelta / [double]$baselineRow.bytesMedian * 100
    }
    $comparisonRows.Add([pscustomobject]@{
            mode = [string]$candidateRow.mode
            scenario = [string]$candidateRow.scenario
            rounds = [int]$candidateRow.rounds
            baselineTimeMedianNs = [double]$baselineRow.timeMedianNs
            candidateTimeMedianNs = [double]$candidateRow.timeMedianNs
            timeDeltaNs = $timeDelta
            timeDeltaPercent = $timePercent
            baselineBytesMedian = [double]$baselineRow.bytesMedian
            candidateBytesMedian = [double]$candidateRow.bytesMedian
            bytesDelta = $bytesDelta
            bytesDeltaPercent = $bytesPercent
        })
}
$summaryRows | Export-Csv -LiteralPath (Join-Path $evidenceRoot 'summary.csv') -Encoding utf8 -NoTypeInformation
$comparisonRows | Export-Csv -LiteralPath (Join-Path $evidenceRoot 'comparison.csv') -Encoding utf8 -NoTypeInformation
$summaryManifest = [ordered]@{
    rounds = $Rounds
    summaryRows = $summaryRows.Count
    comparisonRows = $comparisonRows.Count
    expectedRows = $comparisonRows.Count * $variants.Count
    complete = $summaryRows.Count -eq ($comparisonRows.Count * $variants.Count)
    thresholdEnabled = $thresholdEnabled
    thresholdSource = $thresholdSource
    policyId = $performancePolicyId
    note = if ($thresholdEnabled) { '已启用显式性能预算；门禁结果见 performance-policy.json。' } else { '摘要仅报告 baseline/candidate 的多轮中位数和变化百分比，不构成性能门禁。' }
}
$summaryManifest | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'summary.json') -Encoding utf8
if (!$summaryManifest.complete) {
    throw "多轮摘要结果不完整，期望 $($summaryManifest.expectedRows) 行，实际 $($summaryRows.Count) 行。"
}
$metricCompletenessIssues = [Collections.Generic.List[object]]::new()
foreach ($variant in $variants) {
    foreach ($round in 1..$Rounds) {
        foreach ($mode in $modes) {
            $expectedScopes = @($expectedMetricScopes[$mode])
            foreach ($scope in $expectedScopes) {
                $matches = @($metricRows | Where-Object {
                        $_.variant -eq $variant.label -and $_.round -eq $round -and
                        $_.mode -eq $mode -and $_.scope -eq $scope
                    })
                if ($matches.Count -ne 1) {
                    $metricCompletenessIssues.Add([pscustomobject]@{
                        variant = $variant.label; round = $round; mode = $mode; scope = $scope; actualRows = $matches.Count
                    })
                }
            }
        }
    }
}
$metricManifest = [ordered]@{
    expectedRows = @($expectedMetricScopes.Values | ForEach-Object { $_.Count } | Measure-Object -Sum).Sum * $Rounds * $variants.Count
    actualRows = $metricRows.Count
    availableRows = @($metricRows | Where-Object available).Count
    complete = $metricCompletenessIssues.Count -eq 0
    completenessIssues = @($metricCompletenessIssues)
}
$metricRows | Export-Csv -LiteralPath (Join-Path $evidenceRoot 'cache-metrics.csv') -Encoding utf8 -NoTypeInformation
$availableByVariant = [ordered]@{}
foreach ($variant in $variants) {
    $availableByVariant[$variant.label] = @($metricRows | Where-Object {
            $_.variant -eq $variant.label -and $_.available
        }).Count
}
$metricManifest.availableByVariant = $availableByVariant
$metricManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'cache-metrics.json') -Encoding utf8
if ($metricCompletenessIssues.Count -gt 0) {
    throw "缓存指标结果不完整，共 $($metricCompletenessIssues.Count) 个 scope 缺失或重复；详情见 cache-metrics.json。"
}
$hasColdTimeBudget = $PSBoundParameters.ContainsKey('ColdStartMaxTimeNs') -or $hasPolicyColdStartMaxTimeNs
$hasColdBytesBudget = $PSBoundParameters.ContainsKey('ColdStartMaxBytes') -or $hasPolicyColdStartMaxBytes
$coldBudgetViolations = [Collections.Generic.List[object]]::new()
$coldModes = @($modes | Where-Object { $_ -like '--cold=*' })
$coldRows = @($rows | Where-Object { $_.mode -like '--cold=*' })
$candidateColdRows = @($coldRows | Where-Object { $_.variant -eq 'candidate' })
$coldCompletenessIssues = [Collections.Generic.List[object]]::new()
foreach ($variant in $variants) {
    foreach ($round in 1..$Rounds) {
        foreach ($mode in $coldModes) {
            $matches = @($coldRows | Where-Object {
                    $_.variant -eq $variant.label -and $_.round -eq $round -and $_.mode -eq $mode
                })
            if ($matches.Count -ne 1) {
                $coldCompletenessIssues.Add([pscustomobject]@{
                    variant = $variant.label; round = $round; mode = $mode; actualRows = $matches.Count
                })
            }
        }
    }
}
foreach ($row in $candidateColdRows) {
    $timeNs = [double]$row.time
    if ($row.unit -eq 'us') { $timeNs *= 1000 }
    if ($hasColdTimeBudget -and $timeNs -gt $ColdStartMaxTimeNs) {
        $coldBudgetViolations.Add([pscustomobject]@{
            variant = $row.variant; mode = $row.mode; scenario = $row.scenario
            metric = 'timeNs'; actual = $timeNs; budget = $ColdStartMaxTimeNs
        })
    }
    $bytes = [double]$row.bytes
    if ($hasColdBytesBudget -and $bytes -gt $ColdStartMaxBytes) {
        $coldBudgetViolations.Add([pscustomobject]@{
            variant = $row.variant; mode = $row.mode; scenario = $row.scenario
            metric = 'bytes'; actual = $bytes; budget = $ColdStartMaxBytes
        })
    }
}
$budgetManifest = [ordered]@{
    enabled = $hasColdTimeBudget -or $hasColdBytesBudget
    maxTimeNs = if ($hasColdTimeBudget) { $ColdStartMaxTimeNs } else { $null }
    maxBytes = if ($hasColdBytesBudget) { $ColdStartMaxBytes } else { $null }
    expectedRows = $coldModes.Count * $Rounds * $variants.Count
    candidateRows = $candidateColdRows.Count
    complete = $coldCompletenessIssues.Count -eq 0
    completenessIssues = @($coldCompletenessIssues)
    passed = $coldCompletenessIssues.Count -eq 0 -and $coldBudgetViolations.Count -eq 0
    violations = @($coldBudgetViolations)
}
$budgetManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'cold-start-budget.json') -Encoding utf8
$hasSteadyTimeBudget = $PSBoundParameters.ContainsKey('SteadyStateMaxTimeNs') -or $hasPolicySteadyStateMaxTimeNs
$hasSteadyBytesBudget = $PSBoundParameters.ContainsKey('SteadyStateMaxBytes') -or $hasPolicySteadyStateMaxBytes
$steadyRows = @($rows | Where-Object { $_.mode -eq '--steady' })
$steadyScenarioNames = @($steadyRows | ForEach-Object { [string]$_.scenario } | Sort-Object -Unique)
$steadyExpectedScenarioNames = @(
    'constructor',
    'property getter',
    'property setter',
    'method signature',
    'alternating method signatures',
    'member path read',
    'member path write',
    'member path read explicit options',
    'member path write explicit options',
    'dynamic schema hit',
    'equivalent fresh schema'
)
$steadyCompletenessIssues = [Collections.Generic.List[object]]::new()
foreach ($scenario in $steadyScenarioNames) {
    if ($scenario -notin $steadyExpectedScenarioNames) {
        $steadyCompletenessIssues.Add([pscustomobject]@{
            variant = '*'; round = 0; scenario = $scenario; actualRows = @($steadyRows | Where-Object { $_.scenario -eq $scenario }).Count
        })
    }
}
foreach ($variant in $variants) {
    foreach ($round in 1..$Rounds) {
        foreach ($scenario in $steadyExpectedScenarioNames) {
            $matches = @($steadyRows | Where-Object {
                    $_.variant -eq $variant.label -and $_.round -eq $round -and $_.scenario -eq $scenario
                })
            if ($matches.Count -ne 1) {
                $steadyCompletenessIssues.Add([pscustomobject]@{
                    variant = $variant.label; round = $round; scenario = $scenario; actualRows = $matches.Count
                })
            }
        }
    }
}
$candidateSteadyRows = @($steadyRows | Where-Object { $_.variant -eq 'candidate' })
$steadyBudgetViolations = [Collections.Generic.List[object]]::new()
foreach ($row in $candidateSteadyRows) {
    $timeNs = [double]$row.time
    if ($row.unit -eq 'us') { $timeNs *= 1000 }
    if ($hasSteadyTimeBudget -and $timeNs -gt $SteadyStateMaxTimeNs) {
        $steadyBudgetViolations.Add([pscustomobject]@{
            variant = $row.variant; mode = $row.mode; scenario = $row.scenario
            metric = 'timeNs'; actual = $timeNs; budget = $SteadyStateMaxTimeNs
        })
    }
    $bytes = [double]$row.bytes
    if ($hasSteadyBytesBudget -and $bytes -gt $SteadyStateMaxBytes) {
        $steadyBudgetViolations.Add([pscustomobject]@{
            variant = $row.variant; mode = $row.mode; scenario = $row.scenario
            metric = 'bytes'; actual = $bytes; budget = $SteadyStateMaxBytes
        })
    }
}
$steadyBudgetManifest = [ordered]@{
    enabled = $hasSteadyTimeBudget -or $hasSteadyBytesBudget
    maxTimeNs = if ($hasSteadyTimeBudget) { $SteadyStateMaxTimeNs } else { $null }
    maxBytes = if ($hasSteadyBytesBudget) { $SteadyStateMaxBytes } else { $null }
    scenarios = @($steadyExpectedScenarioNames)
    observedScenarios = @($steadyScenarioNames)
    expectedRows = $steadyExpectedScenarioNames.Count * $Rounds * $variants.Count
    candidateRows = $candidateSteadyRows.Count
    complete = $steadyCompletenessIssues.Count -eq 0
    completenessIssues = @($steadyCompletenessIssues)
    passed = $steadyCompletenessIssues.Count -eq 0 -and $steadyBudgetViolations.Count -eq 0
    violations = @($steadyBudgetViolations)
}
$steadyBudgetManifest | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'steady-state-budget.json') -Encoding utf8
$performancePolicyPassed = $coldCompletenessIssues.Count -eq 0 -and
    $coldBudgetViolations.Count -eq 0 -and
    $steadyCompletenessIssues.Count -eq 0 -and
    $steadyBudgetViolations.Count -eq 0
$performancePolicyManifest = [ordered]@{
    schemaVersion = 1
    enabled = $thresholdEnabled
    source = $thresholdSource
    policyId = $performancePolicyId
    policyPath = if ($performancePolicyFullPath) { $performancePolicyFullPath } else { $null }
    policyInputSnapshot = if ($performancePolicySnapshotPath) { 'performance-policy-input.json' } else { $null }
    targetFramework = $comparisonTargetFramework
    rounds = $Rounds
    baselineCommit = $baselineCommit
    candidateCommit = $candidateCommit
    passed = $performancePolicyPassed
    coldStart = $budgetManifest
    steadyState = $steadyBudgetManifest
}
$performancePolicyManifest | ConvertTo-Json -Depth 8 |
    Set-Content -LiteralPath (Join-Path $evidenceRoot 'performance-policy.json') -Encoding utf8
$binaryHashes = foreach ($variant in $variants) {
    foreach ($name in @('Bing.Utils.dll', 'Bing.Utils.Reflection.dll')) {
        Get-FileHash -LiteralPath (Join-Path $variant.root "output/release/$comparisonTargetFramework/$name") -Algorithm SHA256
    }
}
$binaryHashes += Get-FileHash -LiteralPath $probePath -Algorithm SHA256
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Program.cs') -Destination (Join-Path $evidenceRoot 'probe-source.cs')
Copy-Item -LiteralPath $probeProject -Destination (Join-Path $evidenceRoot 'probe-project.csproj')
Copy-Item -LiteralPath $PSCommandPath -Destination (Join-Path $evidenceRoot 'comparison-script.ps1')
[pscustomobject]@{ baselineCommit = $baselineCommit; candidateCommit = $candidateCommit; machine = $machineSnapshot;
    probeRuntime = @($probeRuntimeDescriptions | Sort-Object); runtime = (& dotnet --list-runtimes);
    sdk = (& dotnet --version); rounds = $Rounds; binaryHashes = $binaryHashes;
    baselineSource = if ($BaselineRef) { "git-ref:$BaselineRef" } else { 'clean-worktree' };
    probeArguments = $modes; sourceArchives = @('baseline-source.zip', 'candidate-source.zip');
     comparisonContract = [ordered]@{
         targetFramework = $comparisonTargetFramework
         modes = @($modes)
         expectedResultScopes = $expectedResultScopes
         expectedMetricScopes = $expectedMetricScopes
         expectedProbeRuntimeLines = $expectedProbeRuntimeLines
         actualProbeRuntimeLines = $probeRuntimeLineCount
         runtimeDescriptions = @($probeRuntimeDescriptions | Sort-Object)
         runtimeContractPassed = $runtimeContractIssues.Count -eq 0
         resultCompleteness = $resultCompletenessManifest
     };
     cacheMetrics = $metricManifest; summary = $summaryManifest; coldStartBudget = $budgetManifest; steadyStateBudget = $steadyBudgetManifest;
     performancePolicy = $performancePolicyManifest } |
    ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $evidenceRoot 'manifest.json') -Encoding utf8
Remove-TemporaryRoots
if (!$performancePolicyPassed) {
    # 超限也保留源码、程序集身份和 manifest，便于区分代码差异与测量波动。
    if ($coldCompletenessIssues.Count -gt 0) {
        throw "冷启动结果不完整，共 $($coldCompletenessIssues.Count) 个 round/mode 缺失或重复；详情见 cold-start-budget.json。"
    }
    if ($coldBudgetViolations.Count -gt 0) {
        throw "冷启动预算未通过，共 $($coldBudgetViolations.Count) 个超限结果；详情见 cold-start-budget.json。"
    }
    if ($steadyCompletenessIssues.Count -gt 0) {
        throw "稳态结果不完整，共 $($steadyCompletenessIssues.Count) 个 round/scenario 缺失或重复；详情见 steady-state-budget.json。"
    }
    throw "稳态预算未通过，共 $($steadyBudgetViolations.Count) 个超限结果；详情见 steady-state-budget.json。"
}
Write-Output "Evidence saved to $evidenceRoot"
