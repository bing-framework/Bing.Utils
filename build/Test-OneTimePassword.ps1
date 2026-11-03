param(
    [ValidateSet('Unit', 'Integration', 'PackConsumer', 'Final')]
    [string]$Stage = 'Unit',
    [switch]$RequireRedis,
    [string]$EvidenceDirectory
)
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$otpRoot = [IO.Path]::GetFullPath((Split-Path -Parent $PSScriptRoot))
if (!$EvidenceDirectory) { $EvidenceDirectory = Join-Path $otpRoot ("output/otp-validation/" + (Get-Date -Format 'yyyyMMdd-HHmmss') + "-" + $Stage) }
$otpEvidence = [IO.Path]::GetFullPath($EvidenceDirectory)
[IO.Directory]::CreateDirectory($otpEvidence) | Out-Null
$otpLog = Join-Path $otpEvidence 'commands.log'
$otpResults = [Collections.Generic.List[object]]::new()
function Invoke-OtpCommand {
    param([string]$Executable, [string[]]$Arguments)
    "$Executable $($Arguments -join ' ')" | Out-File -LiteralPath $otpLog -Append -Encoding utf8
    & $Executable @Arguments 2>&1 | Tee-Object -Variable otpCommandOutput | Write-Host
    $otpExit = $LASTEXITCODE
    $otpCommandOutput | Out-File -LiteralPath $otpLog -Append -Encoding utf8
    if ($otpExit -ne 0) { throw "$Executable failed with exit code $otpExit" }
}
function Invoke-OtpCheck {
    param([string]$Name, [scriptblock]$Action)
    $otpWatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        & $Action
        $otpResults.Add([pscustomobject]@{ name=$Name; status='PASS'; seconds=$otpWatch.Elapsed.TotalSeconds })
    } catch {
        $otpResults.Add([pscustomobject]@{ name=$Name; status='FAIL'; seconds=$otpWatch.Elapsed.TotalSeconds; reason=$_.Exception.Message })
        Write-Warning "$Name : $($_.Exception.Message)"
    }
}
function Test-OtpPackages {
    $otpFeed = Join-Path $otpEvidence 'feed'
    [IO.Directory]::CreateDirectory($otpFeed) | Out-Null
    $otpNames = @('Bing.Utils.Security','Bing.Utils.OneTimePassword','Bing.Utils.OneTimePassword.Sqlite',
        'Bing.Utils.OneTimePassword.DataProtection','Bing.Utils.OneTimePassword.Redis','Bing.Utils.OneTimePassword.DependencyInjection')
    $otpVersion = (& dotnet msbuild src/Bing.Utils.OneTimePassword/Bing.Utils.OneTimePassword.csproj -getProperty:VersionPrefix | Select-Object -Last 1).Trim()
    if ($LASTEXITCODE -ne 0 -or !$otpVersion) { throw 'Cannot resolve package version.' }
    foreach ($otpName in $otpNames) {
        Invoke-OtpCommand dotnet @('pack',"src/$otpName/$otpName.csproj",'-c','Release','-m:1','-o',$otpFeed,'-v','minimal')
        $otpZip = [IO.Compression.ZipFile]::OpenRead((Join-Path $otpFeed "$otpName.$otpVersion.nupkg"))
        try {
            foreach ($otpFramework in @('net8.0','net7.0','net6.0','netstandard2.0')) {
                if (!$otpZip.GetEntry("lib/$otpFramework/$otpName.dll")) { throw "$otpName missing $otpFramework" }
            }
        } finally { $otpZip.Dispose() }
    }
    $otpConsumer = Join-Path $otpEvidence 'consumer'
    [IO.Directory]::CreateDirectory($otpConsumer) | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'OtpPackageConsumer/Program.cs') -Destination $otpConsumer
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'OtpPackageConsumer/OtpPackageConsumer.csproj') -Destination $otpConsumer
    $otpProject = Join-Path $otpConsumer 'OtpPackageConsumer.csproj'
    $otpPriorPackages = $env:NUGET_PACKAGES
    $otpCache = if ($otpPriorPackages) { $otpPriorPackages } else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.nuget/packages' }
    $otpFeedXml = [Security.SecurityElement]::Escape($otpFeed)
    $otpCacheXml = [Security.SecurityElement]::Escape($otpCache)
    $otpMappings = ($otpNames | ForEach-Object { '<package pattern="' + $_ + '" />' }) -join ''
    $otpConfig = Join-Path $otpConsumer 'NuGet.Config'
    $otpConfigXml = @"
<configuration>
 <packageSources><clear />
  <add key="current" value="$otpFeedXml" /><add key="cache" value="$otpCacheXml" />
  <add key="nuget" value="https://api.nuget.org/v3/index.json" />
 </packageSources>
 <packageSourceMapping><clear />
  <packageSource key="current">$otpMappings</packageSource>
  <packageSource key="cache"><package pattern="*" /></packageSource>
  <packageSource key="nuget"><package pattern="*" /></packageSource>
 </packageSourceMapping>
</configuration>
"@
    [IO.File]::WriteAllText($otpConfig, $otpConfigXml, [Text.UTF8Encoding]::new($false))
    try {
        # 独立缓存与精确源映射保证消费本轮包，不读取已安装的旧 Bing 包。
        $env:NUGET_PACKAGES = Join-Path $otpConsumer 'packages'
        Invoke-OtpCommand dotnet @('restore',$otpProject,"-p:OtpPackageVersion=$otpVersion",'--configfile',$otpConfig,'-v','minimal')
        Invoke-OtpCommand dotnet @('build',$otpProject,'-c','Release','-m:1','--no-restore',"-p:OtpPackageVersion=$otpVersion",'-v','minimal')
        foreach ($otpFramework in @('net8.0','net7.0','net6.0')) {
            Invoke-OtpCommand dotnet @('run','--project',$otpProject,'-c','Release','-f',$otpFramework,'--no-build','--no-restore',"-p:OtpPackageVersion=$otpVersion")
        }
    } finally { $env:NUGET_PACKAGES = $otpPriorPackages }
}
Push-Location $otpRoot
try {
    # 变更影响分析：Unit 验证纯契约；Integration 验证真实组件；Final 只执行一次整体构建。
    switch ($Stage) {
        'Unit' {
            foreach ($otpName in @('Bing.Utils.OneTimePassword.Tests','Bing.Utils.OneTimePassword.DependencyInjection.Tests','Bing.Utils.OneTimePassword.Redis.Tests')) {
                Invoke-OtpCheck $otpName { Invoke-OtpCommand dotnet @('test',"tests/$otpName/$otpName.csproj",'-c','Release','-m:1','-v','minimal','--logger','trx','--results-directory',$otpEvidence) }
            }
        }
        'Integration' {
            foreach ($otpName in @('Bing.Utils.OneTimePassword.Sqlite.Tests.Integration','Bing.Utils.OneTimePassword.DataProtection.Tests.Integration','Bing.Utils.OneTimePassword.Tests.Integration','Bing.Utils.OneTimePassword.Redis.Tests.Integration')) {
                Invoke-OtpCheck $otpName { Invoke-OtpCommand dotnet @('test',"tests/$otpName/$otpName.csproj",'-c','Release','-m:1','-v','minimal','--logger','trx','--results-directory',$otpEvidence) }
            }
            if ([string]::IsNullOrWhiteSpace($env:BING_OTP_REDIS)) {
                $otpResults.Add([pscustomobject]@{name='RedisExternal';status='BLOCKED_EXTERNAL';reason='BING_OTP_REDIS not configured; real Redis tests skipped.'})
            }
        }
        'PackConsumer' { Invoke-OtpCheck 'PackConsumer' { Test-OtpPackages } }
        'Final' {
            Invoke-OtpCheck 'SolutionBuild' { Invoke-OtpCommand dotnet @('build','Bing.Utils.sln','-c','Release','-m:1','-v','minimal') }
            Invoke-OtpCheck 'DiffWhitespace' { Invoke-OtpCommand git @('diff','--check') }
        }
    }
} finally {
    Pop-Location
    $otpResults | ConvertTo-Json -Depth 5 | Out-File -LiteralPath (Join-Path $otpEvidence 'results.json') -Encoding utf8
    $otpResults | Format-Table name,status,seconds,reason -AutoSize
    Write-Host "Evidence: $otpEvidence"
}
if (@($otpResults | Where-Object status -eq 'FAIL').Count -gt 0 -or
    ($RequireRedis -and @($otpResults | Where-Object status -eq 'BLOCKED_EXTERNAL').Count -gt 0)) { exit 1 }
