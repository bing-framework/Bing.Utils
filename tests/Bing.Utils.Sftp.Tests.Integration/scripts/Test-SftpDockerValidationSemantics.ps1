#requires -Version 7.3
[CmdletBinding()]
param(
    [string]$EvidencePath = (Join-Path $PSScriptRoot '..\..\..\ai_docs\tasks\2026-10-28-sftp-docker-validation\artifacts\round1-native-semantics.json')
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandArgumentPassing = 'Standard'
$PSNativeCommandUseErrorActionPreference = $false
$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = $utf8
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8
. (Join-Path $PSScriptRoot 'SftpDockerValidation.Helpers.ps1')

if ($PSVersionTable.PSVersion -lt [version]'7.3' -or $PSNativeCommandArgumentPassing -ne 'Standard') {
    throw 'The native invocation probe requires PowerShell 7.3+ Standard argument passing.'
}

$pythonCommand = Get-Command python -ErrorAction Stop
$template = Get-DockerInspectFormatTemplate 'org.bing-utils.sftp-validation'
$echoProgram = 'import json,sys; print(json.dumps(sys.argv[1:]))'
$argvResult = Invoke-NativeProcess -Executable $pythonCommand.Source -Arguments @(
    '-c', $echoProgram, '', 'embedded"quote', 'two words', $template
)
Assert-NativeProcessSucceeded -Result $argvResult -Label 'argv probe'
$actualArguments = @($argvResult.Text.Trim() | ConvertFrom-Json)
$expectedArguments = @('', 'embedded"quote', 'two words', '{{.Id}}|{{index .Config.Labels "org.bing-utils.sftp-validation"}}')
if ($actualArguments.Count -ne $expectedArguments.Count) { throw "Native argv count mismatch: $($actualArguments.Count)." }
for ($index = 0; $index -lt $expectedArguments.Count; $index++) {
    if ($actualArguments[$index] -cne $expectedArguments[$index]) {
        throw "Native argv[$index] mismatch; actual=$($actualArguments[$index] | ConvertTo-Json -Compress); expected=$($expectedArguments[$index] | ConvertTo-Json -Compress)."
    }
}

$stderrProgram = 'import sys; sys.stderr.write("native stderr marker\n"); sys.exit(23)'
$stderrResult = Invoke-NativeProcess -Executable $pythonCommand.Source -Arguments @('-c', $stderrProgram)
if ($stderrResult.ExitCode -ne 23 -or -not $stderrResult.Text.Contains('native stderr marker')) {
    throw 'Native stderr/exit-code capture did not preserve the marker and exit code 23.'
}
Assert-NativeProcessSucceeded -Result $stderrResult -Label 'allowed native probe' -AllowFailure
$allowFailureWorked = $true
$strictFailure = $false
try { Assert-NativeProcessSucceeded -Result $stderrResult -Label 'strict native probe' }
catch [System.InvalidOperationException] {
    $strictFailure = $_.Exception.Message -eq 'strict native probe failed with exit code 23.'
}
if (-not $strictFailure) { throw 'A non-allowed native failure did not produce the expected controlled exception.' }

$expectedId = 'a' * 64
$expectedLabel = 'round1-native-probe'
$removeCalls = [System.Collections.Generic.List[string]]::new()
$inspectName = {
    param($name, $format)
    if ($name -cne 'bing-sftp-validation-probe' -or
        $format -cne '{{.Id}}|{{index .Config.Labels "org.bing-utils.sftp-validation"}}') {
        throw 'Container name lookup did not receive one complete Go format argument.'
    }
    [pscustomobject]@{ ExitCode = 0; Text = "$expectedId|$expectedLabel" }
}.GetNewClosure()
$discovery = Find-RunContainerByName -Name 'bing-sftp-validation-probe' `
    -LabelKey 'org.bing-utils.sftp-validation' -LabelValue $expectedLabel -InspectByName $inspectName
$inspectId = {
    param($id, $format)
    if ($id -cne $expectedId -or $format -cne '{{.Id}}|{{index .Config.Labels "org.bing-utils.sftp-validation"}}') {
        throw 'Identity recheck did not receive the expected ID and single format argument.'
    }
    [pscustomobject]@{ ExitCode = 0; Text = "$expectedId|$expectedLabel" }
}.GetNewClosure()
$remove = { param($id) $removeCalls.Add($id); [pscustomobject]@{ ExitCode = 0; Text = $id } }.GetNewClosure()
$recovery = Remove-RunContainerByIdentity -ContainerId $discovery.ContainerId `
    -LabelKey 'org.bing-utils.sftp-validation' -LabelValue $expectedLabel `
    -InspectById $inspectId -RemoveById $remove
if ($discovery.Status -ne 'FOUND' -or $recovery.Status -ne 'REMOVED' -or
    $removeCalls.Count -ne 1 -or $removeCalls[0] -cne $expectedId) {
    throw 'Partial-create recovery did not discover and remove the uniquely labeled task container.'
}

$mismatchedRemoveCalls = [System.Collections.Generic.List[string]]::new()
$mismatchInspector = { param($id, $format) [pscustomobject]@{ ExitCode = 0; Text = "$expectedId|other-run" } }.GetNewClosure()
$neverRemove = { param($id) $mismatchedRemoveCalls.Add($id); [pscustomobject]@{ ExitCode = 0; Text = $id } }.GetNewClosure()
$mismatch = Remove-RunContainerByIdentity -ContainerId $expectedId `
    -LabelKey 'org.bing-utils.sftp-validation' -LabelValue $expectedLabel `
    -InspectById $mismatchInspector -RemoveById $neverRemove
if ($mismatch.Status -ne 'IDENTITY_MISMATCH' -or $mismatchedRemoveCalls.Count -ne 0) {
    throw 'A label mismatch did not prevent removal.'
}
$discoveryFailure = Find-RunContainerByName -Name 'bing-sftp-validation-probe' `
    -LabelKey 'org.bing-utils.sftp-validation' -LabelValue $expectedLabel -InspectByName {
        param($name, $format)
        [pscustomobject]@{ ExitCode = 1; Text = 'Docker daemon unavailable during container discovery.' }
    }
$cleanupErrors = [System.Collections.Generic.List[string]]::new()
if ($discoveryFailure.Status -eq 'UNKNOWN') { $cleanupErrors.Add($discoveryFailure.Error) }
if ($discoveryFailure.Status -ne 'UNKNOWN' -or $cleanupErrors.Count -ne 1 -or
    $cleanupErrors[0] -notmatch 'cleanup status is unknown') {
    throw 'Failed discovery did not produce an explicit unknown cleanup status and cleanup error.'
}

$processSequence = [System.Collections.Generic.Queue[object]]::new()
$processSequence.Enqueue($null)
$processSequence.Enqueue(@([pscustomobject]@{ ProcessName = 'com.docker.backend'; Id = 4040 }))
$processQuery = { $next = $processSequence.Dequeue(); if ($null -ne $next) { $next } }.GetNewClosure()
$beforeSnapshot = Get-DockerDesktopProcessSnapshot -ProcessQuery $processQuery
$afterSnapshot = Get-DockerDesktopProcessSnapshot -ProcessQuery $processQuery
if ($beforeSnapshot.IsRunning -or -not $afterSnapshot.IsRunning -or
    $afterSnapshot.Processes[0].Name -cne 'com.docker.backend') {
    throw 'Process snapshot helper did not reflect the controlled before/after state change.'
}

$evidence = [pscustomobject]@{
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    NativeArgumentMode = [string]$PSNativeCommandArgumentPassing
    NativeCommandUseErrorActionPreference = $PSNativeCommandUseErrorActionPreference
    NativeArgv = $actualArguments
    StderrExitCode = $stderrResult.ExitCode
    StderrCaptured = $stderrResult.Text.Contains('native stderr marker')
    AllowFailureReturned = $allowFailureWorked
    StrictFailureThrowVerified = $strictFailure
    PartialCreateDiscovery = $discovery.Status
    PartialCreateCleanup = $recovery.Status
    RemovedContainerId = $removeCalls[0]
    LabelMismatch = $mismatch.Status
    LabelMismatchRemoveCalls = $mismatchedRemoveCalls.Count
    FailedDiscovery = $discoveryFailure.Status
    FailedDiscoveryCleanupError = $cleanupErrors[0]
    ProcessSnapshotBefore = $beforeSnapshot
    ProcessSnapshotAfter = $afterSnapshot
}
$absoluteEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
[void](New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteEvidencePath) -Force)
[System.IO.File]::WriteAllText($absoluteEvidencePath, ($evidence | ConvertTo-Json -Depth 8), $utf8)
Write-Output "Semantic probes passed. Evidence: $absoluteEvidencePath"
