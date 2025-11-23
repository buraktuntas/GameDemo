# 🔧 Lobby Ekranı Görünmüyor - Çözüm

## Sorun
Host'a tıkladığınızda sadece bireysel/takım seçenekleri görünüyor, lobby ekranı görünmüyor.

## ✅ Çözüm: Editor Tool'u Çalıştır

### Adım 1: Unity Editor'da Tool'u Aç
1. Unity Editor'da üst menüden **Tools** → **Tactical Combat** → **🎮 Auto Setup Lobby System** seçin
2. Açılan pencerede **🚀 SETUP LOBBY SYSTEM** butonuna tıklayın

### Adım 2: Scene'i Kaydet
1. **File** → **Save Scene** (Ctrl+S) yapın
2. Scene kaydedildi

### Adım 3: Test Et
1. Play butonuna basın
2. Host → Individual → Confirm
3. Lobby ekranı görünmeli

## 🔍 Alternatif: Manuel Kontrol

Eğer tool çalışmazsa:

1. **Hierarchy**'de **Canvas** GameObject'ini bulun
2. **Canvas** altında **LobbyPanel** veya **LobbyUI** var mı kontrol edin
3. Yoksa:
   - **Canvas**'a sağ tıklayın → **Create Empty** → İsmi **LobbyPanel** yapın
   - **LobbyPanel**'e **LobbyUI** component'i ekleyin (Add Component → LobbyUI)
   - **LobbyPanel** GameObject'ini aktif yapın (checkbox işaretli)

4. **UIFlowManager** GameObject'ini bulun
5. Inspector'da **Lobby UI** referansını **LobbyPanel**'e atayın

## 📝 Not
Editor tool'u scene'de LobbyPanel ve gerekli UI elementlerini otomatik oluşturur.
Tool'u çalıştırdıktan sonra mutlaka scene'i kaydedin!






