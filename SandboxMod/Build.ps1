param(
    [switch]$Install,
    [string]$GamePath = $(if ($env:AMONG_US_PATH) { $env:AMONG_US_PATH } else { 'C:\Program Files\Epic Games\AmongUs' }),
    [string]$DotNetPath = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$dotnet = (Get-Command $DotNetPath -ErrorAction Stop).Source
if (!(Test-Path -LiteralPath $GamePath)) { throw "Among Us klasoru bulunamadi: $GamePath" }
& $dotnet build (Join-Path $PSScriptRoot 'KV3.Sandbox.csproj') -c Release --nologo "/p:GamePath=$GamePath"
if ($LASTEXITCODE -ne 0) { throw 'Mod derlenemedi. Calisan ESP degistirilmedi.' }
$dll = Join-Path $PSScriptRoot 'bin\Release\net6.0\KV3.Sandbox.dll'
if (Get-Command Start-MpScan -ErrorAction SilentlyContinue) {
    Start-MpScan -ScanType CustomScan -ScanPath $dll
} else {
    Write-Warning 'Windows Defender tarama komutu bulunamadi; DLL otomatik taranmadi.'
}
if ($Install) {
    if (Get-Process -Name 'Among Us' -ErrorAction SilentlyContinue | Where-Object { !$_.HasExited }) { throw 'Mod hazir; kurulum icin once oyunu kapatin.' }
    $plugins = Join-Path $GamePath 'BepInEx\plugins'
    if (!(Test-Path -LiteralPath $plugins)) { throw 'BepInEx plugins klasoru yok.' }
    $destination = Join-Path $plugins 'KV3.Sandbox.dll'
    if(Test-Path -LiteralPath $destination) {
        Copy-Item -LiteralPath $destination -Destination ($destination + '.' + [DateTime]::UtcNow.ToString('yyyyMMddHHmmss') + '.bak')
    }
    Copy-Item -LiteralPath $dll -Destination $destination -Force
    Write-Output 'KV3 mod kuruldu. ESP degistirilmedi.'
}
