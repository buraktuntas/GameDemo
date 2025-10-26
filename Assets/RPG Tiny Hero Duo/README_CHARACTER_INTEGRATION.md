# RPG Tiny Hero Duo - Karakter Entegrasyonu

Bu dosya RPG Tiny Hero Duo karakterlerinin Tactical Combat oyununa nasıl entegre edileceğini açıklar.

## Hızlı Başlangıç

### Unity Editor'den Kullanım

1. **Character Setup Tool'u Aç**:
   - Unity menüsünden: `Tools > Tactical Combat > Character Setup`

2. **Karakter Seçimi**:
   - **Gender**: Male veya Female
   - **Style**: Polyart (Low Poly) veya PBR (Realistic)

3. **Setup Options**:
   - ✅ **Setup Mirror Networking**: Network componentleri ekle (multiplayer için gerekli)
   - ✅ **Setup Collision/Physics**: CharacterController, Rigidbody, Collider ekle
   - ✅ **Upgrade Materials to URP**: Materyalleri Universal Render Pipeline'a çevir
   - ⬜ **Replace Player Prefab**: Mevcut player prefab'ı bu karakterle değiştir (opsiyonel)

4. **Karakteri Ekle**:
   - `📦 Add Character to Scene` butonuna bas
   - Karakter otomatik olarak (0,0,0) pozisyonuna eklenecek

## Mevcut Karakterler

### Polyart (Low Poly) Stil
- `MaleCharacterPolyart.prefab` - Erkek karakter (Low Poly)
- `FemaleCharacterPolyart.prefab` - Kadın karakter (Low Poly)

### PBR (Realistic) Stil
- `MaleCharacterPBR.prefab` - Erkek karakter (Realistic)
- `FemaleCharacterPBR.prefab` - Kadın karakter (Realistic)

## Animasyon Sistemi

### Included Animations (Sword & Shield Stance)

#### Movement
- `Idle_Normal_SwordAndShield` - Durma animasyonu
- `Idle_Battle_SwordAndShield` - Savaş durma animasyonu
- `MoveFWD_Normal_InPlace_SwordAndShield` - İleri yürüme
- `MoveBWD_Battle_InPlace_SwordAndShield` - Geri yürüme
- `MoveLFT_Battle_InPlace_SwordAndShield` - Sola hareket
- `MoveRGT_Battle_InPlace_SwordAndShield` - Sağa hareket
- `SprintFWD_Battle_InPlace_SwordAndShield` - Koşma

#### Combat
- `Attack01_SwordAndShield` - Saldırı 1
- `Attack02_SwordAndShield` - Saldırı 2
- `Attack03_SwordAndShield` - Saldırı 3
- `Attack04_SwordAndShield` - Saldırı 4 (Combo finisher)
- `Attack04_Spinning_SwordAndShield` - Dönerek saldırı
- `Defend_SwordAndShield` - Kalkan savunması
- `DefendHit_SwordAndShield` - Kalkan ile hasar alma

#### Reactions
- `GetHit01_SwordAndShield` - Hasar alma
- `Dizzy_SwordAndShield` - Sersemlik
- `Die01_SwordAndShield` - Ölüm animasyonu
- `Die01_Stay_SwordAndShield` - Ölü kalma
- `GetUp_SwordAndShield` - Kalkma

#### Special
- `JumpFull_Normal_InPlace_SwordAndShield` - Zıplama
- `JumpStart_Normal_InPlace_SwordAndShield` - Zıplama başlangıcı
- `JumpAir_Normal_InPlace_SwordAndShield` - Havada kalma
- `JumpEnd_Normal_InPlace_SwordAndShield` - Zıplama sonu
- `Victory_Battle_SwordAndShield` - Zafer
- `LevelUp_Battle_SwordAndShield` - Level up

### Animator Controllers

- `SwordAndShieldStance.controller` - Kılıç ve kalkan stance
- `AnimationLayer.controller` - Layer-based animasyon sistemi
- `RootMotion.controller` - Root motion destekli controller

## Network Component Setup

Character Setup tool otomatik olarak şu componentleri ekler:

### 1. NetworkIdentity
- Mirror'ın temel network objesi
- Her network objesi için gerekli

### 2. NetworkTransform
- Pozisyon, rotasyon, scale senkronizasyonu
- Karakterin hareketini tüm oyunculara gönderir

### 3. NetworkAnimator
- Animasyon senkronizasyonu
- Tüm oyuncular karakterin animasyonlarını görür

### 4. PlayerController (Opsiyonel)
- Oyunun player controller scripti
- Hareket, kamera, silah kontrolü

## Physics & Collision Setup

### CharacterController
```csharp
center = (0, 0.9, 0)
radius = 0.3
height = 1.8
```

### Rigidbody
```csharp
mass = 70kg
constraints = FreezeRotation (Devrimi önler)
collisionDetectionMode = ContinuousDynamic
```

### CapsuleCollider
```csharp
center = (0, 0.9, 0)
radius = 0.3
height = 1.8
```

## URP Material Upgrade

Character Setup tool Standard shader'ları URP shader'larına çevirir:

**Önceki**: `Standard`
**Sonraki**: `Universal Render Pipeline/Lit`

### Manuel Material Upgrade

Eğer materyaller pembe/gri görünüyorsa:

1. Character Setup tool'da `🔄 Upgrade All Character Materials to URP` butonuna bas
2. Veya `Tools > Tactical Combat > URP Material Upgrader` kullan

## Player Prefab Replacement

Karakteri oyunun ana player prefab'ı olarak kullanmak için:

1. Character Setup tool'da `Replace Player Prefab` seçeneğini işaretle
2. Karakter eklendiğinde otomatik olarak:
   - Yeni prefab oluşturulur: `Assets/Prefabs/Player_Male_Polyart.prefab`
   - NetworkManager'daki playerPrefab referansı güncellenir
   - Multiplayer spawn sistem kullanır

## Weapons Integration

Karakterler silah taşıyabilir. Silahları karaktere eklemek için:

### 1. Hand Bone Bulma
```csharp
Transform rightHand = character.GetComponentInChildren<Animator>()
    .GetBoneTransform(HumanBodyBones.RightHand);
```

### 2. Silah Ekleme
```csharp
GameObject weapon = Instantiate(weaponPrefab, rightHand);
weapon.transform.localPosition = Vector3.zero;
weapon.transform.localRotation = Quaternion.identity;
```

### 3. Animator Override
Silaha göre animasyonları değiştirmek için AnimatorOverrideController kullan.

## Demo Scenes

RPG Tiny Hero Duo asset'i demo scene'ler içerir:

- **PolyartScene.unity** - Low poly stil demo
- **PBRScene.unity** - Realistic stil demo
- **AnimationLayer.unity** - Animasyon layer sistemi demo

Character Setup tool'dan `🎮 Load Demo Scene` butonu ile açılabilir.

## Troubleshooting

### Karakter Pembe/Gri Görünüyor
**Çözüm**: URP material upgrade yap
```
Tools > Tactical Combat > Character Setup
-> 🔄 Upgrade All Character Materials to URP
```

### Karakter Networkte Görünmüyor
**Çözüm**: NetworkIdentity ekle
```
1. Karakteri seç
2. Tools > Tactical Combat > Character Setup
3. Setup options: ✅ Setup Mirror Networking
4. 🔧 Setup Existing Character in Scene
```

### Animasyonlar Çalışmıyor
**Çözüm**: Animator controller atanmış mı kontrol et
```
1. Karakteri seç
2. Inspector > Animator component
3. Controller: SwordAndShieldStance veya AnimationLayer
```

### Karakter Collision Yapmıyor
**Çözüm**: Collider ve Rigidbody ekle
```
1. Karakteri seç
2. Tools > Tactical Combat > Character Setup
3. Setup options: ✅ Setup Collision/Physics
4. 🔧 Setup Existing Character in Scene
```

### Karakter Devrilip Düşüyor
**Çözüm**: Rigidbody constraints ayarla
```csharp
Rigidbody rb = character.GetComponent<Rigidbody>();
rb.constraints = RigidbodyConstraints.FreezeRotation;
```

## Modular Character System

RPG Tiny Hero Duo modular bir sistem kullanır. Karakterleri özelleştirmek için:

### Body Parts
Mesh klasöründe modular parçalar var:
- `ModularCharacterPolyart.fbx` - Low poly modular parçalar
- `ModularCharacterPBR.fbx` - Realistic modular parçalar

### Customization
Farklı armor, kıyafet, silah kombinasyonları yapabilirsin.

## Script Integration

### Example: Character Movement
```csharp
using UnityEngine;
using Mirror;

public class TinyHeroController : NetworkBehaviour
{
    private Animator animator;
    private CharacterController controller;

    void Start()
    {
        animator = GetComponent<Animator>();
        controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (!isLocalPlayer) return;

        // Movement
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v) * Time.deltaTime * 5f;
        controller.Move(move);

        // Animation
        animator.SetFloat("Speed", move.magnitude);
    }
}
```

## Performance Optimization

### LOD (Level of Detail)
Karakterler için LOD grubu eklemek:

1. Karakteri seç
2. Component > Rendering > LOD Group
3. LOD seviyelerini ayarla:
   - LOD 0 (0-50%): Full detail
   - LOD 1 (50-75%): Medium detail
   - LOD 2 (75-100%): Low detail

### Occlusion Culling
Görünmeyen karakterlerin render edilmemesi için:
```
Window > Rendering > Occlusion Culling
```

## Credits

**Asset**: RPG Tiny Hero Duo by Polyart Studio
**Integration**: Tactical Combat Team
**Date**: 2025

---

## Support

Sorunlar için:
- Unity Console loglarını kontrol et
- Character Setup tool'u kullan
- Demo scene'leri referans al
