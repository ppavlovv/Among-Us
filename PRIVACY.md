# Gizlilik

KV3 Among Us Tools hesap, parola, mesaj veya tarayici verisi toplamaz. Projede
telemetri, reklam SDK'si, uzaktan komut sistemi ya da gelistiriciye veri
gonderen bir HTTP/TCP istemcisi bulunmaz.

Harici menu calisirken yerel `Among Us` surecinin bellegini okur. Desteklenen
ayarlar icin sinirli yerel bellek yazmalari yapabilir. KV3 oyun ici modu ile
INSERT menusu arasindaki iletisim, yalnizca ayni Windows kullanicisina acik ve
oyun PID'sine bagli yerel named pipe uzerinden gerceklesir.

`error.log` yalnizca yerel tanilama icindir ve `.gitignore` ile depodan dislanir.
Bir hata raporuna log eklemeden once oyuncu adlari, dosya yollari, PID'ler ve
diger kisisel bilgileri elle kontrol edin.

Bu depo oyun dosyalarini, BepInEx dosyalarini, hesap belirteclerini, oda
kodlarini veya kullaniciya ait derleme ciktilarini icermez.
