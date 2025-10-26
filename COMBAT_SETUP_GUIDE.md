# 🚀 SAVAŞ SİSTEMİ KURULUM REHBERİ

**5 Dakikada Hazır!**

---

## ✅ ADIM 1: PLAYER PREFAB'E CHARACTER SELECTOR EKLE

1. **Assets/Prefabs/Player.prefab** dosyasını aç (Project window'da çift tıkla)

2. **Add Component** butonuna tıkla

3. **"CharacterSelector"** yaz ve seç

4. **Inspector**'da şu alanları doldur:

```
Character Selector Component:
├─ Male Character Prefab
│  └─ Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR.prefab
│
├─ Female Character Prefab
│  └─ Assets/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR.prefab
│
├─ Visual Offset: (0, 0, 0)
└─ Visual Scale: 1
```

**Nasıl atanır?**:
- Sağdaki küçük daire simgesine tıkla
- "MaleCharacterPBR" ara, seç
- Female için tekrarla

5. **Ctrl+S** ile kaydet

---

## ✅ ADIM 2: SPAWN POINT'LER OLUŞTUR

1. **Hierarchy**'de sağ tık → **Create Empty**

2. İsmi **"SpawnPoint"** yap

3. **Inspector** → **Tag** dropdown → **Add Tag...**

4. **+** butonuna tıkla

5. Tag Name: **"SpawnPoint"** yaz → **Save**

6. Tekrar **SpawnPoint** GameObject'i seç

7. **Tag** dropdown'dan **SpawnPoint** seç

8. **Transform Position** ayarla:
```
Position: (0, 2, 0)  // İlk spawn point
```

9. **Duplicate** (Ctrl+D) ile 2-3 kopyasını oluştur

10. Her birini farklı yere yerleştir:
```
SpawnPoint1: (0, 2, 0)
SpawnPoint2: (10, 2, 0)
SpawnPoint3: (-10, 2, 5)
```

---

## ✅ ADIM 3: IMPACT VFX POOL EKLE

1. **Hierarchy**'de sağ tık → **Create Empty**

2. İsmi **"[ImpactVFXPool]"** yap

3. **Add Component** → **"ImpactVFXPool"** yaz ve seç

4. **Hiçbir şey atamana gerek yok!** (Otomatik default efektler oluşacak)

5. *(İsteğe bağlı)* Pool Size değiştirebilirsin:
```
Pool Size Per Type: 15 (varsayılan)
Effect Lifetime: 3 (varsayılan)
```

---

## ✅ ADIM 4: PLAYER PREFAB'İ KONTROL ET

**Player.prefab** şu component'lere sahip olmalı:

### **Zorunlu Components**:
```
✅ NetworkIdentity
✅ PlayerController
✅ Health
✅ WeaponSystem
✅ CharacterSelector (az önce ekledin)
✅ FPSController (hareket için)
✅ Camera (FPS görüş için)
```

### **Hitbox Components** (TinyHeroPlayerAdapter ile gelecek):
```
✅ Hitbox (Head) - 2.5x damage
✅ Hitbox (Chest) - 1.0x damage
✅ Hitbox (Stomach) - 0.9x damage
✅ Hitbox (Limbs) - 0.75x damage
```

**TinyHeroPlayerAdapter** varsa otomatik setup yapacak. Yoksa:

1. Player prefab'e **Add Component** → **TinyHeroPlayerAdapter**
2. **WeaponSystem** ve **Health** referanslarını ata
3. Kaydet

---

## ✅ ADIM 5: NETWORK MANAGER KURULUMU

1. **Hierarchy**'de **NetworkManager** objesini bul

2. **Inspector**'da kontrol et:

```
Network Manager:
├─ Player Prefab: Assets/Prefabs/Player.prefab
├─ Auto Create Player: ✅ (işaretli)
└─ Spawn Info:
    └─ Registered Spawnable Prefabs:
        └─ Player.prefab (listeye ekle)
```

3. Kaydet

---

## 🧪 ADIM 6: TEST ET!

### **Multiplayer Test**:

#### **Host Başlat**:
```
1. File → Build Settings
2. Build And Run (ilk kez build alıyorsan)
3. Build klasörü seç (örn: "Builds/")
4. Açılan oyunda "Host" butonuna tıkla
```

#### **Client Başlat** (Unity Editor):
```
1. Unity Editor'de Play butonuna bas
2. "Client" butonuna tıkla
3. IP: localhost (veya host IP'si)
4. Connect
```

### **Beklenen Sonuç**:

```
✅ Host: Erkek karakter görünür
✅ Client: Kadın karakter görünür
✅ Birbirlerini görebilirler
✅ Birbirlerine ateş edebilirler
```

---

## 🎯 TEST SENARYOLARI

### **Test 1: Hasar Sistemi**

```
1. Host → Client'e ateş et (Left Click)
2. Console'da kontrol et:

Host Console:
🎯 [WeaponSystem CLIENT] HIT: FemaleCharacter
🎯 [Server] HIT chest - Damage: 25

Client Console:
🎨 [Client RPC] Impact effect - Surface: Flesh
Health UI: 100 → 75
```

**Beklenen**:
- ✅ Client'in canı azalır
- ✅ Kan efekti görünür
- ✅ Ses duyulur

---

### **Test 2: Ölüm & Respawn**

```
1. Client'in canı 0 olana kadar vur (4 kez göğüs vuruşu)
2. Console'da:

💀 [Server] Player died
💀 [Client] Death event received
(5 saniye bekle)
🔄 [Server] Respawning Player
🔄 [Client] Respawn event received
```

**Beklenen**:
- ✅ Client ölünce kontroller kapanır
- ✅ 5 saniye sonra spawn point'te respawn
- ✅ Kontroller tekrar açılır
- ✅ Can 100'e dönmüş

---

### **Test 3: Impact VFX**

#### **Vücuda Vuruş**:
```
Hedef: Düşman karakteri
Beklenen: Kırmızı kan efekti 🩸
```

#### **Duvara Vuruş**:
```
Hedef: Beton duvar
Beklenen: Gri toz efekti 💨
```

#### **Metal Obje**:
```
Hedef: Metal kasa/araba
Beklenen: Beyaz kıvılcımlar ⚡
```

---

## 🐛 SORUN GİDERME

### ❌ **"Birbirlerini vuramıyor"**

**Sebep**: Health component eksik veya başlatılmamış

**Çözüm**:
```csharp
// Health.cs Start() metodunu kontrol et:
private void Start()
{
    if (isServer)
    {
        currentHealth = maxHealth;  // Bu satır OLMALI
    }
}
```

---

### ❌ **"Impact efekt yok"**

**Sebep**: ImpactVFXPool scene'de yok

**Çözüm**:
```
1. Hierarchy → Create Empty
2. Add Component → ImpactVFXPool
3. Play'e bas
4. Console'da "🎨 [ImpactVFXPool] Initializing..." görmelisin
```

---

### ❌ **"Karakter görünmüyor"**

**Sebep**: CharacterSelector prefab ataması yok

**Çözüm**:
```
1. Player.prefab aç
2. CharacterSelector component
3. Male/Female prefab alanları DOLU olmalı
4. Boşsa:
   - Assets/RPG Tiny Hero Duo/Prefab/MaleCharacterPBR
   - Assets/RPG Tiny Hero Duo/Prefab/FemaleCharacterPBR
   Sürükle bırak
```

---

### ❌ **"Debug kutuları hala çıkıyor"**

**Sebep**: Eski kod cache'lenmiş

**Çözüm**:
```
1. Unity'yi kapat
2. Klasörü aç: "My project1/Library/ScriptAssemblies"
3. Tüm dosyaları sil
4. Unity'yi aç (yeniden compile olacak)
```

---

### ❌ **"Respawn çalışmıyor"**

**Sebep**: Spawn point yok veya tag yanlış

**Çözüm**:
```
1. Hierarchy'de "SpawnPoint" ara
2. En az 1 tane olmalı
3. Tag'i kontrol et: "SpawnPoint" olmalı
4. Yoksa ADIM 2'yi tekrarla
```

---

### ❌ **"Host erkek, ama client de erkek"**

**Sebep**: CharacterSelector OnStartClient çalışmamış

**Çözüm**:
```
1. Console'da ara: "🎮 [Client] Requesting FEMALE character"
2. Görünmüyorsa:
   - Player.prefab → CharacterSelector component var mı?
   - NetworkIdentity var mı?
3. Kaydet ve yeniden test et
```

---

## 📊 PERFORMANS KONTROL

### **Console Log'ları** (Normal):

#### **Host Console**:
```
✅ "🎨 [ImpactVFXPool] Initializing..."
✅ "🎮 [Server] Host assigned MALE character"
✅ "🎯 [WeaponSystem CLIENT] HIT: ..."
✅ "🎯 [Server] HIT chest - Damage: 25"
✅ "🎨 [Client RPC] Impact effect..."
```

#### **Client Console**:
```
✅ "🎮 [Client] Requesting FEMALE character"
✅ "🎨 [CharacterSelector] Character changed: Male → Female"
✅ "✅ [CharacterSelector] Female character spawned"
✅ "💀 [Client] Death event received"
✅ "🔄 [Client] Respawn event received"
```

### **Hata OLMAMALI**:
```
❌ "ApplyDamage called on client!" → Health server'da değil
❌ "Hit object has no collider" → Raycast yanlış obje vurdu
❌ "Character prefab is NULL" → Prefab atanmamış
```

---

## ✅ BAŞARI KRİTERLERİ

Tüm bunlar çalışmalı:

- [ ] Host erkek karakteri görüyor
- [ ] Client kadın karakteri görüyor
- [ ] Host → Client'e ateş ediyor → Client canı azalıyor
- [ ] Client → Host'a ateş ediyor → Host canı azalıyor
- [ ] Kan efekti görünüyor (vücut vuruşu)
- [ ] Gri toz efekti görünüyor (duvar vuruşu)
- [ ] Ölüm sonrası 5 saniye bekliyor
- [ ] Respawn çalışıyor, can 100'e dönüyor
- [ ] Console'da hata yok

---

## 🎮 BONUS: KEYBİNDİNGLER

**Savaş**:
```
Left Click: Ateş et
R: Reload (varsa)
Mouse: Nişan al
WASD: Hareket et
Space: Zıpla
Shift: Koş
```

**Test**:
```
F3: FPS göster (Unity Stats)
~ : Console aç (log'ları görmek için)
```

---

## 🚀 SONRAKİ SEVİYE (İsteğe Bağlı)

Sistem çalışıyorsa şunları ekleyebilirsin:

### **1. Weapon ADS (Nişanlama)**:
```csharp
// WeaponSystem.cs'e ekle:
if (Input.GetMouseButtonDown(1)) // Sağ tık
{
    isAiming = !isAiming;
    // Kamera FOV değiştir
}
```

### **2. Recoil (Geri tepme)**:
```csharp
// Fire() sonrası:
playerCamera.transform.Rotate(-recoilAmount, 0, 0);
```

### **3. Kill Feed UI**:
```csharp
// Health.Die() içinde:
UIManager.ShowKillFeed($"{killerName} killed {playerName}");
```

---

## 📖 DAHA FAZLA BİLGİ

Detaylı teknik dokümantasyon:
👉 **[BATTLEFIELD_COMBAT_SYSTEM.md](BATTLEFIELD_COMBAT_SYSTEM.md)**

- Mimari diagramlar
- Anti-cheat açıklamaları
- Performance metrikleri
- Kod örnekleri

---

**Kurulum Süresi**: ~5 dakika
**Zorluk**: Kolay
**Sonuç**: 🎮 Battlefield kalitesinde savaş!

Kurulumda sorun yaşarsan hemen söyle! 🚀
