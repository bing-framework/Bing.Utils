[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string[]]$SearchRoot,
    [Parameter(Mandatory = $true)][string]$OutputPath,
    [string[]]$AssemblyName,
    [string]$RepositoryRoot
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

$searchRootFull = @($SearchRoot | ForEach-Object { [IO.Path]::GetFullPath($_).TrimEnd('\', '/') })
$searchRootFull | ForEach-Object {
    if (!(Test-Path -LiteralPath $_ -PathType Container)) {
        throw "Source map search root was not found: $_"
    }
}
$repositoryRootFull = if ([string]::IsNullOrWhiteSpace($RepositoryRoot)) {
    $searchRootFull[0]
} else {
    [IO.Path]::GetFullPath($RepositoryRoot).TrimEnd('\', '/')
}

function Normalize-SourcePath([string]$Path) {
    if ([string]::IsNullOrWhiteSpace($Path)) {
        return $null
    }

    try {
        $fullPath = [IO.Path]::GetFullPath($Path)
        $rootPrefix = $repositoryRootFull + [IO.Path]::DirectorySeparatorChar
        if (!$fullPath.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
            return $null
        }

        $relative = $fullPath.Substring($repositoryRootFull.Length + 1).Replace('\', '/')
        if ($relative -match '(^|/)(?:bin|obj|output|artifacts|BenchmarkDotNet\.Artifacts|TestResults)(?:/|$)') {
            return $null
        }
        return $relative
    } catch {
        return $null
    }
}

function Normalize-SymbolPart([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) {
        return $Value
    }

    $builder = [Text.StringBuilder]::new()
    $genericDepth = 0
    foreach ($character in $Value.ToCharArray()) {
        if ($character -eq '<') {
            $genericDepth++
            continue
        }
        if ($character -eq '>') {
            if ($genericDepth -gt 0) {
                $genericDepth--
            }
            continue
        }
        if ($genericDepth -gt 0) {
            continue
        }
        $null = $builder.Append($character)
    }

    return $builder.ToString().Replace('+', '.') -replace '`\d+', ''
}

function Get-ReflectionTypeDisplayName([Type]$Type) {
    if ($null -eq $Type) {
        return $null
    }
    if ($Type.IsByRef) {
        return "$(Get-ReflectionTypeDisplayName $Type.GetElementType())&"
    }
    if ($Type.IsPointer) {
        return "$(Get-ReflectionTypeDisplayName $Type.GetElementType())*"
    }
    if ($Type.IsArray) {
        $rank = $Type.GetArrayRank()
        $suffix = if ($rank -eq 1) { '[]' } else { "[$((',' * ($rank - 1)))]" }
        return "$(Get-ReflectionTypeDisplayName $Type.GetElementType())$suffix"
    }
    if ($Type.IsGenericParameter) {
        return $Type.Name
    }
    if ($Type.IsGenericType) {
        $name = $Type.Name -replace '`\d+', ''
        $arguments = @($Type.GetGenericArguments() | ForEach-Object {
            Get-ReflectionTypeDisplayName $_
        })
        return "$name<$($arguments -join ',')>"
    }
    if ($Type.IsNested) {
        return "$(Get-ReflectionTypeDisplayName $Type.DeclaringType).$($Type.Name)"
    }
    return $Type.Name -replace '`\d+', ''
}

function Get-ReflectionMethodSignature([Reflection.MethodBase]$Method) {
    if ($null -eq $Method) {
        return $null
    }
    $genericArguments = if ($Method -is [Reflection.MethodInfo] -and $Method.IsGenericMethod) {
        @($Method.GetGenericArguments())
    } else {
        @()
    }
    $genericSuffix = if ($genericArguments.Count -gt 0) {
        "<$(($genericArguments | ForEach-Object { $_.Name }) -join ',')>"
    } else {
        ''
    }
    $parameters = @($Method.GetParameters() | ForEach-Object {
        Get-ReflectionTypeDisplayName $_.ParameterType
    })
    return "$($Method.Name)$genericSuffix($($parameters -join ','))"
}

function Get-MethodSymbolKey([Type]$Type, [Reflection.MethodBase]$Method) {
    $owner = Normalize-SymbolPart $Type.FullName
    $methodName = Normalize-SymbolPart $Method.Name
    if ([string]::IsNullOrWhiteSpace($owner) -or [string]::IsNullOrWhiteSpace($methodName)) {
        return $null
    }
    return "$owner.$methodName"
}

function Get-DocumentForMethod($PdbReader, [int]$MethodToken) {
    if ($null -eq $PdbReader -or ($MethodToken -shr 24) -ne 0x06) {
        return $null
    }

    try {
        $row = $MethodToken -band 0x00ffffff
        if ($row -le 0) {
            return $null
        }
        $handle = [Reflection.Metadata.Ecma335.MetadataTokens]::MethodDebugInformationHandle($row)
        $debugInfo = $PdbReader.GetMethodDebugInformation($handle)
        $documentHandle = $debugInfo.Document
        if ($documentHandle.IsNil) {
            foreach ($point in $debugInfo.GetSequencePoints()) {
                if (!$point.Document.IsNil) {
                    $documentHandle = $point.Document
                    break
                }
            }
        }
        if ($documentHandle.IsNil) {
            return $null
        }
        return $PdbReader.GetString($PdbReader.GetDocument($documentHandle).Name)
    } catch {
        return $null
    }
}

function Add-SourceMapEntry([hashtable]$Map, [string]$Key, [string]$File) {
    if ([string]::IsNullOrWhiteSpace($Key) -or [string]::IsNullOrWhiteSpace($File)) {
        return
    }
    if (!$Map.ContainsKey($Key)) {
        $Map[$Key] = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    }
    $null = $Map[$Key].Add($File)
}

function Add-SourceMapOverload([hashtable]$Map, [string]$Key, [string]$Signature, [string]$File) {
    if ([string]::IsNullOrWhiteSpace($Key) -or
        [string]::IsNullOrWhiteSpace($Signature) -or
        [string]::IsNullOrWhiteSpace($File)) {
        return
    }
    if (!$Map.ContainsKey($Key)) {
        $Map[$Key] = [Collections.Generic.List[object]]::new()
    }
    foreach ($existing in $Map[$Key]) {
        if ($existing.signature -eq $Signature -and
            [StringComparer]::OrdinalIgnoreCase.Equals($existing.file, $File)) {
            return
        }
    }
    $Map[$Key].Add([pscustomobject]@{
        signature = $Signature
        file = $File
    })
}

function Get-MetadataHandleToken($Handle) {
    $entityHandle = [Reflection.Metadata.EntityHandle]$Handle
    return $script:MetadataEntityTokenMethod.Invoke($null, [object[]]@($entityHandle))
}

$script:MetadataEntityTokenMethod = [Reflection.Metadata.Ecma335.MetadataTokens].GetMethods() |
    Where-Object {
        $_.Name -eq 'GetToken' -and
        $_.GetParameters().Count -eq 1 -and
        $_.GetParameters()[0].ParameterType -eq [Reflection.Metadata.EntityHandle]
    } |
    Select-Object -First 1
if ($null -eq $script:MetadataEntityTokenMethod) {
    throw 'Metadata token API does not support entity handles.'
}

function Get-MetadataTypeName($MetadataReader, $TypeHandle, [hashtable]$TypeCache) {
    $token = Get-MetadataHandleToken $TypeHandle
    $cacheKey = [string]$token
    if ($TypeCache.ContainsKey($cacheKey)) {
        return $TypeCache[$cacheKey]
    }

    $definition = $MetadataReader.GetTypeDefinition($TypeHandle)
    $name = $MetadataReader.GetString($definition.Name)
    if ($definition.GetDeclaringType().IsNil) {
        $namespace = $MetadataReader.GetString($definition.Namespace)
        $fullName = if ([string]::IsNullOrWhiteSpace($namespace)) { $name } else { "$namespace.$name" }
    } else {
        $declaringName = Get-MetadataTypeName $MetadataReader $definition.GetDeclaringType() $TypeCache
        $fullName = "$declaringName.$name"
    }

    $TypeCache[$cacheKey] = $fullName
    return $fullName
}

$methodFiles = @{}
$methodOverloads = @{}
$assemblyPathSet = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($root in $searchRootFull) {
    foreach ($assembly in @(Get-ChildItem -LiteralPath $root -Recurse -File -Filter '*.dll' |
        Where-Object {
            $_.FullName -notmatch '(?i)(?:\\|/)(?:ref|runtimes)(?:\\|/)' -and
            (!$AssemblyName -or $_.Name -in $AssemblyName)
        })) {
        $null = $assemblyPathSet.Add($assembly.FullName)
    }
}
$assemblyPaths = @($assemblyPathSet | ForEach-Object { Get-Item -LiteralPath $_ })

foreach ($assemblyPath in $assemblyPaths) {
    $pdbPath = [IO.Path]::ChangeExtension($assemblyPath.FullName, '.pdb')
    if (!(Test-Path -LiteralPath $pdbPath -PathType Leaf)) {
        continue
    }

    $assemblyStream = $null
    $peReader = $null
    $metadataProvider = $null
    $pdbStream = $null
    $pdbProvider = $null
    try {
        $assemblyStream = [IO.File]::OpenRead($assemblyPath.FullName)
        $peReader = [Reflection.PortableExecutable.PEReader]::new($assemblyStream)
        $metadataProvider = [Reflection.Metadata.MetadataReaderProvider]::FromMetadataImage(
            $peReader.GetMetadata().GetContent())
        $metadataReader = $metadataProvider.GetMetadataReader()
        $pdbStream = [IO.File]::OpenRead($pdbPath)
        $pdbProvider = [Reflection.Metadata.MetadataReaderProvider]::FromPortablePdbStream($pdbStream)
        $pdbReader = $pdbProvider.GetMetadataReader()
        $typeCache = @{}
        $reflectionMethodsByToken = @{}
        try {
            $reflectionAssembly = [Reflection.Assembly]::LoadFrom($assemblyPath.FullName)
            foreach ($reflectionType in @($reflectionAssembly.GetTypes())) {
                $flags = [Reflection.BindingFlags]::Public -bor
                    [Reflection.BindingFlags]::NonPublic -bor
                    [Reflection.BindingFlags]::Static -bor
                    [Reflection.BindingFlags]::Instance -bor
                    [Reflection.BindingFlags]::DeclaredOnly
                foreach ($reflectionMethod in @($reflectionType.GetMethods($flags))) {
                    $reflectionMethodsByToken[$reflectionMethod.MetadataToken] = $reflectionMethod
                }
                foreach ($reflectionConstructor in @($reflectionType.GetConstructors($flags))) {
                    $reflectionMethodsByToken[$reflectionConstructor.MetadataToken] = $reflectionConstructor
                }
            }
        } catch {
            # Reflection loading is an optional signature enhancement. PDB file mapping remains valid.
            Write-Verbose "Could not load reflection signatures from '$($assemblyPath.FullName)': $($_.Exception.Message)"
        }

        foreach ($typeHandle in @($metadataReader.TypeDefinitions)) {
            $typeName = Get-MetadataTypeName $metadataReader $typeHandle $typeCache
            if ($typeName -eq '<Module>') {
                continue
            }
            $typeDefinition = $metadataReader.GetTypeDefinition($typeHandle)
            foreach ($methodHandle in @($typeDefinition.GetMethods())) {
                $methodDefinition = $metadataReader.GetMethodDefinition($methodHandle)
                $methodName = $metadataReader.GetString($methodDefinition.Name)
                $key = Normalize-SymbolPart "$typeName.$methodName"
                $methodToken = Get-MetadataHandleToken $methodHandle
                $file = Normalize-SourcePath (Get-DocumentForMethod $pdbReader $methodToken)
                if ($null -eq $file) {
                    continue
                }
                Add-SourceMapEntry $methodFiles $key $file
                $reflectionMethod = if ($reflectionMethodsByToken.ContainsKey($methodToken)) {
                    $reflectionMethodsByToken[$methodToken]
                } else {
                    $null
                }
                if ($null -ne $reflectionMethod) {
                    Add-SourceMapOverload $methodOverloads $key (Get-ReflectionMethodSignature $reflectionMethod) $file
                }
            }
        }
    } catch {
        # A symbol file can be stale, incomplete, or belong to an unloadable assembly.
        # Such an input must not make the warning checker claim a source location.
        Write-Verbose "Skipping source map assembly '$($assemblyPath.FullName)': $($_.Exception.Message)"
    } finally {
        if ($null -ne $metadataProvider) { $metadataProvider.Dispose() }
        if ($null -ne $peReader) { $peReader.Dispose() }
        if ($null -ne $assemblyStream) { $assemblyStream.Dispose() }
        if ($null -ne $pdbProvider) { $pdbProvider.Dispose() }
        if ($null -ne $pdbStream) { $pdbStream.Dispose() }
    }
}

$entries = foreach ($key in @($methodFiles.Keys | Sort-Object)) {
    $files = @($methodFiles[$key] | Sort-Object)
    $overloads = if ($methodOverloads.ContainsKey($key)) {
        @($methodOverloads[$key] |
            Sort-Object signature, file |
            ForEach-Object {
                [ordered]@{
                    signature = $_.signature
                    file = $_.file
                }
            })
    } else {
        @()
    }
    [ordered]@{
        key = $key
        file = if ($files.Count -eq 1) { $files[0] } else { $null }
        candidates = $files
        overloads = $overloads
    }
}

$report = [ordered]@{
    schemaVersion = 2
    repositoryRoot = $repositoryRootFull
    searchRoot = @($searchRootFull)
    entryCount = @($entries).Count
    uniqueEntryCount = @($entries | Where-Object { $null -ne $_.file }).Count
    ambiguousEntryCount = @($entries | Where-Object { $null -eq $_.file }).Count
    entries = @($entries)
}

$outputDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
if ($outputDirectory -and !(Test-Path -LiteralPath $outputDirectory)) {
    $null = New-Item -ItemType Directory -Path $outputDirectory
}
$report | ConvertTo-Json -Depth 6 | Set-Content -LiteralPath $OutputPath -Encoding utf8
Write-Output ($report | ConvertTo-Json -Depth 6)
