[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../..'))
$checker = Join-Path $PSScriptRoot 'Check-NativeAotWarnings.ps1'
$temporaryRoot = Join-Path ([IO.Path]::GetTempPath()) ('bing-utils-nativeaot-warning-test-' + [guid]::NewGuid().ToString('N'))
$null = New-Item -ItemType Directory -Path $temporaryRoot

function Invoke-Checker([string[]]$Arguments) {
    & pwsh -NoProfile -File $checker @Arguments 2>&1 | Out-Null
    return $LASTEXITCODE
}

try {
    $baselineLog = Join-Path $temporaryRoot 'baseline.log'
    $candidateLog = Join-Path $temporaryRoot 'candidate.log'
    $idBaseline = Join-Path $temporaryRoot 'ids.txt'
    $baselineReport = Join-Path $temporaryRoot 'baseline-report.json'
    $compactBaseline = Join-Path $temporaryRoot 'repository-fingerprint-baseline.json'
    $compactCandidateReport = Join-Path $temporaryRoot 'compact-candidate-report.json'
    $compactIntegrityReport = Join-Path $temporaryRoot 'compact-integrity-report.json'
    $largeCompactCandidateReport = Join-Path $temporaryRoot 'large-compact-candidate-report.json'
    $candidateReport = Join-Path $temporaryRoot 'candidate-report.json'
    $allowlist = Join-Path $temporaryRoot 'diagnostic-allowlist.json'
    $allowlistMissingOwner = Join-Path $temporaryRoot 'diagnostic-allowlist-missing-owner.json'
    $allowlistMissingReason = Join-Path $temporaryRoot 'diagnostic-allowlist-missing-reason.json'
    $allowlistPassReport = Join-Path $temporaryRoot 'allowlist-pass-report.json'
    $allowlistFailReport = Join-Path $temporaryRoot 'allowlist-fail-report.json'
    $nonRepositoryOwnership = Join-Path $temporaryRoot 'non-repository-ownership.json'
    $nonRepositoryOwnershipMissing = Join-Path $temporaryRoot 'non-repository-ownership-missing.json'
    $nonRepositoryOwnershipPassReport = Join-Path $temporaryRoot 'non-repository-ownership-pass-report.json'
    $nonRepositoryOwnershipFailReport = Join-Path $temporaryRoot 'non-repository-ownership-fail-report.json'
    $nonRepositoryOwnershipDriftLog = Join-Path $temporaryRoot 'non-repository-ownership-drift.log'
    $mismatchReport = Join-Path $temporaryRoot 'mismatch-report.json'
    $unparsedReport = Join-Path $temporaryRoot 'unparsed-report.json'
    $mappedLog = Join-Path $temporaryRoot 'mapped.log'
    $mappedReport = Join-Path $temporaryRoot 'mapped-report.json'
    $plainNativeAotLog = Join-Path $temporaryRoot 'plain-nativeaot.log'
    $plainNativeAotReport = Join-Path $temporaryRoot 'plain-nativeaot-report.json'
    $sourceMap = Join-Path $temporaryRoot 'source-map.json'
    $repoProject = Join-Path $root 'src/Bing.Utils/Bing.Utils.csproj'
    $diagnosticContext = 'sdk=fixture;tfm=net8.0;rid=win-x64;mode=single-publish'
    $existingFile = Join-Path $root 'src/Bing.Utils/ExistingWarning.cs'
    $newFile = Join-Path $root 'src/Bing.Utils/NewWarning.cs'
    $generatedFile = Join-Path $root 'src/Bing.Utils/obj/Release/net8.0/generated.g.cs'
    $externalFile = 'C:\Users\test\.nuget\packages\warning-fixture\fixture.cs'

    @(
        "$existingFile(4,2): warning IL2026: existing repository warning [$repoProject::TargetFramework=net8.0]"
        "$generatedFile(5,3): warning IL2046: generated warning [$repoProject::net8.0]"
        "$externalFile(6,4): warning IL2067: external warning [$repoProject::net8.0]"
        "Bing.Utils!Namespace.Type.Method(): warning IL2104: symbolic location"
        "ILC : AOT analysis warning IL3053: Assembly 'Fixture' produced trim warnings."
        'unparsed diagnostic IL9999: malformed'
    ) | Set-Content -LiteralPath $baselineLog -Encoding utf8
    @(
        "$existingFile(4,2): warning IL2026: existing repository warning [$repoProject::TargetFramework=net8.0]"
        "$newFile(9,3): warning IL2026: new repository warning [$repoProject::TargetFramework=net8.0]"
    ) | Set-Content -LiteralPath $candidateLog -Encoding utf8
    'IL2026' | Set-Content -LiteralPath $idBaseline -Encoding utf8
    @{
        schemaVersion = 2
        entries = @(
            @{
                key = 'Bing.Utils.Type.Method'
                file = 'src/Bing.Utils/ExistingWarning.cs'
                candidates = @('src/Bing.Utils/ExistingWarning.cs')
            },
            @{
                key = 'Bing.Utils.Type.Ambiguous'
                file = $null
                candidates = @('src/Bing.Utils/ExistingWarning.cs', 'src/Bing.Utils/NewWarning.cs')
                overloads = @(
                    @{
                        signature = 'Ambiguous(String)'
                        file = 'src/Bing.Utils/ExistingWarning.cs'
                    },
                    @{
                        signature = 'Ambiguous(Int32)'
                        file = 'src/Bing.Utils/NewWarning.cs'
                    }
                )
            }
        )
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $sourceMap -Encoding utf8
    @(
        "ILC : Trim analysis warning IL2026: Bing.Utils.Type.Method(): mapped repository warning [$repoProject::TargetFramework=net8.0]"
        "ILC : Trim analysis warning IL2026: Bing.Utils.Type.Ambiguous(String): signature-mapped repository warning [$repoProject::TargetFramework=net8.0]"
        "ILC : Trim analysis warning IL2026: Bing.Utils.Type.Ambiguous(): ambiguous repository warning [$repoProject::TargetFramework=net8.0]"
    ) | Set-Content -LiteralPath $mappedLog -Encoding utf8
    'ILC : warning IL2026: Bing.Utils.Type.Method(): plain NativeAOT warning format' |
        Set-Content -LiteralPath $plainNativeAotLog -Encoding utf8

    $baselineExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', $baselineReport,
        '-DiagnosticContext', $diagnosticContext)
    if ($baselineExit -ne 0) {
        throw "Baseline warning report fixture failed with exit code $baselineExit."
    }

    $baselineData = Get-Content -LiteralPath $baselineReport -Encoding utf8 -Raw | ConvertFrom-Json
    $baselineShapeValid = $baselineData.schemaVersion -eq 2
    $baselineShapeValid = $baselineShapeValid -and (@($baselineData.diagnostics).Count -eq 5)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.warningOccurrenceCount -eq 6)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.unparsedDiagnosticCount -eq 1)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.originCounts.repositorySource -eq 1)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.originCounts.repositoryGenerated -eq 1)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.originCounts.externalDependencyOrToolchain -eq 1)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.originCounts.unknown -eq 2)
    $baselineShapeValid = $baselineShapeValid -and ($baselineData.diagnostics[0].targetFramework -eq 'net8.0')
    $baselineShapeValid = $baselineShapeValid -and ($null -eq $baselineData.diagnostics[3].file)
    if (!$baselineShapeValid) {
        throw 'Warning report origin classification fixture did not produce the expected schema.'
    }

    $builder = Join-Path $PSScriptRoot 'Build-NativeAotDiagnosticBaseline.ps1'
    & pwsh -NoProfile -File $builder -ReportPath $baselineReport -OutputPath $compactBaseline | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Repository diagnostic baseline builder failed with exit code $LASTEXITCODE."
    }
    $compactData = Get-Content -LiteralPath $compactBaseline -Encoding utf8 -Raw | ConvertFrom-Json
    $compactShapeValid = $compactData.schemaVersion -eq 1
    $compactShapeValid = $compactShapeValid -and ($compactData.kind -eq 'nativeaot-repository-diagnostic-baseline')
    $compactShapeValid = $compactShapeValid -and ($compactData.diagnosticContext -eq $diagnosticContext)
    $compactShapeValid = $compactShapeValid -and ($compactData.repositoryDiagnosticOccurrenceCount -eq 1)
    $compactShapeValid = $compactShapeValid -and ($compactData.entryCount -eq 1)
    $compactShapeValid = $compactShapeValid -and (@($compactData.entries).Count -eq 1)
    if (!$compactShapeValid) {
        throw 'Repository diagnostic baseline builder did not produce the expected compact schema.'
    }

    $compactData.repositoryDiagnosticOccurrenceCount = 2
    $compactData | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $compactBaseline -Encoding utf8
    $integrityExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $compactIntegrityReport,
        '-DiagnosticBaselinePath', $compactBaseline, '-DiagnosticContext', $diagnosticContext,
        '-FailOnNewRepositoryDiagnostic')
    if ($integrityExit -eq 0) {
        throw 'Inconsistent compact repository diagnostic baseline counts unexpectedly passed.'
    }

    $compactData.repositoryDiagnosticOccurrenceCount = [int]::MaxValue
    $compactData.entries[0].maxCount = [int]::MaxValue
    $compactData | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $compactBaseline -Encoding utf8
    $largeCompactExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $largeCompactCandidateReport,
        '-DiagnosticBaselinePath', $compactBaseline, '-DiagnosticContext', $diagnosticContext,
        '-FailOnNewRepositoryDiagnostic')
    if ($largeCompactExit -eq 0) {
        throw 'Compact repository diagnostic baseline with a large maxCount unexpectedly passed.'
    }

    $compactGateExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $compactCandidateReport,
        '-DiagnosticBaselinePath', $compactBaseline, '-DiagnosticContext', $diagnosticContext,
        '-FailOnNewRepositoryDiagnostic')
    $compactCandidateData = Get-Content -LiteralPath $compactCandidateReport -Encoding utf8 -Raw | ConvertFrom-Json
    $compactGateValid = $compactGateExit -ne 0
    $compactGateValid = $compactGateValid -and ($compactCandidateData.diagnosticBaselineFormat -eq 'repository-fingerprint')
    $compactGateValid = $compactGateValid -and ($compactCandidateData.repositoryDiagnosticGateEnabled -eq $true)
    $compactGateValid = $compactGateValid -and ($compactCandidateData.repositoryDiagnosticPassed -eq $false)
    $compactGateValid = $compactGateValid -and (@($compactCandidateData.newRepositoryDiagnostics).Count -eq 1)
    if (!$compactGateValid) {
        throw 'Compact repository diagnostic baseline did not enforce the fingerprint gate.'
    }

    @{
        schemaVersion = 1
        entries = @(
            @{
                code = $baselineData.diagnostics[0].code
                messageFingerprint = $baselineData.diagnostics[0].messageFingerprint
                file = $baselineData.diagnostics[0].file
                maxCount = 1
                owner = 'reflection-maintainers'
                reason = 'Known warning retained by the rooted reflection contract fixture.'
            }
        )
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $allowlist -Encoding utf8

    @{
        schemaVersion = 1
        entries = @(
            @{
                code = $baselineData.diagnostics[0].code
                messageFingerprint = $baselineData.diagnostics[0].messageFingerprint
                file = $baselineData.diagnostics[0].file
                maxCount = 1
                reason = 'Owner is intentionally missing for the negative validation fixture.'
            }
        )
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $allowlistMissingOwner -Encoding utf8

    $missingOwnerExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', (Join-Path $temporaryRoot 'allowlist-missing-owner-report.json'),
        '-DiagnosticAllowlistPath', $allowlistMissingOwner, '-FailOnUnapprovedRepositoryDiagnostic')
    if ($missingOwnerExit -eq 0) {
        throw 'Repository diagnostic allowlist without an owner unexpectedly passed validation.'
    }

    @{
        schemaVersion = 1
        entries = @(
            @{
                code = $baselineData.diagnostics[0].code
                messageFingerprint = $baselineData.diagnostics[0].messageFingerprint
                file = $baselineData.diagnostics[0].file
                maxCount = 1
                owner = 'reflection-maintainers'
            }
        )
    } | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $allowlistMissingReason -Encoding utf8

    $missingReasonExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', (Join-Path $temporaryRoot 'allowlist-missing-reason-report.json'),
        '-DiagnosticAllowlistPath', $allowlistMissingReason, '-FailOnUnapprovedRepositoryDiagnostic')
    if ($missingReasonExit -eq 0) {
        throw 'Repository diagnostic allowlist without a reason unexpectedly passed validation.'
    }

    $allowlistPassExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', $allowlistPassReport,
        '-DiagnosticAllowlistPath', $allowlist, '-FailOnUnapprovedRepositoryDiagnostic')
    $allowlistPassData = Get-Content -LiteralPath $allowlistPassReport -Encoding utf8 -Raw | ConvertFrom-Json
    $allowlistPassValid = $allowlistPassExit -eq 0
    $allowlistPassValid = $allowlistPassValid -and ($allowlistPassData.repositoryAllowlistGateEnabled -eq $true)
    $allowlistPassValid = $allowlistPassValid -and ($allowlistPassData.repositoryAllowlistPassed -eq $true)
    $allowlistPassValid = $allowlistPassValid -and (@($allowlistPassData.unapprovedRepositoryDiagnostics).Count -eq 0)
    $allowlistPassValid = $allowlistPassValid -and (@($allowlistPassData.diagnosticAllowlistOwners).Count -eq 1)
    $allowlistPassValid = $allowlistPassValid -and ($allowlistPassData.diagnosticAllowlistOwners[0] -eq 'reflection-maintainers')
    if (!$allowlistPassValid) {
        throw 'Repository diagnostic allowlist fixture unexpectedly rejected the approved warning.'
    }

    $allowlistFailExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $allowlistFailReport,
        '-DiagnosticAllowlistPath', $allowlist, '-FailOnUnapprovedRepositoryDiagnostic')
    $allowlistFailData = Get-Content -LiteralPath $allowlistFailReport -Encoding utf8 -Raw | ConvertFrom-Json
    $allowlistFailValid = $allowlistFailExit -ne 0
    $allowlistFailValid = $allowlistFailValid -and ($allowlistFailData.repositoryAllowlistPassed -eq $false)
    $allowlistFailValid = $allowlistFailValid -and (@($allowlistFailData.unapprovedRepositoryDiagnostics).Count -eq 1)
    $allowlistFailValid = $allowlistFailValid -and ($allowlistFailData.unapprovedRepositoryDiagnostics[0].delta -eq 1)
    if (!$allowlistFailValid) {
        throw 'Repository diagnostic allowlist fixture did not reject the unapproved warning.'
    }

    $nonRepositoryDiagnostics = @($baselineData.diagnostics | Where-Object { $_.origin -ne 'repositorySource' })
    $ownershipEntries = @($nonRepositoryDiagnostics | ForEach-Object {
        $owner = switch ([string]$_.origin) {
            'repositoryGenerated' { 'reflection-build' }
            'externalDependencyOrToolchain' { 'runtime-dependency' }
            default { 'release-engineering' }
        }
        $action = switch ([string]$_.origin) {
            'repositoryGenerated' { 'Keep generated output out of source diagnostics and investigate new generated warnings.' }
            'externalDependencyOrToolchain' { 'Track the dependency or SDK version and review on toolchain upgrades.' }
            default { 'Resolve the source location before treating the diagnostic as accepted.' }
        }
        [ordered]@{
            code = $_.code
            messageFingerprint = $_.messageFingerprint
            origin = $_.origin
            maxCount = 1
            owner = $owner
            reason = "Fixture ownership for the $($_.origin) diagnostic boundary."
            action = $action
        }
    })
    [ordered]@{
        schemaVersion = 1
        kind = 'nativeaot-nonrepository-diagnostic-ownership'
        diagnosticContext = $diagnosticContext
        entries = $ownershipEntries
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $nonRepositoryOwnership -Encoding utf8

    $ownershipPassExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', $nonRepositoryOwnershipPassReport,
        '-DiagnosticContext', $diagnosticContext, '-NonRepositoryOwnershipPath', $nonRepositoryOwnership,
        '-FailOnUnownedNonRepositoryDiagnostic')
    $ownershipPassData = Get-Content -LiteralPath $nonRepositoryOwnershipPassReport -Encoding utf8 -Raw | ConvertFrom-Json
    $ownershipPassValid = $ownershipPassExit -eq 0
    $ownershipPassValid = $ownershipPassValid -and ($ownershipPassData.nonRepositoryOwnershipGateEnabled -eq $true)
    $ownershipPassValid = $ownershipPassValid -and ($ownershipPassData.nonRepositoryOwnershipPassed -eq $true)
    $ownershipPassValid = $ownershipPassValid -and (@($ownershipPassData.unownedNonRepositoryDiagnostics).Count -eq 0)
    $ownershipPassValid = $ownershipPassValid -and ($ownershipPassData.nonRepositoryOwnershipEntryCount -eq 4)
    $ownershipPassValid = $ownershipPassValid -and (@($ownershipPassData.nonRepositoryOwnershipOwners).Count -eq 3)
    if (!$ownershipPassValid) {
        throw 'Non-repository diagnostic ownership fixture unexpectedly rejected approved diagnostics.'
    }

    $ownershipEntries[0].owner = $null
    [ordered]@{
        schemaVersion = 1
        kind = 'nativeaot-nonrepository-diagnostic-ownership'
        diagnosticContext = $diagnosticContext
        entries = $ownershipEntries
    } | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $nonRepositoryOwnershipMissing -Encoding utf8
    $ownershipMissingExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', (Join-Path $temporaryRoot 'non-repository-ownership-missing-report.json'),
        '-DiagnosticContext', $diagnosticContext, '-NonRepositoryOwnershipPath', $nonRepositoryOwnershipMissing,
        '-FailOnUnownedNonRepositoryDiagnostic')
    if ($ownershipMissingExit -eq 0) {
        throw 'Non-repository diagnostic ownership without an owner unexpectedly passed validation.'
    }

    @((Get-Content -LiteralPath $baselineLog -Encoding utf8),
        "$externalFile(7,1): warning IL2067: external warning [$repoProject::net8.0]") |
        Set-Content -LiteralPath $nonRepositoryOwnershipDriftLog -Encoding utf8
    $ownershipFailExit = Invoke-Checker @(
        '-LogPath', $nonRepositoryOwnershipDriftLog, '-BaselinePath', $idBaseline, '-OutputPath', $nonRepositoryOwnershipFailReport,
        '-DiagnosticContext', $diagnosticContext, '-NonRepositoryOwnershipPath', $nonRepositoryOwnership,
        '-FailOnUnownedNonRepositoryDiagnostic')
    $ownershipFailData = Get-Content -LiteralPath $nonRepositoryOwnershipFailReport -Encoding utf8 -Raw | ConvertFrom-Json
    $ownershipFailValid = $ownershipFailExit -ne 0
    $ownershipFailValid = $ownershipFailValid -and ($ownershipFailData.nonRepositoryOwnershipPassed -eq $false)
    $ownershipFailValid = $ownershipFailValid -and (@($ownershipFailData.unownedNonRepositoryDiagnostics).Count -eq 1)
    $ownershipFailValid = $ownershipFailValid -and ($ownershipFailData.unownedNonRepositoryDiagnostics[0].delta -eq 1)
    if (!$ownershipFailValid) {
        throw 'Non-repository diagnostic ownership fixture did not reject the drift.'
    }

    $unparsedExit = Invoke-Checker @(
        '-LogPath', $baselineLog, '-BaselinePath', $idBaseline, '-OutputPath', $unparsedReport,
        '-FailOnUnparsedDiagnostic')
    $unparsedData = Get-Content -LiteralPath $unparsedReport -Encoding utf8 -Raw | ConvertFrom-Json
    $unparsedGateValid = $unparsedExit -ne 0
    $unparsedGateValid = $unparsedGateValid -and ($unparsedData.unparsedDiagnosticGateEnabled -eq $true)
    $unparsedGateValid = $unparsedGateValid -and ($unparsedData.unparsedDiagnosticPassed -eq $false)
    $unparsedGateValid = $unparsedGateValid -and ($unparsedData.passed -eq $false)
    if (!$unparsedGateValid) {
        throw 'Unparsed diagnostic gate fixture did not fail as expected.'
    }

    $gateExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $candidateReport,
        '-DiagnosticBaselinePath', $baselineReport, '-DiagnosticContext', $diagnosticContext,
        '-FailOnNewRepositoryDiagnostic')
    $candidateData = Get-Content -LiteralPath $candidateReport -Encoding utf8 -Raw | ConvertFrom-Json
    $gateShapeValid = $gateExit -ne 0
    $gateShapeValid = $gateShapeValid -and ($candidateData.warningIdGatePassed -eq $true)
    $gateShapeValid = $gateShapeValid -and ($candidateData.repositoryDiagnosticGateEnabled -eq $true)
    $gateShapeValid = $gateShapeValid -and ($candidateData.repositoryDiagnosticPassed -eq $false)
    $gateShapeValid = $gateShapeValid -and (@($candidateData.newRepositoryDiagnostics).Count -eq 1)
    $gateShapeValid = $gateShapeValid -and ($candidateData.passed -eq $false)
    if (!$gateShapeValid) {
        throw 'Repository diagnostic fingerprint gate fixture did not fail as expected.'
    }

    $mappedExit = Invoke-Checker @(
        '-LogPath', $mappedLog, '-BaselinePath', $idBaseline, '-OutputPath', $mappedReport,
        '-SymbolMapPath', $sourceMap)
    $mappedData = Get-Content -LiteralPath $mappedReport -Encoding utf8 -Raw | ConvertFrom-Json
    $mappedDiagnostic = @($mappedData.diagnostics | Where-Object { $_.symbolMapEvidence -eq 'mapped' })
    $signatureMappedDiagnostic = @($mappedData.diagnostics | Where-Object { $_.symbolMapMatch -eq 'signature' })
    $ambiguousDiagnostic = @($mappedData.diagnostics | Where-Object { $_.symbolMapEvidence -eq 'ambiguous' })
    $mapShapeValid = $mappedExit -eq 0
    $mapShapeValid = $mapShapeValid -and ($mappedData.symbolMapSchemaVersion -eq 2)
    $mapShapeValid = $mapShapeValid -and ($mappedData.symbolMappedDiagnosticCount -eq 2)
    $mapShapeValid = $mapShapeValid -and ($mappedDiagnostic.Count -eq 2)
    $mapShapeValid = $mapShapeValid -and ($mappedDiagnostic[0].file -eq 'src/Bing.Utils/ExistingWarning.cs')
    $mapShapeValid = $mapShapeValid -and ($mappedDiagnostic[0].origin -eq 'repositorySource')
    $mapShapeValid = $mapShapeValid -and ($signatureMappedDiagnostic.Count -eq 1)
    $mapShapeValid = $mapShapeValid -and ($signatureMappedDiagnostic[0].file -eq 'src/Bing.Utils/ExistingWarning.cs')
    $mapShapeValid = $mapShapeValid -and ($signatureMappedDiagnostic[0].symbolSignature -eq 'Ambiguous(String)')
    $mapShapeValid = $mapShapeValid -and ($ambiguousDiagnostic.Count -eq 1)
    $mapShapeValid = $mapShapeValid -and ($ambiguousDiagnostic[0].origin -eq 'unknown')
    if (!$mapShapeValid) {
        throw 'Portable PDB source map fixture did not classify unique and ambiguous symbols as expected.'
    }

    $plainNativeAotExit = Invoke-Checker @(
        '-LogPath', $plainNativeAotLog, '-BaselinePath', $idBaseline, '-OutputPath', $plainNativeAotReport,
        '-SymbolMapPath', $sourceMap)
    $plainNativeAotData = Get-Content -LiteralPath $plainNativeAotReport -Encoding utf8 -Raw | ConvertFrom-Json
    $plainNativeAotDiagnostic = @($plainNativeAotData.diagnostics)
    $plainNativeAotShapeValid = $plainNativeAotExit -eq 0
    $plainNativeAotShapeValid = $plainNativeAotShapeValid -and ($plainNativeAotDiagnostic.Count -eq 1)
    $plainNativeAotShapeValid = $plainNativeAotShapeValid -and ($plainNativeAotData.unparsedDiagnosticCount -eq 0)
    $plainNativeAotShapeValid = $plainNativeAotShapeValid -and ($plainNativeAotDiagnostic[0].tool -eq 'ILC')
    $plainNativeAotShapeValid = $plainNativeAotShapeValid -and ($null -eq $plainNativeAotDiagnostic[0].rawLocation)
    $plainNativeAotShapeValid = $plainNativeAotShapeValid -and ($plainNativeAotDiagnostic[0].symbolMapEvidence -eq 'mapped')
    if (!$plainNativeAotShapeValid) {
        throw 'Plain ILC warning format was not parsed as a source-mapped NativeAOT diagnostic.'
    }

    $mismatchExit = Invoke-Checker @(
        '-LogPath', $candidateLog, '-BaselinePath', $idBaseline, '-OutputPath', $mismatchReport,
        '-DiagnosticBaselinePath', $baselineReport,
        '-DiagnosticContext', 'sdk=fixture;tfm=net8.0;rid=win-x64;mode=different',
        '-FailOnNewRepositoryDiagnostic')
    if ($mismatchExit -eq 0) {
        throw 'Diagnostic context mismatch unexpectedly passed the repository fingerprint gate.'
    }

    Write-Output 'nativeaot-warning-checker-test-ok'
    exit 0
} finally {
    if (Test-Path -LiteralPath $temporaryRoot) {
        Remove-Item -LiteralPath $temporaryRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
