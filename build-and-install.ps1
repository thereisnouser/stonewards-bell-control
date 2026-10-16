param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,

    # r2modman profile directory. If omitted, build only.
    [string]$ProfileDir,

    # Explicit BepInEx root, if different from ProfileDir/BepInEx or GameDir/BepInEx.
    [string]$BepInExDir,

    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release',

    [switch]$NoInstall
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is missing. Install a .NET SDK with netstandard2.1 support.'
}

$game = (Resolve-Path -LiteralPath $GameDir -ErrorAction Stop).Path
$managed = Join-Path $game 'Stonewards_Data\Managed'

if ($ProfileDir) {
    $profile = (Resolve-Path -LiteralPath $ProfileDir -ErrorAction Stop).Path
} else {
    $profile = $null
}

if ($BepInExDir) {
    $bepRoot = (Resolve-Path -LiteralPath $BepInExDir -ErrorAction Stop).Path
} elseif ($profile) {
    $bepRoot = Join-Path $profile 'BepInEx'
} else {
    $bepRoot = Join-Path $game 'BepInEx'
}
$bepCore = Join-Path $bepRoot 'core'

$required = @(
    (Join-Path $managed 'Assembly-CSharp.dll'),
    (Join-Path $managed 'Mirror.dll'),
    (Join-Path $managed 'UnityEngine.dll'),
    (Join-Path $managed 'UnityEngine.CoreModule.dll'),
    (Join-Path $managed 'UnityEngine.UIModule.dll'),
    (Join-Path $managed 'UnityEngine.UIElementsModule.dll'),
    (Join-Path $managed 'UnityEngine.UI.dll'),
    (Join-Path $managed 'UnityEngine.TextRenderingModule.dll'),
    (Join-Path $managed 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managed 'Unity.InputSystem.dll'),
    (Join-Path $bepCore 'BepInEx.dll'),
    (Join-Path $bepCore '0Harmony.dll')
)
foreach ($path in $required) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Missing dependency: $path. Check the game directory and r2modman profile."
    }
}

$project = Join-Path $PSScriptRoot 'src\StonewardsBellControl\StonewardsBellControl.csproj'
if (-not (Test-Path -LiteralPath $project -PathType Leaf)) {
    throw "Project file not found: $project"
}

Write-Host "Building Bell Control ($Configuration)..." -ForegroundColor Cyan
& dotnet build $project --configuration $Configuration "-p:GameDir=$game" "-p:BepInExDir=$bepRoot"
if ($LASTEXITCODE -ne 0) {
    throw "dotnet build failed with exit code $LASTEXITCODE."
}

$dll = Join-Path $PSScriptRoot "src\StonewardsBellControl\bin\$Configuration\netstandard2.1\StonewardsBellControl.dll"
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw "Build finished but the plugin DLL is missing: $dll"
}

Write-Host "Built: $dll" -ForegroundColor Green

if ($NoInstall -or -not $profile) {
    Write-Host 'Build only; no installation performed.' -ForegroundColor Yellow
    exit 0
}

$pluginDir = Join-Path $bepRoot 'plugins\StonewardsBellControl'
New-Item -ItemType Directory -Path $pluginDir -Force | Out-Null
$destination = Join-Path $pluginDir 'StonewardsBellControl.dll'
Copy-Item -LiteralPath $dll -Destination $destination -Force

Write-Host "Installed: $destination" -ForegroundColor Green
Write-Host 'Start Stonewards through the same r2modman profile and host a session. Toggle guest ringing with F8.' -ForegroundColor Cyan
