# Hızlı Kurulum Rehberi - Tactical Combat MVP

**Unity Versiyonu**: Unity 6 (6000.0.x LTS)

## Adım 1: Mirror Networking Kur

### Seçenek A: Package Manager Üzerinden (Önerilen)
1. **Window > Package Manager** aç
2. **"+" > Add package from git URL** tıkla
3. Şunu gir: `https://github.com/vis2k/Mirror.git?path=/Assets/Mirror`
4. **Add** tıkla

### Seçenek B: Asset Store Üzerinden
1. Mirror'ı Unity Asset Store'dan indir
2. Projeye import et

## Adım 2: Input System Yapılandır

Proje zaten hazır scriptlere sahip, ama yapman gerekenler:

1. **Edit > Project Settings > Player**
2. **Other Settings > Active Input Handling** altında: **"Input System Package (New)"** seç
3. İstendiğinde Unity'yi yeniden başlat

## Adım 3: Input Actions Asset Oluştur

1. `Assets/` klasöründe sağ tık
2. Create > Input Actions
3. `InputSystem_Actions` olarak adlandır
4. Açmak için çift tıkla

### Action Map'leri Yapılandır:

**Player Map:**
```
Move (Value, Vector2) - Binding: WASD Composite
Look (Value, Vector2) - Binding: Mouse Delta
Jump (Button) - Binding: Space
Fire (Button) - Binding: Left Mouse Button
UseAbility (Button) - Binding: Q
ToggleBuild (Button) - Binding: B
Interact (Button) - Binding: E
SwitchWeapon (Button) - Binding: Tab
```

**Build Map:**
```
Place (Button) - Binding: Left Mouse Button
Rotate (Button) - Binding: R
SelectWall (Button) - Binding: 1
SelectPlatform (Button) - Binding: 2
SelectRamp (Button) - Binding: 3
```

5. **Save Asset** tıkla
6. Inspector'da **"Generate C# Class"** işaretle
7. **Apply** tıkla

## Adım 4: Layer Oluştur

**Edit > Project Settings > Tags and Layers**

Bu layerları ekle:
- Layer 6: `Player`
- Layer 7: `Structure`
- Layer 8: `Trap`
- Layer 9: `Ground`

## Adım 5: Ana Sahneyi Kur

### A. Temel Sahne Objeleri Oluştur

Yeni sahne oluştur: `Assets/Scenes/GameScene.unity`

**Hierarchy:**
```
GameScene
├── NetworkManager (Boş GameObject)
├── MatchManager (Boş GameObject)
├── BuildValidator (Boş GameObject)
├── Environment
│   ├── Ground (3D > Plane, Scale: 20,1,20, Layer: Ground)
│   ├── MidPoint (3D > Sphere, Sphere Collider ekle, Is Trigger: true)
│   ├── SpawnPoints_TeamA (Boş, alt boş GameObjectler ile)
│   └── SpawnPoints_TeamB (Boş, alt boş GameObjectler ile)
├── Lighting
│   └── Directional Light
└── UI
    └── Canvas
        └── GameHUD (Boş GameObject)
```

### B. Komponentleri Ekle

**NetworkManager GameObject:**
- Component Ekle > **NetworkGameManager** (özel script)
- Transport: **KcpTransport** kullan (Mirror ile gelir)

**MatchManager GameObject:**
- Component Ekle > **MatchManager**

**BuildValidator GameObject:**
- Component Ekle > **BuildValidator**

**MidPoint GameObject:**
- Component Ekle > **ControlPoint**
- Sphere Collider: Radius = 5, Is Trigger = işaretli

**GameHUD GameObject:**
- Component Ekle > **GameHUD**

## Adım 6: Oyuncu Prefab Oluştur

1. **Oyuncu Yapısını Oluştur:**

```
Player (Capsule)
├── Model (Capsule, scale 1,2,1)
├── CameraTarget (Boş, position: 0, 1.6, 0)
└── Weapons (Boş)
    ├── Bow (Boş)
    └── Spear (Boş)
```

2. **Kök Player GameObject'ine Komponent Ekle:**

- CharacterController (Height: 2, Radius: 0.5)
- NetworkIdentity
- NetworkTransform
- PlayerController
- CameraController
- Health
- AbilityController
- WeaponController
- SabotageController
- BuildPlacementController
- PlayerHUDController
- PlayerInput (Actions: InputSystem_Actions, Default Map: Player)

3. **Silah Komponentleri Ekle:**

**Bow GameObject:**
- WeaponBow
- Transform (Alt obje oluştur "ArrowSpawnPoint")

**Spear GameObject:**
- WeaponSpear
- Transform (Alt obje oluştur "StabPoint")

4. **Prefab Olarak Kaydet:**
- Player'ı `Assets/Prefabs/` klasörüne sürükle

## Adım 7: Yapı Prefabları Oluştur

### Wall Prefab
```
Wall (Cube, scale: 2, 2, 0.5)
├── Ekle: Structure component
├── Ekle: Health component
├── Ekle: NetworkIdentity
└── Layer: Structure
```

### Platform Prefab
```
Platform (Cube, scale: 2, 0.5, 2)
├── Ekle: Structure component
├── Ekle: Health component
├── Ekle: NetworkIdentity
└── Layer: Structure
```

### Ramp Prefab
```
Ramp (Döndürülmüş Cube veya kama, scale: 2, 1, 2)
├── Ekle: Structure component
├── Ekle: Health component
├── Ekle: NetworkIdentity
└── Layer: Structure
```

Hepsini `Assets/Prefabs/Structures/` içine kaydet

## Adım 8: Tuzak Prefabları Oluştur

### Spike Trap (Diken Tuzağı)
```
SpikeTrap (Cylinder, scale: 1, 0.2, 1)
├── Ekle: SpikeTrap component
├── Ekle: SabotageTarget component
├── Ekle: NetworkIdentity
├── Ekle: Sphere Collider (Is Trigger: true, Radius: 0.8)
└── Layer: Trap
```

### Glue Trap (Yapışkan Tuzak)
```
GlueTrap (Cylinder, scale: 1, 0.1, 1)
├── Ekle: GlueTrap component
├── Ekle: SabotageTarget component
├── Ekle: NetworkIdentity
├── Ekle: Sphere Collider (Is Trigger: true, Radius: 0.8)
└── Layer: Trap
```

### Springboard (Fırlatma Tahtası)
```
Springboard (Cube, scale: 2, 0.3, 2)
├── Ekle: Springboard component
├── Ekle: NetworkIdentity
├── Ekle: Box Collider (Is Trigger: true)
└── Layer: Trap
```

### Dart Turret (Dart Kulesi)
```
DartTurret (Cube, scale: 0.5, 1, 0.5)
├── Ekle: DartTurret component
├── Ekle: SabotageTarget component
├── Ekle: Health component
├── Ekle: NetworkIdentity
└── Layer: Trap
```

Hepsini `Assets/Prefabs/Traps/` içine kaydet

## Adım 9: Materyal Oluştur

`Assets/Materials/` içinde materyaller oluştur:

1. **TeamA_Material** - Mavi renk (0, 0.5, 1, 1)
2. **TeamB_Material** - Kırmızı renk (1, 0.2, 0, 1)
3. **ValidPlacement_Material** - Yeşil, yarı saydam (0, 1, 0, 0.5)
4. **InvalidPlacement_Material** - Kırmızı, yarı saydam (1, 0, 0, 0.5)

Materyalleri gerektiğinde prefablara uygula.

## Adım 10: NetworkManager Yapılandır

**NetworkManager** GameObject üzerinde:

**Network Game Manager Component:**
- Player Prefab: Player prefabını ata
- Team A Spawn Points: SpawnPoints_TeamA altlarından diziyi ata
- Team B Spawn Points: SpawnPoints_TeamB altlarından diziyi ata

**Registered Spawnable Prefabs (aşağı kaydır):**
Ağ üzerinden spawn edilecek tüm prefabları ekle:
- Player prefab
- Wall, Platform, Ramp
- SpikeTrap, GlueTrap, Springboard, DartTurret
- Arrow projectile (oluşturulduysa)

## Adım 11: BuildValidator Yapılandır

**BuildValidator** GameObject üzerinde:

**Build Validator Component:**
- Obstacle Mask: Layerleri seç (Structure, Trap)
- Wall Prefab: Wall prefabını ata
- Platform Prefab: Platform prefabını ata
- Ramp Prefab: Ramp prefabını ata

## Adım 12: BuildPlacementController Yapılandır

**Player** prefab üzerinde:

**Build Placement Controller Component:**
- Placement Surface: Layer seç (Ground, Structure)
- Obstacle Mask: Layerleri seç (Structure, Trap)
- Wall Ghost Prefab: Wall'ın ghost versiyonunu oluştur (BuildGhost componentli)
- Platform Ghost Prefab: Platform ghost versiyonu
- Ramp Ghost Prefab: Ramp ghost versiyonu
- Wall/Platform/Ramp Prefab: Gerçek prefabları ata

## Adım 13: UI Kur (Hızlı Versiyon)

**GameHUD** GameObject üzerinde, alt TextMeshPro elementleri oluştur:

```
GameHUD
├── PhaseText (TextMeshProUGUI)
├── TimerText (TextMeshProUGUI)
├── RoundText (TextMeshProUGUI)
├── ResourcePanel
│   ├── WallPoints (TextMeshProUGUI)
│   ├── ElevationPoints (TextMeshProUGUI)
│   ├── TrapPoints (TextMeshProUGUI)
│   └── UtilityPoints (TextMeshProUGUI)
└── HealthBar (Slider)
```

Bu UI elementlerini Inspector'da **GameHUD** komponentine ata.

## Adım 14: Test Et!

1. **Her şeyi kaydet**
2. **Editor'da Play**
3. NetworkManager otomatik Host olarak başlamalı
4. 2 oyunculu test için:
   - **Build Settings > "Run in Background" işaretle**
   - **Build and Run** ile standalone build oluştur
   - Bir örnek = Host, bir örnek = Client

### Hızlı Test Kontrol Listesi:
- ✅ Oyuncular farklı takımlarda spawn oluyor
- ✅ İnşa fazı otomatik başlıyor
- ✅ B ile build modu açılabiliyor
- ✅ Yapılar yerleştirilebiliyor (ghost görünüyor)
- ✅ Timer bittikten sonra savaş fazı başlıyor
- ✅ Silahlar çalışıyor (yay/mızrak)
- ✅ Hasar almak canı azaltıyor
- ✅ Ölüm seyirci modunu tetikliyor

## Yaygın Sorunlar

### Input System Hataları
**Sorun:** "Input System package kurulu değil"
**Çözüm:** Edit > Project Settings > Player > Active Input Handling = Input System Package (New)

### Mirror Hataları
**Sorun:** "NetworkIdentity bulunamadı"
**Çözüm:** Mirror kurulu olduğundan ve prefablarda NetworkIdentity komponenti olduğundan emin ol

### Prefab Spawn Olmuyor
**Sorun:** "Spawn edilen obje spawnable prefablarda yok"
**Çözüm:** TÜM ağ objelerini NetworkManager > Registered Spawnable Prefabs'a ekle

### Oyuncu Hareket Edemiyor
**Sorun:** CharacterController sorunları
**Çözüm:** CharacterController'ın Player üzerinde olduğundan emin ol, Radius=0.5, Height=2

### Build Modu Çalışmıyor
**Sorun:** Input algılanmıyor
**Çözüm:** 
1. PlayerInput komponentinin InputActions asset ataması olduğunu kontrol et
2. Input Actions'ın hem Player hem Build map'leri olduğunu kontrol et
3. Action isimlerinin koddakilerle tam eşleştiğini doğrula

## Sonraki Adımlar

Temel fonksiyonellik çalıştıktan sonra:
1. Yapı/tuzaklar için gerçek 3D modeller oluştur
2. Yetenekler ve etkiler için VFX ekle
3. Silahlar ve geri bildirim için SFX ekle
4. Düzen ve ikonlarla UI'yı cilala
5. Örtü olan simetrik harita oluştur
6. 4-8 oyuncuyla denge testi yap

---

**Yardıma mı ihtiyacın var?** Mimari detaylar ve sorun giderme için ana README.md'yi kontrol et.
