# 🎮 Mirror Networking - Profesyonel Test Rehberi

## ❌ Unity Multiplayer Tools (Kullanma)
Unity'nin Multiplayer Tools paketi **Netcode for GameObjects** için tasarlanmıştır. **Mirror Networking** ile uyumlu değildir.

## ✅ Önerilen Çözüm: ParrelSync

### ParrelSync Nedir?
ParrelSync, aynı Unity projesini klonlayıp **iki ayrı Unity Editor instance** çalıştırmanızı sağlar. Bu sayede:
- ✅ Host ve Client'ı aynı anda Editor'de test edebilirsiniz
- ✅ Build almanıza gerek yok
- ✅ Debugging çok daha kolay
- ✅ Hot reload çalışır
- ✅ Mirror Networking ile mükemmel uyumlu

### Kurulum

1. **ParrelSync'i Unity Package Manager'dan ekleyin:**
   ```
   Window > Package Manager > + > Add package from git URL
   ```
   URL: `https://github.com/VeriorPies/ParrelSync.git?path=/ParrelSync`

2. **Veya Unity Asset Store'dan:**
   - Asset Store'da "ParrelSync" arayın
   - Ücretsiz ve açık kaynak

### Kullanım

1. **Projeyi Klonlayın:**
   - Unity Editor'de: `ParrelSync > Clones Manager`
   - `Create New Clone` butonuna tıklayın
   - Klon proje otomatik oluşturulur

2. **Klon Projeyi Açın:**
   - **ÖNEMLİ:** Klon proje **ayrı bir Unity Editor penceresi** olarak açılmalı
   - Eğer otomatik açılmadıysa: `Clones Manager`'da `Open in New Editor` butonuna tıklayın
   - **İki ayrı Unity Editor penceresi** görmelisiniz:
     - **Pencere 1:** Orijinal proje (Host için)
     - **Pencere 2:** Klon proje (Client için)

3. **Test Senaryosu:**
   - **Host Editor (Orijinal):** `Start Host` butonuna tıklayın
   - **Client Editor (Klon):** `Start Client` butonuna tıklayın
   - İkisi de aynı anda çalışır!

### 🔍 Klon Proje Açık mı Kontrol Edin

**"Clone 0 (Running)" görüyorsanız:**
- ✅ Klon zaten çalışıyor olabilir
- ✅ Ama Unity Editor penceresi açık olmayabilir
- ✅ **Çözüm:** `Open in New Editor` butonuna tıklayın

**İki Unity Editor penceresi görmelisiniz:**
1. **Orijinal Proje:** Window title'da proje adı
2. **Klon Proje:** Window title'da proje adı + "(Clone 0)" veya benzeri

**Eğer sadece bir pencere görüyorsanız:**
- `Clones Manager`'da `Open in New Editor` butonuna tıklayın
- Yeni bir Unity Editor penceresi açılmalı

### Avantajlar

✅ **Aynı Kod:** Her iki instance aynı kodu kullanır (sync edilir)
✅ **Debugging:** Breakpoint'ler her iki tarafta çalışır
✅ **Hot Reload:** Kod değişiklikleri her iki tarafta otomatik yüklenir
✅ **Performance:** Build almanıza gerek yok
✅ **Console:** Her iki instance'ın console'unu görebilirsiniz

### 🔄 Nasıl Çalışır? (Teknik Detaylar)

ParrelSync **symlink (sembolik bağlantı)** kullanır:

```
Orijinal Proje/
├── Assets/          ← PAYLAŞILIR (symlink)
├── Library/          ← AYRI (her instance'ın kendi Library'si)
├── Temp/             ← AYRI (her instance'ın kendi Temp'i)
├── Logs/             ← AYRI (her instance'ın kendi Logs'u)
└── ProjectSettings/  ← PAYLAŞILIR (symlink)

Klon Proje/
├── Assets/          ← AYNI DOSYALAR (symlink ile bağlı)
├── Library/         ← AYRI (kendi Library'si)
├── Temp/            ← AYRI (kendi Temp'i)
└── Logs/            ← AYRI (kendi Logs'u)
```

**Sonuç:**
- ✅ **Kod Değişiklikleri:** Otomatik sync (Assets paylaşılıyor)
- ✅ **Prefab Değişiklikleri:** Otomatik sync
- ✅ **Scene Değişiklikleri:** Otomatik sync
- ⚠️ **Library/Temp:** Ayrı (her instance'ın kendi build cache'i)

### Dikkat Edilmesi Gerekenler

⚠️ **Port Çakışması:** Her iki instance farklı port kullanmalı
   - Mirror NetworkManager'da port ayarlarını kontrol edin
   - Host: Port 7777 (default)
   - Client: Port 7778 (farklı port)

⚠️ **File Conflicts:** Aynı anda her iki instance'da dosya değiştirmeyin
   - ParrelSync otomatik sync eder ama çakışma olabilir
   - **Çözüm:** Sadece bir instance'da kod düzenleyin

### 🔄 Kod Güncellemeleri Nasıl Çalışır?

**EVET, otomatik sync edilir!** 

1. **Kod Değişikliği Yapın:**
   - Orijinal veya klon instance'da bir script düzenleyin
   - Kaydedin (Ctrl+S)

2. **Otomatik Sync:**
   - Assets klasörü paylaşıldığı için değişiklik her iki instance'da görünür
   - Unity otomatik olarak değişikliği algılar

3. **Hot Reload:**
   - Play mode'da iseniz, Unity otomatik olarak script'i yeniden yükler
   - Her iki instance'da da güncellenmiş kod çalışır

**Örnek Senaryo:**
```
1. Host Editor'da SimpleBuildMode.cs'yi düzenleyin
2. Kaydedin (Ctrl+S)
3. Klon Editor'da da aynı değişiklik görünür (otomatik)
4. Her iki instance'da da güncellenmiş kod çalışır
```

**⚠️ Dikkat:**
- Aynı anda her iki instance'da aynı dosyayı düzenlemeyin (çakışma olabilir)
- Library/Temp klasörleri ayrı olduğu için build cache'leri farklıdır (sorun değil)

## 🔧 Alternatif: Mirror'ın Kendi Debugging Araçları

### 1. Network Statistics (Built-in)
Mirror'ın kendi network statistics'ini kullanabilirsiniz:

```csharp
// Network Statistics göster
NetworkManager.singleton.showDebugMessages = true;
```

### 2. Custom Network Debugger
Projenizde zaten `NetworkDebugger.cs` var:
- `Assets/Scripts/Debug/NetworkDebugger.cs`
- Runtime'da network durumunu gösterir

### 3. Mirror'ın Network HUD
Mirror'ın built-in HUD'unu kullanabilirsiniz:
- `NetworkManager` component'inde `Show Debug Messages` aktif edin

## 📊 Test Senaryoları

### Senaryo 1: Host + Client (ParrelSync)
1. ParrelSync ile iki instance açın
2. Host: Start Host
3. Client: Start Client
4. Test edin

### Senaryo 2: Build + Editor
1. Build alın (Development Build)
2. Editor'de Host başlatın
3. Build'de Client başlatın
4. **Not:** Bu yöntem daha az güvenilir (timing farkları)

### Senaryo 3: İki Build
1. İki ayrı build alın
2. Her ikisini de çalıştırın
3. **Not:** En gerçekçi test ama debugging zor

## 🎯 Önerilen Workflow

1. **Geliştirme:** ParrelSync kullanın (hızlı, kolay)
2. **Test:** Build + Editor kombinasyonu
3. **Final Test:** İki ayrı build

## 📝 Notlar

- ParrelSync projeyi klonlar, bu yüzden disk alanı kullanır
- Klon projeyi silmek için: `ParrelSync > Clones Manager > Delete Clone`
- ParrelSync açık kaynak ve ücretsiz

---

**Sonuç:** Unity Multiplayer Tools'u kaldırın, ParrelSync kullanın! 🚀

