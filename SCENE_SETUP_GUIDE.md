# 🎮 Scene Kurulum Rehberi - Unity 6

Bu rehber, Tactical Combat MVP projesinin ilk oynanabilir scene'ini kurmak için adım adım talimatlar içerir.

## 📋 Kurulum Adımları

### 1️⃣ Scene Yapısı

```
SampleScene (Hierarchy)
├── NetworkManager (NetworkGameManager)
├── GameManager (MatchManager)
├── Unity6Optimizations
├── Main Camera (+ URPCameraAutoFixer)
├── Directional Light
├── Ground (Terrain/Plane)
├── SpawnPoints/
│   ├── TeamA/
│   │   ├── SpawnPoint_A1
│   │   ├── SpawnPoint_A2
│   │   └── SpawnPoint_A3
│   └── TeamB/
│       ├── SpawnPoint_B1
│       ├── SpawnPoint_B2
│       └── SpawnPoint_B3
└── Mid/
    └── ControlPoint
```

---

## 🔧 Detaylı Kurulum

### ADIM 1: NetworkManager Oluştur

1. **Hierarchy'de sağ tık** → `Create Empty`
2. **İsim**: `NetworkManager`
3. **Inspector** → `Add Component` → `NetworkGameManager`
4. **Transport**: `Add Component` → `KCP Transport` (Mirror'ın varsayılan transport'u)

#### NetworkManager Ayarları:
- **Network Address**: `localhost`
- **Max Connections**: `6` (3v3 için)
- **Player Prefab**: (Adım 5'te oluşturacağız)

---

### ADIM 2: GameManager Oluştur

1. **Hierarchy'de sağ tık** → `Create Empty`
2. **İsim**: `GameManager`
3. **Inspector** → `Add Component` → `MatchManager`

#### MatchManager Ayarları:
- **Build Phase Duration**: `120` (2 dakika)
- **Combat Phase Duration**: `180` (3 dakika)
- **Round End Duration**: `10` (10 saniye)
- **Max Rounds**: `3` (BO3)

---

### ADIM 3: Unity 6 Optimizations Ekle

1. **Hierarchy'de sağ tık** → `Create Empty`
2. **İsim**: `Unity6Optimizations`
3. **Inspector** → `Add Component` → `Unity6Optimizations`

#### Optimization Ayarları (Varsayılan):
- ✅ Enable GPU Instancing
- ✅ Enable SRP Batcher
- ✅ Use Render Graph
- ✅ Enable GPU Resident Drawer
- **Target Frame Rate**: `60`

---

### ADIM 4: Spawn Points Oluştur

#### Team A Spawn Points:

1. **Hierarchy'de sağ tık** → `Create Empty` → İsim: `SpawnPoints`
2. **SpawnPoints altında** → `Create Empty` → İsim: `TeamA`
3. **TeamA altında** → `Create Empty` → İsim: `SpawnPoint_A1`
   - **Position**: `(-10, 0, -10)`
   - **Rotation**: `(0, 45, 0)`
4. Aynı şekilde **SpawnPoint_A2** ve **SpawnPoint_A3** oluştur:
   - A2: Position `(-8, 0, -10)`, Rotation `(0, 45, 0)`
   - A3: Position `(-12, 0, -10)`, Rotation `(0, 45, 0)`

#### Team B Spawn Points:

1. **SpawnPoints altında** → `Create Empty` → İsim: `TeamB`
2. **TeamB altında** 3 spawn point oluştur:
   - B1: Position `(10, 0, 10)`, Rotation `(0, 225, 0)`
   - B2: Position `(8, 0, 10)`, Rotation `(0, 225, 0)`
   - B3: Position `(12, 0, 10)`, Rotation `(0, 225, 0)`

---

### ADIM 5: Ground/Terrain Ekle

**Basit Test Ortamı:**

1. **Hierarchy'de sağ tık** → `3D Object` → `Plane`
2. **İsim**: `Ground`
3. **Transform**:
   - Position: `(0, 0, 0)`
   - Scale: `(5, 1, 5)` → 50x50 birim alan

**Opsiyonel - Materyal:**
1. `Assets/Materials` klasörü oluştur
2. Sağ tık → `Create` → `Material` → İsim: `Ground_Mat`
3. Material'i Ground'a sürükle

---

### ADIM 6: Control Point (Mid) Ekle

1. **Hierarchy'de sağ tık** → `Create Empty` → İsim: `Mid`
2. **Mid altında** → `3D Object` → `Cylinder` → İsim: `ControlPoint`
3. **ControlPoint Transform**:
   - Position: `(0, 0.5, 0)` (haritanın ortası)
   - Scale: `(3, 0.1, 3)` (düz platform)
4. **ControlPoint** → `Add Component` → `Sphere Collider`
   - **Is Trigger**: ✅ (işaretle)
   - **Radius**: `5`
5. **ControlPoint** → `Add Component` → `ControlPoint` (script)
6. **ControlPoint** → `Add Component` → `Network Identity`

---

### ADIM 7: Player Prefab Oluştur

1. **Hierarchy'de sağ tık** → `3D Object` → `Capsule` → İsim: `Player`
2. **Player Transform**:
   - Position: `(0, 1, 0)`
   - Scale: `(1, 1, 1)`

#### Player Component'leri Ekle:

3. **Player** → `Add Component` → `Character Controller`
   - Height: `2`
   - Radius: `0.5`
   - Center: `(0, 1, 0)`

4. **Player** → `Add Component` → `Network Identity`
   - **Local Player Authority**: ✅ (işaretle)

5. **Player** → `Add Component` → `Network Transform`
   - **Sync Position**: ✅
   - **Sync Rotation**: ✅

6. **Player** → `Add Component` → `PlayerController`

7. **Player** → `Add Component` → `Health`
   - **Max Health**: `100`

8. **Player** → `Add Component` → `WeaponController`

9. **Player** → `Add Component` → `AbilityController`

10. **Player** → `Add Component` → `BuildPlacementController`

#### Player Camera Ekle:

11. **Player altında** → `Camera` → İsim: `PlayerCamera`
12. **PlayerCamera** → `Add Component` → `CameraController`
13. **PlayerCamera** → `Add Component` → `Universal Additional Camera Data` (otomatik)
14. **Main Camera'yı devre dışı bırak** (Inspector'da GameObject yanındaki checkbox'ı kaldır)

#### Prefab Olarak Kaydet:

15. `Assets/Prefabs` klasörü oluştur (yoksa)
16. **Player**'ı Hierarchy'den `Assets/Prefabs` klasörüne sürükle
17. Hierarchy'deki Player'ı sil (prefab yeterli)

---

### ADIM 8: NetworkManager'ı Tamamla

1. **Hierarchy** → `NetworkManager` seç
2. **NetworkGameManager** component'inde:
   - **Player Prefab**: `Assets/Prefabs/Player` prefab'ini sürükle
   - **Team A Spawn Points**: TeamA altındaki 3 spawn point'i ekle (array size: 3)
   - **Team B Spawn Points**: TeamB altındaki 3 spawn point'i ekle (array size: 3)

---

### ADIM 9: UI Canvas (Opsiyonel - Şimdilik)

**Basit Test UI:**

1. **Hierarchy'de sağ tık** → `UI` → `Canvas`
2. **Canvas** → `Add Component` → `GameHUD`
3. **Canvas altında** → `UI` → `Text - TextMeshPro` → İsim: `StatusText`
4. **GameHUD** component'inde:
   - **Phase Text**: StatusText'i sürükle

---

## ✅ Test Etme

### Lokal Test (Tek Oyuncu):

1. **Play** butonuna bas (▶️)
2. Konsolu kontrol et:
   - `[Unity 6] GPU Resident Drawer enabled`
   - `[Unity 6] SRP Batcher optimization active`
3. Hareket etmeyi dene: **WASD** tuşları

### Multiplayer Test (2 Oyuncu):

1. **Build Settings** (`Ctrl+Shift+B`)
2. **Add Open Scenes**
3. **Build And Run** → Bir build oluştur
4. Unity Editor'de **Play** bas
5. Build'den **Host** bas
6. Editor'den **Connect** bas (`localhost`)

---

## 🎯 Sonraki Adımlar

- [ ] Weapon prefab'ları oluştur (Bow, Spear)
- [ ] Trap prefab'ları oluştur (Spike, Glue, Springboard, Dart Turret)
- [ ] Structure prefab'ları oluştur (Wall, Cover, Platform)
- [ ] Role ScriptableObject'leri yapılandır
- [ ] UI/HUD tamamla
- [ ] Input Actions yapılandır
- [ ] Test ve balance

---

## 📚 Ek Kaynaklar

- `START_HERE.md` - Genel proje bilgisi
- `SETUP_GUIDE.md` - Paket kurulumu
- `UNITY6_FEATURES.md` - Unity 6 özellikleri
- `PACKAGES_GUIDE.md` - Paket detayları

---

**Hazırlayan**: AI Assistant  
**Versiyon**: Unity 6 (6000.0.x)  
**Mirror**: Latest (Asset Store/GitHub)  
**Tarih**: 2025



