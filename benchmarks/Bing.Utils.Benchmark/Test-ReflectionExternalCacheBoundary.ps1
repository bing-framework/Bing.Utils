param(
    [string]$OutputPath = ''
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $OutputPath = Join-Path $PSScriptRoot 'bin/reflection-external-cache-boundary.json'
}

function Resolve-RepositoryPath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $rootWithSeparator = $repositoryRoot.TrimEnd(
        [System.IO.Path]::DirectorySeparatorChar,
        [System.IO.Path]::AltDirectorySeparatorChar) + [System.IO.Path]::DirectorySeparatorChar
    if (!$fullPath.StartsWith($rootWithSeparator, [System.StringComparison]::OrdinalIgnoreCase) -and
        ![System.StringComparer]::OrdinalIgnoreCase.Equals($fullPath, $repositoryRoot)) {
        throw "Output path must be inside the repository: $Path"
    }
    return $fullPath
}

function Read-RepositoryText {
    param([Parameter(Mandatory = $true)][string]$RelativePath)

    $path = Join-Path $repositoryRoot $RelativePath
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Expected source file was not found: $RelativePath"
    }
    return [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)
}

$outputFile = Resolve-RepositoryPath $OutputPath
$checks = [System.Collections.Generic.List[object]]::new()
$violations = [System.Collections.Generic.List[object]]::new()

function Add-Check {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][bool]$Passed,
        [Parameter(Mandatory = $true)][string]$Details
    )

    $checks.Add([ordered]@{
        id = $Id
        passed = $Passed
        details = $Details
    })
}

function Add-Violation {
    param(
        [Parameter(Mandatory = $true)][string]$Id,
        [Parameter(Mandatory = $true)][string]$Details
    )

    $violations.Add([ordered]@{
        id = $Id
        details = $Details
    })
}

$guardedReflectorSource = 'src/Bing.Utils/Bing/Reflection/TypeReflections/TypeReflections.Attributes.cs'
$reflectionsAttributeSource = 'src/Bing.Utils/Bing/Reflection/Reflections/Reflections.Attributes.cs'
$guardSource = 'src/Bing.Utils.Guard/Bing/Validation/ValidationExceptionHelper.cs'

$sourcePolicies = @(
    [ordered]@{
        id = 'type-reflector-collectible-guard'
        source = $guardedReflectorSource
        required = @(
            'UsePublicAttributeApi',
            'CustomAttributeReflectorRuntimeTypeHandle is null',
            'ReflectionCacheScope.IsCollectible(member)',
            'ReflectionCacheScope.IsCollectible(parameter)',
            'new DirectAttributeProvider(member)',
            'new DirectAttributeProvider(parameter)'
        )
    },
    [ordered]@{
        id = 'reflections-public-attribute-fallback'
        source = $reflectionsAttributeSource
        required = @(
            'UsePublicAttributeApi',
            'TypeReflections.GetAttribute',
            'member.GetCustomAttribute'
        )
    },
    [ordered]@{
        id = 'guard-constructor-direct-invoke'
        source = $guardSource
        required = @(
            'constructor.Invoke(invokeParameters)',
            'ExceptionHelper.PrepareForRethrow'
        )
        forbidden = @(
            'GetReflector',
            'ReflectorCacheUtils'
        )
    }
)

foreach ($policy in $sourcePolicies) {
    $sourceText = Read-RepositoryText $policy.source
    $passed = $true
    foreach ($required in @($policy.required)) {
        if (!$sourceText.Contains([string]$required, [System.StringComparison]::Ordinal)) {
            Add-Violation $policy.id "$($policy.source) is missing required boundary: $required"
            $passed = $false
        }
    }
    $forbiddenPatterns = @()
    if ($policy.PSObject.Properties.Name -contains 'forbidden') {
        $forbiddenPatterns = @($policy.forbidden)
    }
    foreach ($forbidden in $forbiddenPatterns) {
        if ($sourceText.Contains([string]$forbidden, [System.StringComparison]::Ordinal)) {
            Add-Violation $policy.id "$($policy.source) contains forbidden third-party cache call: $forbidden"
            $passed = $false
        }
    }
    Add-Check $policy.id $passed "$($policy.source) policy"
}

$sourceFiles = Get-ChildItem -LiteralPath (Join-Path $repositoryRoot 'src') -Filter '*.cs' -File -Recurse
$reflectorHits = @(
    foreach ($file in $sourceFiles) {
        $relative = $file.FullName.Substring($repositoryRoot.Length).TrimStart('\', '/') -replace '\\', '/'
        if ($relative -eq $guardedReflectorSource) {
            continue
        }
        Select-String -LiteralPath $file.FullName -Pattern '\bGetReflector\s*\('
    }
)
$reflectorPassed = $reflectorHits.Count -eq 0
foreach ($hit in $reflectorHits) {
    $relative = $hit.Path.Substring($repositoryRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    Add-Violation 'unapproved-reflector-entrypoint' "${relative}:$($hit.LineNumber): $($hit.Line.Trim())"
}
Add-Check 'unapproved-reflector-entrypoint' $reflectorPassed 'Only the guarded TypeReflections adapter may call GetReflector.'

$forbiddenHits = @(
    foreach ($file in $sourceFiles) {
        Select-String -LiteralPath $file.FullName -Pattern 'ReflectorCacheUtils|ConstructorInfo\.GetReflector'
    }
)
$forbiddenPassed = $forbiddenHits.Count -eq 0
foreach ($hit in $forbiddenHits) {
    $relative = $hit.Path.Substring($repositoryRoot.Length).TrimStart('\', '/') -replace '\\', '/'
    Add-Violation 'third-party-strong-cache-reference' "${relative}:$($hit.LineNumber): $($hit.Line.Trim())"
}
Add-Check 'third-party-strong-cache-reference' $forbiddenPassed 'No repository source directly references the third-party strong reflector cache.'

$passed = $violations.Count -eq 0
$report = [ordered]@{
    schemaVersion = 1
    checkedAtUtc = [DateTime]::UtcNow.ToString('O')
    checks = @($checks)
    violations = @($violations)
    passed = $passed
}

$outputDirectory = Split-Path -Parent $outputFile
$null = New-Item -ItemType Directory -Path $outputDirectory -Force
$json = $report | ConvertTo-Json -Depth 8
[System.IO.File]::WriteAllText($outputFile, $json + [Environment]::NewLine,
    [System.Text.UTF8Encoding]::new($false))

Write-Output ("reflection-external-cache-boundary: checks=$($checks.Count), violations=$($violations.Count), passed=$passed")
Write-Output ("reflection-external-cache-boundary-report: $outputFile")
if (!$passed) {
    throw "Reflection external cache boundary validation failed with $($violations.Count) violation(s)."
}
