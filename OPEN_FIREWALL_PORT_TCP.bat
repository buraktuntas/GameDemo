@echo off
echo ========================================
echo Unity Editor Multiplayer - Firewall TCP Port Açma
echo ========================================
echo.
echo Bu script port 7777 TCP'yi Windows Firewall'da açacak.
echo Administrator yetkisi gereklidir.
echo.
pause

netsh advfirewall firewall add rule name="Unity Editor Multiplayer Port 7777 TCP" dir=in action=allow protocol=TCP localport=7777

if %errorlevel% == 0 (
    echo.
    echo ✅ Port 7777 TCP başarıyla açıldı!
    echo.
) else (
    echo.
    echo ❌ Hata: Port açılamadı. Administrator olarak çalıştırdığınızdan emin olun.
    echo.
)

pause

