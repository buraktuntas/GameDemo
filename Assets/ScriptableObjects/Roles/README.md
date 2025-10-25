# Rol ScriptableObjectleri

## Rolleri Nasıl Oluşturulur

1. Unity Editor'da, bu klasörde sağ tıklayın
2. **Create > Tactical Combat > Role Definition** seçin
3. Rol parametrelerini yapılandırın

## Rol Yapılandırmaları

### Builder (İnşaatçı)
- **Role ID**: Builder
- **İnşa Hızı**: 1.5x
- **Yetenek**: Hızlı Kurulum (5s, 60s CD)
- **Bütçe**: Duvar 60, Yükseklik 40, Tuzak 30, Yardımcı 20

### Guardian (Koruyucu)
- **Role ID**: Guardian
- **Hasar Direnci**: %10 (0.1)
- **Yetenek**: Siper (3s kalkan, 45s CD)
- **Bütçe**: Duvar 20, Yükseklik 10, Tuzak 10, Yardımcı 5

### Ranger (İzci)
- **Role ID**: Ranger
- **Hareket Hızı**: 1.1x
- **Yetenek**: Keşif Oku (düşmanları açığa çıkarır, 30s CD)
- **Bütçe**: Duvar 10, Yükseklik 10, Tuzak 5, Yardımcı 5

### Saboteur (Sabotajcı)
- **Role ID**: Saboteur
- **Hareket Hızı**: 1.15x
- **Yetenek**: Gölge Adımı (gizlilik, 4s, 40s CD)
- **Bütçe**: Duvar 5, Yükseklik 5, Tuzak 5, Yardımcı 5

## Kullanım

Bu ScriptableObjectler, rol özellikli davranışlarını ve istatistiklerini yapılandırmak için oyuncular üzerindeki `AbilityController` komponenti tarafından yüklenir.
