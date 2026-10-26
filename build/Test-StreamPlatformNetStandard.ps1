param(
    [string]$ResultsDirectory = 'artifacts/stream-platform-netstandard',
    [switch]$VerifyIdentityOnly
)
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)
$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
Push-Location $repositoryRoot
try {
    $results = [IO.Path]::GetFullPath($ResultsDirectory)
    New-Item -ItemType Directory -Path $results -Force | Out-Null
    $checks = @(
        @{ Project='Bing.Utils.Tests'; Assembly='Bing.Utils.dll'; Filter='FullyQualifiedName~TemporaryLifecycleTest|FullyQualifiedName~JsonArrayReaderTest|FullyQualifiedName~AsyncBufferingTest|FullyQualifiedName~DbStreamingFieldsTest|FullyQualifiedName~FileWatcherLifecycleTest|FullyQualifiedName~CsvStreamTest|FullyQualifiedName~CommandStreamingTest' },
        @{ Project='Bing.Utils.Http.Tests'; Assembly='Bing.Utils.Http.dll'; Filter='FullyQualifiedName~DownloadManagerTest|FullyQualifiedName~ServerSentEventsTest' },
        @{ Project='Bing.Utils.Http.Tests.Integration'; Assembly='Bing.Utils.Http.dll'; Filter='FullyQualifiedName~DownloadQuotaIntegrationTest' },
        @{ Project='Bing.Utils.Net.Tests'; Assembly='Bing.Utils.Net.dll'; Filter='FullyQualifiedName~FtpAsyncDirectoryTest|FullyQualifiedName~FtpDirectoryLimitsCompatibilityTest' },
        @{ Project='Bing.Utils.IdUtils.Tests'; Assembly='Bing.Utils.IdUtils.dll'; Filter='FullyQualifiedName~Uuid7Test' }
    )
    $identities = @()
    foreach ($check in $checks) {
        $project = $check.Project
        # CI 已执行全目标框架测试时只核验产物，避免重复执行同一测试集合。
        if (-not $VerifyIdentityOnly) {
            & dotnet test "tests/$project/$project.csproj" -c Release -f netcoreapp3.1 --filter $check.Filter --logger "trx;LogFileName=$project.trx" --results-directory $results -v quiet /clp:ErrorsOnly -m:2 -nr:false
            if ($LASTEXITCODE -ne 0) { throw "$project 的实际 netstandard 回归失败。" }
        }
        $actual = Join-Path $repositoryRoot "tests/$project/bin/Release/netcoreapp3.1/$($check.Assembly)"
        $expected = Join-Path $repositoryRoot "output/release/netstandard2.0/$($check.Assembly)"
        $actualHash = (Get-FileHash -LiteralPath $actual -Algorithm SHA256).Hash
        $expectedHash = (Get-FileHash -LiteralPath $expected -Algorithm SHA256).Hash
        if ($actualHash -ne $expectedHash) { throw "$project 未加载预期的 netstandard2.0 产物。" }
        $identities += [pscustomobject]@{ Project=$project; Assembly=$check.Assembly; Actual=$actual; Expected=$expected; Sha256=$actualHash }
    }
    $identities | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $results 'identity.json') -Encoding utf8
}
finally { Pop-Location }
