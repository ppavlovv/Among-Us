# 🚀 Hızlı Başlangıç Rehberi

> **Guncel V3 notu:** Bu rehberin asagidaki admin ve ozellik adimlari eski
> surumden kalmistir. Guncel kurulum/dogrulama icin [V3-DURUM.md](V3-DURUM.md)
> ve [SandboxMod/README.md](SandboxMod/README.md) dosyalarini okuyun.
> `baslat.bat` normal kullanici olarak calisir; INSERT KV3 islemleri Freeplay
> deneme oyunculari icindir. Online yalnizca yerel No-clip secenegi test edildi.

**Among Us Ultimate In-Game Menu'yu 2 dakikada başlatın!**

---

## ⚡ 3 Adım

### Adım 1: Çalıştır (Admin Mod)
```
Masaüstüne gidip → baslat.bat → Sağ tıkla → "Yönetici Olarak Çalıştır"
```
Veya çift tıklayın (Windows'dan admin istemesi gelecek).

**Ne olacak:**
- ✅ Admin yetkisi istenir (Evet'i seçin)
- ✅ Proje otomatik derlenecek
- ✅ AmongUsMenu.exe başlayacak
- ✅ Konsol penceresi açılacak (KAPATMAYIN!)

### Adım 2: Among Us Aç
Among Us oyununu çalıştırın.

**Otomatik Bağlanma:**
- Menu arka planda Among Us'ı arayıp bağlanacak
- Konsola `✓ Offsetler başarıyla yüklendi!` yazacak
- Menü penceresi göreceksiniz (INSERT'e basınca görünür)

### Adım 3: [INSERT] Tuşuna Bas
Oyun oynarken **[INSERT]** tuşuna basın.

**Menü Açılacak:**
- ✅ Oyun Durumu (Bağlı / Bağlı Değil)
- ✅ Offset Durumu (Yeşil = Çalışıyor)
- ✅ 7 Checkbox Özelliği
- ✅ ESP Butonu

---

## 🎮 Özellikleri Kullanma

### Duvardan Geçme (NoClip)
```
☑ Duvardan Gecme (NoClip)
```
- Tıkla → Açılır
- Duvarları geçebilirsin
- Tekrar tıkla → Kapanır

### Hız Artırma (Speed)
```
☑ Super Hiz (Speed Hack)
```
- 3.5x daha hızlı koş
- Impostor olmadan bile hızlı hareket et

### Impostor Olma (Local)
```
☑ Impostor Menusu (Local)
```
⚠️ **Dikkat**: Sunucu bunu görmez! Sadece client-side.
- Yerel olarak Impostor rolü alırsın
- Öldür butonu göreceksin (ama sunucu reddedebilir)

### Geniş Görüş (Vision)
```
☑ Genis Gorus / Sis Kaldir
```
- Haritayı 10x daha geniş göreceksin
- Sısı kalacak

### Oyuncu Listeleme (ESP)
```
[Oyuncu Radarini Konsola Yazdir (ESP)]
```
- Butona tıkla
- Konsolda:
  - Tüm oyuncu isimleri
  - Rolleri (Impostor/Crewmate)
  - Durumları (Canlı/Ölü)
  - Konumları (X, Y)

---

## ⚠️ Sorun Giderme

### Problem: Menü açılmıyor?
**Çözüm:**
- Among Us çalışıyor mu? Kontrol et
- Admin mode'da çalıştırıyor musun? Kontrol et
- INSERT tuşu doğru mu? (Klavyenin sağında 0 yanında)

### Problem: Menüde "Offset Durumu: Taranıyor..." yazıyor?
**Çözüm:**
- Among Us'ı kapat ve yeniden aç
- Menüyü INSERT'le gizle/göster
- 5 saniye bekle (otomatik tarama)
- Yeşil yazılı "PlayerControl: 0x..." görülüyorsa tamam

### Problem: Hiçbir hack çalışmıyor?
**Çözüm:**
- Offset **yeşil** mi? (Menüde kontrol et)
- Kırmızı ise Among Us versiyonu farklı olabilir
- v18e build 7250 gerekli!
- error.log dosyasına bak

### Problem: Derlenemiyor?
**Çözüm:**
- Admin mode mu kullanıyor musun?
- Windows .NET Framework 4.5+ yüklü mü?
- Antivirusü devre dışı bırak (false positive)

---

## 🎯 İpuçları

### Sağlıklı Kullanım
✅ **Yapın:**
- Single-player offline oyunda test edin
- Hile yapmadan normal oyna
- Local lobilerde (bots'la) test et

❌ **Yapmayın:**
- Çok oyunculu match'lerde kullanmayın
- Diğer oyunculara karşı applylemeyın
- Ranked mode'da oynamayın
- Rekoru bozacak hareketler yapmayın

### Performance
- Menü ekrana basmayın (performans düşer)
- Birden fazla hack açma (desync oluşur)
- Speed + NoClip birlikte = garip davranış

### Log Dosyası
```
error.log
```
- Her hata burada yazılı
- Derleyemezsen veya bağlanamazsan açıp oku

---

## 📱 Kontrol Özeti

| Tuş | İşlev |
|-----|-------|
| `[INSERT]` | Menü aç/kapat |
| `☑ Checkbox` | Özellik aç/kapat |
| `[ESP Butonu]` | Oyuncu taraması yap |

---

## 🔗 Başka Kaynaklar

- **Tam Dokümantasyon**: `README.md`
- **Derleyerek Test**: `BUILD_GUIDE.md`
- **Teknik Detaylar**: Kod içindeki yorumlar
- **Hata Günlüğü**: `error.log`

---

## ✅ Tamam, Hazırım!

```
1️⃣ baslat.bat → Admin → Çalıştır
2️⃣ Among Us Aç
3️⃣ [INSERT] → Menüyü Gör
4️⃣ Hack Seç → Oyna!
```

**Keyifli oyunlar!** 🎮

---

**Son Güncelleme**: 27 Eylül 2026
