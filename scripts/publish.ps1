# Self-contained win-x64 publish
$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
Set-Location $root

dotnet publish AxionUnlockPro/AxionUnlockPro.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=false -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish/win-x64

# ensure folders
New-Item -ItemType Directory -Force -Path publish/win-x64/bin | Out-Null
New-Item -ItemType Directory -Force -Path publish/win-x64/Resources/programmers/samsung/generic | Out-Null
New-Item -ItemType Directory -Force -Path publish/win-x64/Resources/programmers/mtk | Out-Null
New-Item -ItemType Directory -Force -Path publish/win-x64/logs | Out-Null

Write-Host "Published to publish/win-x64"
Write-Host "Copy adb.exe fastboot.exe into publish/win-x64/bin"
Write-Host "Run scripts/fetch-programmers.ps1 then copy Resources into publish folder"
Write-Host "pip install edl mtkclient on the target machine"
