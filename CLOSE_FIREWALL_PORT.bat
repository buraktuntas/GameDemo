@echo off
echo ========================================
echo Unity Editor Multiplayer - Firewall Port Kapatma
echo ========================================
echo.
echo Bu script port 7777 kuralını silecek.
echo Administrator yetkisi gereklidir.
echo.
pause

netsh advfirewall firewall delete rule name="Unity Editor Multiplayer Port 7777"

if %errorlevel% == 0 (
    echo.
    echo ✅ Port 7777 kuralı başarıyla silindi!
    echo.
) else (
    echo.
    echo ⚠️ Kural bulunamadı veya zaten silinmiş.
    echo.
)

pause

