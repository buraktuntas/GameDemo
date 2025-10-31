using UnityEngine;
using UnityEditor;
using TacticalCombat.Player;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Quick Fix - Adds missing InputManager to player
    /// </summary>
    public class PlayerQuickFix : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Quick Fix - Add InputManager")]
        static void AddInputManager()
        {
            GameObject player = Selection.activeGameObject;
            if (player == null)
            {
                EditorUtility.DisplayDialog("No Selection", "Please select the player prefab first!", "OK");
                return;
            }

            // Check if already exists
            if (player.GetComponent<InputManager>() != null)
            {
                EditorUtility.DisplayDialog("Already Exists", "InputManager already exists on this player!", "OK");
                return;
            }

            // Add component
            Undo.AddComponent<InputManager>(player);
            EditorUtility.SetDirty(player);

            Debug.Log("✅ InputManager added to player!");
            EditorUtility.DisplayDialog("Success!", "InputManager has been added to the player prefab!\n\nRun Health Check again to verify.", "OK");
        }

        [MenuItem("GameObject/Tactical Combat/Quick Fix - Add InputManager", false, 2)]
        static void AddInputManagerContext()
        {
            AddInputManager();
        }
    }
}
