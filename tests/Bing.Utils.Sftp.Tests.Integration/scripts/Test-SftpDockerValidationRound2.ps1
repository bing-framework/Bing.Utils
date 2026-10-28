#requires -Version 7.3
[CmdletBinding()]
param(
    [string]$EvidencePath = (Join-Path $PSScriptRoot '..\..\..\ai_docs\tasks\2026-10-28-sftp-docker-validation\artifacts\round2-native-semantics.json'),
    [switch]$CopyArgumentsOnly
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
    throw 'The Round 2 probe requires PowerShell 7.3+ Standard argument passing.'
}

$pythonCommand = Get-Command python -ErrorAction Stop
$containerId = 'b' * 64
$containerName = 'bing-sftp-round2-probe'
$labelKey = 'org.bing-utils.sftp-validation'
$labelValue = 'round2-local-probe'
$formatTemplate = Get-DockerInspectFormatTemplate $labelKey

function Invoke-RunnerContainerFlowProbe {
    param(
        [Parameter(Mandatory)][string[]]$NativeArguments,
        [ValidateSet('OWNED', 'WRONG_LABEL', 'FAILED', 'THROW', 'NOT_FOUND')]
        [string]$Discovery = 'OWNED',
        [ValidateSet('OWNED', 'FAILED')]
        [string]$Identity = 'OWNED',
        [ValidateSet('SUCCESS', 'FAILED', 'THROW')]
        [string]$Removal = 'SUCCESS'
    )

    $state = New-RunContainerState
    $cleanupErrors = [System.Collections.Generic.List[string]]::new()
    $nameCalls = [System.Collections.Generic.List[string]]::new()
    $identityCalls = [System.Collections.Generic.List[string]]::new()
    $removeCalls = [System.Collections.Generic.List[string]]::new()
    $failure = $null

    $run = {
        $result = Invoke-NativeProcess -Executable $pythonCommand.Source -Arguments $NativeArguments
        Assert-NativeProcessSucceeded -Result $result -Label 'Docker container create probe'
        return $result
    }.GetNewClosure()
    $inspectByName = {
        param($name, $format)
        $nameCalls.Add($name)
        if ($name -cne $containerName -or $format -cne $formatTemplate) {
            throw 'The runner cleanup did not inspect the unique container name with one complete format argument.'
        }
        switch ($Discovery) {
            'OWNED' { return [pscustomobject]@{ ExitCode = 0; Text = "$containerId|$labelValue" } }
            'WRONG_LABEL' { return [pscustomobject]@{ ExitCode = 0; Text = "$containerId|another-run" } }
            'FAILED' { return [pscustomobject]@{ ExitCode = 17; Text = 'Docker daemon unavailable during name discovery.' } }
            'THROW' { throw 'Injected name-discovery exception.' }
            'NOT_FOUND' { return [pscustomobject]@{ ExitCode = 1; Text = 'Error: No such object: bing-sftp-round2-probe' } }
        }
    }.GetNewClosure()
    $inspectById = {
        param($id, $format)
        $identityCalls.Add($id)
        if ($id -cne $containerId -or $format -cne $formatTemplate) {
            throw 'The runner cleanup did not recheck the expected ID with one complete format argument.'
        }
        if ($Identity -eq 'FAILED') {
            return [pscustomobject]@{ ExitCode = 17; Text = 'Docker daemon unavailable during identity check.' }
        }
        return [pscustomobject]@{ ExitCode = 0; Text = "$containerId|$labelValue" }
    }.GetNewClosure()
    $removeById = {
        param($id)
        $removeCalls.Add($id)
        if ($id -cne $containerId) { throw 'The runner attempted to remove an unexpected container ID.' }
        if ($Removal -eq 'THROW') { throw 'Injected container-removal exception.' }
        if ($Removal -eq 'FAILED') { return [pscustomobject]@{ ExitCode = 17; Text = 'Injected remove failure.' } }
        return [pscustomobject]@{ ExitCode = 0; Text = $id }
    }.GetNewClosure()

    try {
        [void](Invoke-RunContainerAttempt -State $state -RunContainer $run)
    }
    catch {
        $failure = $_.Exception.Message
    }
    finally {
        [void](Complete-RunContainerCleanup -State $state -Name $containerName `
            -LabelKey $labelKey -LabelValue $labelValue -CleanupErrors $cleanupErrors `
            -InspectByName $inspectByName -InspectById $inspectById -RemoveById $removeById)
    }

    return [pscustomobject]@{
        State = $state
        Failure = $failure
        NameCalls = @($nameCalls)
        IdentityCalls = @($identityCalls)
        RemoveCalls = @($removeCalls)
        CleanupErrors = @($cleanupErrors)
    }
}

if ($CopyArgumentsOnly) {
    $runnerPath = Join-Path $PSScriptRoot 'Invoke-SftpDockerValidation.ps1'
    $tokens = $null
    $parseErrors = $null
    $runnerAst = [System.Management.Automation.Language.Parser]::ParseFile(
        $runnerPath, [ref]$tokens, [ref]$parseErrors
    )
    if ($parseErrors.Count -gt 0) { throw 'The runner failed to parse before extracting docker cp commands.' }

    $copyLabels = @('docker-copy-public-key', 'docker-seed-sample')
    $copyCommands = @($runnerAst.FindAll({
        param($node)
        if ($node -isnot [System.Management.Automation.Language.CommandAst] -or
            $node.GetCommandName() -ne 'Invoke-Docker' -or $node.CommandElements.Count -lt 2) {
            return $false
        }
        $labelNode = $node.CommandElements[1]
        return $labelNode -is [System.Management.Automation.Language.StringConstantExpressionAst] -and
            $labelNode.Value -in $copyLabels
    }, $true))
    if ($copyCommands.Count -ne 2) { throw "Expected exactly two runner docker cp calls; found $($copyCommands.Count)." }

    $containerState = [pscustomobject]@{ ContainerId = 'c' * 64 }
    $publicKeyPath = 'C:\SFTP Validation\generated client key.pub'
    $sampleSourcePath = 'D:\SFTP Validation\中文业务数据.tsv'
    $runId = 'round3-probe'
    $script:round3CapturedCalls = @{}
    function Invoke-Docker {
        param([string]$Label, [string[]]$Arguments, [switch]$AllowFailure)
        $script:round3CapturedCalls[$Label] = @($Arguments)
        return [pscustomobject]@{ ExitCode = 0; Text = '' }
    }

    $parameterEvidence = [System.Collections.Generic.List[object]]::new()
    foreach ($command in $copyCommands) {
        $label = [string]$command.CommandElements[1].Value
        $null = Invoke-Expression $command.Extent.Text
        $arguments = @($script:round3CapturedCalls[$label])
        if ($arguments.Count -ne 3 -or $arguments[0] -cne 'cp') {
            throw "$label did not receive exactly the docker cp command and two path arguments."
        }
        $expectedSource = if ($label -eq 'docker-copy-public-key') { $publicKeyPath } else { $sampleSourcePath }
        $expectedTarget = if ($label -eq 'docker-copy-public-key') {
            "$($containerState.ContainerId):/tmp/sftp-validation-client.pub"
        }
        else {
            "$($containerState.ContainerId):/home/integration/upload/integration-capabilities-$runId.tsv"
        }
        if ($arguments[1] -cne $expectedSource -or $arguments[2] -cne $expectedTarget) {
            throw "$label argv did not preserve the expected source and container target as single arguments."
        }

        $argvProgram = 'import json,sys; print(json.dumps(sys.argv[1:]))'
        $nativeResult = Invoke-NativeProcess -Executable $pythonCommand.Source `
            -Arguments (@('-c', $argvProgram) + $arguments)
        Assert-NativeProcessSucceeded -Result $nativeResult -Label "$label native argv probe"
        $nativeArguments = @($nativeResult.Text.Trim() | ConvertFrom-Json)
        if ($nativeArguments.Count -ne $arguments.Count) { throw "$label native argv count changed." }
        for ($index = 0; $index -lt $arguments.Count; $index++) {
            if ($nativeArguments[$index] -cne $arguments[$index]) { throw "$label native argv[$index] changed." }
        }
        $parameterEvidence.Add([pscustomobject]@{
            Label = $label
            CapturedArguments = $arguments
            NativeArguments = $nativeArguments
        })
    }

    $copyEvidence = [pscustomobject]@{
        PowerShellVersion = $PSVersionTable.PSVersion.ToString()
        NativeArgumentMode = [string]$PSNativeCommandArgumentPassing
        DockerStarted = $false
        SyntheticContainerId = $containerState.ContainerId
        HostSourcePathWithSpaces = $publicKeyPath
        Calls = @($parameterEvidence)
    }
    $absoluteEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
    [void](New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteEvidencePath) -Force)
    [System.IO.File]::WriteAllText($absoluteEvidencePath, ($copyEvidence | ConvertTo-Json -Depth 6), $utf8)
    Write-Output "Runner docker cp argument probes passed. Evidence: $absoluteEvidencePath"
    return
}

$warningCode = 'import sys; print("' + $containerId + '", flush=True); sys.stderr.write("docker warning marker\n"); sys.stderr.flush()'
$warningArguments = @('-c', $warningCode)
$warningNative = Invoke-NativeProcess -Executable $pythonCommand.Source -Arguments $warningArguments
if ($warningNative.ExitCode -ne 0 -or -not $warningNative.Text.Contains($containerId) -or
    -not $warningNative.Text.Contains('docker warning marker')) {
    throw 'The native run probe did not produce exit 0 with both the container ID and stderr warning.'
}
$warningFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $warningArguments
if ($warningFlow.State.RunOutputStatus -ne 'INVALID' -or $warningFlow.Failure -ne 'Docker did not return the expected new container ID.' -or
    $warningFlow.State.ContainerId -cne $containerId -or $warningFlow.State.CleanupStatus -ne 'REMOVED' -or
    -not $warningFlow.State.ContainerCleaned -or $warningFlow.NameCalls.Count -ne 1 -or
    $warningFlow.IdentityCalls.Count -ne 1 -or $warningFlow.RemoveCalls.Count -ne 1 -or $warningFlow.CleanupErrors.Count -ne 0) {
    throw 'ID plus stderr did not fail validation and recover through name, label, identity recheck, and removal.'
}

$malformedArguments = @('-c', 'print("malformed container response", flush=True)')
$malformedNative = Invoke-NativeProcess -Executable $pythonCommand.Source -Arguments $malformedArguments
if ($malformedNative.ExitCode -ne 0 -or $malformedNative.Text.Trim() -cne 'malformed container response') {
    throw 'The malformed run probe did not produce its expected native output.'
}
$malformedFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments
if ($malformedFlow.State.RunOutputStatus -ne 'INVALID' -or $malformedFlow.State.CleanupStatus -ne 'REMOVED' -or
    -not $malformedFlow.State.ContainerCleaned -or $malformedFlow.NameCalls.Count -ne 1 -or
    $malformedFlow.IdentityCalls.Count -ne 1 -or $malformedFlow.RemoveCalls.Count -ne 1 -or
    $malformedFlow.CleanupErrors.Count -ne 0) {
    throw 'Malformed run output did not enter the actual runner recovery and cleanup flow.'
}

$wrongLabelFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Discovery WRONG_LABEL
$failedDiscoveryFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Discovery FAILED
$throwingDiscoveryFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Discovery THROW
$notFoundFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Discovery NOT_FOUND
$failedRemovalFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Removal FAILED
$throwingRemovalFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Removal THROW
$failedIdentityFlow = Invoke-RunnerContainerFlowProbe -NativeArguments $malformedArguments -Identity FAILED

if ($wrongLabelFlow.State.DiscoveryStatus -ne 'IDENTITY_MISMATCH' -or
    $wrongLabelFlow.State.CleanupStatus -ne 'UNKNOWN' -or $wrongLabelFlow.RemoveCalls.Count -ne 0 -or
    $wrongLabelFlow.CleanupErrors.Count -ne 1) { throw 'A label mismatch was not preserved as non-removable unknown cleanup.' }
if ($failedDiscoveryFlow.State.CleanupStatus -ne 'UNKNOWN' -or $failedDiscoveryFlow.CleanupErrors.Count -ne 1) {
    throw 'A failed name discovery did not record UNKNOWN and a cleanup error.'
}
if ($throwingDiscoveryFlow.State.DiscoveryStatus -ne 'DISCOVERY_FAILED' -or
    $throwingDiscoveryFlow.State.CleanupStatus -ne 'UNKNOWN' -or $throwingDiscoveryFlow.CleanupErrors.Count -ne 1) {
    throw 'A thrown discovery error did not record UNKNOWN and a cleanup error.'
}
if ($notFoundFlow.State.DiscoveryStatus -ne 'NOT_FOUND' -or $notFoundFlow.State.CleanupStatus -ne 'NOT_FOUND' -or
    $notFoundFlow.CleanupErrors.Count -ne 0 -or $notFoundFlow.RemoveCalls.Count -ne 0) {
    throw 'Explicitly absent container was not reported as NOT_FOUND without a cleanup error.'
}
if ($failedRemovalFlow.State.CleanupStatus -ne 'FAILED' -or $failedRemovalFlow.State.ContainerCleaned -or
    $failedRemovalFlow.CleanupErrors.Count -ne 1) { throw 'A nonzero removal was not reported as FAILED with a cleanup error.' }
if ($throwingRemovalFlow.State.CleanupStatus -ne 'UNKNOWN' -or $throwingRemovalFlow.State.ContainerCleaned -or
    $throwingRemovalFlow.CleanupErrors.Count -ne 1) { throw 'A thrown removal was not reported as UNKNOWN with a cleanup error.' }
if ($failedIdentityFlow.State.CleanupStatus -ne 'UNKNOWN' -or $failedIdentityFlow.State.ContainerCleaned -or
    $failedIdentityFlow.RemoveCalls.Count -ne 0 -or $failedIdentityFlow.CleanupErrors.Count -ne 1) {
    throw 'A failed identity recheck did not prevent removal and report UNKNOWN.'
}

$evidence = [pscustomobject]@{
    PowerShellVersion = $PSVersionTable.PSVersion.ToString()
    NativeArgumentMode = [string]$PSNativeCommandArgumentPassing
    DockerStarted = $false
    NativeRunWithStderr = [pscustomobject]@{ ExitCode = $warningNative.ExitCode; OutputContainsId = $warningNative.Text.Contains($containerId); StderrCaptured = $warningNative.Text.Contains('docker warning marker') }
    NativeMalformedRun = [pscustomobject]@{ ExitCode = $malformedNative.ExitCode; Output = $malformedNative.Text.Trim() }
    IdWithStderrFlow = $warningFlow
    MalformedOutputFlow = $malformedFlow
    WrongLabelFlow = $wrongLabelFlow
    FailedDiscoveryFlow = $failedDiscoveryFlow
    ThrowingDiscoveryFlow = $throwingDiscoveryFlow
    NotFoundFlow = $notFoundFlow
    FailedRemovalFlow = $failedRemovalFlow
    ThrowingRemovalFlow = $throwingRemovalFlow
    FailedIdentityFlow = $failedIdentityFlow
}
$absoluteEvidencePath = [System.IO.Path]::GetFullPath($EvidencePath)
[void](New-Item -ItemType Directory -Path (Split-Path -Parent $absoluteEvidencePath) -Force)
[System.IO.File]::WriteAllText($absoluteEvidencePath, ($evidence | ConvertTo-Json -Depth 12), $utf8)
Write-Output "Round 2 runner-flow probes passed. Evidence: $absoluteEvidencePath"
