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

        private void OnGUI()
        {
            // ⭐ Sadece FPS mode'da crosshair göster
            if (InputManager.Instance != null)
            {
                bool currentBuildMode = InputManager.Instance.IsInBuildMode || InputManager.Instance.IsInMenu;
                InputManager.CursorMode currentCursorMode = InputManager.Instance.GetCurrentMode();
                
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
                
                // Build mode'da veya menu'de crosshair gizle
                if (currentBuildMode)
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
    }
}


