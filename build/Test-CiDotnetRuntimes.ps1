param(
    [Parameter(Mandatory = $true)]
    [string[]] $TargetFrameworks
)

$utf8 = [System.Text.UTF8Encoding]::new($false)
[Console]::InputEncoding = $utf8
[Console]::OutputEncoding = $utf8
$OutputEncoding = $utf8

$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
Push-Location $repoRoot

try {
    $sdkVersion = (& dotnet --version).Trim()
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to read the selected .NET SDK version.'
    }

    if ($sdkVersion -ne '8.0.425') {
        throw "Expected .NET SDK 8.0.425 from global.json, found '$sdkVersion'."
    }

    $runtimeLines = @(& dotnet --list-runtimes)
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to list installed .NET runtimes.'
    }

    $installed = @{}
    foreach ($line in $runtimeLines) {
        if ($line -match '^Microsoft\.(NETCore\.App|AspNetCore\.App)\s+(\d+\.\d+)\.\d+') {
            $runtimeName = $Matches[1]
            $runtimeVersion = $Matches[2]
            $installed["$runtimeName/$runtimeVersion"] = $true
        }
    }

    $required = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
    foreach ($framework in $TargetFrameworks) {
        if ($framework -notmatch '^net(?:coreapp)?(?<major>\d+)\.(?<minor>\d+)$') {
            throw "Unsupported test target framework '$framework'."
        }

        $runtimeVersion = "$($Matches.major).$($Matches.minor)"
        [void] $required.Add("NETCore.App/$runtimeVersion")
        [void] $required.Add("AspNetCore.App/$runtimeVersion")
    }

    $missing = @($required | Where-Object { -not $installed.ContainsKey($_) })
    if ($missing.Count -gt 0) {
        $runtimeList = $runtimeLines -join [Environment]::NewLine
        throw "Missing required shared runtimes: $($missing -join ', '). Installed runtimes:`n$runtimeList"
    }

    Write-Host "SDK: $sdkVersion; verified runtimes: $($required -join ', ')"
}
finally {
    Pop-Location
}
