# V3 / Metadata 31

Baslatma: baslat.bat. Yonetici izni zorlanmaz. Oyun acik olabilir; derleme oncesi menu kapali olmalidir.
Yalnizca derleme: powershell -NoProfile -File .\RunExternal.ps1 -BuildOnly

GameLayout.cs, kurulu GameAssembly.dll dosyasinin SHA-256 degerini ve canli sinif isimlerini denetler.
Eski offsets.json artik uygulama baslangicinda yuklenmez; eski tahmini tarama kapatildi.
Farkli oyun surumunde bellek yazma engellenir. Desteklenen profil 64-bit / metadata 31'dir.

Oyuncu listesi, yerel oyuncu, siniflar ve NormalGameOptionsV11 canli okuma ile dogrulandi.
Hiz ve gorus kodu yeni duzene uyarlandi; kapatilirken onceki ayarlar geri yuklenir.
Kill timer yeni alanina uyarlandi ve acikken periyodik uygulanir.
Freeplay'de hiz ve gorus ac/kapat, yazilan degeri geri okuma ve onceki ayarlari geri yukleme testleri gecti.
21 entegrasyon kontrolu gecti. Gorsel hareket/oynanis ve kill timer davranisi henuz ayrica dogrulanmadi.

Harici v3 menu katmaninda NoClip, rol degistirme, God Mode, vent ve teleport
desteklenmiyor; eski bellek adreslerine yazilmasi merkezi olarak engellenir.
Ayri KV3 Freeplay modu asagida NoClip ve koruma dahil bazi islemleri saglar.

## Ekran ESP / 2026-09-28

Oyun penceresinin ustune tiklamayi engellemeyen seffaf ESP eklendi.
F4: ekran ESP ac/kapat. INSERT: menu. F3: ayri radar ve oyuncu tablosu.
AmongUsMenu_v3.exe --esp: menu gizli, ESP acik baslatir.
Cizgi, kutu, isim/rol/mesafe ve ekran disi yon oklari desteklenir.
Rol atanmamissa 'Rol bilinmiyor' gosterilir. Rol degistirme veya impostor olmadan oldurme uygulanmadi.

CameraReader.cs, UnityPlayer.dll SHA-256 profiliyle kontrol edilen kamera yakinligini
ve Rigidbody2D konumlarini yalnizca okur. ESP icin onbellek konumu kullanilmaz.
Oyun odagi kaybolunca, oyun kucultulunce veya kamera okunamayinca cizimler temizlenir.
Standart tam istemci alani ve dik 2D kamera varsayilir; kamera sarsintisi hesaba katilmaz.
Ozel viewport/letterbox veya exclusive fullscreen gorunurlugu ayrica dogrulanmadi.

11 ESP ve 14 radar kontrolu gecti. ESP 850x600, 1280x720, 1920x1080'de render edildi.
Canli salt-okunur testte 12 oyuncunun 12'sinin fizik konumu ve kamera okundu.
Bu kontroller oyun goruntusuyle piksel duzeyinde hizalama dogrulamasi degildir.
Son cevrimici oturumda testler oyun bellegine yazmadi.
Yerel gelistirme test betikleri depoya dahil edilmez.

## KV3 Freeplay mod

SandboxMod/ altinda bagimsiz BepInEx IL2CPP modu var. Kurulu oyunda BepInEx yukleyici
ve onceki mod surumu yuklendi; Epic kisayolundan acilan oyunda F6 paneli calisti.
Freeplay'de 6 deneme oyuncusu yanina cekildi. Crewmate iken Dummy 1 olduruldu;
oyunun IsDead durumunda olum dogrulandi, yerel rol Crewmate ve hayatta kaldi.
No-clip ile duvardan gecme kullanici tarafindan dogrulandi.
Online moda gecince yalnizca yerel No-clip acilabilir. Herkese acik odada
yerel collider ac/kapat dogrulandi; sunucu tarafindaki hareket etkisi henuz
dogrulanmadi. Detaylar SandboxMod/README.md.
F6 Freeplay surumu kurulu ve test edildi: donen halka, konum karistirma,
yerel olum/koruma testi, hayaletken oldurme, diriltme, 6 hedeflik toplu oldurme
ve olu deneme oyuncusunu diriltme. INSERT menusu icin KV3 sekmesi ve yerel
named pipe koprusu eklendi; 25 erisim/protokol testi gecti, 0 derleme hatasi.
INSERT Freeplay testi gecti: No-clip ac/kapat, 4/4 deneme oyuncusunu oldurme,
4/4 diriltme ve 4/4 yanina cekme. INSERT kisa tusu da oyun icinde acildi.
Koruma testi gercek oylama veya cevrimici oda davranisini kanitlamaz. Mod
online odalarda diger oyunculara yonelik islem yapmaz. Detaylar SandboxMod/README.md.

Guvenlik ve gizlilik: SECURITY.md ve PRIVACY.md.
