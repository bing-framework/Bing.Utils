param(
    [string]$InventoryPath = '',
    [string]$OutputPath = ''
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ([string]::IsNullOrWhiteSpace($InventoryPath)) {
    $InventoryPath = Join-Path $repositoryRoot 'docs/quality/reflection-cache-inventory.json'
}
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot 'bin/reflection-cache-inventory.json'
}

function Resolve-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootWithSeparator = $repositoryRoot.TrimEnd([System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase) -and
        ![System.StringComparer]::OrdinalIgnoreCase.Equals($fullPath, $repositoryRoot)) {
        throw "路径必须位于仓库根目录内: $Path"
    }
    return $fullPath
}

$inventoryFile = Resolve-RepositoryPath $InventoryPath
$outputFile = Resolve-RepositoryPath $OutputPath
if (!(Test-Path -LiteralPath $inventoryFile -PathType Leaf)) {
    throw "缓存清单不存在: $InventoryPath"
}

try {
    $inventory = Get-Content -LiteralPath $inventoryFile -Raw -Encoding UTF8 | ConvertFrom-Json
} catch {
    throw "缓存清单不是有效的 UTF-8 JSON: $inventoryFile. $($_.Exception.Message)"
}

$errors = [System.Collections.Generic.List[string]]::new()
$unlistedDeclarations = [System.Collections.Generic.List[object]]::new()
$entryReports = [System.Collections.Generic.List[object]]::new()
$ids = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::Ordinal)

if ($inventory.schemaVersion -ne 1) {
    $errors.Add("不支持的缓存清单 schemaVersion: $($inventory.schemaVersion)")
}
if ([string]::IsNullOrWhiteSpace([string]$inventory.scope)) {
    $errors.Add('缓存清单缺少 scope。')
}
if ($null -eq $inventory.entries -or $inventory.entries.Count -eq 0) {
    $errors.Add('缓存清单必须至少包含一个 entries 项。')
}

foreach ($entry in @($inventory.entries)) {
    $id = [string]$entry.id
    $sourceRelative = [string]$entry.source
    $match = [string]$entry.match
    $sourceFile = $null
    $sourceText = ''
    $entryErrors = [System.Collections.Generic.List[string]]::new()

    if ([string]::IsNullOrWhiteSpace($id)) {
        $entryErrors.Add('缺少 id。')
    } elseif (!$ids.Add($id)) {
        $entryErrors.Add("重复的 id: $id")
    }
    if ([string]::IsNullOrWhiteSpace($sourceRelative)) {
        $entryErrors.Add("$id 缺少 source。")
    } else {
        try {
            $sourceFile = Resolve-RepositoryPath (Join-Path $repositoryRoot $sourceRelative)
        } catch {
            $entryErrors.Add("$id 的 source 不在仓库根目录内: $sourceRelative")
        }
    }
    if ($null -ne $sourceFile -and !(Test-Path -LiteralPath $sourceFile -PathType Leaf)) {
        $entryErrors.Add("$id 的 source 不存在: $sourceRelative")
    } elseif ($null -ne $sourceFile) {
        $sourceText = Get-Content -LiteralPath $sourceFile -Raw -Encoding UTF8
    }
    if ([string]::IsNullOrWhiteSpace($match)) {
        $entryErrors.Add("$id 缺少 match。")
    } elseif ($sourceText -and !$sourceText.Contains($match, [System.StringComparison]::Ordinal)) {
        $entryErrors.Add("$id 的源码声明未找到 match: $match")
    }
    foreach ($guard in @($entry.guards)) {
        $guardText = [string]$guard
        if ([string]::IsNullOrWhiteSpace($guardText)) {
            $entryErrors.Add("$id 含空 guard。")
        } elseif (!$sourceText.Contains($guardText, [System.StringComparison]::Ordinal)) {
            $entryErrors.Add("$id 的源码未找到 guard: $guardText")
        }
    }
    $guardSources = @()
    if ($entry.PSObject.Properties.Name -contains 'guardSources') {
        $guardSources = @($entry.guardSources)
    }
    foreach ($guardSource in $guardSources) {
        $guardSourcePath = [string]$guardSource.source
        $guardSourceMatch = [string]$guardSource.match
        if ([string]::IsNullOrWhiteSpace($guardSourcePath) -or [string]::IsNullOrWhiteSpace($guardSourceMatch)) {
            $entryErrors.Add("$id 含不完整的 guardSources 项。")
            continue
        }
        try {
            $guardFile = Resolve-RepositoryPath (Join-Path $repositoryRoot $guardSourcePath)
            if (!(Test-Path -LiteralPath $guardFile -PathType Leaf)) {
                $entryErrors.Add("$id 的 guard source 不存在: $guardSourcePath")
                continue
            }
            $guardText = Get-Content -LiteralPath $guardFile -Raw -Encoding UTF8
            if (!$guardText.Contains($guardSourceMatch, [System.StringComparison]::Ordinal)) {
                $entryErrors.Add("$id 的 guard source 未找到 match: $guardSourceMatch")
            }
        } catch {
            $entryErrors.Add("$id 的 guard source 无效: $guardSourcePath")
        }
    }

    $entryReports.Add([ordered]@{
        id = $id
        source = $sourceRelative
        matchFound = $entryErrors.Count -eq 0 -or ($sourceText -and $sourceText.Contains($match, [System.StringComparison]::Ordinal))
        errors = @($entryErrors)
        status = if ($entryErrors.Count -eq 0) { 'pass' } else { 'fail' }
    })
    foreach ($entryError in $entryErrors) {
        $errors.Add($entryError)
    }
}

# 源码级扫描防止新增的 ConditionalWeakTable 或命名缓存绕过清单。
$sourceRoots = @(
    (Join-Path $repositoryRoot 'src/Bing.Utils/Bing/Reflection'),
    (Join-Path $repositoryRoot 'src/Bing.Utils.Reflection/Bing/Reflection'),
    (Join-Path $repositoryRoot 'src/Bing.Utils.Reflection/Bing/Dynamic')
)
$manifestEntriesBySource = @{}
foreach ($entry in @($inventory.entries)) {
    $sourceRelative = ([string]$entry.source).Replace('\', '/')
    if (!$manifestEntriesBySource.ContainsKey($sourceRelative)) {
        $manifestEntriesBySource[$sourceRelative] = @()
    }
    $manifestEntriesBySource[$sourceRelative] += $entry
}

foreach ($sourceRoot in $sourceRoots) {
    if (!(Test-Path -LiteralPath $sourceRoot -PathType Container)) { continue }
    foreach ($file in Get-ChildItem -LiteralPath $sourceRoot -Filter '*.cs' -File -Recurse) {
        $relative = $file.FullName.Substring($repositoryRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        $lines = Get-Content -LiteralPath $file.FullName -Encoding UTF8
        for ($lineIndex = 0; $lineIndex -lt $lines.Count; $lineIndex++) {
            $line = [string]$lines[$lineIndex]
            if ($line -notmatch '(?i)\bstatic\b' -or
                $line -notmatch '(?i)(ConditionalWeakTable|Cache|Lookup|Entries|Setters|Getters|Invokers|recentSchema)' -or
                $line -notmatch '=' -or $line -match '=>') {
                continue
            }
            $known = $false
            foreach ($entry in @($manifestEntriesBySource[$relative])) {
                if ($line.Contains([string]$entry.match, [System.StringComparison]::Ordinal)) {
                    $known = $true
                    break
                }
            }
            if (!$known) {
                $unlistedDeclarations.Add([ordered]@{
                    source = $relative
                    line = $lineIndex + 1
                    text = $line.Trim()
                })
            }
        }
    }
}

foreach ($unlisted in $unlistedDeclarations) {
    $errors.Add("未列入缓存清单的静态缓存声明: $($unlisted.source):$($unlisted.line)")
}

$report = [ordered]@{
    schemaVersion = 1
    checkedAtUtc = [DateTime]::UtcNow.ToString('O')
    inventory = $InventoryPath
    entryCount = @($inventory.entries).Count
    entries = @($entryReports)
    unlistedDeclarations = @($unlistedDeclarations)
    errors = @($errors)
    passed = $errors.Count -eq 0
}

$outputDirectory = Split-Path -Parent $outputFile
$null = New-Item -ItemType Directory -Path $outputDirectory -Force
$report | ConvertTo-Json -Depth 8 | Out-File -LiteralPath $outputFile -Encoding utf8
Write-Output ("reflection-cache-inventory: entries=$($report.entryCount), unlisted=$($unlistedDeclarations.Count), passed=$($report.passed)")
Write-Output ("reflection-cache-inventory-report: $outputFile")

if (!$report.passed) {
    throw "反射缓存清单校验失败，共 $($errors.Count) 个问题；详情见 $outputFile。"
}
