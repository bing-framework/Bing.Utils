[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$LogPath,
    [Parameter(Mandatory = $true)][string]$BaselinePath,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [switch]$FailOnNewWarning,
    [string]$DiagnosticBaselinePath,
    [string]$DiagnosticContext,
    [string]$SymbolMapPath,
    [switch]$FailOnNewRepositoryDiagnostic,
    [string]$DiagnosticAllowlistPath,
    [switch]$FailOnUnapprovedRepositoryDiagnostic,
    [string]$NonRepositoryOwnershipPath,
    [switch]$FailOnUnownedNonRepositoryDiagnostic,
    [switch]$FailOnUnparsedDiagnostic
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

if (!(Test-Path -LiteralPath $LogPath -PathType Leaf)) {
    throw "NativeAOT log was not found: $LogPath"
}
if (!(Test-Path -LiteralPath $BaselinePath -PathType Leaf)) {
    throw "NativeAOT warning baseline was not found: $BaselinePath"
}
if ($FailOnNewRepositoryDiagnostic -and [string]::IsNullOrWhiteSpace($DiagnosticBaselinePath)) {
    throw 'FailOnNewRepositoryDiagnostic requires DiagnosticBaselinePath.'
}
if ($DiagnosticBaselinePath -and !(Test-Path -LiteralPath $DiagnosticBaselinePath -PathType Leaf)) {
    throw "Diagnostic baseline report was not found: $DiagnosticBaselinePath"
}
if ($DiagnosticBaselinePath -and [string]::IsNullOrWhiteSpace($DiagnosticContext)) {
    throw 'DiagnosticBaselinePath requires DiagnosticContext so reports from different publish configurations cannot be compared.'
}
if ($FailOnUnapprovedRepositoryDiagnostic -and [string]::IsNullOrWhiteSpace($DiagnosticAllowlistPath)) {
    throw 'FailOnUnapprovedRepositoryDiagnostic requires DiagnosticAllowlistPath.'
}
if ($FailOnUnownedNonRepositoryDiagnostic -and [string]::IsNullOrWhiteSpace($NonRepositoryOwnershipPath)) {
    throw 'FailOnUnownedNonRepositoryDiagnostic requires NonRepositoryOwnershipPath.'
}
if ($SymbolMapPath -and !(Test-Path -LiteralPath $SymbolMapPath -PathType Leaf)) {
    throw "NativeAOT source map was not found: $SymbolMapPath"
}
if ($DiagnosticAllowlistPath -and !(Test-Path -LiteralPath $DiagnosticAllowlistPath -PathType Leaf)) {
    throw "NativeAOT diagnostic allowlist was not found: $DiagnosticAllowlistPath"
}
if ($NonRepositoryOwnershipPath -and !(Test-Path -LiteralPath $NonRepositoryOwnershipPath -PathType Leaf)) {
    throw "NativeAOT non-repository ownership manifest was not found: $NonRepositoryOwnershipPath"
}
$normalizedDiagnosticContext = if ([string]::IsNullOrWhiteSpace($DiagnosticContext)) {
    $null
} else {
    $DiagnosticContext.Trim()
}

function Get-WarningIds([string]$Path) {
    $ids = foreach ($line in (Get-Content -LiteralPath $Path -Encoding utf8)) {
        [regex]::Matches($line, '\b(?:IL|ILC)\d{4}\b') |
            ForEach-Object { $_.Value.ToUpperInvariant() }
    }
    @($ids | Sort-Object -Unique)
}

function Get-PathClassification([string]$Path, [string]$RootPath) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return [pscustomobject]@{
            normalized = $null
            origin = 'unknown'
            evidence = 'missing-location'
        }
    }

    $rawPath = $Path.Trim()
    $looksLikeFilePath = [IO.Path]::IsPathRooted($rawPath) -or
        $rawPath -match '(?i)(?:^|[\\/])[^\\/]+\.(?:cs|csproj|props|targets|dll|exe|json|xml|resx|sln)(?:$|[):])'
    if (!$looksLikeFilePath) {
        return [pscustomobject]@{
            normalized = $null
            origin = 'unknown'
            evidence = 'non-file-location'
        }
    }

    try {
        $fullRoot = [IO.Path]::GetFullPath($RootPath).TrimEnd('\', '/')
        $fullPath = if ([IO.Path]::IsPathRooted($rawPath)) {
            [IO.Path]::GetFullPath($rawPath)
        } else {
            [IO.Path]::GetFullPath((Join-Path $fullRoot $rawPath))
        }
        $rootPrefix = $fullRoot + [IO.Path]::DirectorySeparatorChar
        $isInsideRepository = $fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)
        if (!$isInsideRepository) {
            return [pscustomobject]@{
                normalized = $fullPath.Replace('\', '/')
                origin = 'externalDependencyOrToolchain'
                evidence = 'absolute-path-outside-repository'
            }
        }

        $relative = $fullPath.Substring($fullRoot.Length + 1).Replace('\', '/')
        if ($relative -match '(^|/)(?:bin|obj|output|artifacts|BenchmarkDotNet\.Artifacts|TestResults)(?:/|$)') {
            return [pscustomobject]@{
                normalized = $relative
                origin = 'repositoryGenerated'
                evidence = 'generated-path-under-repository'
            }
        }

        return [pscustomobject]@{
            normalized = $relative
            origin = 'repositorySource'
            evidence = 'file-under-repository'
        }
    } catch {
        return [pscustomobject]@{
            normalized = $null
            origin = 'unknown'
            evidence = 'path-normalization-failed'
        }
    }
}

function Normalize-ReportPath([string]$Path, [string]$RootPath) {
    return (Get-PathClassification $Path $RootPath).normalized
}

function Get-MessageFingerprint([string]$Message) {
    $normalized = [regex]::Replace($Message, '\s+', ' ').Trim()
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $bytes = [Text.Encoding]::UTF8.GetBytes($normalized)
        return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '').ToLowerInvariant()
    } finally {
        $sha.Dispose()
    }
}

function Get-RepositoryDiagnosticKey([string]$Code, [string]$MessageFingerprint, [string]$File) {
    $normalizedFile = if ([string]::IsNullOrWhiteSpace($File)) { '<unknown-file>' } else { $File }
    return "$Code|$MessageFingerprint|$normalizedFile"
}

function Get-NonRepositoryDiagnosticKey([string]$Code, [string]$MessageFingerprint, [string]$Origin) {
    return "$Code|$MessageFingerprint|$Origin"
}

function Normalize-SymbolPart([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    $builder = [Text.StringBuilder]::new()
    $genericDepth = 0
    foreach ($character in $Value.ToCharArray()) {
        if ($character -eq '<') {
            $genericDepth++
            continue
        }
        if ($character -eq '>') {
            if ($genericDepth -gt 0) {
                $genericDepth--
            }
            continue
        }
        if ($genericDepth -gt 0) {
            continue
        }
        $null = $builder.Append($character)
    }

    return $builder.ToString().Replace('+', '.') -replace '`\d+', ''
}

function Normalize-SymbolSignature([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $null
    }
    return ([regex]::Replace($Value, '\s+', '')).Trim()
}

function Get-DiagnosticSymbolDetails([string]$Message) {
    if ([string]::IsNullOrWhiteSpace($Message)) {
        return $null
    }

    $separator = $Message.IndexOf(':')
    if ($separator -lt 0) {
        return $null
    }
    $head = $Message.Substring(0, $separator).Trim()
    $open = $head.IndexOf('(')
    if ($open -le 0) {
        return $null
    }
    $methodPart = $head.Substring(0, $open)
    $lastDot = $methodPart.LastIndexOf('.')
    $methodName = if ($lastDot -ge 0) {
        $methodPart.Substring($lastDot + 1)
    } else {
        $methodPart
    }
    [pscustomobject]@{
        key = Normalize-SymbolPart $methodPart
        signature = Normalize-SymbolSignature ($methodName + $head.Substring($open))
    }
}

function Get-DiagnosticSymbolKey([string]$Message) {
    $details = Get-DiagnosticSymbolDetails $Message
    if ($null -eq $details) {
        return $null
    }
    return $details.key
}

$symbolMap = @{}
$symbolMapSchemaVersion = $null
if ($SymbolMapPath) {
    try {
        $symbolMapReport = Get-Content -LiteralPath $SymbolMapPath -Encoding utf8 -Raw | ConvertFrom-Json
    } catch {
        throw "NativeAOT source map is not valid JSON: $SymbolMapPath"
    }
    if ($null -eq $symbolMapReport.entries) {
        throw "NativeAOT source map does not contain entries: $SymbolMapPath"
    }
    $symbolMapSchemaVersion = $symbolMapReport.schemaVersion
    foreach ($entry in @($symbolMapReport.entries)) {
        if ([string]::IsNullOrWhiteSpace([string]$entry.key)) {
            continue
        }
        $symbolMap[[string]$entry.key] = $entry
    }
}

$diagnosticAllowlist = @()
$diagnosticAllowlistSchemaVersion = $null
if ($DiagnosticAllowlistPath) {
    try {
        $diagnosticAllowlistReport = Get-Content -LiteralPath $DiagnosticAllowlistPath -Encoding utf8 -Raw | ConvertFrom-Json
    } catch {
        throw "NativeAOT diagnostic allowlist is not valid JSON: $DiagnosticAllowlistPath"
    }
    if ($null -eq $diagnosticAllowlistReport.entries) {
        throw "NativeAOT diagnostic allowlist does not contain entries: $DiagnosticAllowlistPath"
    }
    $diagnosticAllowlistSchemaVersion = $diagnosticAllowlistReport.schemaVersion
    $seenAllowlistKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($diagnosticAllowlistReport.entries)) {
        $code = ([string]$entry.code).Trim().ToUpperInvariant()
        $fingerprint = ([string]$entry.messageFingerprint).Trim().ToLowerInvariant()
        $file = ([string]$entry.file).Trim().Replace('\', '/')
        $owner = ([string]$entry.owner).Trim()
        $reason = ([string]$entry.reason).Trim()
        if ($code -notmatch '^(?:IL|ILC)\d{4}$') {
            throw "NativeAOT diagnostic allowlist contains an invalid code: $code"
        }
        if ($fingerprint -notmatch '^[0-9a-f]{64}$') {
            throw "NativeAOT diagnostic allowlist contains an invalid message fingerprint for $code."
        }
        if ([string]::IsNullOrWhiteSpace($file) -or $file.StartsWith('<', [StringComparison]::Ordinal)) {
            throw "NativeAOT diagnostic allowlist requires a repository source file for $code."
        }
        $fileInfo = Get-PathClassification $file ([IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
        if ($fileInfo.origin -ne 'repositorySource' -or $fileInfo.normalized -ne $file) {
            throw "NativeAOT diagnostic allowlist file is not a normalized repository source path: $file"
        }
        $maxCount = if ($null -eq $entry.maxCount) { 1 } else { [int]$entry.maxCount }
        if ($maxCount -lt 1) {
            throw "NativeAOT diagnostic allowlist maxCount must be positive for $code/$file."
        }
        if ([string]::IsNullOrWhiteSpace($owner)) {
            throw "NativeAOT diagnostic allowlist requires a non-empty owner for $code/$file."
        }
        if ([string]::IsNullOrWhiteSpace($reason)) {
            throw "NativeAOT diagnostic allowlist requires a non-empty reason for $code/$file."
        }
        if ($owner.Length -gt 128) {
            throw "NativeAOT diagnostic allowlist owner is too long for $code/$file."
        }
        if ($reason.Length -gt 2048) {
            throw "NativeAOT diagnostic allowlist reason is too long for $code/$file."
        }
        $key = Get-RepositoryDiagnosticKey $code $fingerprint $file
        if (!$seenAllowlistKeys.Add($key)) {
            throw "NativeAOT diagnostic allowlist contains a duplicate entry: $key"
        }
        $diagnosticAllowlist += [pscustomobject][ordered]@{
            key = $key
            code = $code
            messageFingerprint = $fingerprint
            file = $file
            maxCount = $maxCount
            owner = $owner
            reason = $reason
        }
    }
}

$nonRepositoryOwnership = @()
$nonRepositoryOwnershipSchemaVersion = $null
$nonRepositoryOwnershipContext = $null
if ($NonRepositoryOwnershipPath) {
    try {
        $ownershipReport = Get-Content -LiteralPath $NonRepositoryOwnershipPath -Encoding utf8 -Raw | ConvertFrom-Json
    } catch {
        throw "NativeAOT non-repository ownership manifest is not valid JSON: $NonRepositoryOwnershipPath"
    }
    if ($ownershipReport.schemaVersion -ne 1 -or $null -eq $ownershipReport.entries) {
        throw "NativeAOT non-repository ownership manifest must use schema version 1 with entries: $NonRepositoryOwnershipPath"
    }
    $nonRepositoryOwnershipSchemaVersion = [int]$ownershipReport.schemaVersion
    if ($null -ne $ownershipReport.diagnosticContext) {
        $nonRepositoryOwnershipContext = ([string]$ownershipReport.diagnosticContext).Trim()
        if (![string]::IsNullOrWhiteSpace($normalizedDiagnosticContext) -and
            $nonRepositoryOwnershipContext -ne $normalizedDiagnosticContext) {
            throw "NativeAOT non-repository ownership context does not match the current context. Manifest='$nonRepositoryOwnershipContext'; Current='$normalizedDiagnosticContext'."
        }
    }
    $seenOwnershipKeys = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in @($ownershipReport.entries)) {
        $code = ([string]$entry.code).Trim().ToUpperInvariant()
        $fingerprint = ([string]$entry.messageFingerprint).Trim().ToLowerInvariant()
        $origin = ([string]$entry.origin).Trim()
        $owner = ([string]$entry.owner).Trim()
        $reason = ([string]$entry.reason).Trim()
        $action = ([string]$entry.action).Trim()
        $maxCount = 0
        $maxCountValid = [int]::TryParse([string]$entry.maxCount, [Globalization.NumberStyles]::Integer,
            [Globalization.CultureInfo]::InvariantCulture, [ref]$maxCount)
        if ($code -notmatch '^(?:IL|ILC)\d{4}$') {
            throw "NativeAOT non-repository ownership manifest contains an invalid code: $code"
        }
        if ($fingerprint -notmatch '^[0-9a-f]{64}$') {
            throw "NativeAOT non-repository ownership manifest contains an invalid message fingerprint for $code."
        }
        if ($origin -notin @('repositoryGenerated', 'externalDependencyOrToolchain', 'unknown')) {
            throw "NativeAOT non-repository ownership manifest contains an invalid origin for ${code}: $origin"
        }
        if (!$maxCountValid -or $maxCount -lt 1) {
            throw "NativeAOT non-repository ownership maxCount must be positive for $code/$origin."
        }
        if ([string]::IsNullOrWhiteSpace($owner)) {
            throw "NativeAOT non-repository ownership requires a non-empty owner for $code/$origin."
        }
        if ([string]::IsNullOrWhiteSpace($reason)) {
            throw "NativeAOT non-repository ownership requires a non-empty reason for $code/$origin."
        }
        if ([string]::IsNullOrWhiteSpace($action)) {
            throw "NativeAOT non-repository ownership requires a non-empty action for $code/$origin."
        }
        if ($owner.Length -gt 128 -or $reason.Length -gt 2048 -or $action.Length -gt 2048) {
            throw "NativeAOT non-repository ownership metadata is too long for $code/$origin."
        }
        $key = Get-NonRepositoryDiagnosticKey $code $fingerprint $origin
        if (!$seenOwnershipKeys.Add($key)) {
            throw "NativeAOT non-repository ownership manifest contains a duplicate entry: $key"
        }
        $nonRepositoryOwnership += [pscustomobject][ordered]@{
            key = $key
            code = $code
            messageFingerprint = $fingerprint
            origin = $origin
            maxCount = $maxCount
            owner = $owner
            reason = $reason
            action = $action
        }
    }
}

$script:CompilerDiagnosticPattern = '^(?<location>.+?)(?:\((?<line>\d+)(?:,(?<column>\d+))?\))?:\s+(?:(?<stage>Trim|AOT) analysis\s+)?(?<severity>warning|error)\s+(?<code>(?:IL|ILC)\d{4}):\s+(?<message>.*?)(?:\s+\[(?<project>.+)\])?$'
$script:NativeAotDiagnosticPattern = '^ILC\s*:\s*(?:(?<stage>Trim|AOT) analysis\s+)?(?<severity>warning|error)\s+(?<code>(?:IL|ILC)\d{4}):\s+(?<message>.*?)(?:\s+\[(?<project>.+)\])?$'

function Get-DiagnosticDetails([string[]]$Lines, [string]$RootPath, [hashtable]$SourceMap) {
    $details = foreach ($lineText in $Lines) {
        $match = [regex]::Match($lineText, $script:NativeAotDiagnosticPattern)
        $tool = 'ILC'
        if (!$match.Success) {
            $match = [regex]::Match($lineText, $script:CompilerDiagnosticPattern)
            $tool = 'ILLink'
        }
        if (!$match.Success) {
            continue
        }
        if ($match.Groups['stage'].Success) {
            $tool = 'ILC'
        }

        $rawLocation = if ($match.Groups['location'].Success) {
            $match.Groups['location'].Value.Trim()
        } else { $null }
        $symbolDetails = Get-DiagnosticSymbolDetails $match.Groups['message'].Value
        $symbolKey = if ($null -eq $symbolDetails) { $null } else { $symbolDetails.key }
        $symbolSignature = if ($null -eq $symbolDetails) { $null } else { $symbolDetails.signature }
        $symbolMapEntry = if ([string]::IsNullOrWhiteSpace($rawLocation) -and
            $SourceMap -and $symbolKey -and $SourceMap.ContainsKey($symbolKey)) {
            $SourceMap[$symbolKey]
        } else { $null }
        $symbolMapMatch = $null
        $mappedLocation = $null
        if ($symbolMapEntry -and ![string]::IsNullOrWhiteSpace([string]$symbolMapEntry.file)) {
            $mappedLocation = [string]$symbolMapEntry.file
            $symbolMapMatch = 'base'
        } elseif ($symbolMapEntry -and $symbolSignature -and $symbolMapEntry.overloads) {
            $signatureMatches = @($symbolMapEntry.overloads | Where-Object {
                (Normalize-SymbolSignature ([string]$_.signature)) -eq $symbolSignature
            })
            $signatureFiles = @($signatureMatches |
                Where-Object { ![string]::IsNullOrWhiteSpace([string]$_.file) } |
                ForEach-Object { [string]$_.file } |
                Sort-Object -Unique)
            if ($signatureFiles.Count -eq 1) {
                $mappedLocation = $signatureFiles[0]
                $symbolMapMatch = 'signature'
            }
        }
        $locationInfo = if ($mappedLocation) {
            Get-PathClassification $mappedLocation $RootPath
        } else {
            Get-PathClassification $rawLocation $RootPath
        }
        $locationEvidence = if ($mappedLocation) {
            "pdb-symbol-map:$($locationInfo.evidence)"
        } elseif ($symbolMapEntry) {
            'pdb-symbol-map-ambiguous'
        } else {
            $locationInfo.evidence
        }
        $projectText = $match.Groups['project'].Value
        $rawProject = if ([string]::IsNullOrWhiteSpace($projectText)) { $null } else { $projectText.Trim() }
        $projectPath = $rawProject
        $projectContext = $null
        $targetFramework = $null
        $separator = if ($null -eq $rawProject) { -1 } else {
            $rawProject.IndexOf('::', [StringComparison]::Ordinal)
        }
        if ($separator -ge 0) {
            $projectPath = $rawProject.Substring(0, $separator).Trim()
            $projectContext = $rawProject.Substring($separator + 2).Trim()
            if ($projectContext -match '^TargetFramework=(?<tfm>[^;]+)$') {
                $targetFramework = $Matches.tfm.Trim()
            } elseif ($projectContext -match '^(?<tfm>net(?:standard|coreapp|[0-9])[^;]*)$') {
                $targetFramework = $Matches.tfm.Trim()
            }
        }
        $projectInfo = Get-PathClassification $projectPath $RootPath

        [pscustomobject][ordered]@{
            code = $match.Groups['code'].Value.ToUpperInvariant()
            tool = $tool
            severity = if ($match.Groups['severity'].Success) {
                $match.Groups['severity'].Value.ToLowerInvariant()
            } else { 'warning' }
            rawLocation = $rawLocation
            file = $locationInfo.normalized
            line = if ($match.Groups['line'].Success) { [int]$match.Groups['line'].Value } else { $null }
            column = if ($match.Groups['column'].Success) { [int]$match.Groups['column'].Value } else { $null }
            origin = $locationInfo.origin
            originEvidence = $locationEvidence
            symbol = $symbolKey
            symbolSignature = $symbolSignature
            symbolMapFile = $mappedLocation
            symbolMapMatch = $symbolMapMatch
            symbolMapEvidence = if ($mappedLocation) { 'mapped' } elseif ($symbolMapEntry) { 'ambiguous' } else { 'not-found' }
            rawProject = $rawProject
            project = $projectInfo.normalized
            projectOrigin = $projectInfo.origin
            projectOriginEvidence = $projectInfo.evidence
            projectContext = $projectContext
            targetFramework = $targetFramework
            message = $match.Groups['message'].Value.Trim()
            messageFingerprint = Get-MessageFingerprint $match.Groups['message'].Value
        }
    }
    @($details)
}

$actual = Get-WarningIds $LogPath
$repoRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$logLines = @(Get-Content -LiteralPath $LogPath -Encoding utf8)
$diagnostics = @(Get-DiagnosticDetails $logLines $repoRoot $symbolMap)
$diagnosticLines = @($logLines | Where-Object { $_ -match '\b(?:IL|ILC)\d{4}\s*:' })
$unparsedDiagnostics = [Collections.Generic.List[object]]::new()
foreach ($line in $diagnosticLines) {
    if ($line -notmatch $script:NativeAotDiagnosticPattern -and
        $line -notmatch $script:CompilerDiagnosticPattern) {
        $unparsedDiagnostics.Add([ordered]@{
            rawLine = $line
            warningIds = @(
                [regex]::Matches($line, '\b(?:IL|ILC)\d{4}\b') |
                    ForEach-Object { $_.Value.ToUpperInvariant() } | Sort-Object -Unique
            )
            origin = 'unknown'
            originEvidence = 'unparsed-diagnostic-line'
        })
    }
}
$originCounts = [ordered]@{}
$warningIdsByOrigin = [ordered]@{}
foreach ($group in @($diagnostics | Group-Object -Property origin)) {
    $items = @($group.Group)
    $originCounts[$group.Name] = $items.Count
    $warningIdsByOrigin[$group.Name] = @($items | ForEach-Object { $_.code } | Sort-Object -Unique)
}
$warningOccurrenceCount = $diagnosticLines.Count
$baseline = @(Get-Content -LiteralPath $BaselinePath -Encoding utf8 |
        ForEach-Object { $_.Trim().ToUpperInvariant() } |
        Where-Object { $_ -match '^(?:IL|ILC)\d{4}$' } |
        Sort-Object -Unique)
$new = @($actual | Where-Object { $_ -notin $baseline })
$resolved = @($baseline | Where-Object { $_ -notin $actual })
$repositorySourceDiagnostics = @($diagnostics | Where-Object { $_.origin -eq 'repositorySource' })
$repositoryDiagnosticRecords = @($repositorySourceDiagnostics | ForEach-Object {
    $file = if ([string]::IsNullOrWhiteSpace($_.file)) { '<unknown-file>' } else { $_.file }
    [pscustomobject]@{
        key = Get-RepositoryDiagnosticKey $_.code $_.messageFingerprint $file
        code = $_.code
        file = $_.file
        messageFingerprint = $_.messageFingerprint
    }
})
$nonRepositoryDiagnosticRecords = @($diagnostics | Where-Object { $_.origin -ne 'repositorySource' } | ForEach-Object {
    [pscustomobject]@{
        key = Get-NonRepositoryDiagnosticKey $_.code $_.messageFingerprint $_.origin
        code = $_.code
        origin = $_.origin
        file = $_.file
        messageFingerprint = $_.messageFingerprint
    }
})
$repositoryDiagnosticComparisonEnabled = ![string]::IsNullOrWhiteSpace($DiagnosticBaselinePath)
$baselineRepositoryDiagnosticRecords = @()
$baselineRepositoryDiagnosticCounts = @{}
$diagnosticBaselineFormat = $null
if ($repositoryDiagnosticComparisonEnabled) {
    try {
        $baselineReport = Get-Content -LiteralPath $DiagnosticBaselinePath -Encoding utf8 -Raw | ConvertFrom-Json
    } catch {
        throw "Diagnostic baseline report is not valid JSON: $DiagnosticBaselinePath"
    }
    $baselineDiagnosticContext = if ($null -eq $baselineReport.diagnosticContext) {
        $null
    } else {
        [string]$baselineReport.diagnosticContext
    }
    if ([string]::IsNullOrWhiteSpace($baselineDiagnosticContext)) {
        throw "Diagnostic baseline report does not contain diagnosticContext: $DiagnosticBaselinePath"
    }
    if ($baselineDiagnosticContext.Trim() -ne $normalizedDiagnosticContext) {
        throw "Diagnostic baseline context does not match the current context. Baseline='$($baselineDiagnosticContext.Trim())'; Current='$normalizedDiagnosticContext'."
    }
    if ($baselineReport.kind -eq 'nativeaot-repository-diagnostic-baseline') {
        if ($baselineReport.schemaVersion -ne 1 -or $null -eq $baselineReport.entries) {
            throw "NativeAOT fingerprint baseline must use schema version 1 with entries: $DiagnosticBaselinePath"
        }
        $diagnosticBaselineFormat = 'repository-fingerprint'
        $seenBaselineKeys = @{}
        $baselineEntries = @($baselineReport.entries)
        $declaredEntryCount = 0
        $declaredEntryCountValid = [int]::TryParse([string]$baselineReport.entryCount, [Globalization.NumberStyles]::Integer,
            [Globalization.CultureInfo]::InvariantCulture, [ref]$declaredEntryCount)
        $declaredOccurrenceCount = 0L
        $declaredOccurrenceCountValid = [long]::TryParse([string]$baselineReport.repositoryDiagnosticOccurrenceCount,
            [Globalization.NumberStyles]::Integer, [Globalization.CultureInfo]::InvariantCulture, [ref]$declaredOccurrenceCount)
        if (!$declaredEntryCountValid -or $declaredEntryCount -le 0 -or
            !$declaredOccurrenceCountValid -or $declaredOccurrenceCount -le 0 -or
            $declaredEntryCount -ne $baselineEntries.Count) {
            throw "NativeAOT fingerprint baseline has inconsistent entry or occurrence counts: $DiagnosticBaselinePath"
        }
        $compactOccurrenceCount = 0L
        foreach ($entry in $baselineEntries) {
            $code = ([string]$entry.code).Trim().ToUpperInvariant()
            $file = ([string]$entry.file).Trim().Replace('\', '/')
            $fingerprint = ([string]$entry.messageFingerprint).Trim().ToLowerInvariant()
            $maxCount = 0
            $maxCountValid = [int]::TryParse([string]$entry.maxCount, [Globalization.NumberStyles]::Integer,
                [Globalization.CultureInfo]::InvariantCulture, [ref]$maxCount)
            $invalidEntry = ($code -notmatch '^(?:IL|ILC)\d{4}$') -or
                [string]::IsNullOrWhiteSpace($file) -or
                [IO.Path]::IsPathRooted($file) -or
                ($file -match '(^|/)\.\.(?:/|$)') -or
                ($fingerprint -notmatch '^[0-9a-f]{64}$') -or
                !$maxCountValid -or
                $maxCount -le 0
            if ($invalidEntry) {
                throw "NativeAOT fingerprint baseline contains an invalid entry: $($entry | ConvertTo-Json -Compress)"
            }
            $key = Get-RepositoryDiagnosticKey $code $fingerprint $file
            if ($seenBaselineKeys.ContainsKey($key)) {
                throw "NativeAOT fingerprint baseline contains a duplicate entry: $key"
            }
            $seenBaselineKeys[$key] = $true
            if ($compactOccurrenceCount -gt [long]::MaxValue - $maxCount) {
                throw "NativeAOT fingerprint baseline occurrence count exceeds Int64 capacity: $DiagnosticBaselinePath"
            }
            $compactOccurrenceCount += $maxCount
            $baselineRepositoryDiagnosticCounts[$key] = $maxCount
        }
        if ($compactOccurrenceCount -ne $declaredOccurrenceCount) {
            throw "NativeAOT fingerprint baseline occurrence count does not match maxCount totals: $DiagnosticBaselinePath"
        }
    } else {
        if ($null -eq $baselineReport.diagnostics) {
            throw "Diagnostic baseline report does not contain diagnostics or a fingerprint baseline: $DiagnosticBaselinePath"
        }
        $diagnosticBaselineFormat = 'full-report'
        $baselineRepositoryDiagnosticRecords = @($baselineReport.diagnostics |
            Where-Object { $_.origin -eq 'repositorySource' } |
            ForEach-Object {
                $file = if ([string]::IsNullOrWhiteSpace($_.file)) { '<unknown-file>' } else { $_.file }
                [pscustomobject]@{
                    key = Get-RepositoryDiagnosticKey $_.code $_.messageFingerprint $file
                    code = $_.code
                    file = $_.file
                    messageFingerprint = $_.messageFingerprint
                }
            })
    }
}

$unapprovedRepositoryDiagnostics = [Collections.Generic.List[object]]::new()
if ($DiagnosticAllowlistPath) {
    $allowlistCounts = @{}
    foreach ($entry in $diagnosticAllowlist) {
        $allowlistCounts[$entry.key] = $entry.maxCount
    }
    foreach ($group in @($repositoryDiagnosticRecords | Group-Object -Property key)) {
        $allowedCount = if ($allowlistCounts.ContainsKey($group.Name)) { $allowlistCounts[$group.Name] } else { 0 }
        if ($group.Count -gt $allowedCount) {
            $record = $group.Group[0]
            $unapprovedRepositoryDiagnostics.Add([pscustomobject]@{
                key = $group.Name
                code = $record.code
                file = $record.file
                messageFingerprint = $record.messageFingerprint
                allowedCount = $allowedCount
                actualCount = $group.Count
                delta = $group.Count - $allowedCount
            })
        }
    }
}
$unownedNonRepositoryDiagnostics = [Collections.Generic.List[object]]::new()
if ($NonRepositoryOwnershipPath) {
    $ownershipCounts = @{}
    foreach ($entry in $nonRepositoryOwnership) {
        $ownershipCounts[$entry.key] = $entry.maxCount
    }
    foreach ($group in @($nonRepositoryDiagnosticRecords | Group-Object -Property key)) {
        $allowedCount = if ($ownershipCounts.ContainsKey($group.Name)) { $ownershipCounts[$group.Name] } else { 0 }
        if ($group.Count -gt $allowedCount) {
            $record = $group.Group[0]
            $unownedNonRepositoryDiagnostics.Add([pscustomobject]@{
                key = $group.Name
                code = $record.code
                origin = $record.origin
                file = $record.file
                messageFingerprint = $record.messageFingerprint
                allowedCount = $allowedCount
                actualCount = $group.Count
                delta = $group.Count - $allowedCount
            })
        }
    }
}
$newRepositoryDiagnostics = [Collections.Generic.List[object]]::new()
if ($repositoryDiagnosticComparisonEnabled) {
    $baselineCounts = @{}
    if ($diagnosticBaselineFormat -eq 'repository-fingerprint') {
        $baselineCounts = $baselineRepositoryDiagnosticCounts
    } else {
        $baselineGroups = @($baselineRepositoryDiagnosticRecords | Group-Object -Property key)
        foreach ($group in $baselineGroups) {
            $baselineCounts[$group.Name] = $group.Count
        }
    }
    foreach ($group in @($repositoryDiagnosticRecords | Group-Object -Property key)) {
        $baselineCount = if ($baselineCounts.ContainsKey($group.Name)) { $baselineCounts[$group.Name] } else { 0 }
        if ($group.Count -gt $baselineCount) {
            $record = $group.Group[0]
            $newRepositoryDiagnostics.Add([pscustomobject]@{
                key = $group.Name
                code = $record.code
                file = $record.file
                messageFingerprint = $record.messageFingerprint
                baselineCount = $baselineCount
                actualCount = $group.Count
                delta = $group.Count - $baselineCount
            })
        }
    }
}

$report = [ordered]@{
    schemaVersion = 2
    logPath = [IO.Path]::GetFullPath($LogPath)
    baselinePath = [IO.Path]::GetFullPath($BaselinePath)
    warningIds = @($actual)
    baselineWarningIds = @($baseline)
    newWarningIds = @($new)
    resolvedWarningIds = @($resolved)
    warningIdCount = $actual.Count
    newWarningIdCount = $new.Count
    diagnostics = @($diagnostics)
    diagnosticCount = $diagnostics.Count
    warningOccurrenceCount = $warningOccurrenceCount
    unparsedDiagnosticCount = $unparsedDiagnostics.Count
    unparsedDiagnostics = @($unparsedDiagnostics)
    originCounts = $originCounts
    warningIdsByOrigin = $warningIdsByOrigin
    symbolMapPath = if ($SymbolMapPath) { [IO.Path]::GetFullPath($SymbolMapPath) } else { $null }
    symbolMapSchemaVersion = $symbolMapSchemaVersion
    symbolMappedDiagnosticCount = @($diagnostics | Where-Object { $_.symbolMapEvidence -eq 'mapped' }).Count
    diagnosticContext = $normalizedDiagnosticContext
    repositorySourceDiagnosticCount = $repositorySourceDiagnostics.Count
    repositorySourceDiagnosticKeyCount = @($repositoryDiagnosticRecords | Select-Object -ExpandProperty key -Unique).Count
    repositoryDiagnosticComparisonEnabled = $repositoryDiagnosticComparisonEnabled
    diagnosticBaselinePath = if ($repositoryDiagnosticComparisonEnabled) {
        [IO.Path]::GetFullPath($DiagnosticBaselinePath)
    } else { $null }
    diagnosticBaselineFormat = $diagnosticBaselineFormat
    newRepositoryDiagnostics = @($newRepositoryDiagnostics)
    repositoryDiagnosticGateEnabled = [bool]$FailOnNewRepositoryDiagnostic
    repositoryDiagnosticPassed = !$FailOnNewRepositoryDiagnostic -or $newRepositoryDiagnostics.Count -eq 0
    diagnosticAllowlistPath = if ($DiagnosticAllowlistPath) {
        [IO.Path]::GetFullPath($DiagnosticAllowlistPath)
    } else { $null }
    diagnosticAllowlistSchemaVersion = $diagnosticAllowlistSchemaVersion
    diagnosticAllowlistEntryCount = $diagnosticAllowlist.Count
    diagnosticAllowlistOwners = @($diagnosticAllowlist | Select-Object -ExpandProperty owner -Unique)
    unapprovedRepositoryDiagnostics = @($unapprovedRepositoryDiagnostics)
    repositoryAllowlistGateEnabled = [bool]$FailOnUnapprovedRepositoryDiagnostic
    repositoryAllowlistPassed = !$FailOnUnapprovedRepositoryDiagnostic -or $unapprovedRepositoryDiagnostics.Count -eq 0
    nonRepositoryOwnershipPath = if ($NonRepositoryOwnershipPath) {
        [IO.Path]::GetFullPath($NonRepositoryOwnershipPath)
    } else { $null }
    nonRepositoryOwnershipSchemaVersion = $nonRepositoryOwnershipSchemaVersion
    nonRepositoryOwnershipContext = $nonRepositoryOwnershipContext
    nonRepositoryOwnershipEntryCount = $nonRepositoryOwnership.Count
    nonRepositoryOwnershipOwners = @($nonRepositoryOwnership | Select-Object -ExpandProperty owner -Unique)
    unownedNonRepositoryDiagnostics = @($unownedNonRepositoryDiagnostics)
    nonRepositoryOwnershipGateEnabled = [bool]$FailOnUnownedNonRepositoryDiagnostic
    nonRepositoryOwnershipPassed = !$FailOnUnownedNonRepositoryDiagnostic -or $unownedNonRepositoryDiagnostics.Count -eq 0
    unparsedDiagnosticGateEnabled = [bool]$FailOnUnparsedDiagnostic
    unparsedDiagnosticPassed = !$FailOnUnparsedDiagnostic -or $unparsedDiagnostics.Count -eq 0
    warningIdGatePassed = $new.Count -eq 0
    passed = $new.Count -eq 0 -and (!$FailOnNewRepositoryDiagnostic -or $newRepositoryDiagnostics.Count -eq 0) -and
        (!$FailOnUnapprovedRepositoryDiagnostic -or $unapprovedRepositoryDiagnostics.Count -eq 0) -and
        (!$FailOnUnownedNonRepositoryDiagnostic -or $unownedNonRepositoryDiagnostics.Count -eq 0) -and
        (!$FailOnUnparsedDiagnostic -or $unparsedDiagnostics.Count -eq 0)
    gateEnabled = [bool]$FailOnNewWarning
}

$outputDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
if ($outputDirectory -and !(Test-Path -LiteralPath $outputDirectory)) {
    $null = New-Item -ItemType Directory -Path $outputDirectory
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output ($report | ConvertTo-Json -Depth 6)

if ($FailOnNewWarning -and $new.Count -gt 0) {
    throw "Unregistered NativeAOT warning ID(s): $($new -join ', '). See $OutputPath."
}
if ($FailOnNewRepositoryDiagnostic -and $newRepositoryDiagnostics.Count -gt 0) {
    throw "New repository NativeAOT diagnostic fingerprints detected: $($newRepositoryDiagnostics.Count). See $OutputPath."
}
if ($FailOnUnapprovedRepositoryDiagnostic -and $unapprovedRepositoryDiagnostics.Count -gt 0) {
    throw "Unapproved repository NativeAOT diagnostic fingerprints detected: $($unapprovedRepositoryDiagnostics.Count). See $OutputPath."
}
if ($FailOnUnownedNonRepositoryDiagnostic -and $unownedNonRepositoryDiagnostics.Count -gt 0) {
    throw "Unowned non-repository NativeAOT diagnostics detected: $($unownedNonRepositoryDiagnostics.Count). See $OutputPath."
}
if ($FailOnUnparsedDiagnostic -and $unparsedDiagnostics.Count -gt 0) {
    throw "Unparsed NativeAOT diagnostic line(s) detected: $($unparsedDiagnostics.Count). See $OutputPath."
}
