using UnityEngine;
using UnityEditor;
using TacticalCombat.Combat;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ BUILD ERROR FIXER
    /// Build hatalarını düzeltir
    /// </summary>
    public class BuildErrorFixer : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/Fix Build Errors")]
        public static void ShowWindow()
        {
            GetWindow<BuildErrorFixer>("Build Error Fixer");
        }
        
        private void OnGUI()
        {
            GUILayout.Label("🔧 Build Error Fixer", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            if (GUILayout.Button("🗑️ Delete Old .wav Files"))
            {
                DeleteOldWavFiles();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🏷️ Fix Tag Conflicts"))
            {
                FixTagConflicts();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("🔄 Refresh Asset Database"))
            {
                RefreshAssetDatabase();
            }
            
            GUILayout.Space(10);
            
            if (GUILayout.Button("✅ Test Build"))
            {
                TestBuild();
            }
        }
        
        private void DeleteOldWavFiles()
        {
            Debug.Log("🗑️ Deleting old .wav files...");
            
            string[] wavFiles = {
                "Assets/Audio/EmptySound.wav",
                "Assets/Audio/FireSound_1.wav",
                "Assets/Audio/FireSound_2.wav",
                "Assets/Audio/FireSound_3.wav",
                "Assets/Audio/HitSound_1.wav",
                "Assets/Audio/HitSound_2.wav",
                "Assets/Audio/ReloadSound.wav"
            };
            
            int deletedCount = 0;
            foreach (string file in wavFiles)
            {
                if (System.IO.File.Exists(file))
                {
                    AssetDatabase.DeleteAsset(file);
                    deletedCount++;
                    Debug.Log($"✅ Deleted: {file}");
                }
            }
            
            Debug.Log($"🗑️ Deleted {deletedCount} old .wav files");
        }
        
        private void FixTagConflicts()
        {
            Debug.Log("🏷️ Fixing tag conflicts...");
            
            // Get current tags
            SerializedObject tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            
            // Check for duplicate tags
            for (int i = 0; i < tagsProp.arraySize; i++)
            {
                string currentTag = tagsProp.GetArrayElementAtIndex(i).stringValue;
                
                // Check if this tag appears elsewhere
                for (int j = i + 1; j < tagsProp.arraySize; j++)
                {
                    string otherTag = tagsProp.GetArrayElementAtIndex(j).stringValue;
                    
                    if (currentTag == otherTag)
                    {
                        Debug.LogWarning($"⚠️ Duplicate tag found: {currentTag}");
                        
                        // Remove duplicate
                        tagsProp.DeleteArrayElementAtIndex(j);
                        Debug.Log($"✅ Removed duplicate tag: {currentTag}");
                        
                        // Apply changes
                        tagManager.ApplyModifiedProperties();
                        
                        // Restart the process
                        FixTagConflicts();
                        return;
                    }
                }
            }
            
            Debug.Log("✅ Tag conflicts fixed!");
        }
        
        private void RefreshAssetDatabase()
        {
            Debug.Log("🔄 Refreshing Asset Database...");
            
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
            
            Debug.Log("✅ Asset Database refreshed!");
        }
        
        private void TestBuild()
        {
            Debug.Log("✅ Testing build...");
            
            // Check for common build issues
            CheckForBuildIssues();
            
            Debug.Log("✅ Build test completed!");
        }
        
        private void CheckForBuildIssues()
        {
            Debug.Log("🔍 Checking for build issues...");
            
            // Check for .wav files
            string[] wavFiles = System.IO.Directory.GetFiles("Assets/Audio", "*.wav", System.IO.SearchOption.AllDirectories);
            if (wavFiles.Length > 0)
            {
                Debug.LogWarning($"⚠️ Found {wavFiles.Length} .wav files that may cause issues:");
                foreach (string file in wavFiles)
                {
                    Debug.LogWarning($"   - {file}");
                }
            }
            
            // Check for missing prefabs
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                Debug.LogError("❌ Player prefab not found!");
            }
            else
            {
                Debug.Log("✅ Player prefab found");
            }
            
            // Check for missing scripts
            WeaponSystem[] weaponSystems = FindObjectsByType<WeaponSystem>(FindObjectsSortMode.None);
            if (weaponSystems.Length == 0)
            {
                Debug.LogWarning("⚠️ No WeaponSystem components found in scene");
            }
            else
            {
                Debug.Log($"✅ Found {weaponSystems.Length} WeaponSystem components");
            }
        }
    }
}
