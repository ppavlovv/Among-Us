# KV3 Among Us Tools

Windows icin acik kaynak Among Us yardimci araclari. Proje iki parcadan olusur:

- Harici INSERT menusu: ekran ESP'si, radar, oyuncu listesi ve desteklenen yerel oyun ayarlari.
- KV3 oyun ici modu: BepInEx IL2CPP uzerinden Freeplay deneme araclari ve online modda yalnizca yerel No-clip anahtari.

Proje sahibi ve gelistirici: **ytpavlov**

> Bu proje Among Us, Innersloth veya Epic Games ile baglantili ya da onlar
> tarafindan onaylanmis degildir. Among Us ve ilgili markalar kendi sahiplerine aittir.

## Durum

Bu kaynak surumu Among Us `v18e (build 7250)`, IL2CPP metadata 31 ve asagidaki
`GameAssembly.dll` SHA-256 degeri icin hazirlanmistir:

```text
3A86BF7D6E69594809D9BA9965F9B5BC4EB88934FDE1C05DBEC191899D1763DF
```

Farkli oyun surumlerinde bellek yazma islemleri otomatik olarak engellenir.
Offsetler ve oyun yapilari yeni surumlerde degisebilir.

| Ozellik | Freeplay | Online |
| --- | --- | --- |
| Ekran ESP, cizgi, kutu, isim/rol/mesafe | Var | Salt okunur olarak var |
| Radar ve oyuncu listesi | Var | Salt okunur olarak var |
| Yerel No-clip | Dogrulandi | Yerel collider ac/kapat dogrulandi; sunucu hareketi garanti edilmez |
| Hiz ve gorus ayarlari | Dogrulandi | Sunucu/host kurallarina bagli, garanti edilmez |
| Deneme oyuncusunu oldurme/diriltme | Dogrulandi | Kapali |
| Deneme oyuncularini cekme/karistirma/dondurme | Dogrulandi | Kapali |
| Olum ve surgun koruma testi | Freeplay testi var | Kapali |

Online odalarda diger oyunculari oldurme, cekme, diriltme veya rol degistirme
desteklenmez. Kick/ban atlatma ya da anti-cheat atlatma ozelligi yoktur.

## Gereksinimler

- Windows 10/11 x64
- Desteklenen Among Us surumu
- Harici menu icin Windows PowerShell ve 64-bit .NET Framework derleyicisi
- KV3 modu derlemek icin .NET SDK 8 veya uyumlu yeni bir SDK
- KV3 oyun ici modu icin BepInEx 6 IL2CPP

Oyun klasoru varsayilan olarak su konumda aranir:

```text
C:\Program Files\Epic Games\AmongUs
```

Farkli bir konum icin `AMONG_US_PATH` ortam degiskenini kullanabilir veya mod
derleme betigine `-GamePath` verebilirsiniz.

## Harici Menunun Kurulumu

1. Depoyu ZIP olarak indirin veya Git ile klonlayin.
2. Among Us'i Epic Games Launcher uzerinden acin.
3. Proje klasorundeki `baslat.bat` dosyasini normal kullanici olarak calistirin.
4. Oyunda `INSERT` tusuyla menuyu acip kapatin.

Elle derlemek icin:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\RunExternal.ps1 -BuildOnly
```

Derlenen `AmongUsMenu_v3.exe` proje klasorunde olusur. Derleme ciktilari Git
tarafindan izlenmez; resmi surum yayinlarken binary'leri ayri GitHub Release
varligi olarak ekleyin.

## KV3 Modunun Kurulumu

1. BepInEx 6 IL2CPP'yi kendi resmi kaynagindan Among Us klasorune kurun.
2. Oyunu bir kez acip kapatarak `BepInEx/interop` dosyalarinin olusmasini bekleyin.
3. Oyunun kapali oldugundan emin olun.
4. Asagidaki komutu proje klasorunde calistirin:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\SandboxMod\Build.ps1 -Install
```

Oyun farkli klasordeyse:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\SandboxMod\Build.ps1 `
  -Install -GamePath "D:\Games\AmongUs"
```

Mod yuklenince F6 oyun ici paneli acar. INSERT menusundeki `KV3` sekmesi ayni
yerel moda, oyun PID'sine bagli bir named pipe ile baglanir. TCP/HTTP portu acilmaz.

Mod yukleyicisini gecici kapatmak icin oyunu kapatip su komutu kullanin:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Disable-ModLoader.ps1 `
  -GamePath "C:\Program Files\Epic Games\AmongUs"
```

## Tuslar

| Tus | Islev |
| --- | --- |
| `INSERT` | Harici menuyu ac/kapat |
| `F3` | Radar penceresini ac/kapat |
| `F4` | Ekran ESP'sini ac/kapat |
| `F6` | KV3 oyun ici panelini ac/kapat |

## Derleme ve Test

Harici menu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\RunExternal.ps1 -BuildOnly
```

KV3 modu:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\SandboxMod\Build.ps1
```

Politika ve protokol testleri:

```powershell
dotnet run --project .\SandboxMod\Tests\PolicyTests.csproj -c Release
```

Mevcut test paketi 25 kontrol icerir. Bu testler Unity calisma zamanini tek
basina kanitlamaz; gercek oyun dogrulamalari [V3-DURUM.md](V3-DURUM.md) icinde tutulur.

## Guvenlik ve Gizlilik

- Kaynaktan derlemeniz onerilir.
- Depo telemetri, reklam, uzaktan komut veya veri toplama kodu icermez.
- Oyun dosyalari, BepInEx binary'leri, EXE/DLL ciktilari, loglar ve yedekler depoya eklenmez.
- Indirdiginiz binary'leri calistirmadan once hash ve antivirus taramasi yapin.
- Ayrintilar icin [PRIVACY.md](PRIVACY.md) ve [SECURITY.md](SECURITY.md) dosyalarina bakin.

## Sorumluluk Reddi

Bu yazilim egitim, arastirma ve yerel test amaciyla, herhangi bir garanti
verilmeden "oldugu gibi" sunulur. Yazilimin kullanimindan dogabilecek hesap
cezasi, oyun erisimi kaybi, veri kaybi, sistem sorunu veya baska bir zarardan
proje sahibi ve katkida bulunanlar sorumlu degildir.

Kullanan kisi oyunun hizmet sartlarini, topluluk kurallarini ve bulundugu
ulkenin yasalarini kontrol etmekten sorumludur. Herkese acik oyunlarda diger
oyuncularin deneyimini bozan kullanim desteklenmez.

## Lisans

Kaynak kod [MIT Lisansi](LICENSE) ile yayinlanir. Ucuncu taraf projeler ve oyun
dosyalari kendi lisanslarina tabidir ve bu depoya dahil degildir.

## Iletisim

- YouTube: [@ytpavlov](https://www.youtube.com/@ytpavlov)
- Yan YouTube hesabi: [@pavlovsmurf](https://www.youtube.com/@pavlovsmurf)
- Discord: [discord.gg/ytpavlov](https://discord.gg/ytpavlov)
- Instagram: [@ytpavlov](https://www.instagram.com/ytpavlov)
- TikTok: [@ytpavlov](https://www.tiktok.com/@ytpavlov)

Hata bildirirken oda kodu, hesap bilgisi, gercek ad, yerel dosya yolu veya
duzenlenmemis log paylasmayin.
