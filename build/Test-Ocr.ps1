param(
    [ValidateSet('Unit', 'Integration', 'PackConsumer', 'Engine', 'Host', 'Resource', 'Final')]
    [string]$Stage = 'Unit',
    [switch]$RequireEngine,
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$EvidenceDirectory,
    [string]$PythonExecutable = 'python',
    [ValidateSet('maximum-image','queue')]
    [string]$ResourceScenario = 'queue'
)
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$ocrRepository = [IO.Path]::GetFullPath($RepositoryRoot)
if (!$EvidenceDirectory) {
    $EvidenceDirectory = Join-Path $ocrRepository ("output/ocr-validation/" + (Get-Date -Format 'yyyyMMdd-HHmmss') + "-" + $Stage)
}
$ocrEvidence = [IO.Path]::GetFullPath($EvidenceDirectory)
[IO.Directory]::CreateDirectory($ocrEvidence) | Out-Null
$ocrResults = [Collections.Generic.List[object]]::new()
$ocrLog = Join-Path $ocrEvidence 'commands.log'

function Invoke-OcrCommand {
    param([string]$Executable, [string[]]$Arguments)
    "$Executable $($Arguments -join ' ')" | Out-File -LiteralPath $ocrLog -Append -Encoding utf8
    & $Executable @Arguments 2>&1 | Tee-Object -Variable ocrCommandOutput | Write-Host
    $ocrExit = $LASTEXITCODE
    $ocrCommandOutput | Out-File -LiteralPath $ocrLog -Append -Encoding utf8
    if ($ocrExit -ne 0) { throw "$Executable 退出码 $ocrExit：$($Arguments -join ' ')" }
}
function Invoke-OcrCheck {
    param([string]$Name, [scriptblock]$Action)
    $ocrWatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action
        $ocrResults.Add([pscustomobject]@{ name=$Name; status='PASS'; seconds=$ocrWatch.Elapsed.TotalSeconds })
    } catch {
        $ocrResults.Add([pscustomobject]@{ name=$Name; status='FAIL'; seconds=$ocrWatch.Elapsed.TotalSeconds; reason=$_.Exception.Message })
        Write-Warning "$Name：$($_.Exception.Message)"
    }
}
function Test-OcrPackages {
    $ocrToolDir = Join-Path $ocrEvidence 'tools'
    Invoke-OcrCommand dotnet @('tool','install','FlubuCore.Tool','--tool-path',$ocrToolDir,'--version','8.0.0')
    $ocrFlubu = Join-Path $ocrToolDir $(if ($IsWindows) { 'flubu.exe' } else { 'flubu' })
    Invoke-OcrCommand $ocrFlubu @('ocr.pack.check','--noColor','--noint','-env=prod')

    $ocrPackRoot = Join-Path $ocrEvidence 'feed'
    [IO.Directory]::CreateDirectory($ocrPackRoot) | Out-Null
    # 图像适配器依赖的同仓库包也从当前源码打包，防止消费旧版绘图程序集。
    $ocrNames = @('Bing.Utils.Drawing.Shared','Bing.Utils.Drawing.SkiaSharp','Bing.Utils.Ocr',
        'Bing.Utils.Ocr.SkiaSharp','Bing.Utils.Ocr.PaddleServing','Bing.Utils.Ocr.DependencyInjection')
    $ocrVersion = (& dotnet msbuild (Join-Path $ocrRepository 'src/Bing.Utils.Ocr/Bing.Utils.Ocr.csproj') -getProperty:VersionPrefix | Select-Object -Last 1).Trim()
    if ($LASTEXITCODE -ne 0 -or !$ocrVersion) { throw '无法读取 OCR 包版本。' }
    foreach ($ocrName in $ocrNames) {
        Invoke-OcrCommand dotnet @('pack', (Join-Path $ocrRepository "src/$ocrName/$ocrName.csproj"), '-c', 'Release', '-o', $ocrPackRoot, '-v', 'minimal')
    }
    foreach ($ocrName in $ocrNames | Where-Object { $_ -like 'Bing.Utils.Ocr*' }) {
        $ocrArchive = [IO.Compression.ZipFile]::OpenRead((Join-Path $ocrPackRoot "$ocrName.$ocrVersion.nupkg"))
        try {
            foreach ($ocrFramework in @('net8.0','net7.0','net6.0','netstandard2.0')) {
                if (!$ocrArchive.GetEntry("lib/$ocrFramework/$ocrName.dll")) { throw "$ocrName 缺少 $ocrFramework 资产。" }
            }
        } finally { $ocrArchive.Dispose() }
    }
    $ocrConsumer = Join-Path $ocrEvidence 'consumer'
    [IO.Directory]::CreateDirectory($ocrConsumer) | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'OcrPackageConsumer/Program.cs') -Destination $ocrConsumer
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'OcrPackageConsumer/OcrPackageConsumer.csproj') -Destination $ocrConsumer
    $ocrProject = Join-Path $ocrConsumer 'OcrPackageConsumer.csproj'
    $ocrPriorPackages = $env:NUGET_PACKAGES
    $ocrCache = if ($ocrPriorPackages) { $ocrPriorPackages } else {
        Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages'
    }
    $ocrConfig = Join-Path $ocrConsumer 'NuGet.Config'
    $ocrFeedXml = [Security.SecurityElement]::Escape($ocrPackRoot)
    $ocrCacheXml = [Security.SecurityElement]::Escape($ocrCache)
    $ocrMappings = ($ocrNames | ForEach-Object { '<package pattern="' + $_ + '" />' }) -join ''
    $ocrConfigXml = @"
<configuration>
  <packageSources><clear />
    <add key="current" value="$ocrFeedXml" />
    <add key="cache" value="$ocrCacheXml" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping><clear />
    <packageSource key="current">$ocrMappings</packageSource>
    <packageSource key="cache"><package pattern="*" /></packageSource>
    <packageSource key="nuget"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
    [IO.File]::WriteAllText($ocrConfig, $ocrConfigXml, [Text.UTF8Encoding]::new($false))
    try {
        # 独立缓存确保四个 OCR 包取自本次 feed；缓存仅作为外部依赖的还原源。
        $env:NUGET_PACKAGES = Join-Path $ocrConsumer 'packages'
        Invoke-OcrCommand dotnet @('restore', $ocrProject, "-p:OcrPackageVersion=$ocrVersion",
            '--configfile', $ocrConfig, '-v', 'minimal')
        Invoke-OcrCommand dotnet @('build', $ocrProject, '-c', 'Release', '--no-restore', "-p:OcrPackageVersion=$ocrVersion", '-v', 'minimal')
        foreach ($ocrFramework in @('net8.0','net7.0','net6.0')) {
            Invoke-OcrCommand dotnet @('run', '--project', $ocrProject, '-c', 'Release', '-f', $ocrFramework,
                '--no-build', '--no-restore', "-p:OcrPackageVersion=$ocrVersion")
        }
    } finally { $env:NUGET_PACKAGES = $ocrPriorPackages }
}
Push-Location $ocrRepository
try {
    # Change Impact Analysis：每个阶段仅覆盖其实现边界；Final 才执行一次解决方案检查。
    switch ($Stage) {
        'Unit' {
            foreach ($ocrName in @('Bing.Utils.Ocr.Tests','Bing.Utils.Ocr.DependencyInjection.Tests','Bing.Utils.Ocr.PaddleServing.Tests')) {
                Invoke-OcrCheck $ocrName { Invoke-OcrCommand dotnet @('test',"tests/$ocrName/$ocrName.csproj",'-c','Release','-v','minimal','--logger','trx',"--results-directory=$ocrEvidence") }
            }
        }
        'Integration' {
            foreach ($ocrName in @('Bing.Utils.Ocr.SkiaSharp.Tests.Integration','Bing.Utils.Ocr.PaddleServing.Tests.Integration','Bing.Utils.Ocr.Tests.Integration')) {
                Invoke-OcrCheck $ocrName { Invoke-OcrCommand dotnet @('test',"tests/$ocrName/$ocrName.csproj",'-c','Release','-f','net8.0','--filter','Category!=OcrEngine','-v','minimal','--logger','trx',"--results-directory=$ocrEvidence") }
            }
            Invoke-OcrCheck 'WebGeometry' { Invoke-OcrCommand node @('--test','samples/OcrWeb/tests/geometry.test.mjs') }
        }
        'Host' {
            Invoke-OcrCheck 'Host' {
                Push-Location (Join-Path $ocrRepository 'deploy/ocr')
                try { Invoke-OcrCommand $PythonExecutable @('-B','-m','unittest','discover','-s','tests','-v') }
                finally { Pop-Location }
            }
        }
        'Resource' {
            Invoke-OcrCheck 'Resource' {
                Push-Location (Join-Path $ocrRepository 'deploy/ocr')
                try { Invoke-OcrCommand $PythonExecutable @('-B','resource_probe.py','--scenario',$ResourceScenario,'--output',(Join-Path $ocrEvidence 'resource.json')) }
                finally { Pop-Location }
            }
        }
        'PackConsumer' { Invoke-OcrCheck 'PackConsumer' { Test-OcrPackages } }
        'Engine' {
            if (!$env:OCR_ENDPOINT -or !$env:OCR_MODEL_PROFILE -or !$env:OCR_FONT_PATH) {
                $ocrResults.Add([pscustomobject]@{ name='Engine'; status='BLOCKED_EXTERNAL'; reason='需要 OCR_ENDPOINT、OCR_MODEL_PROFILE、OCR_FONT_PATH；请先按 deploy/ocr/README.md 准备固定模型并启动宿主。' })
            } else {
                Invoke-OcrCheck 'Engine' {
                    Invoke-OcrCommand dotnet @('test','tests/Bing.Utils.Ocr.PaddleServing.Tests.Integration/Bing.Utils.Ocr.PaddleServing.Tests.Integration.csproj',
                        '-c','Release','-f','net8.0','--filter','Category=OcrEngine','-v','minimal','--logger','trx',"--results-directory=$ocrEvidence")
                }
            }
        }
        'Final' {
            # 解决方案含 netstandard-only 工程；构建保留项目 TFM，测试统一 net8.0。
            Invoke-OcrCheck 'SolutionBuild' { Invoke-OcrCommand dotnet @('build','Bing.Utils.sln','-c','Release','-v','minimal') }
            Invoke-OcrCheck 'SolutionTest' { Invoke-OcrCommand dotnet @('test','Bing.Utils.sln','-c','Release','-f','net8.0','--no-build','--filter','Category!=OcrEngine','-v','minimal','--logger','trx',"--results-directory=$ocrEvidence") }
            Invoke-OcrCheck 'NuGetAudit' { Invoke-OcrCommand pwsh @('-NoProfile','-File',(Join-Path $PSScriptRoot 'Test-NuGetAudit.ps1')) }
            Invoke-OcrCheck 'PackConsumer' { Test-OcrPackages }
        }
    }
} finally {
    Pop-Location
    $ocrResults | ConvertTo-Json -Depth 6 | Out-File -LiteralPath (Join-Path $ocrEvidence 'results.json') -Encoding utf8
    $ocrResults | Format-Table name,status,seconds,reason -AutoSize
    Write-Host "证据：$ocrEvidence"
}
if (@($ocrResults | Where-Object status -eq 'FAIL').Count -gt 0 -or
    ($RequireEngine -and @($ocrResults | Where-Object status -eq 'BLOCKED_EXTERNAL').Count -gt 0)) { exit 1 }
