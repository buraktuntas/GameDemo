using UnityEngine;
using TacticalCombat.Player;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Basit crosshair - ekran ortasında
    /// </summary>
    public class SimpleCrosshair : MonoBehaviour
    {
        [Header("Crosshair Settings")]
        [SerializeField] private Color crosshairColor = Color.white;
        [SerializeField] private float size = 10f;
        [SerializeField] private float thickness = 2f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        // Debug state tracking
        private bool lastBuildModeState = false;
        private InputManager.CursorMode lastCursorMode = InputManager.CursorMode.Locked;

        // ✅ PERFORMANCE FIX: Cache local InputManager instead of finding every frame
        private InputManager cachedLocalInputManager;
        private float lastCacheTime;
        private const float CACHE_REFRESH_INTERVAL = 2f; // Refresh every 2 seconds

        private void OnGUI()
        {
            // ✅ PERFORMANCE FIX: Use cached InputManager, refresh periodically
            if (cachedLocalInputManager == null || Time.time - lastCacheTime > CACHE_REFRESH_INTERVAL)
            {
                cachedLocalInputManager = FindLocalPlayerInputManager();
                lastCacheTime = Time.time;
            }

            if (cachedLocalInputManager != null)
            {
                bool currentBuildMode = cachedLocalInputManager.IsInBuildMode || cachedLocalInputManager.IsInMenu || cachedLocalInputManager.IsPaused;
                InputManager.CursorMode currentCursorMode = cachedLocalInputManager.GetCurrentMode();
                
                // Debug logları sadece state değiştiğinde göster
                if (showDebugInfo)
                {
                    if (currentBuildMode != lastBuildModeState)
                    {
                        Debug.Log($"🎯 Crosshair state changed - Build mode: {currentBuildMode}");
                        lastBuildModeState = currentBuildMode;
                    }
                    
                    if (currentCursorMode != lastCursorMode)
                    {
                        Debug.Log($"🎯 Crosshair state changed - Cursor mode: {currentCursorMode}");
                        lastCursorMode = currentCursorMode;
                    }
                }
                
                // ✅ FIX: Build mode'da crosshair görünmeli
                if (currentBuildMode)
                {
                    // Build mode'da crosshair görünmeli (cursor gizli olduğu için)
                    // return; // ← Bu satırı kaldır
                }
                
                // ✅ FIX: İmleç görünürse crosshair gizle (ama build mode hariç)
                if (Cursor.visible && !currentBuildMode)
                {
                    return; // Crosshair gizle
                }
                
                // Cursor locked değilse crosshair gizle
                if (currentCursorMode != InputManager.CursorMode.Locked)
                {
                    return; // Crosshair gizle
                }
            }
            
            // Ekran merkezi
            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 2f;

            // Eski GUI kullanarak crosshair çiz
            Color oldColor = GUI.color;
            GUI.color = crosshairColor;

            // Yatay çizgi
            GUI.DrawTexture(
                new Rect(centerX - size, centerY - thickness / 2, size * 2, thickness),
                Texture2D.whiteTexture
            );

            // Dikey çizgi
            GUI.DrawTexture(
                new Rect(centerX - thickness / 2, centerY - size, thickness, size * 2),
                Texture2D.whiteTexture
            );

            GUI.color = oldColor;
        }
        
        /// <summary>
        /// Local player'ın InputManager'ını bul
        /// </summary>
        private InputManager FindLocalPlayerInputManager()
        {
            // Tüm InputManager'ları bul ve local player'ı bul
            InputManager[] allInputManagers = FindObjectsByType<InputManager>(FindObjectsSortMode.None);
            
            foreach (var inputManager in allInputManagers)
            {
                // NetworkBehaviour olup olmadığını kontrol et
                var networkBehaviour = inputManager.GetComponent<Mirror.NetworkBehaviour>();
                if (networkBehaviour != null && networkBehaviour.isLocalPlayer)
                {
                    return inputManager;
                }
            }
            
            return null;
        }
    }
}


