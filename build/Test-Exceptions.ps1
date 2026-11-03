param(
    [ValidateSet('Consumer', 'Context', 'Probe')]
    [string] $Mode = 'Consumer',
    [string] $Configuration = 'Release',
    [string] $BingUtilsRoot = (Join-Path $PSScriptRoot '..\output\release'),
    [string] $BingUtilsAssemblyPath,
    [string] $Label = 'candidate',
    [string] $ResultsPath,
    [string] $ContextLogRoot,
    [int] $TimeoutSeconds = 20
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

$repoRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$consumerProject = Join-Path $PSScriptRoot 'ExceptionsConsumer\ExceptionsConsumer.csproj'
$probeProject = Join-Path $repoRoot 'benchmarks\ExceptionsProbe\ExceptionsProbe.csproj'
$tfms = @('net8.0', 'net7.0', 'net6.0', 'netstandard2.0')

function Resolve-AssemblyPath {
    param([string] $Root, [string] $Tfm)
    $path = [System.IO.Path]::GetFullPath((Join-Path $Root "$Tfm\Bing.Utils.dll"))
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Bing.Utils.dll not found for ${Tfm}: $path"
    }
    return $path
}

function Get-ConsumerOutputPath {
    param([string] $Root)
    return Join-Path $Root "$Configuration\net8.0\ExceptionsConsumer.dll"
}

function Build-Consumer {
    param([string] $AssemblyPath, [string] $OutputKey)
    $consumerRoot = Join-Path $PSScriptRoot 'ExceptionsConsumer'
    $binRoot = Join-Path $consumerRoot "bin\$OutputKey"
    $objRoot = Join-Path $consumerRoot "obj\$OutputKey"
    New-Item -ItemType Directory -Force -Path $binRoot, $objRoot | Out-Null

    & dotnet build $consumerProject --configuration $Configuration --nologo -t:Rebuild `
        "-p:BingUtilsAssemblyPath=$AssemblyPath" `
        "-p:BaseOutputPath=$binRoot\" `
        "-p:BaseIntermediateOutputPath=$objRoot\" `
        "-p:MSBuildProjectExtensionsPath=$objRoot\" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Consumer build failed for $AssemblyPath with exit code $LASTEXITCODE."
    }

    $consumerDll = Get-ConsumerOutputPath $binRoot
    $copiedAssembly = Join-Path (Split-Path -Parent $consumerDll) 'Bing.Utils.dll'
    # 曾发现同名旧产物抢先解析；每次消费者构建后验证显式输入与实际复制的 DLL 一致。
    if ((Get-FileHash -LiteralPath $AssemblyPath -Algorithm SHA256).Hash -ne (Get-FileHash -LiteralPath $copiedAssembly -Algorithm SHA256).Hash) {
        throw "Consumer copied a different Bing.Utils asset: $copiedAssembly"
    }
    Write-Host "Verified explicit asset: $AssemblyPath"
    return $consumerDll
}

function Run-SupervisedContext {
    param([string] $ConsumerDll, [string] $OutputKey)
    if ($ContextLogRoot) {
        $logRoot = [System.IO.Path]::GetFullPath($ContextLogRoot)
    }
    else {
        $logRoot = Join-Path $PSScriptRoot ".artifacts\context\$OutputKey"
    }
    New-Item -ItemType Directory -Force -Path $logRoot | Out-Null
    $stdoutPath = Join-Path $logRoot 'stdout.log'
    $stderrPath = Join-Path $logRoot 'stderr.log'

    $startInfo = [System.Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = 'dotnet'
    $startInfo.WorkingDirectory = $repoRoot
    $startInfo.UseShellExecute = $false
    $startInfo.CreateNoWindow = $true
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.StandardOutputEncoding = [System.Text.UTF8Encoding]::new($false)
    $startInfo.StandardErrorEncoding = [System.Text.UTF8Encoding]::new($false)
    $startInfo.ArgumentList.Add($ConsumerDll)
    $startInfo.ArgumentList.Add('context')
    $startInfo.Environment['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    $startInfo.Environment['DOTNET_NOLOGO'] = '1'

    $process = [System.Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    if (-not $process.Start()) {
        throw 'Could not start the controlled context consumer.'
    }

    $stdoutTask = $process.StandardOutput.ReadToEndAsync()
    $stderrTask = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit($TimeoutSeconds * 1000)) {
        try {
            $process.Kill($true)
        }
        catch {
            & taskkill.exe /PID $process.Id /T /F | Out-Null
        }
        $process.WaitForExit()
        $stdout = $stdoutTask.GetAwaiter().GetResult()
        $stderr = $stderrTask.GetAwaiter().GetResult()
        [System.IO.File]::WriteAllText($stdoutPath, $stdout, [System.Text.UTF8Encoding]::new($false))
        [System.IO.File]::WriteAllText($stderrPath, $stderr, [System.Text.UTF8Encoding]::new($false))
        throw "Context consumer timed out after $TimeoutSeconds seconds; process tree was terminated. Logs: $logRoot"
    }

    $stdout = $stdoutTask.GetAwaiter().GetResult()
    $stderr = $stderrTask.GetAwaiter().GetResult()
    [System.IO.File]::WriteAllText($stdoutPath, $stdout, [System.Text.UTF8Encoding]::new($false))
    [System.IO.File]::WriteAllText($stderrPath, $stderr, [System.Text.UTF8Encoding]::new($false))
    if ($process.ExitCode -ne 0) {
        throw "Context consumer failed with exit code $($process.ExitCode). Logs: $logRoot"
    }
    Write-Output $stdout.TrimEnd()
}

function Run-Probe {
    param([string] $AssemblyPath)
    $probeLabel = "$Label-net8.0"
    $probeRoot = Join-Path $repoRoot 'benchmarks\ExceptionsProbe'
    $artifactRoot = Join-Path $probeRoot ".artifacts\$probeLabel"
    $binRoot = Join-Path $probeRoot "bin\$probeLabel"
    $objRoot = Join-Path $probeRoot "obj\$probeLabel"
    if (-not $ResultsPath) {
        $script:ResultsPath = Join-Path $artifactRoot 'results.json'
    }
    New-Item -ItemType Directory -Force -Path $binRoot, $objRoot, (Split-Path -Parent $ResultsPath) | Out-Null

    & dotnet build $probeProject --configuration $Configuration --nologo -t:Rebuild `
        "-p:BingUtilsAssemblyPath=$AssemblyPath" `
        "-p:BaseOutputPath=$binRoot\" `
        "-p:BaseIntermediateOutputPath=$objRoot\" `
        "-p:MSBuildProjectExtensionsPath=$objRoot\" | Out-Host
    if ($LASTEXITCODE -ne 0) {
        throw "Probe build failed for $AssemblyPath with exit code $LASTEXITCODE."
    }

    $probeDll = Join-Path $binRoot "$Configuration\net8.0\ExceptionsProbe.dll"
    & dotnet $probeDll --label $Label --results $ResultsPath
    if ($LASTEXITCODE -ne 0) {
        throw "Probe failed for $AssemblyPath with exit code $LASTEXITCODE."
    }
}

Push-Location $repoRoot
try {
    switch ($Mode) {
        'Consumer' {
            if ($BingUtilsAssemblyPath) {
                $assemblyPath = [System.IO.Path]::GetFullPath($BingUtilsAssemblyPath)
                if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
                    throw "Bing.Utils assembly not found: $assemblyPath"
                }
                $consumerDll = Build-Consumer $assemblyPath "$Label-explicit"
                & dotnet $consumerDll
                if ($LASTEXITCODE -ne 0) {
                    throw "Consumer smoke failed for $assemblyPath with exit code $LASTEXITCODE."
                }
            }
            else {
                foreach ($tfm in $tfms) {
                    $assemblyPath = Resolve-AssemblyPath $BingUtilsRoot $tfm
                    $consumerDll = Build-Consumer $assemblyPath $tfm
                    & dotnet $consumerDll
                    if ($LASTEXITCODE -ne 0) {
                        throw "Consumer smoke failed for $tfm with exit code $LASTEXITCODE."
                    }
                }
            }
        }
        'Context' {
            if ($BingUtilsAssemblyPath) {
                $assemblyPath = [System.IO.Path]::GetFullPath($BingUtilsAssemblyPath)
            }
            else {
                $assemblyPath = Resolve-AssemblyPath $BingUtilsRoot 'net8.0'
            }
            if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
                throw "Bing.Utils assembly not found: $assemblyPath"
            }
            $consumerDll = Build-Consumer $assemblyPath 'context-net8.0'
            Run-SupervisedContext $consumerDll 'net8.0'
        }
        'Probe' {
            if ($BingUtilsAssemblyPath) {
                $assemblyPath = [System.IO.Path]::GetFullPath($BingUtilsAssemblyPath)
            }
            else {
                $assemblyPath = Resolve-AssemblyPath $BingUtilsRoot 'net8.0'
            }
            if (-not (Test-Path -LiteralPath $assemblyPath -PathType Leaf)) {
                throw "Bing.Utils assembly not found: $assemblyPath"
            }
            Run-Probe $assemblyPath
        }
    }
}
finally {
    Pop-Location
}
