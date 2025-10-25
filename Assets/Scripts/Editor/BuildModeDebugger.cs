using UnityEngine;
using UnityEditor;
using TacticalCombat.Building;

namespace TacticalCombat.Editor
{
    [CustomEditor(typeof(SimpleBuildMode))]
    public class BuildModeDebugger : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            
            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("Build Mode Setup Checker", MessageType.Info);
            
            SimpleBuildMode buildMode = (SimpleBuildMode)target;
            
            // Check wall prefab
            SerializedProperty wallPrefab = serializedObject.FindProperty("wallPrefab");
            if (wallPrefab.objectReferenceValue == null)
            {
                EditorGUILayout.HelpBox("⚠️ Wall Prefab eksik! Prefabs/Wall'u sürükle.", MessageType.Warning);
                
                if (GUILayout.Button("Auto-Assign Wall Prefab"))
                {
                    GameObject wall = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Wall.prefab");
                    if (wall != null)
                    {
                        wallPrefab.objectReferenceValue = wall;
                        serializedObject.ApplyModifiedProperties();
                        Debug.Log("✅ Wall prefab assigned!");
                    }
                    else
                    {
                        Debug.LogError("Wall prefab not found at Assets/Prefabs/Wall.prefab");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ Wall Prefab OK", MessageType.Info);
            }
            
            // Check ground layer
            SerializedProperty groundLayer = serializedObject.FindProperty("groundLayer");
            if (groundLayer.intValue == 0)
            {
                EditorGUILayout.HelpBox("⚠️ Ground Layer eksik! 'Default' veya 'Ground' layer seç.", MessageType.Warning);
                
                if (GUILayout.Button("Set Ground Layer to 'Default'"))
                {
                    groundLayer.intValue = LayerMask.GetMask("Default");
                    serializedObject.ApplyModifiedProperties();
                    Debug.Log("✅ Ground layer set to Default");
                }
            }
            else
            {
                EditorGUILayout.HelpBox("✅ Ground Layer OK", MessageType.Info);
            }
        }
    }
}


