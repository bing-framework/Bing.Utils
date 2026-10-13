[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$ReportPath,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

if (!(Test-Path -LiteralPath $ReportPath -PathType Leaf)) {
    throw "NativeAOT warning report was not found: $ReportPath"
}

try {
    $report = Get-Content -LiteralPath $ReportPath -Encoding utf8 -Raw | ConvertFrom-Json
} catch {
    throw "NativeAOT warning report is not valid JSON: $ReportPath"
}

if ($report.schemaVersion -ne 2 -or $null -eq $report.diagnostics) {
    throw "NativeAOT warning report must use schema version 2 with diagnostics: $ReportPath"
}

$diagnosticContext = [string]$report.diagnosticContext
if ([string]::IsNullOrWhiteSpace($diagnosticContext)) {
    throw 'NativeAOT warning report must contain diagnosticContext before a fingerprint baseline can be generated.'
}

$records = @($report.diagnostics | Where-Object { $_.origin -eq 'repositorySource' } | ForEach-Object {
    $code = ([string]$_.code).Trim().ToUpperInvariant()
    $file = ([string]$_.file).Trim().Replace('\', '/')
    $fingerprint = ([string]$_.messageFingerprint).Trim().ToLowerInvariant()
    if ($code -notmatch '^(?:IL|ILC)\d{4}$') {
        throw "Repository diagnostic has an invalid code: $code"
    }
    $invalidFile = [string]::IsNullOrWhiteSpace($file) -or [IO.Path]::IsPathRooted($file) -or ($file -match '(^|/)\.\.(?:/|$)')
    if ($invalidFile) {
        throw "Repository diagnostic has a non-normalized source file: $file"
    }
    if ($fingerprint -notmatch '^[0-9a-f]{64}$') {
        throw "Repository diagnostic has an invalid message fingerprint for $code/$file."
    }
    [pscustomobject]@{
        code = $code
        file = $file
        messageFingerprint = $fingerprint
        key = "$code|$fingerprint|$file"
    }
})

if ($records.Count -eq 0) {
    throw "NativeAOT warning report contains no repository source diagnostics: $ReportPath"
}

$entries = @($records |
    Group-Object -Property key |
    ForEach-Object {
        $record = $_.Group[0]
        [ordered]@{
            code = $record.code
            messageFingerprint = $record.messageFingerprint
            file = $record.file
            maxCount = $_.Count
        }
    } |
    Sort-Object code, file, messageFingerprint)

$baseline = [ordered]@{
    schemaVersion = 1
    kind = 'nativeaot-repository-diagnostic-baseline'
    diagnosticContext = $diagnosticContext.Trim()
    sourceReportFile = [IO.Path]::GetFileName([IO.Path]::GetFullPath($ReportPath))
    sourceReportSchemaVersion = [int]$report.schemaVersion
    repositoryDiagnosticOccurrenceCount = $records.Count
    entryCount = $entries.Count
    entries = $entries
}

$outputDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
if ($outputDirectory -and !(Test-Path -LiteralPath $outputDirectory)) {
    $null = New-Item -ItemType Directory -Path $outputDirectory
}
$baseline | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output ($baseline | ConvertTo-Json -Depth 6)
