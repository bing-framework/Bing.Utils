$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$solutionPath = Join-Path $repositoryRoot 'Bing.Utils.sln'
$baselinePath = Join-Path $PSScriptRoot 'nuget-vulnerability-baseline.json'

$lines = & dotnet list $solutionPath package --vulnerable --include-transitive --format json
if ($LASTEXITCODE -ne 0) {
    throw "NuGet 审计命令失败，退出码 $LASTEXITCODE。"
}

$report = ($lines -join [Environment]::NewLine) | ConvertFrom-Json
if ($report.version -ne 1 -or $null -eq $report.projects -or @($report.projects).Count -eq 0) {
    throw 'NuGet 审计报告没有包含有效项目。'
}
if ($null -ne $report.problems -and @($report.problems).Count -gt 0) {
    throw 'NuGet 审计报告包含数据源或扫描错误。'
}
if (-not (Test-Path -LiteralPath $baselinePath -PathType Leaf)) {
    throw "NuGet 漏洞基线不存在：$baselinePath"
}

$baseline = Get-Content -LiteralPath $baselinePath -Raw -Encoding utf8 | ConvertFrom-Json
if ($baseline.version -ne 1 -or $null -eq $baseline.vulnerabilities) {
    throw "NuGet 漏洞基线格式无效：$baselinePath"
}

function Get-VulnerabilityKey {
    param(
        [string]$Project,
        [string]$Framework,
        [string]$Package,
        [string]$Version,
        [string]$Severity,
        [string]$AdvisoryUrl
    )
    return "$Project|$Framework|$Package|$Version|$Severity|$AdvisoryUrl"
}

$known = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($item in $baseline.vulnerabilities) {
    [void]$known.Add((Get-VulnerabilityKey $item.project $item.framework $item.package $item.version $item.severity $item.advisoryUrl))
}

$current = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$newlyBlocked = [Collections.Generic.List[string]]::new()
foreach ($project in $report.projects) {
    $projectPath = [IO.Path]::GetFullPath($project.path)
    $relativeProject = [IO.Path]::GetRelativePath($repositoryRoot, $projectPath).Replace([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    foreach ($framework in $project.frameworks) {
        foreach ($package in (@($framework.topLevelPackages) + @($framework.transitivePackages))) {
            if ($null -eq $package) { continue }
            foreach ($vulnerability in $package.vulnerabilities) {
                if ($vulnerability.severity -notin @('Low', 'Moderate', 'High', 'Critical')) {
                    throw "NuGet 审计报告包含未知漏洞级别：$($vulnerability.severity)。"
                }
                if ($vulnerability.severity -notin @('High', 'Critical')) { continue }

                $key = Get-VulnerabilityKey $relativeProject $framework.framework $package.id $package.resolvedVersion $vulnerability.severity $vulnerability.advisoryurl
                [void]$current.Add($key)
                if (-not $known.Contains($key)) {
                    $newlyBlocked.Add($key)
                }
            }
        }
    }
}

if ($newlyBlocked.Count -gt 0) {
    throw ("发现未登记的高危或严重 NuGet 漏洞：" + [Environment]::NewLine + (($newlyBlocked | Sort-Object -Unique) -join [Environment]::NewLine))
}

$resolved = @($known | Where-Object { -not $current.Contains($_) } | Sort-Object)
if ($resolved.Count -gt 0) {
    Write-Output ("已有 {0} 条基线漏洞不再出现，请从基线删除：" -f $resolved.Count)
    $resolved | ForEach-Object { Write-Output "  $_" }
}

Write-Output ("NuGet 全解决方案审计通过：未新增高危或严重漏洞；当前历史基线 {0} 条。" -f $current.Count)