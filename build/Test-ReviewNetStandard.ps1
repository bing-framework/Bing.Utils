param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [Text.UTF8Encoding]::new($false)
$OutputEncoding = [Text.UTF8Encoding]::new($false)

$repositoryRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$previousAssembly = $env:BING_REVIEW_NETSTANDARD_DLL
Push-Location $repositoryRoot
try {
    if (-not $NoBuild) {
        & dotnet build src/Bing.Utils/Bing.Utils.csproj -c Release -f netstandard2.0
        if ($LASTEXITCODE -ne 0) { throw 'netstandard2.0 构建失败。' }
        & dotnet build tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0
        if ($LASTEXITCODE -ne 0) { throw '回归测试构建失败。' }
    }
    # 测试通过独立 AssemblyLoadContext 加载此产物，并断言真实目标框架。
    $env:BING_REVIEW_NETSTANDARD_DLL = Join-Path $repositoryRoot 'output/release/netstandard2.0/Bing.Utils.dll'
    & dotnet test tests/Bing.Utils.Tests/Bing.Utils.Tests.csproj -c Release -f net8.0 --no-build --filter 'FullyQualifiedName~ZipReviewRegressionTest|FullyQualifiedName~NetStandardPerformanceApiTest'
    if ($LASTEXITCODE -ne 0) { throw 'netstandard2.0 ZIP/性能与扩展能力 API 回归测试失败。' }
}
finally {
    $env:BING_REVIEW_NETSTANDARD_DLL = $previousAssembly
    Pop-Location
}
