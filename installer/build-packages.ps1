# Qutre 3D - reproducible release build.
# Usage:  powershell -ExecutionPolicy Bypass -File installer\build-packages.ps1

param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root "src\Qutre3D\Qutre3D.csproj"
$dist = Join-Path $root "dist"
$installers = Join-Path $dist "installers"
$stageRoot = Join-Path $dist "stage"

$dotnet = Join-Path $env:USERPROFILE "dotnet\dotnet.exe"
if (!(Test-Path $dotnet)) { $dotnet = "dotnet" }

$version = (Select-String -Path $project -Pattern '<Version>(.*?)</Version>').Matches[0].Groups[1].Value
Write-Host "Qutre 3D $version" -ForegroundColor Cyan

New-Item -ItemType Directory -Force $installers | Out-Null
Remove-Item -Recurse -Force $stageRoot -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Force $stageRoot | Out-Null

function Publish-App([string]$rid) {
    $out = Join-Path $dist $rid
    Remove-Item -Recurse -Force $out -ErrorAction SilentlyContinue
    Write-Host "Publishing $rid ..." -ForegroundColor Yellow
    & $dotnet publish $project -c $Configuration -r $rid --self-contained true -o $out `
        -p:DebugType=None -p:DebugSymbols=false | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }
    return $out
}

# ---------------------------------------------------------------- Windows
$win = Publish-App "win-x64"

$portable = Join-Path $installers "Qutre3D-$version-win-x64-portable.zip"
Remove-Item $portable -ErrorAction SilentlyContinue
Write-Host "Packing $portable ..." -ForegroundColor Yellow
Compress-Archive -Path (Join-Path $win "*") -DestinationPath $portable -CompressionLevel Optimal

$isccCandidates = @(
    (Join-Path $env:LOCALAPPDATA "Programs\Inno Setup 6\ISCC.exe"),
    "C:\Program Files (x86)\Inno Setup 6\ISCC.exe",
    "C:\Program Files\Inno Setup 6\ISCC.exe"
)
$iscc = $isccCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($iscc) {
    Write-Host "Building Windows installer ..." -ForegroundColor Yellow
    & $iscc (Join-Path $root "installer\windows\qutre3d.iss") | Select-Object -Last 3
    if ($LASTEXITCODE -ne 0) { throw "Inno Setup failed" }
} else {
    Write-Warning "Inno Setup not found - Windows installer skipped (portable zip is ready)."
}

# ---------------------------------------------------------------- Linux
foreach ($rid in @("linux-x64", "linux-arm64")) {
    $app = Publish-App $rid

    $folder = "qutre3d-$version-$rid"
    $stage = Join-Path $stageRoot $folder
    New-Item -ItemType Directory -Force $stage | Out-Null

    Copy-Item (Join-Path $app "*") $stage -Recurse -Force
    Copy-Item (Join-Path $root "installer\linux\install.sh") $stage -Force
    Copy-Item (Join-Path $root "assets\icon-256.png") (Join-Path $stage "assets") -Force

    $tar = Join-Path $installers "Qutre3D-$version-$rid.tar.gz"
    Remove-Item $tar -ErrorAction SilentlyContinue
    Write-Host "Packing $tar ..." -ForegroundColor Yellow
    tar -czf $tar -C $stageRoot $folder
    if ($LASTEXITCODE -ne 0) { throw "tar failed for $rid" }
}

Write-Host ""
Write-Host "Done:" -ForegroundColor Green
Get-ChildItem $installers | ForEach-Object { "{0}  {1:N1} MB" -f $_.Name, ($_.Length / 1MB) }
