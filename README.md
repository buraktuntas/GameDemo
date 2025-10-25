# 🎮 Tactical Combat MVP

Unity 6 ile geliştirilmiş profesyonel FPS tabanlı taktiksel savaş oyunu.

## ✨ Özellikler

### 🎯 **FPS Sistemi**
- **Profesyonel FPS Controller** - Smooth hareket ve kamera kontrolü
- **Head Bob & FOV Kick** - Gerçekçi FPS deneyimi
- **Stamina Sistemi** - Koşma için stamina tüketimi
- **Landing Detection** - İniş sesleri ve efektleri
- **Footstep Sounds** - Adım sesleri

### 🏗️ **Build Sistemi (Valheim Tarzı)**
- **Ghost Preview** - Yeşil/kırmızı yerleştirme önizlemesi
- **Grid Snapping** - Hassas grid yerleştirme
- **Rotation System** - R tuşu ile rotasyon
- **Structural Integrity** - Valheim tarzı stabilite sistemi
- **Overlap Detection** - Çakışma kontrolü

### 🌐 **Network Sistemi**
- **Mirror Networking** - Multiplayer desteği
- **Team Management** - Takım sistemi
- **Role System** - Rol tabanlı yetenekler
- **Spawn System** - Otomatik spawn noktaları

### 🎨 **UI/UX**
- **Smart Crosshair** - Durum bazlı crosshair
- **Input Manager** - Merkezi input yönetimi
- **Pause Menu** - ESC ile pause
- **Debug Tools** - F1/F2 debug overlay

## 🎮 Kontroller

### **Hareket**
- `WASD` - Hareket
- `Mouse` - Bakış
- `Space` - Zıplama
- `Shift` - Koşma

### **Build Mode**
- `B` - Build mode aç/kapat
- `R` - Rotasyon (hold to rotate)
- `LMB` - Yerleştir
- `ESC` - Build mode'dan çık

### **Combat**
- `LMB` - Ateş et
- `ESC` - Pause menu

### **Debug**
- `F1` - Detaylı debug bilgisi
- `F2` - Debug overlay aç/kapat

## 🛠️ Kurulum

### **Gereksinimler**
- Unity 6.0+
- Mirror Networking
- Universal Render Pipeline (URP)

### **Kurulum Adımları**
1. Projeyi Unity'de aç
2. `Tools > Tactical Combat > Recreate Player Prefab (FINAL)` çalıştır
3. `Tools > Tactical Combat > Setup Build System` çalıştır
4. Play mode'a gir ve test et

## 📁 Proje Yapısı

```
Assets/
├── Scripts/
│   ├── Player/           # Oyuncu kontrolleri
│   ├── Building/         # Build sistemi
│   ├── Combat/           # Savaş sistemi
│   ├── Network/          # Network yönetimi
│   ├── UI/              # Kullanıcı arayüzü
│   └── Editor/          # Editor araçları
├── Prefabs/             # Prefab'lar
└── Scenes/              # Sahne dosyaları
```

## 🔧 Editor Araçları

- **Recreate Player Prefab** - Player prefab'ını yeniden oluştur
- **Setup Build System** - Build sistemini kur
- **Setup NetworkManager** - Network ayarlarını yap
- **Create Wall Prefab** - Duvar prefab'ı oluştur

## 🎯 Özellik Detayları

### **FPS Controller**
- Smooth hareket interpolasyonu
- Gerçekçi zıplama fiziği
- Mouse sensitivity ayarları
- Head bob ve FOV kick efektleri
- Stamina sistemi ile koşma limiti

### **Build System**
- Valheim tarzı ghost preview
- Grid snapping ile hassas yerleştirme
- Structural integrity kontrolü
- Overlap detection
- Material feedback (yeşil/kırmızı)

### **Network System**
- Mirror tabanlı multiplayer
- Team ve role yönetimi
- Otomatik spawn sistemi
- Network sync optimizasyonu

## 🚀 Gelecek Özellikler

- [ ] Daha fazla yapı türü
- [ ] Gelişmiş savaş sistemi
- [ ] AI düşmanlar
- [ ] Daha fazla harita
- [ ] Ses sistemi iyileştirmeleri
- [ ] Grafik optimizasyonları

## 📝 Lisans

Bu proje eğitim amaçlı geliştirilmiştir.

## 👨‍💻 Geliştirici

**Burak Tuntaş**
- GitHub: [@buraktuntas](https://github.com/buraktuntas)
- Repository: [GameDemo](https://github.com/buraktuntas/GameDemo)

---

*Unity 6 + Mirror Networking ile geliştirilmiştir.*