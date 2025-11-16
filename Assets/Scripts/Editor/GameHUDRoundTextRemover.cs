using UnityEngine;
using UnityEditor;
using TMPro;
using TacticalCombat.UI;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ NEW: Removes Round Text from existing GameHUD in scene
    /// </summary>
    public class GameHUDRoundTextRemover : EditorWindow
    {
        [MenuItem("TacticalCombat/Tools/Remove Round Text from GameHUD")]
        public static void ShowWindow()
        {
            GetWindow<GameHUDRoundTextRemover>("Remove Round Text");
        }

        private void OnGUI()
        {
            GUILayout.Label("Remove Round Text", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "This will find and remove/deactivate Round Text GameObject from GameHUD in the scene.\n\n" +
                "Round system has been removed, so Round Text is no longer needed.",
                MessageType.Info
            );

            GUILayout.Space(10);

            if (GUILayout.Button("Remove Round Text from Scene", GUILayout.Height(30)))
            {
                RemoveRoundText();
            }
        }

        private void RemoveRoundText()
        {
            // Find GameHUD in scene
            GameHUD gameHUD = FindFirstObjectByType<GameHUD>();
            
            if (gameHUD == null)
            {
                EditorUtility.DisplayDialog(
                    "Not Found",
                    "GameHUD not found in scene.\n\nPlease make sure GameHUD exists in the scene.",
                    "OK"
                );
                return;
            }

            int removedCount = 0;

            // Find RoundText GameObject in GameHUD hierarchy
            Transform[] allChildren = gameHUD.GetComponentsInChildren<Transform>(true);
            
            foreach (Transform child in allChildren)
            {
                // Check if this is RoundText GameObject
                if (child.name.Contains("RoundText") || child.name.Contains("Round"))
                {
                    TextMeshProUGUI text = child.GetComponent<TextMeshProUGUI>();
                    if (text != null && (text.text.Contains("Round") || child.name == "RoundText"))
                    {
                        Debug.Log($"✅ [RoundTextRemover] Found and removing: {child.name}");
                        DestroyImmediate(child.gameObject);
                        removedCount++;
                    }
                }
            }

            // Also check TimerPanel children
            Transform timerPanel = gameHUD.transform.Find("TimerPanel");
            if (timerPanel != null)
            {
                Transform roundText = timerPanel.Find("RoundText");
                if (roundText != null)
                {
                    Debug.Log($"✅ [RoundTextRemover] Found RoundText in TimerPanel, removing...");
                    DestroyImmediate(roundText.gameObject);
                    removedCount++;
                }
            }

            if (removedCount > 0)
            {
                EditorUtility.SetDirty(gameHUD);
                EditorUtility.DisplayDialog(
                    "Success!",
                    $"✅ Removed {removedCount} Round Text GameObject(s) from GameHUD.\n\n" +
                    "Round Text will no longer appear in the game.",
                    "OK"
                );
                Debug.Log($"✅ [RoundTextRemover] Removed {removedCount} Round Text GameObject(s)");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "Not Found",
                    "No Round Text GameObject found in GameHUD.\n\n" +
                    "It may have already been removed, or it doesn't exist.",
                    "OK"
                );
            }
        }
    }
}






