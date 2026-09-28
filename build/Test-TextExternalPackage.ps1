param(
    [string]$RepositoryRoot = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$repository = [System.IO.Path]::GetFullPath($RepositoryRoot)
$project = Join-Path $repository 'src/Bing.Utils.Text/Bing.Utils.Text.csproj'
$consumer = Join-Path $repository ("output/text-external-consumer/" + [Guid]::NewGuid().ToString('N'))
$consumerProject = Join-Path $consumer 'Consumer.csproj'
$utf8 = [System.Text.UTF8Encoding]::new($false)
$sourceCache = if ($env:NUGET_PACKAGES) { $env:NUGET_PACKAGES } else {
    Join-Path $env:USERPROFILE '.nuget/packages'
}

function Invoke-Dotnet {
    param([string[]]$Arguments)

    & dotnet @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet 命令失败：$($Arguments -join ' ')"
    }
}

$versionOutput = & dotnet msbuild $project -getProperty:VersionPrefix
if ($LASTEXITCODE -ne 0) {
    throw '无法读取 Text 包版本。'
}
$version = ($versionOutput | Select-Object -Last 1).Trim()
if ([string]::IsNullOrWhiteSpace($version)) {
    throw 'Text 包版本为空。'
}

Invoke-Dotnet -Arguments @('restore', $project, '-p:NuGetAudit=false',
    '--source', $sourceCache, '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('build', $project, '-c', 'Release', '--no-restore', '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('pack', $project, '-c', 'Release', '--no-build', '--no-restore', '--verbosity', 'quiet')

Invoke-Dotnet -Arguments @('restore', $project, '-p:BingTextExternalOnly=true', '-p:NuGetAudit=false',
    '--source', $sourceCache, '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('build', $project, '-c', 'Release', '-p:BingTextExternalOnly=true',
    '--no-restore', '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('pack', $project, '-c', 'Release', '-p:BingTextExternalOnly=true',
    '--no-build', '--no-restore', '--verbosity', 'quiet')

Invoke-Dotnet -Arguments @('restore', $project, '-p:NuGetAudit=false',
    '--source', $sourceCache, '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('build', $project, '-c', 'Release', '--no-restore', '--verbosity', 'quiet')
Invoke-Dotnet -Arguments @('pack', $project, '-c', 'Release', '--no-build', '--no-restore', '--verbosity', 'quiet')

$defaultPackage = Join-Path $repository "output/release/Bing.Utils.Text.$version.nupkg"
$externalPackage = Join-Path $repository "output/external/Release/Bing.Utils.Text.External.$version.nupkg"
$defaultAssembly = Join-Path $repository 'output/release/netstandard2.0/Bing.Utils.Text.dll'
if (!(Test-Path -LiteralPath $defaultPackage) -or !(Test-Path -LiteralPath $externalPackage)) {
    throw '交替构建后缺少 Text 包。'
}

$assembly = [System.Reflection.Assembly]::LoadFrom($defaultAssembly)
$resourceNames = $assembly.GetManifestResourceNames()
foreach ($name in @('Bing.Text.Pinyin.Data.characters.gz', 'Bing.Text.Pinyin.Data.phrases.gz',
        'Bing.Text.Chinese.Data.s2t.gz', 'Bing.Text.Chinese.Data.t2s.gz')) {
    if ($resourceNames -notcontains $name) {
        throw "默认程序集缺少资源：$name"
    }
}
if (!$assembly.GetType('Bing.Text.Chinese.ChineseConverter', $false)) {
    throw '默认程序集缺少静态简繁入口。'
}

Add-Type -AssemblyName System.IO.Compression
$archive = [System.IO.Compression.ZipFile]::OpenRead($externalPackage)
try {
    $entryNames = @($archive.Entries | ForEach-Object FullName)
    $assemblyEntry = $archive.GetEntry('lib/netstandard2.0/Bing.Utils.Text.dll')
    if ($null -eq $assemblyEntry) {
        throw '轻量包缺少 Text 程序集。'
    }
    if ($entryNames | Where-Object { $_ -like 'licenses/*' -or $_ -like '*.gz' }) {
        throw '轻量包中仍包含外置数据资源。'
    }
    $entryStream = $assemblyEntry.Open()
    $sha256 = [System.Security.Cryptography.SHA256]::Create()
    try {
        $packageAssemblyHash = [BitConverter]::ToString($sha256.ComputeHash($entryStream)).Replace('-', '')
    }
    finally {
        $sha256.Dispose()
        $entryStream.Dispose()
    }
}
finally {
    $archive.Dispose()
}

[System.IO.Directory]::CreateDirectory($consumer) | Out-Null
$projectXml = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Bing.Utils.Text.External" Version="$version" />
  </ItemGroup>
</Project>
"@
[System.IO.File]::WriteAllText($consumerProject, $projectXml, $utf8)

$program = @'
using System;
using System.IO;
using System.Linq;
using Bing.Text.Chinese;
using Bing.Text.Pinyin;
using Bing.Text.Segmentation;

var root = args[0];
var assembly = typeof(PinyinCatalog).Assembly;
if (assembly.GetType("Bing.Text.Chinese.ChineseConverter", false) != null ||
    typeof(PinyinUtil).GetMethod("GetPinyinWithTone", new[] { typeof(string), typeof(string) }) != null ||
    assembly.GetManifestResourceNames().Any(name =>
        name.StartsWith("Bing.Text.Pinyin.Data.", StringComparison.Ordinal) ||
        name.StartsWith("Bing.Text.Chinese.Data.", StringComparison.Ordinal)))
    throw new Exception("轻量程序集仍包含内嵌数据入口。");

string Resource(string path) => Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar));

using var characters = File.OpenRead(Resource("asset/pinyin/data/characters.gz"));
using var phrases = File.OpenRead(Resource("asset/pinyin/data/phrases.gz"));
var pinyin = new PinyinCatalog(characters, phrases);
if (pinyin.GetPinyinWithTone("重庆银行", " ") != "Chóng Qìng Yín Háng")
    throw new Exception("外置拼音结果错误。");

using var s2t = File.OpenRead(Resource("asset/opencc/data/s2t.gz"));
using var t2s = File.OpenRead(Resource("asset/opencc/data/t2s.gz"));
var standard = new ChineseConversionCatalog(s2t, t2s);
if (standard.ToTraditional("发展和头发") != "發展和頭髮")
    throw new Exception("外置基础简繁结果错误。");

using var twForward = File.OpenRead(Resource("asset/opencc/data/tw-forward.gz"));
using var twReverse = File.OpenRead(Resource("asset/opencc/data/tw-reverse.gz"));
var taiwan = standard.WithRegionalRules(twForward, twReverse);
if (taiwan.ToTraditional("鼠标和软件") != "滑鼠和軟體" ||
    taiwan.ToSimplified("滑鼠和軟體") != "鼠标和软件")
    throw new Exception("外置台湾地区转换结果错误。");

using var hkForward = File.OpenRead(Resource("asset/opencc/data/hk-forward.gz"));
using var hkReverse = File.OpenRead(Resource("asset/opencc/data/hk-reverse.gz"));
var hongKong = standard.WithRegionalRules(hkForward, hkReverse);
if (hongKong.ToTraditional("户") != "户")
    throw new Exception("外置香港地区转换结果错误。");

using var dictionary = File.OpenRead(Resource("asset/segmentation/dict.txt"));
var segmenter = new ChineseSegmenter(dictionary);
if (string.Concat(segmenter.Cut("我爱中国")) != "我爱中国")
    throw new Exception("外置分词词典结果错误。");

using var emptyDictionary = new MemoryStream(Array.Empty<byte>());
using var model = File.OpenRead(Resource("asset/segmentation/hmm-model.gz"));
var inferred = new ChineseSegmenter(emptyDictionary).WithHmmModel(model);
if (!inferred.Cut("南京市长江大桥").SequenceEqual(new[] { "南京市", "长江大桥" }))
    throw new Exception("外置 HMM 模型结果错误。");

Console.WriteLine("轻量包消费者验证通过。");
'@
[System.IO.File]::WriteAllText((Join-Path $consumer 'Program.cs'), $program, $utf8)

$previousPackages = $env:NUGET_PACKAGES
try {
    $env:NUGET_PACKAGES = Join-Path $consumer 'packages'
    Invoke-Dotnet -Arguments @('restore', $consumerProject, '-p:NuGetAudit=false',
        '--source', (Split-Path -Parent $externalPackage),
        '--source', (Split-Path -Parent $defaultPackage),
        '--source', $sourceCache, '--verbosity', 'quiet')
    $restoredAssembly = Join-Path $env:NUGET_PACKAGES "bing.utils.text.external/$version/lib/netstandard2.0/Bing.Utils.Text.dll"
    if (!(Test-Path -LiteralPath $restoredAssembly) -or
        (Get-FileHash -LiteralPath $restoredAssembly -Algorithm SHA256).Hash -ne $packageAssemblyHash) {
        throw '消费者还原的程序集与当前轻量包不一致。'
    }
    Invoke-Dotnet -Arguments @('run', '--project', $consumerProject, '-c', 'Release',
        '--no-restore', '--', $repository)
}
finally {
    $env:NUGET_PACKAGES = $previousPackages
}
