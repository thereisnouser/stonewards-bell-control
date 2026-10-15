param(
    [Parameter(Mandatory = $true)]
    [string]$GameDir,
    [ValidateSet('Release', 'Debug')]
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'The .NET SDK is required. Install an SDK that supports netstandard2.1.'
}

$resolvedGameDir = (Resolve-Path -LiteralPath $GameDir -ErrorAction Stop).Path
$managedDir = Join-Path $resolvedGameDir 'Stonewards_Data\Managed'
$bepInExCore = Join-Path $resolvedGameDir 'BepInEx\core'

$requiredFiles = @(
    (Join-Path $managedDir 'Assembly-CSharp.dll'),
    (Join-Path $managedDir 'Mirror.dll'),
    (Join-Path $managedDir 'UnityEngine.CoreModule.dll'),
    (Join-Path $managedDir 'UnityEngine.IMGUIModule.dll'),
    (Join-Path $managedDir 'Unity.InputSystem.dll'),
    (Join-Path $bepInExCore 'BepInEx.dll'),
    (Join-Path $bepInExCore '0Harmony.dll')
)

foreach ($file in $requiredFiles) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Required dependency was not found: $file"
    }
}

$project = Join-Path $PSScriptRoot '..\src\StonewardsBellControl\StonewardsBellControl.csproj'

& dotnet build $project -c $Configuration "-p:GameDir=$resolvedGameDir"
if ($LASTEXITCODE -ne 0) {
    throw "Build failed (dotnet exit code: $LASTEXITCODE)."
}

$dll = Join-Path $PSScriptRoot "..\src\StonewardsBellControl\bin\$Configuration\netstandard2.1\StonewardsBellControl.dll"
if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) {
    throw "Build finished without the expected plugin DLL: $dll"
}

Write-Host "Plugin DLL: $((Resolve-Path -LiteralPath $dll).Path)" -ForegroundColor Green
