param(
    [string]$GamePath = $(if ($env:AMONG_US_PATH) { $env:AMONG_US_PATH } else { 'C:\Program Files\Epic Games\AmongUs' }),
    [string]$ExpectedHash = '8C6CDBC38836DEE87E3368F5DE1994D7C0CCEBF29E4CE7ABA3C0981F9375412C'
)
$ErrorActionPreference = 'Stop'
if (Get-Process -Name 'Among Us' -ErrorAction SilentlyContinue | Where-Object { !$_.HasExited }) { throw 'Once oyunu kapatin.' }
$loader = Join-Path $GamePath 'winhttp.dll'
$disabled = Join-Path $GamePath 'winhttp.dll.disabled'
if (!(Test-Path -LiteralPath $loader)) { Write-Output 'Mod yukleyici zaten kapali.'; exit }
if (Test-Path -LiteralPath $disabled) { throw 'Devre disi dosyasi zaten var; uzerine yazilmadi.' }
if ((Get-FileHash -LiteralPath $loader -Algorithm SHA256).Hash -ne $ExpectedHash) {
    throw 'Yukleyici dosyasi beklenen BepInEx surumunden farkli; degistirilmedi.'
}
Move-Item -LiteralPath $loader -Destination $disabled
Write-Output 'Mod yukleyici kapatildi. Oyun ve ESP dosyalari korundu.'
