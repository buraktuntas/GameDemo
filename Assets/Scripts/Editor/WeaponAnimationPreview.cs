using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ✅ WEAPON ANIMATION PREVIEW: Animasyon oynarken silahın nasıl görüneceğini gösterir
    /// </summary>
    public class WeaponAnimationPreview : EditorWindow
    {
        private AnimatorController animatorController;
        private string selectedAnimation = "";
        private bool isPlaying = false;
        private GameObject previewPlayer;
        private Vector2 scrollPosition;

        [MenuItem("Tools/Tactical Combat/Preview Weapon Animation")]
        public static void ShowWindow()
        {
            GetWindow<WeaponAnimationPreview>("Weapon Animation Preview");
        }

        private void OnGUI()
        {
            GUILayout.Label("Weapon Animation Preview", EditorStyles.boldLabel);
            GUILayout.Space(10);

            EditorGUILayout.HelpBox(
                "Bu tool animasyon oynarken silahın nasıl görüneceğini gösterir.\n\n" +
                "• FPS View: Silah Camera'ya bağlı → Animasyondan ETKİLENMEZ (sabit kalır)\n" +
                "• Third-Person: Silah Hand Bone'a bağlı → Animasyonla BİRLİKTE HAREKET EDER",
                MessageType.Info);
            GUILayout.Space(10);

            // Animator Controller
            GUILayout.Label("Animator Controller:", EditorStyles.boldLabel);
            animatorController = (AnimatorController)EditorGUILayout.ObjectField(
                animatorController,
                typeof(AnimatorController),
                false,
                GUILayout.Height(20)
            );
            GUILayout.Space(10);

            // Auto-find from Player prefab
            if (GUILayout.Button("Auto-Find from Player Prefab", GUILayout.Height(30)))
            {
                FindFromPlayerPrefab();
            }

            GUILayout.Space(10);

            EditorGUI.BeginDisabledGroup(animatorController == null);
            
            // Animation list
            if (animatorController != null)
            {
                GUILayout.Label("Available Animations:", EditorStyles.boldLabel);
                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(200));
                
                foreach (var layer in animatorController.layers)
                {
                    foreach (var state in layer.stateMachine.states)
                    {
                        if (state.state.motion != null)
                        {
                            string animName = state.state.motion.name;
                            bool isSelected = selectedAnimation == animName;
                            
                            if (GUILayout.Toggle(isSelected, animName, EditorStyles.radioButton))
                            {
                                selectedAnimation = animName;
                            }
                        }
                    }
                }
                
                EditorGUILayout.EndScrollView();
            }

            GUILayout.Space(10);

            // Preview controls
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(selectedAnimation));
            
            if (GUILayout.Button(isPlaying ? "Stop Preview" : "Start Preview", GUILayout.Height(40)))
            {
                if (isPlaying)
                {
                    StopPreview();
                }
                else
                {
                    StartPreview();
                }
            }

            EditorGUI.EndDisabledGroup();
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Info section
            EditorGUILayout.HelpBox(
                "📌 ÖNEMLİ:\n\n" +
                "1. FPS Görünümü:\n" +
                "   • Silah Camera/WeaponHolder'a bağlı\n" +
                "   • Animasyon oynarken silah SABİT kalır\n" +
                "   • Sadece ateş etme/reload animasyonları silahı etkiler\n\n" +
                "2. Third-Person Görünümü:\n" +
                "   • Silah Hand Bone'a bağlı\n" +
                "   • Animasyon oynarken silah EL İLE BİRLİKTE hareket eder\n" +
                "   • Yürüme, koşma, zıplama animasyonlarında silah da hareket eder\n\n" +
                "3. Hybrid Yaklaşım:\n" +
                "   • FPS için Camera'da WeaponHolder\n" +
                "   • Third-Person için Hand Bone'da WeaponHolder\n" +
                "   • İki farklı silah görünümü (bir görünür, diğeri gizli)",
                MessageType.Info);
        }

        private void FindFromPlayerPrefab()
        {
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            GameObject playerInstance = PrefabUtility.LoadPrefabContents(prefabPath);
            
            Animator animator = playerInstance.GetComponentInChildren<Animator>();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animatorController = animator.runtimeAnimatorController as AnimatorController;
                if (animatorController != null)
                {
                    Debug.Log($"✅ Found Animator Controller: {animatorController.name}");
                }
            }

            PrefabUtility.UnloadPrefabContents(playerInstance);
        }

        private void StartPreview()
        {
            if (string.IsNullOrEmpty(selectedAnimation) || animatorController == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select an animation!", "OK");
                return;
            }

            // Load Player prefab
            string prefabPath = "Assets/Prefabs/Player.prefab";
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (playerPrefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Player prefab not found!", "OK");
                return;
            }

            // Instantiate in scene for preview
            previewPlayer = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (previewPlayer != null)
            {
                previewPlayer.transform.position = Vector3.zero;
                previewPlayer.transform.rotation = Quaternion.identity;
                
                Animator animator = previewPlayer.GetComponentInChildren<Animator>();
                if (animator != null)
                {
                    // Play animation
                    animator.Play(selectedAnimation);
                    Debug.Log($"✅ Playing animation: {selectedAnimation}");
                }

                isPlaying = true;
                
                // Focus scene view
                if (SceneView.lastActiveSceneView != null)
                {
                    SceneView.lastActiveSceneView.FrameSelected();
                }
            }
        }

        private void StopPreview()
        {
            if (previewPlayer != null)
            {
                Object.DestroyImmediate(previewPlayer);
                previewPlayer = null;
            }

            isPlaying = false;
        }

        private void OnDestroy()
        {
            StopPreview();
        }
    }
}

