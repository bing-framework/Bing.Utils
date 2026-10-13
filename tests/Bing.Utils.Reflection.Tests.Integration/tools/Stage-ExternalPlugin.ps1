[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PluginAssemblyPath,
    [Parameter(Mandatory = $true)][string]$HostAssemblyPath,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string[]]$AdditionalAssemblyPath = @()
)

[Console]::InputEncoding = [System.Text.UTF8Encoding]::new($false)
[Console]::OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$OutputEncoding = [System.Text.UTF8Encoding]::new($false)
$ErrorActionPreference = 'Stop'

foreach ($path in @($PluginAssemblyPath, $HostAssemblyPath) + @($AdditionalAssemblyPath)) {
    if (!(Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "External plugin deployment input was not found: $path"
    }
}

$outputFullPath = [IO.Path]::GetFullPath($OutputDirectory)
if (!(Test-Path -LiteralPath $outputFullPath -PathType Container)) {
    $null = New-Item -ItemType Directory -Path $outputFullPath -Force
}

Copy-Item -LiteralPath $PluginAssemblyPath -Destination (Join-Path $outputFullPath ([IO.Path]::GetFileName($PluginAssemblyPath))) -Force
Copy-Item -LiteralPath $HostAssemblyPath -Destination (Join-Path $outputFullPath ([IO.Path]::GetFileName($HostAssemblyPath))) -Force
foreach ($path in $AdditionalAssemblyPath) {
    Copy-Item -LiteralPath $path -Destination (Join-Path $outputFullPath ([IO.Path]::GetFileName($path))) -Force
}

Write-Output "reflection-plugin-staged:$outputFullPath"
