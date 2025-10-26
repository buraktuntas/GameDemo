# RPG Tiny Hero Duo - Hızlı Başlangıç Rehberi

## 🎯 5 Dakikada Karakteri Oyuna Ekle

### Adım 1: Character Setup Tool'u Aç
```
Unity Menu > Tools > Tactical Combat > Character Setup
```

### Adım 2: Karakter Seç
- **Gender**: Male veya Female seç
- **Style**: Polyart (low poly) veya PBR (realistic) seç

### Adım 3: Setup Options'ı Ayarla
✅ Setup Mirror Networking (multiplayer için gerekli)
✅ Setup Collision/Physics (hareket için gerekli)
✅ Upgrade Materials to URP (doğru renk için gerekli)
⬜ Replace Player Prefab (opsiyonel - sadece player prefab değiştirmek istersen)

### Adım 4: Karakteri Ekle
**Seçenek 1 - Yeni Karakter Ekle:**
```
📦 Add Character to Scene butonuna bas
```

**Seçenek 2 - Scene'deki Karakteri Setup Et:**
```
1. Hierarchy'den karakteri seç
2. 🔧 Setup Existing Character in Scene butonuna bas
```

### Adım 5: Test Et
1. Play butonuna bas
2. Karakter otomatik olarak network'e bağlanacak
3. Hareket et ve animasyonları test et

---

## 🔧 Multiplayer Test

### Host Olarak Test
1. Unity Editor'de Play'e bas
2. Build > Build and Run ile ikinci bir instance aç
3. İkinci instance'dan "Client" olarak bağlan

### Beklenen Sonuç
- ✅ Her iki taraf da karakterleri görür
- ✅ Animasyonlar senkronize olur
- ✅ Pozisyonlar senkronize olur

---

## 🎨 Materyal Sorunları

### Problem: Karakter pembe/gri görünüyor
**Sebep**: URP shader upgrade yapılmamış

**Çözüm 1 - Tek Karakter:**
```
1. Karakteri seç
2. Tools > Tactical Combat > Character Setup
3. ✅ Upgrade Materials to URP işaretle
4. 🔧 Setup Existing Character in Scene
```

**Çözüm 2 - Tüm Karakterler:**
```
1. Tools > Tactical Combat > Character Setup
2. 🔄 Upgrade All Character Materials to URP
```

---

## 🎮 Animasyon Sistemi

### Kullanılabilir Animasyonlar

#### Hareket
- Idle (durma)
- Walk (yürüme)
- Sprint (koşma)
- Strafe (yana hareket)

#### Savaş
- Attack 1, 2, 3, 4 (combo)
- Defend (savunma)
- GetHit (hasar alma)
- Die (ölüm)

#### Özel
- Jump (zıplama)
- Victory (zafer)
- LevelUp (seviye atlama)

### Animator Controller
Varsayılan controller: `SwordAndShieldStance.controller`

#### Animator Parametreleri
```csharp
// Speed (float): Hareket hızı (0-1)
animator.SetFloat("Speed", speed);

// Attack (trigger): Saldırı tetikle
animator.SetTrigger("Attack");

// Jump (trigger): Zıplama tetikle
animator.SetTrigger("Jump");

// IsDead (bool): Ölüm durumu
animator.SetBool("IsDead", isDead);
```

---

## 🔌 Kod Entegrasyonu

### Basit Kullanım (CharacterIntegration ile)
```csharp
using TacticalCombat.Player;

public class MyCharacterController : MonoBehaviour
{
    private CharacterIntegration characterIntegration;

    void Start()
    {
        characterIntegration = GetComponent<CharacterIntegration>();
    }

    void Update()
    {
        // Saldırı
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            characterIntegration.PlayAttackAnimation();
        }

        // Zıplama
        if (Input.GetKeyDown(KeyCode.Space))
        {
            characterIntegration.PlayJumpAnimation();
        }
    }
}
```

### Manuel Animator Kontrolü
```csharp
using UnityEngine;

public class ManualAnimatorControl : MonoBehaviour
{
    private Animator animator;

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        // Hareket hızı
        float speed = Input.GetAxis("Vertical");
        animator.SetFloat("Speed", Mathf.Abs(speed));

        // Saldırı
        if (Input.GetKeyDown(KeyCode.Mouse0))
        {
            animator.SetTrigger("Attack01");
        }
    }
}
```

---

## 🛠️ Gelişmiş Setup

### Player Prefab Olarak Kullan

#### Yöntem 1 - Otomatik (Tool ile)
```
1. Character Setup tool'u aç
2. ✅ Replace Player Prefab işaretle
3. 📦 Add Character to Scene
4. Karakter otomatik olarak player prefab olur
```

#### Yöntem 2 - Manuel
```
1. Karakteri scene'e ekle
2. Gerekli componentleri ekle:
   - NetworkIdentity
   - PlayerController
   - RigidbodyPlayerMovement
   - WeaponSystem
3. Prefab olarak kaydet: Assets/Prefabs/Player_TinyHero.prefab
4. NetworkManager > Player Prefab'ı bu prefab yap
```

### Silah Ekleme
```csharp
// Hand bone'u bul
Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);

// Silahı ekle
GameObject weapon = Instantiate(weaponPrefab, rightHand);
weapon.transform.localPosition = new Vector3(0.05f, 0.02f, -0.01f);
weapon.transform.localRotation = Quaternion.Euler(-90, 0, 0);
```

### Kamera Setup (First Person)
```csharp
// Head bone'u bul
Transform head = animator.GetBoneTransform(HumanBodyBones.Head);

// Kamerayı head'e ekle
Camera camera = Camera.main;
camera.transform.SetParent(head);
camera.transform.localPosition = new Vector3(0, 0.1f, 0.05f);
camera.transform.localRotation = Quaternion.identity;
```

---

## 📊 Component Checklist

Karakter doğru çalışıyor mu kontrol et:

### Network Components ✅
- [ ] NetworkIdentity
- [ ] NetworkTransform
- [ ] NetworkAnimator (eğer animasyon varsa)

### Physics Components ✅
- [ ] CharacterController VEYA
- [ ] Rigidbody + CapsuleCollider

### Animation Components ✅
- [ ] Animator (controller atanmış)
- [ ] CharacterIntegration (opsiyonel, ama önerilen)

### Game Components ✅
- [ ] PlayerController (player için)
- [ ] WeaponSystem (silah kullanımı için)
- [ ] HealthSystem (can sistemi için)

---

## 🐛 Troubleshooting

### Karakter Hareket Etmiyor
**Kontrol 1**: CharacterController veya Rigidbody var mı?
```
Inspector > Add Component > CharacterController
```

**Kontrol 2**: Input sistemi çalışıyor mu?
```
PlayerController component var mı kontrol et
```

### Animasyonlar Görünmüyor
**Kontrol 1**: Animator controller atanmış mı?
```
Inspector > Animator > Controller: SwordAndShieldStance
```

**Kontrol 2**: Animasyon parametreleri doğru mu?
```
Window > Animation > Animator
Parametreleri kontrol et: Speed, Attack, Jump, IsDead
```

### Network'te Görünmüyor
**Kontrol 1**: NetworkIdentity var mı?
```
Inspector > Add Component > NetworkIdentity
```

**Kontrol 2**: Player prefab olarak atanmış mı?
```
NetworkManager > Player Prefab: [Karakter prefab]
```

### Collision Çalışmıyor
**Kontrol 1**: Collider var mı?
```
Inspector > Add Component > Capsule Collider
```

**Kontrol 2**: Layer doğru mu?
```
Inspector > Layer: Default veya Player
```

---

## 📚 Daha Fazla Bilgi

### Detaylı Dökümanlar
- `Assets/RPG Tiny Hero Duo/README_CHARACTER_INTEGRATION.md` - Tam entegrasyon rehberi
- `Assets/Scripts/Player/README_RigidbodyMovement.md` - Hareket sistemi
- Unity Editor: `Tools > Tactical Combat > Character Setup` - Otomatik setup tool

### Asset Dökümanları
- Original asset klasöründe demo scene'ler var
- `RPG Tiny Hero Duo/Scene/` klasörüne bak

### Support
Sorun yaşarsan:
1. Unity Console'u kontrol et (Ctrl+Shift+C)
2. Character Setup tool'u kullan (otomatik fix yapar)
3. Demo scene'leri referans al

---

## ✅ Başarı Kriterleri

Karakter başarıyla entegre edildi sayılır eğer:

- [x] Scene'de görünüyor
- [x] Materyal doğru renklerde (pembe/gri değil)
- [x] Network'te senkronize oluyor
- [x] Hareket animasyonları çalışıyor
- [x] Collision çalışıyor
- [x] Player olarak spawn olabiliyor

Hepsini tamamladıysan, karakter hazır! 🎉

---

**Son Güncelleme**: 2025
**Versiyon**: 1.0
**Asset**: RPG Tiny Hero Duo by Polyart Studio
