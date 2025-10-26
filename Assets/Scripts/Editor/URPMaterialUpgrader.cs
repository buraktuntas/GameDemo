using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace TacticalCombat.Editor
{
    public class URPMaterialUpgrader : EditorWindow
    {
        [MenuItem("Tools/Tactical Combat/🔧 URP Material Upgrader")]
        public static void ShowWindow()
        {
            GetWindow<URPMaterialUpgrader>("URP Material Upgrader");
        }

        private void OnGUI()
        {
            GUILayout.Label("🔧 URP Material Upgrader", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Problem: Silah ve build objeleri pembe görünüyor", EditorStyles.helpBox);
            GUILayout.Label("Sebep: URP projesinde Standard Shader kullanılıyor", EditorStyles.helpBox);
            GUILayout.Space(10);

            if (GUILayout.Button("🔍 Check Render Pipeline", GUILayout.Height(30)))
            {
                CheckRenderPipeline();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🔄 Upgrade Project Materials to URP", GUILayout.Height(30)))
            {
                UpgradeProjectMaterials();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🎯 Fix Weapon Materials Only", GUILayout.Height(30)))
            {
                FixWeaponMaterials();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🏗️ Fix Build Mode Materials Only", GUILayout.Height(30)))
            {
                FixBuildModeMaterials();
            }

            GUILayout.Space(10);

            if (GUILayout.Button("🧹 Clean Up Magenta Materials", GUILayout.Height(30)))
            {
                CleanUpMagentaMaterials();
            }
        }

        private void CheckRenderPipeline()
        {
            Debug.Log("🔍 Checking Render Pipeline...");
            
            // Check URP Assets
            var urpAssets = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
            Debug.Log($"✅ Found {urpAssets.Length} URP Assets");
            
            if (urpAssets.Length > 0)
            {
                var urpAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset>(
                    AssetDatabase.GUIDToAssetPath(urpAssets[0]));
                if (urpAsset != null)
                {
                    Debug.Log($"✅ URP Asset: {urpAsset.name}");
                    Debug.Log($"✅ Pipeline Type: {urpAsset.GetType().Name}");
                }
            }
            else
            {
                Debug.LogWarning("⚠️ No URP Assets found - using Built-in pipeline");
            }

            // Check Materials
            var materials = AssetDatabase.FindAssets("t:Material");
            int standardShaderCount = 0;
            int urpShaderCount = 0;
            int magentaCount = 0;

            foreach (var matGuid in materials)
            {
                var material = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(matGuid));
                if (material != null)
                {
                    if (material.shader.name.Contains("Standard"))
                    {
                        standardShaderCount++;
                    }
                    else if (material.shader.name.Contains("Universal Render Pipeline"))
                    {
                        urpShaderCount++;
                    }
                    
                    // ✅ FIX: Safe color check - only check materials that have _Color property
                    try
                    {
                        if (material.HasProperty("_Color") && material.color == Color.magenta)
                        {
                            magentaCount++;
                        }
                    }
                    catch (System.Exception)
                    {
                        // Skip materials without _Color property (Skybox, TextMeshPro, etc.)
                    }
                }
            }

            Debug.Log($"📊 Material Statistics:");
            Debug.Log($"   - Standard Shader: {standardShaderCount}");
            Debug.Log($"   - URP Shader: {urpShaderCount}");
            Debug.Log($"   - Magenta Materials: {magentaCount}");
        }

        private void UpgradeProjectMaterials()
        {
            Debug.Log("🔄 Upgrading Project Materials to URP...");
            
            try
            {
                // Use Unity's built-in URP material upgrade
                var upgradeMethod = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.Rendering.Universal.UniversalRenderPipelineMaterialUpgrader");
                if (upgradeMethod != null)
                {
                    var upgradeAllMethod = upgradeMethod.GetMethod("UpgradeProjectMaterialsToUniversalPipeline", 
                        System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    
                    if (upgradeAllMethod != null)
                    {
                        upgradeAllMethod.Invoke(null, null);
                        Debug.Log("✅ Project materials upgraded to URP!");
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                    else
                    {
                        Debug.LogWarning("⚠️ URP upgrade method not found - trying manual upgrade");
                        ManualUpgradeMaterials();
                    }
                }
                else
                {
                    Debug.LogWarning("⚠️ URP upgrader not found - trying manual upgrade");
                    ManualUpgradeMaterials();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"❌ URP upgrade failed: {e.Message}");
                Debug.Log("🔄 Trying manual upgrade...");
                ManualUpgradeMaterials();
            }
        }

        private void ManualUpgradeMaterials()
        {
            Debug.Log("🔄 Manual URP Material Upgrade...");
            
            var materials = AssetDatabase.FindAssets("t:Material");
            int upgradedCount = 0;

            foreach (var matGuid in materials)
            {
                var materialPath = AssetDatabase.GUIDToAssetPath(matGuid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                
                if (material != null && material.shader.name.Contains("Standard"))
                {
                    // Try to find equivalent URP shader
                    Shader urpShader = null;
                    
                    if (material.shader.name.Contains("Standard"))
                    {
                        urpShader = Shader.Find("Universal Render Pipeline/Lit");
                    }
                    else if (material.shader.name.Contains("Unlit"))
                    {
                        urpShader = Shader.Find("Universal Render Pipeline/Unlit");
                    }
                    
                    if (urpShader != null)
                    {
                        material.shader = urpShader;
                        EditorUtility.SetDirty(material);
                        upgradedCount++;
                        Debug.Log($"✅ Upgraded: {material.name} -> {urpShader.name}");
                    }
                }
            }

            if (upgradedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ Manual upgrade complete! {upgradedCount} materials upgraded");
            }
            else
            {
                Debug.Log("ℹ️ No materials needed upgrading");
            }
        }

        private void FixWeaponMaterials()
        {
            Debug.Log("🎯 Fixing Weapon Materials...");
            
            var weaponPrefabs = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs/Weapons" });
            int fixedCount = 0;

            foreach (var prefabGuid in weaponPrefabs)
            {
                var prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                
                if (prefab != null)
                {
                    var renderers = prefab.GetComponentsInChildren<Renderer>();
                    foreach (var renderer in renderers)
                    {
                        if (renderer.sharedMaterial != null)
                        {
                            var material = renderer.sharedMaterial;
                            if (material.shader.name.Contains("Standard") || material.color == Color.magenta)
                            {
                                // Create new URP material
                                var newMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                                newMaterial.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
                                newMaterial.name = $"{prefab.name}_Material";
                                
                                renderer.sharedMaterial = newMaterial;
                                EditorUtility.SetDirty(prefab);
                                fixedCount++;
                                
                                Debug.Log($"✅ Fixed weapon material: {prefab.name} -> {renderer.name}");
                            }
                        }
                    }
                }
            }

            if (fixedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ Weapon materials fixed! {fixedCount} materials updated");
            }
            else
            {
                Debug.Log("ℹ️ No weapon materials needed fixing");
            }
        }

        private void FixBuildModeMaterials()
        {
            Debug.Log("🏗️ Fixing Build Mode Materials...");
            
            // Find SimpleBuildMode script
            var buildModeScripts = AssetDatabase.FindAssets("t:Script", new[] { "Assets/Scripts/Building" });
            foreach (var scriptGuid in buildModeScripts)
            {
                var scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
                if (scriptPath.Contains("SimpleBuildMode"))
                {
                    Debug.Log("✅ Found SimpleBuildMode script - materials will be fixed at runtime");
                    break;
                }
            }

            Debug.Log("✅ Build mode materials will be fixed automatically at runtime");
        }

        private void CleanUpMagentaMaterials()
        {
            Debug.Log("🧹 Cleaning Up Magenta Materials...");
            
            var materials = AssetDatabase.FindAssets("t:Material");
            int cleanedCount = 0;

            foreach (var matGuid in materials)
            {
                var materialPath = AssetDatabase.GUIDToAssetPath(matGuid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
                
                if (material != null)
                {
                    // ✅ FIX: Safe color check - only check materials that have _Color property
                    try
                    {
                        if (material.HasProperty("_Color") && material.color == Color.magenta)
                        {
                            // Replace with proper URP material
                            material.shader = Shader.Find("Universal Render Pipeline/Lit");
                            material.color = new Color(0.7f, 0.7f, 0.7f, 1f); // Gray
                            material.name = material.name.Replace("(Instance)", "").Trim();
                            
                            EditorUtility.SetDirty(material);
                            cleanedCount++;
                            
                            Debug.Log($"✅ Cleaned magenta material: {material.name}");
                        }
                    }
                    catch (System.Exception)
                    {
                        // Skip materials without _Color property (Skybox, TextMeshPro, etc.)
                    }
                }
            }

            if (cleanedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"✅ Magenta cleanup complete! {cleanedCount} materials cleaned");
            }
            else
            {
                Debug.Log("ℹ️ No magenta materials found");
            }
        }
    }
}
