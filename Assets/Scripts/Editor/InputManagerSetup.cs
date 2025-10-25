using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    public class InputManagerSetup : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Create InputManager")]
        public static void CreateInputManager()
        {
            // Zaten var mı kontrol et
            InputManager existing = FindFirstObjectByType<InputManager>();
            if (existing != null)
            {
                Debug.LogWarning("⚠️ InputManager zaten sahnede var!");
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            
            // Yeni InputManager oluştur
            GameObject inputManager = new GameObject("[InputManager]");
            inputManager.AddComponent<InputManager>();
            
            // Hierarchy'de en üste yerleştir
            inputManager.transform.SetAsFirstSibling();
            
            // Seç
            Selection.activeGameObject = inputManager;
            
            Debug.Log("✅ InputManager oluşturuldu!");
            
            EditorUtility.DisplayDialog(
                "InputManager Oluşturuldu",
                "✅ [InputManager] GameObject'i sahnede oluşturuldu!\n\n" +
                "Bu obje:\n" +
                "• Cursor kontrolünü merkezi olarak yönetir\n" +
                "• Build mode'da kamera dönmesini engeller\n" +
                "• Input çakışmalarını önler\n\n" +
                "Artık test edebilirsin!",
                "Tamam"
            );
        }
        
        [MenuItem("Tools/Tactical Combat/Test Input System")]
        public static void TestInputSystem()
        {
            EditorUtility.DisplayDialog(
                "Input System Test",
                "Oyunu başlat ve şunları test et:\n\n" +
                "✅ Oyun başlar → Cursor kilitli\n" +
                "✅ Mouse hareket → Kamera dönüyor\n" +
                "✅ B tuşuna bas → Cursor açık\n" +
                "✅ Build mode'dayken mouse hareket → Kamera DÖNMÜYOR!\n" +
                "✅ Sol tık → Yapı yerleşiyor\n" +
                "✅ Sağ tık veya B → Cursor tekrar kilitli\n\n" +
                "Artık cursor savaşı yok! 🎉",
                "Anladım"
            );
        }
    }
}

