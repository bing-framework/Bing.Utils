#requires -Version 7.3
[CmdletBinding()]
param(
    [ValidateRange(30, 600)]
    [int]$DockerStartupTimeoutSeconds = 180,
    [string]$Image = 'atmoz/sftp:latest'
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandArgumentPassing = 'Standard'
$PSNativeCommandUseErrorActionPreference = $false
if ($PSNativeCommandArgumentPassing -ne 'Standard') { throw 'PowerShell Standard native argument passing could not be selected.' }
$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = $utf8
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8
. (Join-Path $PSScriptRoot 'SftpDockerValidation.Helpers.ps1')

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..\..'))
$taskRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot 'ai_docs\tasks\2026-10-28-sftp-docker-validation'))
$runId = '{0}-{1}' -f [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
$runDirectory = Join-Path (Join-Path $taskRoot 'artifacts') $runId
$temporaryRoot = Join-Path $taskRoot ".tmp-$runId"
$containerName = "bing-sftp-validation-$($runId -replace '[^a-zA-Z0-9-]', '-')"
$containerLabelKey = 'org.bing-utils.sftp-validation'
$containerLabelValue = $runId
$containerState = New-RunContainerState
$temporaryCreated = $false
$temporaryCleaned = $false
$dockerDesktopStarted = $false
$dockerProcessSnapshotAtProbe = $null
$dockerProcessSnapshotAtFinal = $null
$dockerProbeText = ''
$dockerProbeExitCode = $null
$selectedPort = $null
$hostKeyFingerprint = $null
$generatedPassword = $null
$failureMessage = $null
$unitSummary = $null
$passwordSummary = $null
$privateKeySummary = $null
$sampleSummary = $null
$cleanupErrors = [System.Collections.Generic.List[string]]::new()
$environmentNames = @(
    'BING_SFTP_HOST', 'BING_SFTP_PORT', 'BING_SFTP_USER', 'BING_SFTP_PASSWORD',
    'BING_SFTP_PRIVATE_KEY_PATH', 'BING_SFTP_PRIVATE_KEY_PASSPHRASE',
    'BING_SFTP_HOST_KEY_SHA256', 'BING_SFTP_REMOTE_DIRECTORY',
    'BING_SFTP_PRIVATE_KEY', 'BING_SFTP_HOSTKEY_SHA256', 'BING_REPORT_CULTURE'
)
$savedEnvironment = @{}
foreach ($name in $environmentNames) {
    $savedEnvironment[$name] = [pscustomobject]@{
        Exists = Test-Path "Env:$name"
        Value = [Environment]::GetEnvironmentVariable($name, 'Process')
    }
}

function Save-Text([string]$Path, [string]$Text) {
    $parent = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $parent)) {
        [void](New-Item -ItemType Directory -Path $parent -Force)
    }

    if ($script:generatedPassword) {
        $Text = $Text.Replace($script:generatedPassword, '[REDACTED]')
    }
    $Text = [regex]::Replace($Text,
        '(?s)-----BEGIN [A-Z ]*PRIVATE KEY-----.*?-----END [A-Z ]*PRIVATE KEY-----',
        '[REDACTED PRIVATE KEY]')
    [System.IO.File]::WriteAllText($Path, $Text, $utf8)
}

function Write-Status([string]$Message) {
    $line = '[{0}] {1}' -f [DateTime]::UtcNow.ToString('o'), $Message
    Write-Host $line
    Save-Text (Join-Path $runDirectory 'runner.log') ((Get-Content -LiteralPath (Join-Path $runDirectory 'runner.log') -Raw -Encoding UTF8 -ErrorAction SilentlyContinue) + $line + [Environment]::NewLine)
}

function Invoke-Docker([string]$Label, [string[]]$Arguments, [switch]$AllowFailure) {
    $result = Invoke-NativeProcess -Executable 'docker' -Arguments $Arguments
    Save-Text (Join-Path $runDirectory "$Label.log") $result.Text
    Assert-NativeProcessSucceeded -Result $result -Label $Label -AllowFailure:$AllowFailure
    return $result
}

function Invoke-DotNet([string]$Label, [string[]]$Arguments, [int]$ExpectedTests) {
    $result = Invoke-NativeProcess -Executable 'dotnet' -Arguments $Arguments
    Save-Text (Join-Path $runDirectory "$Label.log") $result.Text
    Assert-NativeProcessSucceeded -Result $result -Label $Label

    $trxPath = Join-Path $runDirectory "$Label.trx"
    if (-not (Test-Path -LiteralPath $trxPath)) {
        throw "$Label did not produce its expected TRX file."
    }
    [xml]$trx = [System.IO.File]::ReadAllText($trxPath, [System.Text.Encoding]::UTF8)
    $results = @($trx.SelectNodes("//*[local-name()='UnitTestResult']"))
    $passed = @($results | Where-Object { $_.GetAttribute('outcome') -eq 'Passed' }).Count
    $failed = @($results | Where-Object { $_.GetAttribute('outcome') -in @('Failed', 'Error') }).Count
    $skipped = @($results | Where-Object { $_.GetAttribute('outcome') -in @('NotExecuted', 'Inconclusive', 'Skipped') }).Count
    $summary = [pscustomobject]@{
        Label = $Label
        Total = $results.Count
        Passed = $passed
        Failed = $failed
        Skipped = $skipped
        ExitCode = $result.ExitCode
        Trx = [System.IO.Path]::GetFileName($trxPath)
    }
    if ($results.Count -ne $ExpectedTests -or $passed -ne $ExpectedTests -or $failed -ne 0 -or $skipped -ne 0) {
        throw "$Label result mismatch: total=$($results.Count), passed=$passed, failed=$failed, skipped=$skipped; expected $ExpectedTests passing tests and no skips."
    }
    Save-Text $trxPath ([System.IO.File]::ReadAllText($trxPath, [System.Text.Encoding]::UTF8))
    return $summary
}

function Set-ProcessEnvironment([hashtable]$Values) {
    foreach ($name in $environmentNames) {
        [Environment]::SetEnvironmentVariable($name, $null, 'Process')
    }
    foreach ($entry in $Values.GetEnumerator()) {
        [Environment]::SetEnvironmentVariable($entry.Key, [string]$entry.Value, 'Process')
    }
}

function New-AvailableLoopbackPort {
    $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
    $listener.Start()
    try { return [int]$listener.LocalEndpoint.Port }
    finally { $listener.Stop() }
}

function Wait-ForSftpPort([int]$Port, [int]$TimeoutSeconds) {
    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    while ([DateTime]::UtcNow -lt $deadline) {
        $client = [System.Net.Sockets.TcpClient]::new()
        try {
            $pending = $client.ConnectAsync('127.0.0.1', $Port)
            if ($pending.Wait(800) -and $client.Connected) { return }
        }
        catch { }
        finally { $client.Dispose() }
        Start-Sleep -Milliseconds 500
    }
    throw "SFTP container port 127.0.0.1:$Port did not become ready within $TimeoutSeconds seconds."
}

try {
    [void](New-Item -ItemType Directory -Path $runDirectory -Force)
    [void](New-Item -ItemType Directory -Path $temporaryRoot -Force)
    $temporaryCreated = $true
    Write-Status "RunId=$runId"

    $dockerCliVersion = 'unavailable'
    $initialContext = 'unavailable'
    if (Get-Command docker -ErrorAction SilentlyContinue) {
        $cliVersion = Invoke-NativeProcess -Executable 'docker' -Arguments @('--version')
        Assert-NativeProcessSucceeded -Result $cliVersion -Label 'docker version'
        $dockerCliVersion = $cliVersion.Text.Trim()
        $contextResult = Invoke-NativeProcess -Executable 'docker' -Arguments @('context', 'show')
        Assert-NativeProcessSucceeded -Result $contextResult -Label 'docker context show'
        $initialContext = $contextResult.Text.Trim()
    }
    Save-Text (Join-Path $runDirectory 'environment.txt') @"
RunId=$runId
UtcStarted=$([DateTime]::UtcNow.ToString('o'))
DockerCli=$dockerCliVersion
DockerContext=$initialContext
"@

    $unitSummary = Invoke-DotNet 'sftp-unit' @(
        'test', 'tests/Bing.Utils.Sftp.Tests/Bing.Utils.Sftp.Tests.csproj',
        '--configuration', 'Debug', '--logger', 'trx;LogFileName=sftp-unit.trx',
        '--results-directory', $runDirectory
    ) 8

    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        throw 'Docker CLI is unavailable.'
    }
    $probeResult = Invoke-NativeProcess -Executable 'docker' -Arguments @('version', '--format', '{{.Server.Version}}')
    $dockerProbeExitCode = $probeResult.ExitCode
    $dockerProbeText = $probeResult.Text
    if ($dockerProbeExitCode -ne 0) {
        $desktopPath = 'C:\Program Files\Docker\Docker\Docker Desktop.exe'
        if (-not (Test-Path -LiteralPath $desktopPath)) {
            throw 'Docker daemon is unavailable and Docker Desktop is not installed at the standard path.'
        }
        if (-not (Get-Process -Name 'Docker Desktop' -ErrorAction SilentlyContinue)) {
            Start-Process -FilePath $desktopPath -WindowStyle Hidden
            $dockerDesktopStarted = $true
        }
        $deadline = [DateTime]::UtcNow.AddSeconds($DockerStartupTimeoutSeconds)
        do {
            Start-Sleep -Seconds 5
            $probeResult = Invoke-NativeProcess -Executable 'docker' -Arguments @('version', '--format', '{{.Server.Version}}')
            $dockerProbeExitCode = $probeResult.ExitCode
            $dockerProbeText = $probeResult.Text
            if ($dockerProbeExitCode -eq 0) { break }
        } while ([DateTime]::UtcNow -lt $deadline)
    }

    $dockerService = Get-Service -Name 'com.docker.service' -ErrorAction SilentlyContinue
    $dockerServiceStatus = if ($dockerService) { "$($dockerService.Status)/$($dockerService.StartType)" } else { 'not installed' }
    $dockerProcessSnapshotAtProbe = Get-DockerDesktopProcessSnapshot
    Save-Text (Join-Path $runDirectory 'docker-daemon-probe.log') @"
ExitCode=$dockerProbeExitCode
DockerService=$dockerServiceStatus
DockerDesktopProcessCount=$($dockerProcessSnapshotAtProbe.ProcessCount)
DockerDesktopProcessNames=$([string]::Join(',', @($dockerProcessSnapshotAtProbe.Processes | ForEach-Object { $_.Name })))
Output=$dockerProbeText
"@
    if ($dockerProbeExitCode -ne 0) {
        throw "Docker daemon did not become ready within $DockerStartupTimeoutSeconds seconds after the Docker Desktop readiness attempt."
    }

    $contextResult = Invoke-NativeProcess -Executable 'docker' -Arguments @('context', 'show')
    Assert-NativeProcessSucceeded -Result $contextResult -Label 'docker context show'
    $context = $contextResult.Text.Trim()
    $dockerVersionResult = Invoke-NativeProcess -Executable 'docker' -Arguments @('version', '--format', 'client={{.Client.Version}} server={{.Server.Version}}')
    Assert-NativeProcessSucceeded -Result $dockerVersionResult -Label 'docker version'
    $dockerVersion = $dockerVersionResult.Text.Trim()
    Save-Text (Join-Path $runDirectory 'environment.txt') @"
RunId=$runId
UtcStarted=$([DateTime]::UtcNow.ToString('o'))
DockerContext=$context
DockerVersion=$dockerVersion
DockerDesktopStartedByRunner=$dockerDesktopStarted
DockerDesktopRunningAtDaemonProbe=$($dockerProcessSnapshotAtProbe.IsRunning)
DockerDesktopProbeSampledAtUtc=$($dockerProcessSnapshotAtProbe.SampledAtUtc)
ImageRequested=$Image
ContainerLabel=$containerLabelKey=$containerLabelValue
PublishedAddress=127.0.0.1
MountedHostDirectories=none
Privileged=false
DockerSocketMounted=false
"@

    [void](Invoke-Docker 'docker-pull' @('pull', $Image))
    $imageInfo = Invoke-Docker 'docker-image-identity' @('image', 'inspect', '--format', '{{.Id}}|{{json .RepoDigests}}|{{.Os}}|{{.Architecture}}|{{.Created}}', $Image)
    Save-Text (Join-Path $runDirectory 'image-identity.txt') $imageInfo.Text
    $imageId = ($imageInfo.Text.Trim() -split '\|')[0]
    if ($imageId -notmatch '^sha256:[0-9a-f]{64}$') { throw 'Could not resolve the pulled image to an immutable image ID.' }

    $privateKeyPath = Join-Path $temporaryRoot 'client_rsa'
    $publicKeyPath = "$privateKeyPath.pub"
    $keyResult = Invoke-NativeProcess -Executable 'ssh-keygen' -Arguments @(
        '-q', '-t', 'rsa', '-b', '3072', '-m', 'PEM', '-N', '',
        '-C', 'bing-sftp-docker-validation', '-f', $privateKeyPath
    )
    Save-Text (Join-Path $runDirectory 'ssh-keygen.log') $keyResult.Text
    Assert-NativeProcessSucceeded -Result $keyResult -Label 'ssh-keygen'
    if (-not (Test-Path -LiteralPath $publicKeyPath)) {
        throw 'ssh-keygen failed to create the task-local client key pair.'
    }
    $randomBytes = New-Object byte[] 24
    $randomGenerator = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try { $randomGenerator.GetBytes($randomBytes) }
    finally { $randomGenerator.Dispose() }
    $generatedPassword = [Convert]::ToBase64String($randomBytes).TrimEnd('=').Replace('+', 'a').Replace('/', 'b')
    [Array]::Clear($randomBytes, 0, $randomBytes.Length)

    $selectedPort = New-AvailableLoopbackPort
    $runArguments = @(
        'run', '--detach', '--name', $containerName,
        '--label', "$containerLabelKey=$containerLabelValue",
        '--publish', "127.0.0.1:${selectedPort}:22",
        $imageId,
        "integration:${generatedPassword}:1001:100:upload"
    )
    $existingName = Invoke-Docker 'docker-container-name-check' @('container', 'inspect', $containerName) -AllowFailure
    if ($existingName.ExitCode -eq 0) { throw 'The generated container name is already in use; no existing container was changed.' }
    $runContainer = {
        Invoke-Docker 'docker-container-create' $runArguments
    }.GetNewClosure()
    $containerResult = Invoke-RunContainerAttempt -State $containerState -RunContainer $runContainer

    Wait-ForSftpPort -Port $selectedPort -TimeoutSeconds 90
    $publishedPort = Invoke-Docker 'docker-published-port' @('port', $containerState.ContainerId, '22/tcp')
    if ($publishedPort.Text.Trim() -ne "127.0.0.1:$selectedPort") {
        throw "Container port was not bound exclusively to 127.0.0.1:$selectedPort."
    }

    [void](Invoke-Docker 'docker-copy-public-key' @('cp', $publicKeyPath, "$($containerState.ContainerId):/tmp/sftp-validation-client.pub"))
    [void](Invoke-Docker 'docker-install-public-key' @(
        'exec', $containerState.ContainerId, 'sh', '-c',
        'mkdir -p /home/integration/.ssh && chown integration /home/integration/.ssh && chmod 700 /home/integration/.ssh && install -o integration -m 600 /tmp/sftp-validation-client.pub /home/integration/.ssh/authorized_keys && rm /tmp/sftp-validation-client.pub'
    ))
    $fingerprintOutput = Invoke-Docker 'docker-server-host-key' @(
        'exec', $containerState.ContainerId, 'ssh-keygen', '-lf', '/etc/ssh/ssh_host_ed25519_key.pub', '-E', 'sha256'
    )
    $fingerprintMatch = [regex]::Match($fingerprintOutput.Text, 'SHA256:[A-Za-z0-9+/]+={0,2}')
    if (-not $fingerprintMatch.Success) { throw 'Could not obtain the controlled container Ed25519 host-key fingerprint.' }
    $hostKeyFingerprint = $fingerprintMatch.Value
    Save-Text (Join-Path $runDirectory 'host-key.txt') @"
Source=container /etc/ssh/ssh_host_ed25519_key.pub via ssh-keygen -E sha256
Fingerprint=$hostKeyFingerprint
Verification=both authentication runs use this required fingerprint; the wrong-fingerprint case must be rejected, while successful SFTP operations require the negotiated key to match it
"@

    $baseEnvironment = @{
        BING_SFTP_HOST = '127.0.0.1'
        BING_SFTP_PORT = [string]$selectedPort
        BING_SFTP_USER = 'integration'
        BING_SFTP_HOST_KEY_SHA256 = $hostKeyFingerprint
        BING_SFTP_REMOTE_DIRECTORY = '/upload'
    }
    Set-ProcessEnvironment ($baseEnvironment + @{ BING_SFTP_PASSWORD = $generatedPassword })
    $passwordSummary = Invoke-DotNet 'sftp-integration-password' @(
        'test', 'tests/Bing.Utils.Sftp.Tests.Integration/Bing.Utils.Sftp.Tests.Integration.csproj',
        '--configuration', 'Debug', '--logger', 'trx;LogFileName=sftp-integration-password.trx',
        '--results-directory', $runDirectory
    ) 7

    Set-ProcessEnvironment ($baseEnvironment + @{ BING_SFTP_PRIVATE_KEY_PATH = $privateKeyPath })
    $privateKeySummary = Invoke-DotNet 'sftp-integration-private-key' @(
        'test', 'tests/Bing.Utils.Sftp.Tests.Integration/Bing.Utils.Sftp.Tests.Integration.csproj',
        '--configuration', 'Debug', '--logger', 'trx;LogFileName=sftp-integration-private-key.trx',
        '--results-directory', $runDirectory
    ) 7

    $sampleSourcePath = Join-Path $temporaryRoot '中文业务数据.tsv'
    $sampleRemotePath = "/upload/integration-capabilities-$runId.tsv"
    $sampleReportPath = Join-Path $runDirectory 'sample-report.txt'
    $sampleText = "姓名`t金额`r`n张伟`t128.75`r`n李娜`t42.50`r`n"
    $utf16LeWithBom = [System.Text.UnicodeEncoding]::new($false, $true)
    [System.IO.File]::WriteAllText($sampleSourcePath, $sampleText, $utf16LeWithBom)
    [void](Invoke-Docker 'docker-seed-sample' @('cp', $sampleSourcePath, "$($containerState.ContainerId):/home/integration/upload/integration-capabilities-$runId.tsv"))
    [void](Invoke-Docker 'docker-set-sample-mode' @(
        'exec', $containerState.ContainerId, 'chown', 'integration', "/home/integration/upload/integration-capabilities-$runId.tsv"
    ))

    Set-ProcessEnvironment ($baseEnvironment + @{
        BING_SFTP_PRIVATE_KEY = $privateKeyPath
        BING_SFTP_HOSTKEY_SHA256 = $hostKeyFingerprint
        BING_REPORT_CULTURE = 'zh-CN'
    })
    $sampleResult = Invoke-NativeProcess -Executable 'dotnet' -Arguments @(
        'run', '--project', 'samples/IntegrationCapabilities/IntegrationCapabilities.csproj',
        '--configuration', 'Debug', '--', $sampleRemotePath, $sampleReportPath
    )
    $sampleExitCode = $sampleResult.ExitCode
    Save-Text (Join-Path $runDirectory 'sample-run.log') $sampleResult.Text
    Assert-NativeProcessSucceeded -Result $sampleResult -Label 'IntegrationCapabilities network sample'
    if ($sampleExitCode -ne 0 -or -not (Test-Path -LiteralPath $sampleReportPath)) {
        throw "IntegrationCapabilities network sample failed with exit code $sampleExitCode."
    }

    $reportBytes = [System.IO.File]::ReadAllBytes($sampleReportPath)
    $reportHasBom = $reportBytes.Length -ge 3 -and $reportBytes[0] -eq 0xEF -and $reportBytes[1] -eq 0xBB -and $reportBytes[2] -eq 0xBF
    $strictUtf8 = [System.Text.UTF8Encoding]::new($false, $true)
    $reportText = $strictUtf8.GetString($reportBytes)
    if ($reportHasBom -or -not $reportText.Contains('张伟') -or -not $reportText.Contains('李娜') -or
        -not $reportText.Contains('128.75') -or -not $reportText.Contains('42.5') -or -not $reportText.Contains('2 条')) {
        throw 'Sample report did not contain the expected two Chinese rows, amounts, and BOM-free UTF-8 encoding.'
    }
    $sampleSummary = [pscustomobject]@{
        ExitCode = $sampleExitCode
        SourceEncoding = 'UTF-16LE with BOM'
        SourceRows = 2
        Report = [System.IO.Path]::GetFileName($sampleReportPath)
        ReportEncoding = 'UTF-8 without BOM; strict decode passed'
        NamesVerified = @('张伟', '李娜')
        AmountsVerified = @('128.75', '42.5')
        RemotePath = $sampleRemotePath
    }
    Save-Text (Join-Path $runDirectory 'sample-verification.txt') ($sampleSummary | ConvertTo-Json -Depth 4)
    Write-Status 'L1 unit test, both L3 authentication matrices, and the network sample passed.'
}
catch {
    $failureMessage = $_.Exception.Message
    try { Write-Status "FAILED: $failureMessage" } catch { }
}
finally {
    [void](Complete-RunContainerCleanup -State $containerState `
        -Name $containerName -LabelKey $containerLabelKey -LabelValue $containerLabelValue `
        -CleanupErrors $cleanupErrors -InspectByName {
            param($name, $format)
            Invoke-Docker 'docker-container-cleanup-discovery' @(
                'container', 'inspect', '--format', $format, $name
            ) -AllowFailure
        } -InspectById {
            param($id, $format)
            Invoke-Docker 'docker-container-cleanup-identity' @(
                'container', 'inspect', '--format', $format, $id
            ) -AllowFailure
        } -RemoveById {
            param($id)
            Invoke-Docker 'docker-container-remove' @('rm', '--force', $id) -AllowFailure
        } -BeforeRemove {
            param($id)
            $containerLogs = Invoke-Docker 'docker-container-logs' @('logs', $id) -AllowFailure
            Save-Text (Join-Path $runDirectory 'container.log') $containerLogs.Text
        })

    if ($temporaryCreated -and (Test-Path -LiteralPath $temporaryRoot)) {
        try {
            $fullTask = [System.IO.Path]::GetFullPath($taskRoot).TrimEnd('\', '/') + [System.IO.Path]::DirectorySeparatorChar
            $fullTemporary = [System.IO.Path]::GetFullPath($temporaryRoot)
            $expectedName = "\.tmp-$([regex]::Escape($runId))$"
            if (-not $fullTemporary.StartsWith($fullTask, [System.StringComparison]::OrdinalIgnoreCase) -or
                [System.IO.Path]::GetDirectoryName($fullTemporary).TrimEnd('\', '/') -ne $taskRoot.TrimEnd('\', '/') -or
                [System.IO.Path]::GetFileName($fullTemporary) -notmatch $expectedName) {
                throw 'Temporary cleanup path failed the task-root containment check.'
            }
            Remove-Item -LiteralPath $fullTemporary -Recurse -Force
            $temporaryCleaned = -not (Test-Path -LiteralPath $fullTemporary)
        }
        catch { $cleanupErrors.Add('Temporary directory cleanup failed: ' + $_.Exception.Message) }
    }

    foreach ($name in $environmentNames) {
        $saved = $savedEnvironment[$name]
        if ($saved.Exists) { [Environment]::SetEnvironmentVariable($name, $saved.Value, 'Process') }
        else { [Environment]::SetEnvironmentVariable($name, $null, 'Process') }
    }

    $dockerProcessSnapshotAtFinal = Get-DockerDesktopProcessSnapshot
    $summary = [pscustomobject]@{
        RunId = $runId
        Status = if ($failureMessage -match 'Docker (daemon|CLI)|Docker Desktop|SFTP container port' -or $failureMessage -match 'Docker.*(unavailable|ready|installed)') { 'BLOCKED_EXTERNAL' } elseif ($failureMessage -or $cleanupErrors.Count -gt 0) { 'FAILED' } else { 'PASS' }
        Failure = $failureMessage
        DockerDesktopStartedByRunner = $dockerDesktopStarted
        DockerDesktopProcessSnapshotAtDaemonProbe = $dockerProcessSnapshotAtProbe
        DockerDesktopProcessSnapshotAtFinal = $dockerProcessSnapshotAtFinal
        DockerDesktopLeftRunning = $dockerProcessSnapshotAtFinal.IsRunning
        ContainerId = $containerState.ContainerId
        ContainerCleaned = $containerState.ContainerCleaned
        ContainerDiscoveryStatus = $containerState.DiscoveryStatus
        ContainerCleanupStatus = $containerState.CleanupStatus
        TemporaryDirectoryCleaned = $temporaryCleaned
        EnvironmentRestored = $true
        Unit = $unitSummary
        PasswordAuthentication = $passwordSummary
        PrivateKeyAuthentication = $privateKeySummary
        NetworkSample = $sampleSummary
        UnverifiedFaultWindows = @('rename request accepted by server and reply dropped', 'remote temporary-file deletion failure')
        CleanupErrors = @($cleanupErrors)
    }
    try { Save-Text (Join-Path $runDirectory 'run-summary.json') ($summary | ConvertTo-Json -Depth 8) }
    catch { Write-Host ('Unable to save run summary: ' + $_.Exception.Message) }
    if ($failureMessage) { Write-Host "Validation failed: $failureMessage" }
    if ($cleanupErrors.Count -gt 0) { Write-Host ('Cleanup issues: ' + [string]::Join('; ', $cleanupErrors)) }
    if (-not $failureMessage -and $cleanupErrors.Count -eq 0) { Write-Host "Validation completed. Evidence: $runDirectory" }
}

if ($failureMessage -or $cleanupErrors.Count -gt 0) { exit 1 }
exit 0
