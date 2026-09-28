# 🛠️ Derleme Rehberi (Build Guide)

**Among Us Menu'yu sıfırdan derlemek ve özelleştirmek için teknik kılavuz.**

---

## 📋 İçindekiler

1. [Ön Gereksinimler](#ön-gereksinimler)
2. [Manuel Derleme](#manuel-derleme)
3. [Otomatik Derleme (baslat.bat)](#otomatik-derleme)
4. [Troubleshooting](#troubleshooting)
5. [Yeni Feature Ekleme](#yeni-feature-ekleme)

---

## ✅ Ön Gereksinimler

### Sistem Gereksinimleri
- **OS**: Windows 10 / 11 (Pro veya Üstü)
- **Yetki**: Administrator (Yönetici Hesabı)
- **.NET Framework**: 4.5 veya üstü (Windows'ta varsayılan gelir)
- **C# Derleyici**: CSC.exe (Visual Studio veya .NET Framework ile birlikte)

### Kontrol Etme
```powershell
# .NET Framework version
Get-ChildItem "HKLM:\Software\Microsoft\NET Framework Setup\NDP"

# CSC yolunu bul
Get-ChildItem -Path "C:\Windows\Microsoft.NET" -Recurse -Filter "csc.exe"
```

---

## 🔨 Manuel Derleme

### Yöntem 1: CSC.exe ile Direkt Derleme

```powershell
# PowerShell'i Admin modunda açın

cd <proje-klasoru>

# CSC.exe yolunu tanımla (varsa)
$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

# Derleme
& $CSC `
  /nologo `
  /r:System.Windows.Forms.dll `
  /r:System.Drawing.dll `
  /r:System.dll `
  /r:System.Core.dll `
  /target:winexe `
  /out:"AmongUsMenu.exe" `
  /win32manifest:"app.manifest" `
  /recurse:"*.cs"

# Sonuç
if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Derleme başarılı!" -ForegroundColor Green
    Get-Item "AmongUsMenu.exe" | Select-Object Name, Length, LastWriteTime
} else {
    Write-Host "❌ Derleme başarısız!" -ForegroundColor Red
}
```

### Yöntem 2: Batch Script ile

```batch
@echo off
setlocal

cd /d "%~dp0"

set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe

if not exist "%CSC%" (
    echo Hata: CSC.exe bulunamadi
    pause
    exit /B 1
)

echo Derleniyor...
"%CSC%" ^
  /nologo ^
  /r:System.Windows.Forms.dll ^
  /r:System.Drawing.dll ^
  /r:System.dll ^
  /r:System.Core.dll ^
  /target:winexe ^
  /out:"AmongUsMenu.exe" ^
  /win32manifest:"app.manifest" ^
  /recurse:"*.cs"

if %ERRORLEVEL% equ 0 (
    echo.
    echo [+] Derleme basarili!
    dir /b AmongUsMenu.exe
) else (
    echo [-] Derleme basarisiz!
    pause
    exit /B 1
)

pause
```

---

## 🤖 Otomatik Derleme

### `baslat.bat` Kullanarak

```batch
# Masaüstündeki baslat.bat dosyasına çift tıkla
# veya sağ tıklayıp "Yönetici Olarak Çalıştır" seç
```

**Ne olur:**
1. Admin yetkisi kontrol edilir
2. CSC.exe aranır
3. Proje derlenecek
4. AmongUsMenu.exe oluşturulacak
5. Program otomatik başlatılacak

**Çıktı:**
```
===================================================
     AMONG US ULTIMATE IN-GAME MENU (AKTIF)         
===================================================

[*] Proje derleniyor (AmongUsMenu.exe)...
[+] Derleme basarili!

[*] MENU BASLATILDI!
```

---

## 🐛 Troubleshooting

### Hata: "CSC.exe bulunamadi"

**Neden?** .NET Framework doğru kurulmamış

**Çözüm 1:**
```powershell
# Manual olarak Framework'ü kurmanın alternatif yolu
# https://dotnet.microsoft.com/download/dotnet-framework

# Visual Studio (en kolay)
# https://visualstudio.microsoft.com/downloads/
```

**Çözüm 2:**
```powershell
# Framework'ü kontrol et
Get-ChildItem "C:\Windows\Microsoft.NET\Framework64" -Directory
```

### Hata: "System.Windows.Forms not found"

**Neden?** Windows Forms referansı tanınmıyor

**Çözüm:**
```powershell
# Referansları elle ekle
$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

& $CSC `
  /r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Windows.Forms.dll" `
  /r:"C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\System.Drawing.dll" `
  # ... diğer parametreler
```

### Hata: Compile Errors (CS1234 vs.)

**Neden?** Kod söntaksi hataları

**Çözüm:**
```powershell
# Hataları kaydet
$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

& $CSC /r:System.Windows.Forms.dll /r:System.Drawing.dll /target:winexe `
  /out:"AmongUsMenu.exe" /win32manifest:"app.manifest" /recurse:"*.cs" 2>&1 | `
  Out-File "compile_errors.txt"

# Hata dosyasını oku
Get-Content "compile_errors.txt"
```

### Hata: "Access Denied"

**Neden?** Yetki eksikliği veya dosya kilitli

**Çözüm:**
```powershell
# Admin modda çalıştır
Start-Process powershell -Verb RunAs
```

### Hata: Error.log "Yönetici yetkisi gereklidir"

**Neden?** Runtime'da Yönetici yetkisi yok

**Çözüm:**
```batch
# Masaüstündeki kısayolda Yönetici Olarak Çalıştır seçeneği aç
# amr AmongUsMenu.exe → Sağ Tıkla → Özellikler → Gelişmiş
# → "Yönetici olarak çalıştır" tıkla
```

---

## 🎯 Yeni Feature Ekleme

### Adım 1: Yeni Dosya Oluştur

```
Features\MyNewFeature.cs
```

Örnek template:

```csharp
using System;

namespace AmongUsExternal.Features
{
    public class MyNewFeature
    {
        private MemoryManager memory;
        public bool IsEnabled { get; private set; }

        public MyNewFeature(MemoryManager memory)
        {
            this.memory = memory;
        }

        public bool Toggle()
        {
            IsEnabled = !IsEnabled;
            Apply();
            return IsEnabled;
        }

        public void Apply()
        {
            if (!memory.IsConnected) return;

            try
            {
                // Your memory hacking code here
                Logger.LogInfo("MyNewFeature applied!");
            }
            catch (Exception ex)
            {
                Logger.LogError("MyNewFeature error", ex);
            }
        }
    }
}
```

### Adım 2: OverlayForm'a Ekle

`OverlayForm.cs` içinde:

```csharp
// 1. Deklarasyon ekle
private MyNewFeature myNewFeature;

// 2. Constructor'da initialize et
this.myNewFeature = new MyNewFeature(memory);

// 3. UI'a checkbox ekle
int startY = 110;
int stepY = 32;
CheckBox chkMyFeature = CreateCheckbox("My New Feature", startY + (stepY * 8), 
    delegate { myNewFeature.Toggle(); });

// 4. Projeyi derle
```

### Adım 3: Compile & Test

```powershell
cd <proje-klasoru>

$CSC = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

& $CSC /nologo /r:System.Windows.Forms.dll /r:System.Drawing.dll `
  /target:winexe /out:"AmongUsMenu.exe" /win32manifest:"app.manifest" `
  /recurse:"*.cs"

if ($LASTEXITCODE -eq 0) {
    Write-Host "✅ Başarılı!" -ForegroundColor Green
    & ".\AmongUsMenu.exe"
}
```

---

## 📦 Build Outputs

### AmongUsMenu.exe
- **Boyut**: ~30 KB (standalone executable)
- **Framework**: .NET Framework 4.5+
- **Gereksiz Dosyalar**: Yok (self-contained)

### error.log
- Her hata loglanır
- Runtime'da oluşturulur
- Sorun giderme için

---

## 🔐 Security Considerations

### Manifest Dosyası
`app.manifest` → Admin istiyoruz:
```xml
<?xml version="1.0" encoding="utf-8"?>
<assembly manifestVersion="1.0" xmlns="...">
  <trustInfo xmlns="urn:schemas-microsoft-com:asm.v3">
    <security>
      <requestedPrivileges>
        <requestedExecutionLevel level="requireAdministrator" />
      </requestedPrivileges>
    </security>
  </trustInfo>
</assembly>
```

### Win32 API Calls
Dikkat: `ReadProcessMemory` / `WriteProcessMemory` yüksek riskli!
- Antivirüsler raporlayabilir
- UAC (User Account Control) isteyebilir

---

## 📝 Compile Flags Özeti

| Flag | Anlam |
|------|-------|
| `/nologo` | Microsoft banner'ı bastırma |
| `/r:System.Windows.Forms.dll` | Windows Forms referansı |
| `/r:System.Drawing.dll` | Drawing referansı |
| `/target:winexe` | Windows GUI executable |
| `/out:"file.exe"` | Çıktı dosyası adı |
| `/win32manifest:"file.manifest"` | Admin manifest'i |
| `/recurse:"*.cs"` | Tüm .cs dosyalarını derle |

---

## 🚀 Release Build

### Production için Hazırla

```powershell
# 1. Release klasörü oluştur
New-Item -ItemType Directory -Path ".\Release" -Force

# 2. EXE'yi kopyala
Copy-Item "AmongUsMenu.exe" ".\Release\"

# 3. Readme'yi kopyala
Copy-Item "README.md" ".\Release\"
Copy-Item "QUICKSTART.md" ".\Release\"

# 4. Zip'le (isteğe bağlı)
Compress-Archive -Path ".\Release" -DestinationPath "AmongUsMenu_v1.0.zip"

# 5. Kontrol
Get-Item ".\Release\*"
```

---

## ✅ Derleme Kontrol Listesi

- [ ] Admin mode'da çalışıyor mum?
- [ ] CSC.exe bulunuyor mu?
- [ ] Tüm `.cs` dosyaları `Features/` klasöründe mi?
- [ ] `app.manifest` dosyası var mı?
- [ ] `.csproj` dosyası güncellenmiş mi?
- [ ] Hata log'unda error yok mu?
- [ ] AmongUsMenu.exe oluşturuldu mu?
- [ ] Executable çalıştırılıyor mu?

---

## 📞 Sorunlar?

```
error.log →  Hata mesajlarını oku
BUILD_GUIDE.md → Burada cevapları ara
README.md → Teknik detaylar
```

---

**Son Güncelleme**: 27 Eylül 2026 | **Sürüm**: 1.0
