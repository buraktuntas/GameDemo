using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering.Universal;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// Unity 6 URP için kamera setup helper
    /// Tools > TacticalCombat > Fix URP Cameras
    /// </summary>
    public class URPCameraFixer : EditorWindow
    {
        [MenuItem("Tools/TacticalCombat/Fix URP Cameras")]
        public static void FixAllCameras()
        {
            Camera[] cameras = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            int fixedCount = 0;

            foreach (Camera cam in cameras)
            {
                // Check if it already has UniversalAdditionalCameraData
                var cameraData = cam.GetComponent<UniversalAdditionalCameraData>();
                
                if (cameraData == null)
                {
                    // Add the component
                    cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
                    fixedCount++;
                    Debug.Log($"✅ Added UniversalAdditionalCameraData to {cam.name}");
                }
            }

            if (fixedCount > 0)
            {
                Debug.Log($"<color=green>✅ Fixed {fixedCount} camera(s) for URP!</color>");
                EditorUtility.DisplayDialog("URP Camera Fixer", 
                    $"Successfully added Universal Additional Camera Data to {fixedCount} camera(s)!", 
                    "OK");
            }
            else
            {
                Debug.Log("All cameras already have URP components!");
                EditorUtility.DisplayDialog("URP Camera Fixer", 
                    "All cameras already have Universal Additional Camera Data.", 
                    "OK");
            }
        }

        [MenuItem("Tools/TacticalCombat/Scene Setup Helper")]
        public static void ShowSceneSetupHelper()
        {
            GetWindow<URPSceneSetupWindow>("Scene Setup Helper");
        }
    }

    public class URPSceneSetupWindow : EditorWindow
    {
        private void OnGUI()
        {
            GUILayout.Label("Unity 6 URP Scene Setup", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "Bu araçlar Unity 6 ve URP için scene'inizi otomatik olarak yapılandırır.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Fix All URP Cameras", GUILayout.Height(30)))
            {
                URPCameraFixer.FixAllCameras();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Gereksinimler:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("• Universal Render Pipeline yüklü olmalı");
            EditorGUILayout.LabelField("• Scene'de en az bir Camera olmalı");
        }
    }
}



