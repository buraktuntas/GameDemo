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
        [SerializeField] private bool showDebugInfo = true;

        private void OnGUI()
        {
            // ⭐ Sadece FPS mode'da crosshair göster
            if (InputManager.Instance != null)
            {
                // Build mode'da veya menu'de crosshair gizle
                if (InputManager.Instance.IsInBuildMode || 
                    InputManager.Instance.IsInMenu)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log("🎯 Crosshair hidden - Build mode or Menu active");
                    }
                    return; // Crosshair gizle
                }
                
                // Cursor locked değilse crosshair gizle
                if (InputManager.Instance.GetCurrentMode() != InputManager.CursorMode.Locked)
                {
                    if (showDebugInfo)
                    {
                        Debug.Log($"🎯 Crosshair hidden - Cursor mode: {InputManager.Instance.GetCurrentMode()}");
                    }
                    return; // Crosshair gizle
                }
                
                if (showDebugInfo)
                {
                    Debug.Log("🎯 Crosshair visible - FPS mode active");
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


