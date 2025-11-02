using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Sahnedeki tüm eksik script referanslarını temizler
    /// </summary>
    public class CleanMissingScripts : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Clean Missing Scripts")]
        static void ShowWindow()
        {
            CleanAllMissingScripts();
        }

        static void CleanAllMissingScripts()
        {
            // Sahnedeki tüm objeleri bul
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            int totalCleaned = 0;
            int objectsWithMissing = 0;

            foreach (GameObject obj in allObjects)
            {
                // Bu objedeki eksik componentleri say
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);

                if (missingCount > 0)
                {
                    objectsWithMissing++;
                    totalCleaned += missingCount;

                    Debug.LogWarning($"⚠️ Missing scripts found on: {GetFullPath(obj)} ({missingCount} missing)");

                    // Eksik componentleri kaldır
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);

                    Debug.Log($"✅ Cleaned {missingCount} missing scripts from: {obj.name}");
                }
            }

            if (objectsWithMissing > 0)
            {
                Debug.Log("═══════════════════════════════════════");
                Debug.Log($"✅ CLEANUP COMPLETE!");
                Debug.Log($"   Objects cleaned: {objectsWithMissing}");
                Debug.Log($"   Total missing scripts removed: {totalCleaned}");
                Debug.Log("═══════════════════════════════════════");

                EditorUtility.DisplayDialog("Cleanup Complete!",
                    $"Removed {totalCleaned} missing script(s) from {objectsWithMissing} object(s).\n\n" +
                    "Check Console for details.",
                    "OK");
            }
            else
            {
                Debug.Log("✅ No missing scripts found in scene!");

                EditorUtility.DisplayDialog("All Clean!",
                    "No missing scripts found in the scene.",
                    "OK");
            }
        }

        /// <summary>
        /// GameObject'in hierarchy'deki tam yolunu döndürür
        /// </summary>
        static string GetFullPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        [MenuItem("Tools/Tactical Combat/Find Missing Scripts (Report Only)")]
        static void FindMissingScripts()
        {
            // Sahnedeki tüm objeleri bul
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            int totalMissing = 0;
            int objectsWithMissing = 0;

            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 SCANNING FOR MISSING SCRIPTS...");
            Debug.Log("═══════════════════════════════════════");

            foreach (GameObject obj in allObjects)
            {
                int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(obj);

                if (missingCount > 0)
                {
                    objectsWithMissing++;
                    totalMissing += missingCount;

                    Debug.LogWarning($"⚠️ {GetFullPath(obj)} - {missingCount} missing script(s)", obj);
                }
            }

            Debug.Log("═══════════════════════════════════════");

            if (objectsWithMissing > 0)
            {
                Debug.LogWarning($"⚠️ FOUND {totalMissing} missing script(s) on {objectsWithMissing} object(s)");
                Debug.Log("   Run 'Tools > Tactical Combat > Clean Missing Scripts' to remove them.");

                EditorUtility.DisplayDialog("Missing Scripts Found!",
                    $"Found {totalMissing} missing script(s) on {objectsWithMissing} object(s).\n\n" +
                    "Check Console for details.\n\n" +
                    "Run 'Clean Missing Scripts' to remove them.",
                    "OK");
            }
            else
            {
                Debug.Log("✅ No missing scripts found!");

                EditorUtility.DisplayDialog("All Clean!",
                    "No missing scripts found in the scene.",
                    "OK");
            }
        }
    }
}
