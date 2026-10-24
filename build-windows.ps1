# Build xTrades for Windows (x64 and ARM64) as self-contained executables.
# Usage:  .\build-windows.ps1            — builds both architectures to dist\
#         .\build-windows.ps1 -Arch x64  — x64 only
# Output: dist\xTrades-win-x64\TradingHub.exe  (and dist\xTrades-win-arm64\ if built)
param(
    [ValidateSet("x64", "arm64", "both")]
    [string]$Arch = "both"
)
Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$root = Split-Path $MyInvocation.MyCommand.Path
Push-Location $root

$env:DOTNET_CLI_TELEMETRY_OPTOUT = "1"
$env:DOTNET_NOLOGO = "1"

$project = "src\TradingHub.App\TradingHub.App.csproj"
$out = Join-Path $root "dist"

function Build-Arch([string]$rid) {
    $label = "xTrades-win-$($rid -replace 'win-','')"
    $dest = Join-Path $out $label
    Write-Host "Building $label ..."
    if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
    dotnet publish $project -c Release -r $rid --self-contained true -o $dest -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed for $rid" }
    Write-Host "  -> $dest"
}

if ($Arch -eq "x64" -or $Arch -eq "both") { Build-Arch "win-x64" }
if ($Arch -eq "arm64" -or $Arch -eq "both") { Build-Arch "win-arm64" }

Write-Host ""
Write-Host "Done. Executables in:"
Get-ChildItem $out -Filter "xTrades-win-*" | ForEach-Object {
    $exe = Join-Path $_.FullName "TradingHub.exe"
    if (Test-Path $exe) { Write-Host "  $exe" }
}

Pop-Location
