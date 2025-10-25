# Paket Kurulum Rehberi

**Unity Versiyonu**: Unity 6 (6000.0.x LTS)

## Gerekli Paketler

Bu proje aşağıdaki Unity paketlerini gerektiriyor:

### 1. Mirror Networking

**Git URL ile Kurulum (Önerilen):**
1. Unity'yi aç
2. Window > Package Manager
3. "+" butonuna tıkla
4. "Add package from git URL" seç
5. Şunu gir: `https://github.com/vis2k/Mirror.git?path=/Assets/Mirror`
6. "Add" tıkla

**VEYA manifest.json ile Kurulum:**

`Packages/manifest.json` dosyasını aç ve dependencies'e ekle:
```json
"com.mirror-networking.mirror": "https://github.com/vis2k/Mirror.git?path=/Assets/Mirror"
```

### 2. Unity Input System

**Package Manager ile Kurulum:**
1. Window > Package Manager
2. "+" butonuna tıkla
3. "Add package by name" seç
4. Şunu gir: `com.unity.inputsystem`
5. "Add" tıkla

**VEYA manifest.json ile:**
```json
"com.unity.inputsystem": "1.9.0"
```

### 3. TextMeshPro

**Kurulum:**
- Unity 6'da varsayılan olarak kurulu gelir
- Eğer yoksa: Window > Package Manager > "TextMeshPro" ara > Install

### 4. Universal Render Pipeline (URP)

**URP projesi oluşturduysanız zaten kurulu olmalı**

Unity 6 için:
```json
"com.unity.render-pipelines.universal": "18.0.4"
```

**Not**: Unity 6 ile URP 18.x versiyonu gelir ve yeni özellikler içerir (gelişmiş lighting, GPU Resident Drawer, vb.).

## Tam manifest.json

`Packages/manifest.json` dosyanız şöyle görünmeli:

```json
{
  "dependencies": {
    "com.mirror-networking.mirror": "https://github.com/vis2k/Mirror.git?path=/Assets/Mirror",
    "com.unity.collab-proxy": "2.4.4",
    "com.unity.feature.development": "1.0.2",
    "com.unity.inputsystem": "1.9.0",
    "com.unity.render-pipelines.universal": "18.0.4",
    "com.unity.textmeshpro": "4.0.0",
    "com.unity.timeline": "1.7.5",
    "com.unity.ugui": "1.0.0",
    "com.unity.visualscripting": "1.9.0",
    "com.unity.modules.ai": "1.0.0",
    "com.unity.modules.androidjni": "1.0.0",
    "com.unity.modules.animation": "1.0.0",
    "com.unity.modules.assetbundle": "1.0.0",
    "com.unity.modules.audio": "1.0.0",
    "com.unity.modules.cloth": "1.0.0",
    "com.unity.modules.director": "1.0.0",
    "com.unity.modules.imageconversion": "1.0.0",
    "com.unity.modules.imgui": "1.0.0",
    "com.unity.modules.jsonserialize": "1.0.0",
    "com.unity.modules.particlesystem": "1.0.0",
    "com.unity.modules.physics": "1.0.0",
    "com.unity.modules.physics2d": "1.0.0",
    "com.unity.modules.screencapture": "1.0.0",
    "com.unity.modules.terrain": "1.0.0",
    "com.unity.modules.terrainphysics": "1.0.0",
    "com.unity.modules.tilemap": "1.0.0",
    "com.unity.modules.ui": "1.0.0",
    "com.unity.modules.uielements": "1.0.0",
    "com.unity.modules.umbra": "1.0.0",
    "com.unity.modules.unityanalytics": "1.0.0",
    "com.unity.modules.unitywebrequest": "1.0.0",
    "com.unity.modules.unitywebrequestassetbundle": "1.0.0",
    "com.unity.modules.unitywebrequestaudio": "1.0.0",
    "com.unity.modules.unitywebrequesttexture": "1.0.0",
    "com.unity.modules.unitywebrequestwww": "1.0.0",
    "com.unity.modules.vehicles": "1.0.0",
    "com.unity.modules.video": "1.0.0",
    "com.unity.modules.vr": "1.0.0",
    "com.unity.modules.wind": "1.0.0",
    "com.unity.modules.xr": "1.0.0"
  }
}
```

## Paket Doğrulama

Kurulumdan sonra, paketlerin kurulu olduğunu doğrula:

1. **Window > Package Manager**
2. "In Project" filtresini kontrol et
3. Bunların mevcut olduğunu doğrula:
   - ✅ Mirror Networking
   - ✅ Input System
   - ✅ TextMeshPro
   - ✅ Universal RP

## Yaygın Sorunlar

### Mirror Kurulumu Başarısız Oldu
- **Hata**: Git kurulu değil
- **Çözüm**: Git'i https://git-scm.com/ adresinden kur
- **Alternatif**: Bunun yerine Mirror'ı Asset Store'dan indir

### Input System Çakışmaları
- **Hata**: "Birden fazla input backend aktif"
- **Çözüm**: 
  1. Edit > Project Settings > Player
  2. Active Input Handling = "Input System Package (New)"
  3. Unity'yi yeniden başlat

### URP Bulunamadı
- **Sorun**: Proje Built-in pipeline ile oluşturulmuş
- **Çözüm**: URP template ile yeni proje oluştur ve assetleri kopyala

### Unity 6 Yenilikler
Unity 6 şunları getirir:
- **GPU Resident Drawer**: Daha iyi performans (binlerce objeyle)
- **Gelişmiş Lighting**: Daha gerçekçi ışıklandırma ve gölgeler
- **Adaptive Probe Volumes**: Dinamik Global Illumination
- **Render Graph**: Modern, optimize edilmiş render pipeline
- **Entity Component System (ECS)**: İsteğe bağlı yüksek performans

## Kurulum Sonrası Adımlar

Tüm paketleri kurduktan sonra:

1. **Input System Yapılandır**:
   - Edit > Project Settings > Player
   - Active Input Handling = "Input System Package (New)"
   - Unity'yi yeniden başlat

2. **Mirror Doğrula**:
   - Packages altında `Mirror` klasörünün göründüğünü kontrol et
   - `NetworkIdentity`, `NetworkBehaviour` kullanılabilir olmalı

3. **Input Actions Asset Oluştur**:
   - SETUP_GUIDE.md Adım 3'ü takip et

## Mirror Transport

Mirror varsayılan olarak **KcpTransport** içerir, bu oyunumuz için mükemmel:
- Düşük gecikme
- Güvenilir
- P2P için iyi çalışır
- Ek kurulum gerektirmez

## Unity 6'ya Özel Notlar

Unity 6 ile gelen yenilikler:
- **Daha Hızlı Derleme**: Incremental build improvements
- **Gelişmiş Profiling**: Daha detaylı performans analizi
- **Yeni Multiplayer Tools**: NetworkManager için debug araçları
- **WebGPU Desteği**: Web builds için daha iyi performans

## Opsiyonel Paketler (gelecekteki geliştirmeler için)

Bunlar MVP için gerekli DEĞİL ama daha sonra yararlı:

### ProBuilder (level tasarımı için)
```json
"com.unity.probuilder": "5.2.2"
```

### Cinemachine (gelişmiş kamera için)
```json
"com.unity.cinemachine": "2.10.0"
```

### Unity Services (daha sonra matchmaking için)
```json
"com.unity.services.core": "1.12.0"
```

---

**Hazır mısın?** Paketler kurulduğunda, sahne kurulumu için SETUP_GUIDE.md'ye geç.
