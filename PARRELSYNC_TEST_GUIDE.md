# 🎮 ParrelSync ile Host-Client Test Rehberi

## 📋 Adım Adım Test Senaryosu

### 1️⃣ Hazırlık

1. **ParrelSync Kurulu Olmalı:**
   - `ParrelSync > Clones Manager` menüsü görünüyor olmalı
   - `Clone 0 (Running)` görünüyor olmalı

2. **İki Unity Editor Penceresi Açık Olmalı:**
   - **Pencere 1:** Orijinal proje (Host için)
   - **Pencere 2:** Klon proje (Client için)
   - Eğer klon pencere açık değilse: `Clones Manager > Open in New Editor`

---

### 2️⃣ Host Başlatma (Orijinal Proje)

**Pencere 1 (Orijinal Proje):**

1. **Play Mode'a Basın** ▶️
2. **Main Menu'de:**
   - `Host Game` butonuna tıklayın
   - Veya `H` tuşuna basın (eğer SimpleNetworkHUD aktifse)

3. **Lobby Ekranı:**
   - Host otomatik olarak lobby'ye girer
   - `Ready` butonuna tıklayın (isteğe bağlı - host zaten ready)

4. **Bekleyin:**
   - Client'ın bağlanmasını bekleyin
   - Player list'te host görünmeli

---

### 3️⃣ Client Bağlanma (Klon Proje)

**Pencere 2 (Klon Proje):**

1. **Play Mode'a Basın** ▶️
2. **Main Menu'de:**
   - `Join Game` butonuna tıklayın
   - Veya `C` tuşuna basın (eğer SimpleNetworkHUD aktifse)

3. **IP Adresi:**
   - Default: `127.0.0.1` veya `localhost` (aynı bilgisayarda test için)
   - LAN için: Host'un IP adresini girin

4. **Lobby Ekranı:**
   - Client otomatik olarak lobby'ye girer
   - `Ready` butonuna tıklayın

5. **Kontrol:**
   - Player list'te hem host hem client görünmeli
   - Her ikisi de `[READY]` durumunda olmalı

---

### 4️⃣ Oyunu Başlatma

**Host (Pencere 1):**

1. **Tüm oyuncular ready olduğunda:**
   - `Start Game` butonu aktif olur
   - Butona tıklayın

2. **Oyun Başlar:**
   - Her iki pencerede de oyun başlamalı
   - Build phase başlar (8 saniye)

---

### 5️⃣ Test Senaryoları

#### ✅ Test 1: Bağlantı
- [ ] Host başlatıldı mı?
- [ ] Client bağlandı mı?
- [ ] Her iki tarafta da player list görünüyor mu?

#### ✅ Test 2: Lobby
- [ ] Ready butonu çalışıyor mu?
- [ ] Player list sync oluyor mu?
- [ ] Start Game butonu görünüyor mu? (sadece host'ta)

#### ✅ Test 3: Oyun Başlatma
- [ ] Oyun başladı mı?
- [ ] Her iki tarafta da build phase başladı mı?
- [ ] Her iki tarafta da player spawn oldu mu?

#### ✅ Test 4: Build Mode
- [ ] B tuşu çalışıyor mu?
- [ ] Yapı yerleştirme çalışıyor mu?
- [ ] Yapılar her iki tarafta da görünüyor mu?

#### ✅ Test 5: Combat
- [ ] Combat phase başladı mı?
- [ ] Silah atışı çalışıyor mu?
- [ ] Damage sync oluyor mu?

---

## 🔧 Sorun Giderme

### ❌ Client Bağlanamıyor

**Sorun:** Client "Connection failed" hatası veriyor

**Çözümler:**
1. **IP Adresi Kontrolü:**
   - Host: `127.0.0.1` veya `localhost`
   - Client: Aynı IP'yi kullanmalı

2. **Port Kontrolü:**
   - NetworkManager'da port ayarlarını kontrol edin
   - Her iki instance'da aynı port olmalı (default: 7777)

3. **Firewall:**
   - Windows Firewall'u kontrol edin
   - Unity'ye izin verin

### ❌ Player List Görünmüyor

**Sorun:** Client host'u görmüyor veya host client'ı görmüyor

**Çözümler:**
1. **LobbyManager Kontrolü:**
   - Her iki tarafta da LobbyManager var mı?
   - Scene'de LobbyManager GameObject'i var mı?

2. **Network Sync:**
   - Console'da hata var mı?
   - `LobbyManager` log'larını kontrol edin

### ❌ Oyun Başlamıyor

**Sorun:** Start Game butonuna tıklanınca hiçbir şey olmuyor

**Çözümler:**
1. **Ready Kontrolü:**
   - Tüm oyuncular ready mi?
   - Host ready mi?

2. **MatchManager Kontrolü:**
   - Scene'de MatchManager var mı?
   - Console'da hata var mı?

---

## 🎯 Hızlı Test (Keyboard Shortcuts)

Eğer `SimpleNetworkHUD` aktifse:

- **Host:** `H` tuşuna basın
- **Client:** `C` tuşuna basın

Bu, Main Menu'yi bypass eder ve direkt network başlatır.

---

## 📝 Notlar

- **Aynı Bilgisayarda Test:** `127.0.0.1` veya `localhost` kullanın
- **LAN Test:** Host'un IP adresini kullanın
- **Port:** Default 7777, değiştirmeyin (her iki tarafta aynı olmalı)
- **Console:** Her iki pencerede de console'u açık tutun (F1 veya Window > General > Console)

---

## ✅ Başarı Kriterleri

Test başarılı sayılır eğer:
1. ✅ Host başlatıldı
2. ✅ Client bağlandı
3. ✅ Her iki tarafta da player list görünüyor
4. ✅ Ready sistemi çalışıyor
5. ✅ Oyun başladı
6. ✅ Her iki tarafta da oyun çalışıyor

---

**İyi testler! 🚀**

