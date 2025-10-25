# Kurulum Kontrol Listesi

**Proje:** Tactical Combat MVP  
**Unity:** 6000.2.8f1 ✅  
**Tarih:** Ekim 2025

---

## ✅ KURULU OLANLAR

### Unity & Temel Paketler
- [x] Unity 6000.2.8f1 (Unity 6 LTS)
- [x] Input System 1.14.2 (Güncel! ✅)
- [x] Visual Scripting 1.9.7
- [x] Timeline 1.8.9
- [x] Test Framework 1.6.0
- [x] URP 17.2.0 (⚠️ Güncellenmeli 18.x'e)

### Proje Dosyaları
- [x] Tüm Scripts (31 dosya)
- [x] Unity6Optimizations.cs
- [x] StructureOptimizer.cs
- [x] InputSystem_Actions.inputactions
- [x] Klasör yapısı (Scripts, Prefabs, Materials, vb.)
- [x] Dokümantasyon (README, SETUP_GUIDE, vb.)

---

## ❌ KURULMASI GEREKENLER

### 1. Mirror Networking ❌ EN ÖNEMLİ!

**Neden Gerekli:** Multiplayer için temel paket

**Kurulum Adımları:**
```
1. Unity Editor'ı aç
2. Window > Package Manager
3. "+" butonuna tıkla
4. "Add package from git URL" seç
5. Şunu yapıştır:
   https://github.com/vis2k/Mirror.git?path=/Assets/Mirror
6. "Add" tıkla
7. Kurulumu bekle (~1-2 dakika)
```

**Doğrulama:**
- Package Manager'da "Mirror" görünmeli
- Assets altında Mirror klasörü olmalı
- Console'da hata olmamalı

---

### 2. URP Güncelleme ⚠️ GÜNCELLENMELİ

**Şu Anki Versiyon:** 17.2.0  
**Olması Gereken:** 18.0.4+

**Güncelleme Adımları:**
```
1. Window > Package Manager
2. "In Project" seçili olduğundan emin ol
3. "Universal RP" paketini bul
4. Paketin üzerine tıkla
5. Sağ altta versiyon dropdown'u var
6. "18.0.4" veya en güncel 18.x'i seç
7. "Update to 18.0.4" tıkla
8. Güncellemeyi bekle
9. Unity yeniden compile edecek
```

**Neden Güncellenmeli:**
- GPU Resident Drawer (Unity 6 özelliği)
- Render Graph optimizasyonları
- Gelişmiş lighting
- Performans iyileştirmeleri

---

### 3. TextMeshPro Kontrolü (Opsiyonel)

**Durum:** Muhtemelen kurulu (Unity 6'da built-in)

**Kontrol:**
```
1. Window > Package Manager
2. Üstte "Packages:" dropdown'dan "Unity Registry" seç
3. "TextMeshPro" ara
4. Eğer "Installed" yazıyorsa ✅ Tamam
5. Değilse "Install" tıkla
```

---

## 🔧 Kurulum Sonrası Yapılacaklar

### 1. Input Actions Generate
```
1. Assets klasöründe "InputSystem_Actions.inputactions" dosyasını bul
2. Dosyaya tıkla
3. Inspector'da "Generate C# Class" checkbox'ını işaretle
4. "Apply" tıkla
```

### 2. URP Settings Kontrol
```
1. Edit > Project Settings > Graphics
2. Scriptable Render Pipeline Settings'te URP Asset seçili olmalı
3. URP Asset'e çift tıkla
4. Inspector'da kontrol et:
   - SRP Batcher: ✓ Enabled
   - GPU Instancing: ✓ Enabled
   - Render Scale: 1.0
```

### 3. Quality Settings
```
1. Edit > Project Settings > Quality
2. Ayarlar:
   - VSync Count: Don't Sync
   - Shadow Quality: All
   - Shadow Resolution: Very High Resolution
   - Shadows: Soft Shadows
```

### 4. Physics Settings
```
1. Edit > Project Settings > Physics
2. Layers oluştur (eğer yoksa):
   - Layer 6: Player
   - Layer 7: Structure
   - Layer 8: Trap
   - Layer 9: Ground
```

---

## 📝 Kurulum Sırası (Önerilen)

### Adım 1: Mirror Networking (5 dakika)
En önemli! Önce bunu kur.

### Adım 2: URP Güncelleme (2 dakika)
Unity 6 özelliklerinden faydalanmak için.

### Adım 3: TextMeshPro Kontrol (1 dakika)
Muhtemelen zaten kurulu.

### Adım 4: Input Actions Generate (1 dakika)
Kod oluşturması için.

### Adım 5: Settings Kontrolü (5 dakika)
URP, Quality, Physics ayarları.

**Toplam Süre: ~15 dakika**

---

## ✅ Doğrulama Checklist

Kurulum tamamlandıktan sonra kontrol et:

- [ ] Package Manager'da Mirror görünüyor
- [ ] URP versiyonu 18.0.4 veya üzeri
- [ ] Console'da kırmızı hata yok (sarı uyarı normal olabilir)
- [ ] InputSystem_Actions.cs dosyası oluştu (Assets klasöründe)
- [ ] Project Settings > Graphics > URP Asset seçili
- [ ] Layers (6-9) oluşturuldu

---

## 🐛 Olası Sorunlar ve Çözümler

### Mirror Kurulumu Hata Veriyor
**Sorun:** Git kurulu değil  
**Çözüm:** 
1. Git'i indir: https://git-scm.com/
2. Kur ve Unity'yi yeniden başlat
3. Tekrar dene

**Alternatif:**
Mirror'ı Asset Store'dan indir (ücretsiz)

### URP Güncellemesi Hata Veriyor
**Sorun:** Versiyon uyumsuzluğu  
**Çözüm:**
1. Unity'yi 6000.2.8f1 veya daha yeni versiyona güncelle
2. Package Manager'ı yenile (Ctrl+R)
3. Tekrar dene

### Input Actions C# Oluşmuyor
**Sorun:** Generate checkbox işaretli ama dosya yok  
**Çözüm:**
1. InputActions dosyasını sil
2. Right Click > Create > Input Actions
3. Yeniden oluştur ve Generate'e bas

---

## 📞 Yardım

Sorun yaşıyorsan:
1. Console'daki tam hata mesajını oku
2. SETUP_GUIDE.md dosyasına bak
3. PACKAGES_GUIDE.md dosyasını kontrol et

---

**Hazır olduğunda:** `SETUP_GUIDE.md` dosyasını takip ederek sahne kurulumuna geç!



