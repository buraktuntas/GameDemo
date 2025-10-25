using UnityEngine;
using UnityEditor;

namespace TacticalCombat.Editor
{
    public class TagSetup
    {
        [MenuItem("Tools/Tactical Combat/Setup Tags")]
        public static void SetupTags()
        {
            Debug.Log("🏷️ Setting up tags...");
            
            // Define required tags
            string[] requiredTags = {
                "Metal",
                "Wood", 
                "Stone",
                "Glass",
                "Flesh",
                "Player",
                "Enemy",
                "Ground",
                "Wall",
                "Structure"
            };
            
            // Get current tags
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            int addedCount = 0;
            
            foreach (string tag in requiredTags)
            {
                // Check if tag already exists
                bool tagExists = false;
                for (int i = 0; i < tagsProp.arraySize; i++)
                {
                    if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    {
                        tagExists = true;
                        break;
                    }
                }
                
                // Add tag if it doesn't exist
                if (!tagExists)
                {
                    tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
                    tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
                    addedCount++;
                    Debug.Log($"✅ Added tag: {tag}");
                }
                else
                {
                    Debug.Log($"ℹ️ Tag already exists: {tag}");
                }
            }
            
            // Apply changes
            tagManager.ApplyModifiedProperties();
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"🏷️ TAG SETUP COMPLETE!");
            Debug.Log($"   • Added {addedCount} new tags");
            Debug.Log($"   • Total tags: {tagsProp.arraySize}");
            Debug.Log("═══════════════════════════════════════════");
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
        
        [MenuItem("Tools/Tactical Combat/Assign Tags to Scene Objects")]
        public static void AssignTagsToSceneObjects()
        {
            Debug.Log("🏷️ Assigning tags to scene objects...");
            
            // Find all GameObjects in scene
            GameObject[] allObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            
            int assignedCount = 0;
            
            foreach (GameObject obj in allObjects)
            {
                string objName = obj.name.ToLower();
                
                // Assign tags based on object name
                if (objName.Contains("ground") || objName.Contains("terrain"))
                {
                    obj.tag = "Ground";
                    assignedCount++;
                }
                else if (objName.Contains("wall") || objName.Contains("floor") || objName.Contains("roof"))
                {
                    obj.tag = "Structure";
                    assignedCount++;
                }
                else if (objName.Contains("player"))
                {
                    obj.tag = "Player";
                    assignedCount++;
                }
                else if (objName.Contains("enemy"))
                {
                    obj.tag = "Enemy";
                    assignedCount++;
                }
                else if (objName.Contains("metal"))
                {
                    obj.tag = "Metal";
                    assignedCount++;
                }
                else if (objName.Contains("wood"))
                {
                    obj.tag = "Wood";
                    assignedCount++;
                }
                else if (objName.Contains("stone"))
                {
                    obj.tag = "Stone";
                    assignedCount++;
                }
            }
            
            Debug.Log("═══════════════════════════════════════════");
            Debug.Log($"🏷️ TAG ASSIGNMENT COMPLETE!");
            Debug.Log($"   • Assigned tags to {assignedCount} objects");
            Debug.Log("═══════════════════════════════════════════");
        }
    }
}
