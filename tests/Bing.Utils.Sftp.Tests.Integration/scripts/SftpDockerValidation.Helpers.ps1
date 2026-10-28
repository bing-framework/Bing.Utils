function Invoke-NativeProcess {
    param(
        [Parameter(Mandatory)][string]$Executable,
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Arguments
    )

    $output = @(& $Executable @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    $text = [string]::Join([Environment]::NewLine, @($output | ForEach-Object { [string]$_ }))
    return [pscustomobject]@{ ExitCode = $exitCode; Text = $text }
}

function Assert-NativeProcessSucceeded {
    param(
        [Parameter(Mandatory)]$Result,
        [Parameter(Mandatory)][string]$Label,
        [switch]$AllowFailure
    )

    if ($Result.ExitCode -ne 0 -and -not $AllowFailure) {
        throw [System.InvalidOperationException]::new("$Label failed with exit code $($Result.ExitCode).")
    }
}

function Get-DockerInspectFormatTemplate([string]$LabelKey) {
    return '{{.Id}}|{{index .Config.Labels "' + $LabelKey + '"}}'
}

function Get-DockerDesktopProcessSnapshot {
    param([scriptblock]$ProcessQuery = {
        Get-Process -Name 'Docker Desktop', 'com.docker.backend', 'com.docker.proxy' -ErrorAction SilentlyContinue
    })

    $processes = @(& $ProcessQuery)
    return [pscustomobject]@{
        SampledAtUtc = [DateTime]::UtcNow.ToString('o')
        IsRunning = $processes.Count -gt 0
        ProcessCount = $processes.Count
        Processes = @($processes | ForEach-Object { [pscustomobject]@{ Name = $_.ProcessName; Id = $_.Id } })
    }
}

function Find-RunContainerByName {
    param(
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$LabelKey,
        [Parameter(Mandatory)][string]$LabelValue,
        [Parameter(Mandatory)][scriptblock]$InspectByName
    )

    $format = Get-DockerInspectFormatTemplate $LabelKey
    $result = & $InspectByName $Name $format
    if ($result.ExitCode -eq 0) {
        $parts = $result.Text.Trim().Split('|')
        if ($parts.Length -eq 2 -and $parts[0] -match '^[0-9a-f]{64}$' -and $parts[1] -eq $LabelValue) {
            return [pscustomobject]@{ Status = 'FOUND'; ContainerId = $parts[0]; Error = $null; Format = $format }
        }
        return [pscustomobject]@{ Status = 'IDENTITY_MISMATCH'; ContainerId = $null; Error = 'Name lookup returned an ID or label that did not match this run.'; Format = $format }
    }

    if ($result.Text -match '(?i)No such (object|container)') {
        return [pscustomobject]@{ Status = 'NOT_FOUND'; ContainerId = $null; Error = $null; Format = $format }
    }
    return [pscustomobject]@{ Status = 'UNKNOWN'; ContainerId = $null; Error = "Name lookup failed with exit code $($result.ExitCode); cleanup status is unknown."; Format = $format }
}

function Remove-RunContainerByIdentity {
    param(
        [Parameter(Mandatory)][string]$ContainerId,
        [Parameter(Mandatory)][string]$LabelKey,
        [Parameter(Mandatory)][string]$LabelValue,
        [Parameter(Mandatory)][scriptblock]$InspectById,
        [Parameter(Mandatory)][scriptblock]$RemoveById
    )

    $format = Get-DockerInspectFormatTemplate $LabelKey
    $identity = & $InspectById $ContainerId $format
    if ($identity.ExitCode -ne 0) {
        return [pscustomobject]@{ Status = 'UNKNOWN'; Error = "Identity check failed with exit code $($identity.ExitCode); container was not removed." }
    }

    $parts = $identity.Text.Trim().Split('|')
    if ($parts.Length -ne 2 -or $parts[0] -cne $ContainerId -or $parts[1] -cne $LabelValue) {
        return [pscustomobject]@{ Status = 'IDENTITY_MISMATCH'; Error = 'Container ID/unique label did not match this run; container was left untouched.' }
    }

    $removed = & $RemoveById $ContainerId
    if ($removed.ExitCode -eq 0) {
        return [pscustomobject]@{ Status = 'REMOVED'; Error = $null }
    }
    return [pscustomobject]@{ Status = 'FAILED'; Error = "Own container removal failed with exit code $($removed.ExitCode)." }
}

function New-RunContainerState {
    return [pscustomobject]@{
        ContainerId = $null
        ContainerCreated = $false
        RunAttempted = $false
        DiscoveryStatus = 'NOT_ATTEMPTED'
        CleanupStatus = 'NOT_CREATED'
        ContainerCleaned = $false
        RunOutputStatus = 'NOT_ATTEMPTED'
    }
}

function Invoke-RunContainerAttempt {
    param(
        [Parameter(Mandatory)]$State,
        [Parameter(Mandatory)][scriptblock]$RunContainer
    )

    $State.RunAttempted = $true
    $State.CleanupStatus = 'DISCOVERY_PENDING'
    $State.RunOutputStatus = 'PENDING'
    try {
        $result = & $RunContainer
    }
    catch {
        $State.RunOutputStatus = 'COMMAND_FAILED'
        throw
    }

    $candidate = [string]$result.Text
    $candidate = $candidate.Trim()
    if ($result.ExitCode -ne 0 -or $candidate -notmatch '^[0-9a-f]{64}$') {
        $State.RunOutputStatus = 'INVALID'
        throw 'Docker did not return the expected new container ID.'
    }

    $State.ContainerId = $candidate
    $State.ContainerCreated = $true
    $State.DiscoveryStatus = 'RUN_SUCCEEDED'
    $State.CleanupStatus = 'PENDING'
    $State.RunOutputStatus = 'VALID'
    return $result
}

function Complete-RunContainerCleanup {
    param(
        [Parameter(Mandatory)]$State,
        [Parameter(Mandatory)][string]$Name,
        [Parameter(Mandatory)][string]$LabelKey,
        [Parameter(Mandatory)][string]$LabelValue,
        [Parameter(Mandatory)][AllowEmptyCollection()][System.Collections.Generic.List[string]]$CleanupErrors,
        [Parameter(Mandatory)][scriptblock]$InspectByName,
        [Parameter(Mandatory)][scriptblock]$InspectById,
        [Parameter(Mandatory)][scriptblock]$RemoveById,
        [scriptblock]$BeforeRemove
    )

    if (-not $State.ContainerId -and $State.RunAttempted) {
        try {
            $discovery = Find-RunContainerByName -Name $Name -LabelKey $LabelKey `
                -LabelValue $LabelValue -InspectByName $InspectByName
            $State.DiscoveryStatus = $discovery.Status
            if ($discovery.Status -eq 'FOUND') {
                $State.ContainerId = $discovery.ContainerId
                $State.ContainerCreated = $true
                $State.CleanupStatus = 'PENDING'
            }
            elseif ($discovery.Status -eq 'NOT_FOUND') {
                $State.CleanupStatus = 'NOT_FOUND'
            }
            else {
                $State.CleanupStatus = 'UNKNOWN'
                $CleanupErrors.Add($discovery.Error)
            }
        }
        catch {
            $State.DiscoveryStatus = 'DISCOVERY_FAILED'
            $State.CleanupStatus = 'UNKNOWN'
            $CleanupErrors.Add('Container discovery failed; cleanup status is unknown: ' + $_.Exception.Message)
        }
    }

    if ($State.ContainerCreated -and $State.ContainerId) {
        try {
            if ($BeforeRemove) { & $BeforeRemove $State.ContainerId | Out-Null }
            $removal = Remove-RunContainerByIdentity -ContainerId $State.ContainerId `
                -LabelKey $LabelKey -LabelValue $LabelValue `
                -InspectById $InspectById -RemoveById $RemoveById
            $State.CleanupStatus = $removal.Status
            if ($removal.Status -eq 'REMOVED') {
                $State.ContainerCleaned = $true
            }
            else {
                $CleanupErrors.Add($removal.Error)
            }
        }
        catch {
            $State.CleanupStatus = 'UNKNOWN'
            $CleanupErrors.Add('Container cleanup failed; cleanup status is unknown: ' + $_.Exception.Message)
        }
    }

    return $State
}
