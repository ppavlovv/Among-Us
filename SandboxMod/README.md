# KV3 Freeplay Tools

Bu, calisan harici ESP'den bagimsiz bir BepInEx IL2CPP modudur. F6 paneli acar;
harici v3 uygulamasinin INSERT menusu ayni Freeplay islemlerini KV3 sekmesinde sunar.
Oyunculari etkileyen islemler yalnizca Freeplay ve isDummy oyunculari icindir.
Online odada yalnizca yerel No-clip acilabilir; sunucunun hareketi kabul ettigi
henuz dogrulanmadi. Koruma dahil tum ayarlar yeni oturumda kapanir.

INSERT ile oyun arasinda yalnizca ayni Windows kullanicisina acik, oyun PID'sine
bagli yerel named pipe kullanilir. Dinlenen TCP/HTTP portu yoktur. Komutlar oyun
is parcaciginda yeniden oturum ve hedef denetiminden gecer. Harici arayuzde
toplu oldurme ayrica onay ister; F6'da iki tiklama onayi vardir.

## Islemler

- No-clip: yerel Collider2D.enabled degerini Unity API'siyle kapatir; kapatilinca geri yukler.
  Online modda yalnizca bu yerel ayar aciktir; sunucu tarafinda duvar gecisi garanti edilmez.
- Koruma: yerel oyuncu icin MurderPlayer, Die ve Exiled cagrilarini Freeplay'de durdurur.
- Diriltme: PlayerControl.Revive fonksiyonunu cagirir.
- Oldurme: yerel MurderPlayer fonksiyonunu Succeeded sonucu ile cagirir. Rol veya IsDead
  bayragi degistirilmez; hayalet/crewmate olarak kullanim kodda engellenmez.
- Konum: secili veya tum deneme oyunculari icin NetTransform.SnapTo cagirir.
- Toplu oldurme: ikinci tiklamayla onay ister; hedefleri yeniden kontrol ederek sirayla uygular.
- Donen halka: deneme oyuncularini yerel oyuncunun etrafinda dondurur, kapatinca
  acildigi konumlara geri koyar. Freeplay disinda calismaz.
- Konum karistirma: deneme oyuncularinin konumlarini kendi aralarinda degistirir.
- Toplu diriltme: olu deneme oyunculari icin Revive cagirir; siradaki toplu oldurme
  islemlerini iptal eder. Ceset gorunumu oyunda kalabilir.
- Koruma/olum testi: koruma acikken yerel Die+Exiled cagrilarinin durdurulmasini dener;
  koruma kapaliyken yerel oyuncuyu oldurur, ardindan diriltme denenebilir.

Bu mod kick/ban atlatmaz, ag paketi veya RPC gondermez ve cevrimici etkileri garanti etmez.
Koruma, oy bitimini veya toplantinin tum yan etkilerini geri alan bir diriltme sistemi degildir.

## Derleme

`powershell -NoProfile -File .\SandboxMod\Build.ps1`

Kurulum icin oyun kapaliyken ayni komuta `-Install` eklenir.
Referanslar kurulu oyunun BepInEx/core ve BepInEx/interop klasorlerinden alinir.
NuGet yalnizca Microsoft .NET referans paketleri icin kullanilir; mod icinde ag istemcisi yoktur.

## Dogrulama

25 durum/erisim/protokol testi gecti: `Tests/PolicyTests.csproj`.
Gercek IL2CPP referanslariyla Release derlemesi: 0 hata, 0 uyari.
INSERT koprusunun ana menude salt-okunur istegi, islemleri kapali ve 0 hedef
olarak yanitladi. INSERT KV3 sekmesinde Freeplay hedef listesi ve No-clip ac/kapat
dogrulandi. Toplu oldurme 4 deneme oyuncusunun 4'unu oldurdu; toplu diriltme
4'unu hayatta durumuna getirdi ve yanina cekme 4/4 konumu dogruladi.
Son panel Freeplay'de acildi ve butun dugmeler ekrana sigdi. Alti deneme oyuncusu
SnapTo ile yanina cekildi. Donen halka hareket etti, kapatinca konumlari geri geldi;
konum karistirma 6 hedefe uygulandi.
Crewmate rolu degismeden oldurme ve 6 hedeflik toplu oldurme sirasi oyun durumunda
dogrulandi. Koruma acikken yerel Die+Exiled testinde IsDead false kaldi; bu, gercek
oylama akisini test etmez. Koruma kapatildiginda yerel oyuncu hayalet oldu ve
GuardianAngel rolundeyken Dummy 1'i oldurebildi. Yerel Revive, IsDead durumunu false
yapti; hayalet rolu/arayuzu tamamen temizlendigi dogrulanmadi. Toplu diriltme,
Dummy 1'i tekrar hayatta durumuna getirdi; ceset gorunumu kaldi.
No-clip ile duvardan gecme kullanici tarafindan Freeplay'de dogrulandi.
Herkese acik online odada INSERT No-clip ac/kapat yerel collider'i kapatip
geri acti; gercek duvar gecisi veya sunucunun hareketi kabul ettigi dogrulanmadi.
Odadan ayrilinca No-clip ve hedef listesi sifirlandi. Online oldurme, cekme,
kick/ban ve gercek oylama akisi bu surumde desteklenmez/dogrulanmadi.

## Geri Donus

Oyun kapaliyken proje kokundeki `Disable-ModLoader.ps1` mod yukleyicisini yeniden adlandirir.
Oyun DLL'lerine veya ESP'ye dokunmaz. Yukleyici dosyalari silinmez.
Yerel yedekler ve derleme ciktilari depoya dahil edilmez.
Paket kaynagi: https://builds.bepinex.dev/projects/bepinex_be/788
Surum: 6.0.0-be.788+5b766a3b7f6c164d4798924a93f3acf4db769d06.
Indirilen ZIP SHA256: F4CC496BD098A0DF4164B81E3737297707F13A47C2478DBA2F60EEFAB784817A.
Defender taramasi tespit bildirmedi; guvenlik garantisi veya bagimsiz imza dogrulamasi degildir.
