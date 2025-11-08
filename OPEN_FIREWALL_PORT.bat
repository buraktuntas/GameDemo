@echo off
echo ========================================
echo Unity Editor Multiplayer - Firewall Port Açma
echo ========================================
echo.
echo Bu script port 7777'yi Windows Firewall'da açacak.
echo Administrator yetkisi gereklidir.
echo.
pause

netsh advfirewall firewall add rule name="Unity Editor Multiplayer Port 7777" dir=in action=allow protocol=UDP localport=7777

if %errorlevel% == 0 (
    echo.
    echo ✅ Port 7777 başarıyla açıldı!
    echo.
) else (
    echo.
    echo ❌ Hata: Port açılamadı. Administrator olarak çalıştırdığınızdan emin olun.
    echo.
)

pause

