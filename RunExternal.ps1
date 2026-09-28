param([switch]$BuildOnly)

$ErrorActionPreference = 'Stop'
$cscPath = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (!(Test-Path -LiteralPath $cscPath)) { throw '64-bit .NET Framework derleyicisi bulunamadi.' }
$exePath = Join-Path $PSScriptRoot 'AmongUsMenu_v3.exe'
$candidate = Join-Path $PSScriptRoot 'AmongUsMenu_v3.new.exe'
$running = Get-Process -Name 'AmongUsMenu_v3','AmongUsMenu' -ErrorAction SilentlyContinue |
    Where-Object { $_.Path -and (Split-Path $_.Path) -eq $PSScriptRoot }
if ($running) { throw 'Once acik menu penceresini kapatin; oyun acik kalabilir.' }

$sources = @(Get-ChildItem -LiteralPath $PSScriptRoot -Filter '*.cs' -File) +
    @(Get-ChildItem -LiteralPath (Join-Path $PSScriptRoot 'Features') -Filter '*.cs' -File)
$compileArgs = @('/nologo', '/platform:x64', '/target:winexe',
    '/r:System.Windows.Forms.dll', '/r:System.Drawing.dll', '/r:System.dll', '/r:System.Core.dll',
    "/out:$candidate", "/win32manifest:$(Join-Path $PSScriptRoot 'app.manifest')") + $sources.FullName
& $cscPath @compileArgs
if ($LASTEXITCODE -ne 0) { throw 'Derleme basarisiz; mevcut EXE korundu.' }
Move-Item -LiteralPath $candidate -Destination $exePath -Force
Copy-Item -LiteralPath $exePath -Destination (Join-Path $PSScriptRoot 'AmongUsMenu.exe') -Force
Write-Host 'V3 derlendi. Metadata 31 surum kontrolu etkin.'
if (!$BuildOnly) { Start-Process -FilePath $exePath -WorkingDirectory $PSScriptRoot }
