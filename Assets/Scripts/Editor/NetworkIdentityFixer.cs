using UnityEngine;
using UnityEditor;
using Mirror;

namespace TacticalCombat.Editor
{
    public static class NetworkIdentityFixer
    {
        [MenuItem("Tools/Tactical Combat/Fix NetworkIdentity Issues", false, 105)]
        public static void FixNetworkIdentityIssues()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔧 FIXING NETWORKIDENTITY ISSUES");
            Debug.Log("═══════════════════════════════════════");

            int fixedCount = 0;

            // Find all GameObjects with NetworkIdentity
            NetworkIdentity[] allIdentities = Object.FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None);

            Debug.Log($"Found {allIdentities.Length} NetworkIdentity components");

            foreach (var identity in allIdentities)
            {
                // Check if this is on root object
                Transform parent = identity.transform.parent;
                if (parent != null)
                {
                    // Check if parent also has NetworkIdentity
                    NetworkIdentity parentIdentity = parent.GetComponent<NetworkIdentity>();
                    if (parentIdentity != null)
                    {
                        Debug.LogWarning($"❌ Found child NetworkIdentity: {GetPath(identity.transform)}");
                        Debug.LogWarning($"   Parent: {GetPath(parent)} also has NetworkIdentity!");

                        // Ask to remove
                        bool remove = EditorUtility.DisplayDialog(
                            "Invalid NetworkIdentity Found",
                            $"Found NetworkIdentity on child object:\n\n" +
                            $"{identity.gameObject.name}\n\n" +
                            $"Under parent: {parent.name}\n\n" +
                            $"NetworkIdentity should only be on ROOT objects!\n\n" +
                            $"Remove NetworkIdentity from {identity.gameObject.name}?",
                            "Yes, Remove It",
                            "Skip");

                        if (remove)
                        {
                            Undo.DestroyObjectImmediate(identity);
                            Debug.Log($"✅ Removed NetworkIdentity from {identity.gameObject.name}");
                            fixedCount++;
                        }
                    }
                }
            }

            // Also check for multiple NetworkIdentity on same object
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (var obj in allObjects)
            {
                NetworkIdentity[] identitiesOnObj = obj.GetComponents<NetworkIdentity>();
                if (identitiesOnObj.Length > 1)
                {
                    Debug.LogError($"❌ {obj.name} has {identitiesOnObj.Length} NetworkIdentity components!");

                    bool fix = EditorUtility.DisplayDialog(
                        "Multiple NetworkIdentity",
                        $"{obj.name} has {identitiesOnObj.Length} NetworkIdentity components!\n\n" +
                        "There should only be ONE.\n\n" +
                        "Remove duplicates?",
                        "Yes",
                        "No");

                    if (fix)
                    {
                        // Keep first, remove others
                        for (int i = 1; i < identitiesOnObj.Length; i++)
                        {
                            Undo.DestroyObjectImmediate(identitiesOnObj[i]);
                            Debug.Log($"✅ Removed duplicate NetworkIdentity #{i} from {obj.name}");
                            fixedCount++;
                        }
                    }
                }
            }

            Debug.Log("═══════════════════════════════════════");
            Debug.Log($"✅ Fixed {fixedCount} NetworkIdentity issues");
            Debug.Log("═══════════════════════════════════════");

            if (fixedCount > 0)
            {
                EditorUtility.DisplayDialog("Fixed",
                    $"✅ Fixed {fixedCount} NetworkIdentity issue(s)!\n\n" +
                    "Save the scene and restart play mode.",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("No Issues",
                    "✅ No NetworkIdentity issues found!",
                    "OK");
            }
        }

        private static string GetPath(Transform t)
        {
            string path = t.name;
            while (t.parent != null)
            {
                t = t.parent;
                path = t.name + "/" + path;
            }
            return path;
        }
    }
}
