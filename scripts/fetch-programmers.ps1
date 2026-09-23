# Fetch Samsung Firehose loaders from Alephgsm into Resources/programmers
# Run from repo root:  powershell -ExecutionPolicy Bypass -File scripts/fetch-programmers.ps1

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$dest = Join-Path $root "Resources\programmers\samsung"
$tmp = Join-Path $env:TEMP "SAMSUNG-EDL-Loaders"

Write-Host "Cloning Alephgsm/SAMSUNG-EDL-Loaders (shallow)..."
if (Test-Path $tmp) { Remove-Item -Recurse -Force $tmp }
git clone --depth 1 https://github.com/Alephgsm/SAMSUNG-EDL-Loaders.git $tmp

New-Item -ItemType Directory -Force -Path (Join-Path $dest "generic") | Out-Null

$generic = Join-Path $tmp "Generic Samsung Firehose"
if (Test-Path $generic) {
    Copy-Item (Join-Path $generic "*.elf") (Join-Path $dest "generic") -Force
    Write-Host "Generic platform loaders copied."
}

Get-ChildItem $tmp -Directory | Where-Object { $_.Name -like "Samsung*" } | ForEach-Object {
    $modelMatch = [regex]::Match($_.Name, "\((SM-[A-Z0-9]+)\)")
    if (-not $modelMatch.Success) { return }
    $model = $modelMatch.Groups[1].Value
    $firehose = Join-Path $_.FullName "firehose"
    if (-not (Test-Path $firehose)) { return }
    $out = Join-Path $dest $model
    New-Item -ItemType Directory -Force -Path $out | Out-Null
    Copy-Item (Join-Path $firehose "*") $out -Force
    Write-Host "  $model"
}

Write-Host "Done. Programmers under Resources/programmers/samsung"
Write-Host "Optional: pip install edl   (bkerler) for real EDL execution"
