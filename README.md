# Tactical Combat MVP - Unity Projesi

İnşa ve Savaş fazlarına sahip, 4 benzersiz rol, tuzak sistemleri, sabotaj mekanikleri ve görüş kontrolü içeren çok oyunculu taktiksel savaş oyunu.

## 🎮 Oyun Genel Bakış

**Tür**: Çok Oyunculu Taktiksel Savaş  
**Unity Versiyonu**: Unity 6 (6000.0.x LTS)  
**Render Pipeline**: URP  
**Ağ**: Mirror (P2P Host Authority)  
**Input**: Unity Input System  
**Maç Formatı**: 3'te 2 (BO3), Raund başına Tek Can  

### Oyun Akışı
1. **Lobby Fazı**: Oyuncular bağlanır ve rol seçer
2. **İnşa Fazı** (2:30): Yapılar, tuzaklar ve savunmaların serbest yerleşimi
3. **Savaş Fazı** (8:00 maks): Rol yetenekleriyle tek canlık savaş
4. **Raund Sonu**: Kazananı belirle, BO3 skorunu güncelle
5. Bir takım 2 raund kazanana kadar tekrarla

## 🏗️ Proje Yapısı

```
Assets/
├── Scripts/
│   ├── Core/              # Temel oyun sistemleri (MatchManager, veri modelleri, enumlar)
│   ├── Player/            # Oyuncu controller, kamera, yetenekler
│   ├── Building/          # İnşa sistemi, yapılar, ghost önizleme
│   ├── Combat/            # Can, silahlar (yay/mızrak), mermiler
│   ├── Traps/             # Tuzak sistemi (diken, yapışkan, fırlatma tahtası, dart kulesi)
│   ├── Sabotage/          # Sabotaj mekanikleri ve minigame
│   ├── Vision/            # Kontrol noktası ve görüş sistemi
│   ├── Network/           # Network manager ve kurulum
│   └── UI/                # HUD ve UI komponentleri
├── ScriptableObjects/
│   └── Roles/             # Rol tanımlamaları (Builder, Guardian, Ranger, Saboteur)
├── Prefabs/
│   ├── Structures/        # Duvar, Platform, Rampa, vb.
│   ├── Traps/             # Tuzak prefabları
│   └── Weapons/           # Silah ve mermi prefabları
├── Materials/             # Takım materyalleri ve görsel geri bildirim
└── Scenes/                # Oyun sahneleri
```

## 🎯 Temel Sistemler

### 1. Maç Akış Sistemi
- **MatchManager**: Faz geçişlerini, BO3 takibini, kazanma koşullarını düzenler
- Zamanlayıcılarla otomatik faz ilerlemesi
- Sunucu-otoriteli durum yönetimi

### 2. Oyuncu Sistemi
- **Hareket**: Zıplama, hava kontrolü olan character controller
- **Kamera**: Çarpışma algılamalı üçüncü şahıs omuz kamerası
- **İnşa Modu**: İnşa fazı için geçiş sistemi
- **Takım Atama**: Otomatik dengeli takım ataması

### 3. Rol Sistemi (4 Rol)

#### Builder (İnşaatçı)
- **Bütçe**: Duvar 60, Yükseklik 40, Tuzak 30, Yardımcı 20
- **Aktif Yetenek**: Hızlı Kurulum (5s, 60s cooldown)
  - Arttırılmış inşa hızı ve azaltılmış maliyetler

#### Guardian (Koruyucu)
- **Bütçe**: Duvar 20, Yükseklik 10, Tuzak 10, Yardımcı 5
- **Aktif Yetenek**: Siper (3s, 45s cooldown)
  - Mermi engelleyen kalkan konisi

#### Ranger (İzci)
- **Bütçe**: Duvar 10, Yükseklik 10, Tuzak 5, Yardımcı 5
- **Aktif Yetenek**: Keşif Oku (30s cooldown)
  - Bir sonraki ok düşman pozisyonlarını açığa çıkarır

#### Saboteur (Sabotajcı)
- **Bütçe**: Duvar 5, Yükseklik 5, Tuzak 5, Yardımcı 5
- **Aktif Yetenek**: Gölge Adımı (4s, 40s cooldown)
  - Azaltılmış görünürlükle sessiz hareket

### 4. İnşa Sistemi
- **Serbest Yerleştirme**: Raycast tabanlı ghost önizleme
- **Doğrulama**: Sunucu taraflı kontroller (bütçe, çakışma, faz)
- **Yapılar**: Duvarlar, Platformlar, Rampalar, Çekirdek
- **Bütçe Sistemi**: Role göre ağırlıklı kaynak tahsisi

### 5. Savaş Sistemi
- **Tek Can**: Raund başına yeniden doğuş yok
- **Silahlar**:
  - **Yay**: Menzilli silah (50 hasar, 30 m/s)
  - **Mızrak**: Yakın dövüş silahı (75 hasar, 2.5m menzil)
- **Kazanma Koşulları**: 
  - Tüm düşmanları etkisiz hale getir
  - Düşman çekirdek yapısını yok et

### 6. Tuzak Sistemi

#### Statik Tuzaklar
- **Diken Tuzak**: 50 hasar, tek kullanımlık
- **Yapışkan Tuzak**: Düşmanları 3s yavaşlatır

#### Mekanik Tuzaklar
- **Fırlatma Tahtası**: Oyuncuları fırlatır (tekrar kullanılabilir, 2s cooldown)
- **Dart Kulesi**: Otomatik hedefleme, 25 hasar/atış, 8m menzil

### 7. Sabotaj Sistemi
- **Etkileşim**: 2.5s kanal süresi
- **Başarı**: Hedefi 15s devre dışı bırakır
- **Başarısızlık**: Sabotajcının pozisyonunu 5s açığa çıkarır
- **Minigame**: Basılı tutma tamamlama etkileşimi

### 8. Görüş Kontrolü Sistemi
- **Orta Kontrol Noktası**: Tartışmasız varlık ile ele geçirme (5s)
- **Görüş Darbesi**: Her 3s'de 20m yarıçapında düşmanları açığa çıkarır
- **Stratejik Değer**: Bilgi avantajı

## 📦 Kurulum ve Yapılandırma

### Ön Gereksinimler
1. **Unity 6 (6000.0.x LTS)** URP template ile
2. **Mirror Networking** (Package Manager üzerinden kur)
3. **Unity Input System** (Package Manager üzerinden kur)
4. **TextMeshPro** (Unity 6'da varsayılan olarak gelir)

### Adım Adım Kurulum

#### 1. Gerekli Paketleri Kur

**Window > Package Manager** aç:

**Package Manager UI üzerinden:**
- Input System: `com.unity.inputsystem`
- TextMeshPro: `com.unity.textmeshpro`

**Git URL üzerinden (Mirror için):**
```
https://github.com/vis2k/Mirror.git?path=/Assets/Mirror
```

Veya `Packages/manifest.json` dosyasına ekle:
```json
{
  "dependencies": {
    "com.unity.inputsystem": "1.7.0",
    "com.unity.textmeshpro": "3.0.6",
    "com.unity.render-pipelines.universal": "14.0.9"
  }
}
```

#### 2. Input System Yapılandır

1. **Edit > Project Settings > Player** git
2. **Active Input Handling** altında, **Input System Package (New)** seç
3. İstendiğinde Unity'yi yeniden başlat

#### 3. Input Actions Kur

Proje `Assets/InputSystem_Actions.inputactions` dosyasında önceden yapılandırılmış Input Actions içerir.

İki Action Map oluşturman gerekiyor:

**Player Action Map:**
- `Move` (Vector2) - WASD veya Sol Stick
- `Look` (Vector2) - Fare Delta veya Sağ Stick
- `Jump` (Button) - Space veya Güney Buton
- `Fire` (Button) - Sol Fare veya Sağ Tetik
- `UseAbility` (Button) - Q veya Batı Buton
- `ToggleBuild` (Button) - B veya Kuzey Buton
- `Interact` (Button) - E veya Doğu Buton
- `SwitchWeapon` (Button) - Tab veya D-Pad Yukarı

**Build Action Map:**
- `Place` (Button) - Sol Fare veya Sağ Tetik
- `Rotate` (Button) - R veya Sağ Bumper
- `SelectWall` (Button) - 1 veya D-Pad Sol
- `SelectPlatform` (Button) - 2 veya D-Pad Yukarı
- `SelectRamp` (Button) - 3 veya D-Pad Sağ

#### 4. Sahne Kurulumu Oluştur

1. Yeni sahne oluştur: `Assets/Scenes/GameScene.unity`
2. Aşağıdaki GameObjectleri ekle:

**Network Kurulumu:**
```
- NetworkManager (NetworkGameManager componenti ekle)
- MatchManager (MatchManager componenti ekle)
- BuildValidator (BuildValidator componenti ekle)
```

**Harita Kurulumu:**
```
- Ground (Plane, scale 20x1x20)
- Mid Point (ControlPoint componenti ekle, küre trigger collider ekle)
- Team A Spawn Points (Boş GameObjectler)
- Team B Spawn Points (Boş GameObjectler)
```

**UI Kurulumu:**
```
- Canvas
  ├── GameHUD (GameHUD componenti ekle)
  └── (Alt UI elementlerini çocuk olarak oluştur)
```

#### 5. Prefab Oluştur

Aşağıdaki prefabları oluşturman gerekecek:

**Player Prefab:**
```
Player (NetworkIdentity, NetworkTransform)
├── Model (Capsule)
├── CameraTarget (Boş)
└── Weapons
    ├── Bow (WeaponBow)
    └── Spear (WeaponSpear)

Komponentler:
- CharacterController
- PlayerController
- CameraController
- Health
- AbilityController
- WeaponController
- SabotageController
- BuildPlacementController
- PlayerHUDController
- PlayerInput (InputActions assetine referans)
```

**Yapı Prefabları:**
- Wall (Küp, Structure + Health + NetworkIdentity)
- Platform (Küp, Structure + Health + NetworkIdentity)
- Ramp (Kama, Structure + Health + NetworkIdentity)

**Tuzak Prefabları:**
- SpikeTrap (SpikeTrap + SabotageTarget + NetworkIdentity)
- GlueTrap (GlueTrap + SabotageTarget + NetworkIdentity)
- Springboard (Springboard + NetworkIdentity)
- DartTurret (DartTurret + SabotageTarget + NetworkIdentity)

#### 6. Network Manager Yapılandır

**NetworkGameManager** componentinde:
- **Player Prefab**'ı Player prefabına ayarla
- Yapı/tuzak prefablarını **Registered Spawnable Prefabs**'a ekle
- Spawn point dizilerini ata

#### 7. Layer Kurulumu

Aşağıdaki layerları oluştur:
- `Player` (Layer 6)
- `Structure` (Layer 7)
- `Trap` (Layer 8)
- `Ground` (Layer 9)

**Edit > Project Settings > Physics**'de layer çarpışma matrisini yapılandır.

## 🎮 Nasıl Oynanır

### Lokal Test (Tek Makine)

1. GameScene'i aç
2. **Play**'e tıkla
3. Game görünümünde, host otomatik başlayacak (veya NetworkSetup UI kullan)
4. 2 oyuncu test için:
   - Build > Build Settings > "Run in Background" işaretle
   - File > Build and Run
   - Bir instance Host olacak, diğeri Client

### Kontroller

**Hareket:**
- WASD - Hareket
- Space - Zıpla
- Fare - Bakış
- Tab - Silah Değiştir

**İnşa Fazı:**
- B - İnşa Modunu Aç/Kapat
- Sol Tık - Yapı Yerleştir
- R - Yapıyı Döndür
- 1/2/3 - Yapı Tipi Seç

**Savaş Fazı:**
- Sol Tık - Silah Ateşle
- Q - Rol Yeteneği Kullan
- E - Etkileşim (Sabotaj)

## 🔧 Yapılandırma

### Oyun Dengesi

`GameConstants.cs`'de değerleri düzenle:
```csharp
BUILD_DURATION = 150f;        // 2:30 inşa süresi
COMBAT_DURATION = 480f;       // 8:00 maks savaş
SPIKE_TRAP_DAMAGE = 50;
BOW_DAMAGE = 50;
SPEAR_DAMAGE = 75;
// ... vb
```

### Rol Bütçeleri

`DataModels.cs > BuildBudget.GetRoleBudget()` içinde değiştir:
```csharp
RoleId.Builder => new BuildBudget(60, 40, 30, 20),
```

## 📊 Mimari

### Network Otorite Modeli
- **Host Otoritesi**: Tüm kritik kararlar host tarafından doğrulanır
- **Client Tahmini**: Hareket ve görsel geri bildirim
- **Sunucu Doğrulaması**: Yerleştirme, hasar, yetenekler, sabotaj

### Önemli RPC Akışları

**İnşa Yerleştirme:**
```
Client: Ghost Önizleme → Input
Client → Server: CmdRequestPlace(BuildRequest)
Server: Doğrula → Yapıyı Spawn Et
Server → Tüm Clientlar: Obje Spawn Edildi
```

**Savaş:**
```
Client: Ateş Inputu
Client → Server: CmdFire()
Server: Çarpma Algılama Simülasyonu
Server → Hedef: Hasar Uygula
Server → Tüm Clientlar: RpcHitFx
```

**Sabotaj:**
```
Client: Etkileşimi Basılı Tut
Client → Server: CmdStartSabotage()
Server: İlerlemeyi Takip Et
Server: Başarı/Başarısızlık Kararı
Server → Tüm Clientlar: RpcSabotageResult
```

## 🐛 Bilinen Sınırlamalar (MVP)

1. **Özel Sunucu Yok**: Mirror P2P kullanır (host otoritesi)
2. **Temel Anti-Hile**: Client tahmini tam kilitli değil
3. **Lobby UI Yok**: 2+ oyuncuyla otomatik başlar
4. **Placeholder VFX/SFX**: Tam efektler yerine debug logları
5. **Kalıcılık Yok**: Oyuncu istatistikleri veya ilerleme yok
6. **Tek Harita**: Sadece simetrik test haritası

## 🚀 Sonraki Adımlar

MVP doğrulamadan sonra:

1. **Network Yükseltmesi**: Özel sunucular (Facepunch/Epic Online Services)
2. **Lobby Sistemi**: Rol seçimi, hazır olma, harita oylama
3. **Görsel Cilalama**: VFX, SFX, animasyonlar, UI iyileştirmeleri
4. **Ek İçerik**: Daha fazla rol, tuzak, harita
5. **Sıralama Modu**: MMR, sezonlar, lider tabloları
6. **Workshop Entegrasyonu**: Oyuncu tarafından oluşturulan kale planları

## 📝 Geliştirme Yol Haritası

### Sprint 0 (Hafta 1) ✅
- [x] URP + Mirror + Input System ile proje kurulumu
- [x] Temel veri modelleri ve mimari

### Sprint 1 (Hafta 2-3)
- [x] Maç akışı ve faz sistemi
- [x] Doğrulama ile inşa sistemi
- [ ] Gerçek prefablar oluştur ve yerleştirmeyi test et

### Sprint 2 (Hafta 4-5)
- [x] Savaş sistemi (Can, Silahlar)
- [x] Yetenekler ile rol sistemi
- [ ] Multiplayer savaşını test et

### Sprint 3 (Hafta 6-7)
- [x] Tuzak uygulamaları
- [x] Sabotaj mekanikleri
- [ ] Denge ayarlaması

### Sprint 4 (Hafta 8)
- [x] Görüş kontrolü sistemi
- [x] UI/HUD cilalanması
- [ ] Playtest seansları

## 🤝 Katkıda Bulunma

Bu bir MVP projesidir. Denge önerileri veya hata raporları için:
1. BO3 döngüsünü playtest et
2. Belirli senaryolarla sorunları belgele
3. Gerekçeleriyle denge değişiklikleri öner

## 📄 Lisans

Bu proje eğitim/portfolyo amaçlıdır.

## 🎯 Hedef Metrikler

**Performans:**
- 8 oyunculu host'ta 60 FPS
- Takım başına 100+ yapı desteği

**Maç Süresi:**
- İnşa: Raund başına 2:30
- Savaş: Raund başına 4-8 dakika
- Toplam maç: 15-30 dakika (BO3)

**Denge Hedefleri:**
- Tüm roller %45-55 kazanma oranına sahip
- Orta kontrolü %15-20 kazanma oranı avantajı sağlar
- Sabotaj başarı oranı: %60-70 (yetenekli oyuncu)

---

**Durum**: Temel sistemler uygulandı, prefab oluşturma ve playtest gerekiyor.

**Unity Versiyonu**: Unity 6 (6000.0.x LTS)  
**Son Güncelleme**: Ekim 2025
