# 🎯 OFFSET BULMA REHBERİ

## ⚠️ Neden Hiçbir Şey Çalışmıyor?

Kanka, sorunun sebebini açıklamak gerek. Şu an programımız çalışıyor ama **oyuna yazamıyor** çünkü:

### 🔴 Ana Sorun: Offset'ler Yanlış

**Offset nedir?**
Among Us'un `GameAssembly.dll` dosyası içindeki fonksiyon ve değişkenlerin **bellek adresleri**. Bunları biliyoruz sanki bir haritadaki koordinatlar gibi.

**Sorun:**
- Program `0x03018B40` adresine yazmaya çalışıyor
- Ama senin oyunundaki gerçek adres bambaşka olabilir
- Her Among Us güncellemesinde bu adresler değişir
- **Tahmin ederek offset koymak = %99 çalışmaz**

### 🟢 Çözüm: 3 Yöntem

---

## 📌 Yöntem 1: offsets.json Manuel Düzenleme (Kolay)

**Eğer bilinen bir yerden offset'lerin varsa:**

1. `offsets.json` dosyasını Notepad ile aç
2. Değerleri güncel offsetlerinle değiştir
3. `baslat.bat`'ı yeniden çalıştır

```json
{
  "PlayerControl_Class": "0x03018B40",  ← Bunu değiştir
  "GameOptionsData_Class": "0x03018BA0", ← Bunu değiştir
  ...
}
```

**Offset nereden bulunur?**
- GitHub: `github.com/search?q=AmongUs+offsets+v18e`
- Discord community sunucuları
- UnknownCheats forumu

---

## 📌 Yöntem 2: Cheat Engine ile (Orta Zorluk)

### Adım Adım:

**1. Cheat Engine'i indir:**
- https://www.cheatengine.org

**2. Among Us'ı aç ve oyna**

**3. Cheat Engine'i aç ve Among Us process'ini seç:**
- File → Open Process → Among Us

**4. Local Player pointer'ını bul:**

**a) Speed değerini bul:**
- Karakterin normal hızda hareket ederken CE'de `Float` seç
- Value: `1.0` yaz, First Scan
- Ekstra hız verirsen (`Vision hack` gibi) `Value Changed` scan yap
- Value'yu değiştir ve yeni scan yap → Adres bulundu

**b) Adresi Pointer Scan yap:**
- Sağ tık → Pointer Scan for this address
- Static olan pointer'ları bul
- Bu senin `PlayerControl_Class` offset'in

**5. Değerleri offsets.json'a gir**

---

## 📌 Yöntem 3: IL2CppDumper (Zor - Kesin)

### En Doğru Yöntem:

**1. IL2CppDumper indir:**
- https://github.com/Perfare/Il2CppDumper

**2. Among Us klasörünü aç:**
```
C:\Program Files\Epic Games\AmongUs\
```

**3. Şu dosyaları IL2CppDumper'a sürükle:**
- `GameAssembly.dll`
- `Among Us_Data\il2cpp_data\Metadata\global-metadata.dat`

**4. Çıktı dosyaları:**
- `dump.cs` - Tüm class'lar, method'lar, offset'ler
- `script.py` - IDA Pro için script
- `il2cpp.h` - C++ header

**5. dump.cs içinde ara:**
```csharp
// Class: PlayerControl
// Il2CppClassOffset: 0x2E3B040  ← Bu senin offset'in!
public class PlayerControl : NetworkedPlayerInfo {
    ...
    public static PlayerControl LocalPlayer; // Offset: 0x0
    public static List<PlayerControl> AllPlayerControls; // Offset: 0x8
    ...
}
```

**6. Bu değerleri offsets.json'a yaz**

---

## 🔍 Program Bize Neyi Söylüyor?

Menüyü açtığında **"Teshis"** sekmesine git ve **"Offset Dogrulamayi Calistir"** butonuna bas:

### ✅ Yeşil (Çalışıyor):
```
PlayerControl: OK (0x7FF8AB2C3F00)
GameOptions: OK (0x7FF8AB2C3F60)
LocalPlayer: OK (0x1B2E4D5F800)
Genel Durum: TAMAMEN CALISIYOR
```

### 🔴 Kırmızı (Hatalı):
```
PlayerControl: HATA (0x0)
GameOptions: HATA (0x0)
LocalPlayer: HATA (0x0)
Genel Durum: OFFSETLER HATALI
```

**Kırmızı görüyorsan** = offset'ler yanlış, `offsets.json`'a doğru olanları gir.

---

## 🎮 Alternatif: Hazır Community Menu

Eğer offset bulma işi çok zorsa, **community'nin baktığı hazır menülerden** birini deneyebilirsin:

**Popüler Alternatifler:**
- **AmongUsMenu (GitHub)**: https://github.com/dylanmeadows2003/AmongUsMenu
- **Emeraldo's Menu**: Discord community'de
- **AutoMuteUs**: Sadece muting için ama güvenli

⚠️ **Dikkat**: Bu tür tool'ları indirmeden önce **antivirus tara**!

---

## 📊 Menü Durumu Anlama

Program açılınca üstteki durum panelinde şunları göreceksin:

| Durum | Anlamı | Yapman Gereken |
|-------|--------|----------------|
| 🟡 "Bekleniyor" | Among Us kapalı | Oyunu aç |
| 🟢 "Baglandi PID: 1234" | Program oyuna bağlı | Devam et |
| 🔴 "PlayerControl=0x0" | Offset yanlış | offsets.json düzenle |
| 🟢 "Tum Offsetler CALISIYOR" | Herşey OK! | Hack'leri kullan |

---

## 🛠️ Debug Bilgisi Toplama

Sorun devam ediyorsa `error.log` dosyasını incele:

```powershell
notepad .\error.log
```

Şunları arayacaksın:
- `PlayerControl adresi 0x0` → Offset yanlış
- `Attach hatasi` → Yönetici modda çalıştır
- `OpenProcess basarisiz` → Antivirus/Windows Defender engelliyor
- `Module listesi okunamadi` → Yetki eksik

---

## 🎯 Hızlı Test Adımları

Her offset değiştirdiğinde:

1. `offsets.json` kaydet
2. Programı kapat
3. `baslat.bat` çalıştır  
4. Among Us aç
5. INSERT bas
6. **Teshis** sekmesine git
7. **"Offset Dogrulamayi Calistir"** tıkla
8. Sonucu oku

---

## 💡 En Sık Karşılaşılan Sorunlar

### "Program bagli ama hiçbir şey olmuyor"
✅ **Neden**: Offset'ler yanlış
✅ **Çözüm**: offsets.json'a doğru değerleri gir

### "Speed hack açılıyor ama hızlanmıyorum"
✅ **Neden**: `GameOptions_PlayerSpeedMod` offset'i yanlış  
✅ **Çözüm**: 0x10 yerine güncel değeri bul (0x08, 0x0C, 0x14 dene)

### "NoClip çalışıyor ama duvarları geçemiyorum"
✅ **Neden**: `Collider_Enabled` offset'i yanlış  
✅ **Çözüm**: 0x18, 0x1A, 0x20 değerlerini dene

### "Menu açılmıyor (INSERT çalışmıyor)"
✅ **Neden**: Program başlatılamadı  
✅ **Çözüm**: `baslat.bat`'ı Yönetici olarak çalıştır

### "Bağlanamıyorum (Access Denied)"
✅ **Neden**: Antivirus/Defender engelliyor  
✅ **Çözüm**: 
- Windows Defender'da AmongUsMenu.exe'yi izin ver
- Antivirus taramasında hariç tut
- Yönetici modda çalıştır

---

## 🎓 İleri Seviye: Kendi Offset Scanner'ını Yaz

Programın içinde şu class var:
```csharp
public class OffsetScanner {
    public long FindPattern(string pattern);
    public long FindStringReference(string searchString);
}
```

**Kendi pattern arayışını yazabilirsin:**

```csharp
// PlayerControl class pattern (örnek)
long addr = scanner.FindPattern("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B F9");

// String referansı ile
long strAddr = scanner.FindStringReference("PlayerControl");
```

---

## 📞 Yardım Almak İçin

Hala çalışmıyorsa:

1. **error.log** dosyasını paylaş
2. **Teshis** sekmesinden ekran görüntüsü al  
3. **Among Us versiyonunu** öğren (Ana menü sağ alt köşe)
4. `offsets.json` içeriğini paylaş

Bu bilgilerle tam olarak nerede takıldığın anlaşılır.

---

## ⚡ Özet

```
1. offsets.json → Doğru offset'leri gir
2. baslat.bat → Yönetici olarak çalıştır
3. Among Us aç
4. INSERT → Menü
5. Teshis → Dogrulama Calistir
6. Yeşil = Hazır, Kırmızı = offsets.json düzenle
```

**Unutma: Offset olmadan hiçbir hack çalışmaz!** 🎯

---

**Son Güncelleme**: 27 Eylül 2026 | **Sürüm**: v1.1
