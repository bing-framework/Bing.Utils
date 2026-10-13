param(
    [string]$OutputDirectory = (Join-Path $PSScriptRoot 'bin\net8.0')
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)

$dotnet = (Get-Command dotnet -ErrorAction Stop).Source
$dotnetRoot = Split-Path $dotnet -Parent
$sdkVersion = (& $dotnet --version).Trim()
$csc = Join-Path $dotnetRoot "sdk\$sdkVersion\Roslyn\bincore\csc.dll"
if (!(Test-Path -LiteralPath $csc -PathType Leaf)) {
    throw "Roslyn compiler was not found: $csc"
}

$refPack = Get-ChildItem -LiteralPath (Join-Path $dotnetRoot 'packs\Microsoft.NETCore.App.Ref') -Directory |
    Where-Object { $_.Name -like '8.*' } |
    Sort-Object { [Version]$_.Name } -Descending |
    Select-Object -First 1
if ($null -eq $refPack) {
    throw 'A net8.0 reference pack was not found.'
}

$refDirectory = Join-Path $refPack.FullName 'ref\net8.0'
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
$responseDirectory = Join-Path ([IO.Path]::GetTempPath()) ('bing-utils-runtime-cycle-rsp-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $responseDirectory -Force | Out-Null
$referenceArguments = Get-ChildItem -LiteralPath $refDirectory -Filter '*.dll' -File |
    ForEach-Object { "/reference:`"$($_.FullName)`"" }

function Invoke-Csc {
    param(
        [Parameter(Mandatory)]
        [string]$OutputPath,
        [Parameter(Mandatory)]
        [string[]]$SourcePaths,
        [string[]]$AdditionalReferences = @(),
        [ValidateSet('library', 'exe')]
        [string]$Target = 'library'
    )

    $responsePath = Join-Path $responseDirectory (([IO.Path]::GetFileNameWithoutExtension($OutputPath)) + '.rsp')
    $arguments = @(
        '/nostdlib+'
        '/nullable:enable'
        "/target:$Target"
        "/out:`"$OutputPath`""
        '/deterministic+'
    ) + $referenceArguments
    $arguments += @($AdditionalReferences | ForEach-Object { "/reference:`"$($_)`"" })
    $arguments += @($SourcePaths | ForEach-Object { "`"$($_)`"" })
    [IO.File]::WriteAllLines($responsePath, $arguments, $utf8)
    & $dotnet $csc "@$responsePath"
    if ($LASTEXITCODE -ne 0) {
        throw "csc failed for $OutputPath with exit code $LASTEXITCODE"
    }
}

try {
    $stubPath = Join-Path $OutputDirectory 'RuntimeCycleFixture.A.dll'
    $bPath = Join-Path $OutputDirectory 'RuntimeCycleFixture.B.dll'
    $cPath = Join-Path $OutputDirectory 'RuntimeCycleFixture.C.dll'
    $dPath = Join-Path $OutputDirectory 'RuntimeCycleFixture.D.dll'
    $ePath = Join-Path $OutputDirectory 'RuntimeCycleFixture.E.dll'
    $loaderPath = Join-Path $OutputDirectory 'CycleLoader.dll'
    $stubSource = Join-Path $PSScriptRoot 'CycleAStub.cs'
    $aSource = Join-Path $PSScriptRoot 'CycleA.cs'
    $bSource = Join-Path $PSScriptRoot 'CycleB.cs'
    $cSource = Join-Path $PSScriptRoot 'CycleC.cs'
    $dSource = Join-Path $PSScriptRoot 'CycleD.cs'
    $eSource = Join-Path $PSScriptRoot 'CycleE.cs'
    $loaderSource = Join-Path $PSScriptRoot 'CycleLoader.cs'

    # 先生成 A 占位程序集，使 B 可以获得对 A 的真实 AssemblyRef。
    Invoke-Csc -OutputPath $stubPath -SourcePaths $stubSource
    Invoke-Csc -OutputPath $bPath -SourcePaths $bSource -AdditionalReferences $stubPath
    # 用同名最终 A 覆盖占位程序集，使最终图形成 A -> B -> A。
    Invoke-Csc -OutputPath $stubPath -SourcePaths $aSource -AdditionalReferences $bPath
    # C 只引用 B，形成 A -> B -> A 与 C -> B 的分支图。
    Invoke-Csc -OutputPath $cPath -SourcePaths $cSource -AdditionalReferences $bPath
    # D 分别引用最终 A 和 C，扩展为两个独立的下游分支。
    Invoke-Csc -OutputPath $dPath -SourcePaths $dSource -AdditionalReferences @($stubPath, $cPath)
    # E 分别引用 D 和 B，形成跨层深分支。
    Invoke-Csc -OutputPath $ePath -SourcePaths $eSource -AdditionalReferences @($dPath, $bPath)
    Invoke-Csc -OutputPath $loaderPath -SourcePaths $loaderSource -Target exe

    $runtimeDirectory = Join-Path $dotnetRoot 'shared\Microsoft.NETCore.App'
    $runtimeVersion = (Get-ChildItem -LiteralPath $runtimeDirectory -Directory |
        Where-Object { $_.Name -like '8.*' } |
        Sort-Object { [Version]$_.Name } -Descending |
        Select-Object -First 1).Name
    $runtimeConfig = [ordered]@{
        runtimeOptions = [ordered]@{
            tfm = 'net8.0'
            framework = [ordered]@{
                name = 'Microsoft.NETCore.App'
                version = $runtimeVersion
            }
        }
    } | ConvertTo-Json -Depth 4
    [IO.File]::WriteAllText((Join-Path $OutputDirectory 'CycleLoader.runtimeconfig.json'), $runtimeConfig, $utf8)

    foreach ($mode in @('path', 'resolver', 'bytes')) {
        & $dotnet $loaderPath $OutputDirectory $mode
        if ($LASTEXITCODE -ne 0) {
            throw "CycleLoader failed for mode $mode with exit code $LASTEXITCODE"
        }
    }

    Write-Output "runtime-cycle-fixtures:verified:$OutputDirectory"
}
finally {
    if (Test-Path -LiteralPath $responseDirectory) {
        Remove-Item -LiteralPath $responseDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}
