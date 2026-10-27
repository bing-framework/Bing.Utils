param(
    [Parameter(Mandatory = $true)]
    [string]$PackagingAssemblyPath
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

# 仅生成测试夹具；测试运行不加载此程序集，不引用检测器实现。
[System.Reflection.Assembly]::LoadFrom((Resolve-Path -LiteralPath $PackagingAssemblyPath).Path) | Out-Null
$opcMainType = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml'
$opcRelationshipType = 'http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument'

function Add-OpcPart($Package, [string]$Name, [string]$ContentType) {
    $part = $Package.CreatePart([System.Uri]::new($Name, [System.UriKind]::Relative),
        $ContentType, [System.IO.Packaging.CompressionOption]::Normal)
    $stream = $part.GetStream()
    try {
        $bytes = [System.Text.Encoding]::UTF8.GetBytes('<document/>')
        $stream.Write($bytes, 0, $bytes.Length)
    }
    finally { $stream.Dispose() }
}

$opcCases = @(
    @{ File = 'ordinary.zip'; Main = '/word/document.xml' },
    @{ File = 'space-default.zip'; Main = '/custom/document%20name.xml' },
    @{ File = 'space-override.zip'; Main = '/custom/document%20name.xml'; Override = $true },
    @{ File = 'main-colon-extension.zip'; Main = '/custom/document.a:b' },
    @{ File = 'main-escaped-extension.zip'; Main = '/custom/document.a%2Bb' },
    @{ File = 'attachment-colon-extension.zip'; Main = '/word/document.xml'; Extra = '/custom/attachment.a:b' },
    @{ File = 'attachment-escaped-extension.zip'; Main = '/word/document.xml'; Extra = '/custom/attachment.a%2Bb' }
)
foreach ($case in $opcCases) {
    $memory = [System.IO.MemoryStream]::new()
    try {
        $package = [System.IO.Packaging.Package]::Open($memory, [System.IO.FileMode]::Create, [System.IO.FileAccess]::ReadWrite)
        try {
            # 相同 xml 扩展名先声明其他 MIME，使生产器自己生成主部件 Override。
            if ($case.Override) { Add-OpcPart $package '/custom/metadata.xml' 'application/xml' }
            Add-OpcPart $package $case.Main $opcMainType
            if ($case.Extra) { Add-OpcPart $package $case.Extra 'application/octet-stream' }
            $package.CreateRelationship([System.Uri]::new($case.Main, [System.UriKind]::Relative),
                [System.IO.Packaging.TargetMode]::Internal, $opcRelationshipType, 'rId1') | Out-Null
        }
        finally { $package.Dispose() }
        # 固定 ZIP 时间字段，使重新生成夹具不会只产生时间差异。
        $archive = [System.IO.Compression.ZipArchive]::new($memory, [System.IO.Compression.ZipArchiveMode]::Update, $true)
        try {
            foreach ($entry in $archive.Entries) { $entry.LastWriteTime = [System.DateTimeOffset]::Parse('2000-01-01T00:00:00+00:00') }
        }
        finally { $archive.Dispose() }
        $bytes = $memory.ToArray()
        $verifyStream = [System.IO.MemoryStream]::new($bytes, $false)
        try {
            $verifyPackage = [System.IO.Packaging.Package]::Open($verifyStream, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read)
            try {
                $part = $verifyPackage.GetPart([System.Uri]::new($case.Main, [System.UriKind]::Relative))
                if ($part.ContentType -ne $opcMainType) { throw 'Unexpected main part content type.' }
                $partCount = @($verifyPackage.GetParts()).Count
            }
            finally { $verifyPackage.Dispose() }
        }
        finally { $verifyStream.Dispose() }
        [System.IO.File]::WriteAllBytes((Join-Path $PSScriptRoot $case.File), $bytes)
        Write-Output ($case.File + ': ' + $partCount + ' parts, ' + $bytes.Length + ' bytes')
    }
    finally { $memory.Dispose() }
}
