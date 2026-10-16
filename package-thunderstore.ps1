# Builds a verified Stonewards Bell Control Thunderstore upload ZIP.
# Does not publish or upload anything to Thunderstore.
[CmdletBinding()]
param(
    [string]$GameDir,
    [string]$ProfileDir = (Join-Path $env:APPDATA 'r2modmanPlus-local\Stonewards\profiles\stonewards-bell-control'),
    [switch]$SkipBuild
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest
$root = $PSScriptRoot
$packageDir = Join-Path $root 'thunderstore'
$manifestPath = Join-Path $packageDir 'manifest.json'
$readmePath = Join-Path $packageDir 'README.md'
$changelogPath = Join-Path $packageDir 'CHANGELOG.md'
$iconPath = Join-Path $packageDir 'icon.png'
$licensePath = Join-Path $root 'LICENSE'
$projectPath = Join-Path $root 'src\StonewardsBellControl\StonewardsBellControl.csproj'
$pluginSource = Join-Path $root 'src\StonewardsBellControl\BellControlPlugin.cs'
$dll = Join-Path $root 'src\StonewardsBellControl\bin\Release\netstandard2.1\StonewardsBellControl.dll'

foreach ($path in @($manifestPath, $readmePath, $changelogPath,
                    $iconPath, $licensePath, $projectPath, $pluginSource)) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Required release file not found: $path"
    }
}

$manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
if ($manifest.name -ne 'BellControl' -or
    $manifest.version_number -notmatch '^\d+\.\d+\.\d+$') {
    throw 'Unexpected Thunderstore name or invalid version_number.'
}

$source = Get-Content -LiteralPath $pluginSource -Raw
$versionMatch = [regex]::Match($source, 'PluginVersion\s*=\s*"([^"]+)"')
if (-not $versionMatch.Success -or
    $versionMatch.Groups[1].Value -ne $manifest.version_number) {
    throw "PluginVersion must match manifest version $($manifest.version_number)."
}
$version = $manifest.version_number
$changeText = Get-Content -LiteralPath $changelogPath -Raw
if ($changeText -notmatch ('(?m)^##\s+' + [regex]::Escape($version) + '\b')) {
    throw "Thunderstore changelog must contain an entry for $version."
}

# Thunderstore requires a 256x256 PNG, not just a .png extension.
$iconBytes = [System.IO.File]::ReadAllBytes($iconPath)
$signature = [byte[]](137, 80, 78, 71, 13, 10, 26, 10)
if ($iconBytes.Length -lt 24) { throw 'Invalid PNG: too small.' }
for ($i = 0; $i -lt 8; $i++) {
    if ($iconBytes[$i] -ne $signature[$i]) { throw 'Invalid PNG signature.' }
}
$width = ([int]$iconBytes[16] -shl 24) -bor ([int]$iconBytes[17] -shl 16) -bor
         ([int]$iconBytes[18] -shl 8) -bor [int]$iconBytes[19]
$height = ([int]$iconBytes[20] -shl 24) -bor ([int]$iconBytes[21] -shl 16) -bor
          ([int]$iconBytes[22] -shl 8) -bor [int]$iconBytes[23]
if ($width -ne 256 -or $height -ne 256) {
    throw "Thunderstore requires a 256x256 PNG, got $width x $height."
}

if (-not $SkipBuild) {
    if ([string]::IsNullOrWhiteSpace($GameDir)) {
        throw 'Provide -GameDir, or use -SkipBuild with a previously compiled DLL.'
    }
    if (-not (Test-Path -LiteralPath $ProfileDir -PathType Container)) {
        throw "Expected r2modman profile not found: $ProfileDir"
    }
    Write-Host "Building Bell Control $version for Thunderstore..." -ForegroundColor Cyan
    & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $root 'build-and-install.ps1') `
        -GameDir $GameDir -ProfileDir $ProfileDir -Configuration Release -NoInstall
    if ($LASTEXITCODE -ne 0) { throw "Release compilation failed ($LASTEXITCODE)." }
}

if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw "Compiled DLL is missing: $dll"
}
$dllItem = Get-Item -LiteralPath $dll
if ($dllItem.Length -lt 4096) { throw 'Compiled DLL is unexpectedly small.' }

# Never upload a stale binary, even when -SkipBuild was specified.
$inputs = @(Get-ChildItem -LiteralPath (Join-Path $root 'src\StonewardsBellControl') -Filter '*.cs' -File)
$inputs += Get-Item -LiteralPath $projectPath
$newest = $inputs | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1
if ($dllItem.LastWriteTimeUtc -lt $newest.LastWriteTimeUtc) {
    throw "Compiled DLL is older than $($newest.Name). Rebuild before packaging."
}

# The constant is embedded in the BepInPlugin assembly metadata.
$dllText = [System.Text.Encoding]::ASCII.GetString([System.IO.File]::ReadAllBytes($dll))
if (-not $dllText.Contains($version)) {
    throw "Compiled DLL does not contain plugin version $version; rebuild."
}

$dest = Join-Path $root 'dist'
New-Item -ItemType Directory -Path $dest -Force | Out-Null
$zip = Join-Path $dest "thereisnouser-BellControl-$version.zip"
if (Test-Path -LiteralPath $zip) {
    throw "Release ZIP already exists: $zip. Remove it explicitly only if rebuilding."
}
$items = @(
    @{ Source = $manifestPath; Entry = 'manifest.json' },
    @{ Source = $iconPath; Entry = 'icon.png' },
    @{ Source = $readmePath; Entry = 'README.md' },
    @{ Source = $changelogPath; Entry = 'CHANGELOG.md' },
    @{ Source = $licensePath; Entry = 'LICENSE' },
    @{ Source = $dll; Entry = 'plugins/StonewardsBellControl.dll' }
)

# Write exact forward-slash paths. No game DLLs, profile configs or source.
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$tmp = "$zip.$([Guid]::NewGuid().ToString('N')).tmp"
$archive = $null
$check = $null
try {
    $archive = [System.IO.Compression.ZipFile]::Open(
        $tmp, [System.IO.Compression.ZipArchiveMode]::Create)
    foreach ($item in $items) {
        [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile(
            $archive, $item.Source, $item.Entry,
            [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
    $archive.Dispose()
    $archive = $null

    $check = [System.IO.Compression.ZipFile]::OpenRead($tmp)
    $actual = @($check.Entries | ForEach-Object { $_.FullName })
    $expected = @($items | ForEach-Object { $_.Entry })
    if ($actual.Count -ne $expected.Count -or
        @(Compare-Object -ReferenceObject $expected -DifferenceObject $actual).Count -gt 0) {
        throw 'Thunderstore ZIP contains an unexpected file layout.'
    }
    $check.Dispose()
    $check = $null
    Move-Item -LiteralPath $tmp -Destination $zip
}
finally {
    if ($null -ne $check) { $check.Dispose() }
    if ($null -ne $archive) { $archive.Dispose() }
    if (Test-Path -LiteralPath $tmp) { Remove-Item -LiteralPath $tmp -Force }
}

Write-Host "Thunderstore package ready: $zip" -ForegroundColor Green
Write-Host "Version: $version | Icon: 256x256 | Files: $($items.Count)" -ForegroundColor Cyan
Write-Host "ZIP SHA256: $((Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash)"
Write-Host 'Not uploaded. Test and submit it from your Thunderstore team.' -ForegroundColor Yellow
