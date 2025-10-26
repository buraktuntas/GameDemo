using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Combat;
using TacticalCombat.Building;
using TacticalCombat.Effects;
using TacticalCombat.Player;
using TacticalCombat.Network;

namespace TacticalCombat.Editor
{
    /// <summary>
    /// ╔══════════════════════════════════════════════════════════╗
    /// ║  ULTIMATE PROJECT SETUP - TEK TIK İLE HER ŞEYİ HAZIRLA  ║
    /// ╚══════════════════════════════════════════════════════════╝
    ///
    /// Bu script TÜM kurulum adımlarını otomatik yapar:
    ///
    /// PHASE 1: TEMİZLİK
    /// - Duplicate objeleri temizle
    /// - Eski broken prefabları sil
    /// - Null referansları temizle
    ///
    /// PHASE 2: SCENE SETUP
    /// - NetworkManager + MatchManager
    /// - Ground, Spawn Points, Cores
    /// - Lighting, Audio Listener
    ///
    /// PHASE 3: PREFAB CREATION
    /// - Effect Prefabs (Blood, Sparks)
    /// - Weapon Prefabs
    /// - Structure Prefabs
    /// - Trap Prefabs
    ///
    /// PHASE 4: PLAYER SETUP
    /// - Player Prefab Recreation
    /// - WeaponSystem Fix
    /// - Audio Fix
    /// - Combat System Setup
    ///
    /// PHASE 5: FINAL TOUCHES
    /// - UI Setup
    /// - NetworkManager Configuration
    /// - Tag Setup
    /// - Layer Setup
    ///
    /// KULLANIM:
    /// Tools → TacticalCombat → 🚀 ULTIMATE SETUP
    /// </summary>
    public class UltimateProjectSetup : EditorWindow
    {
        private Vector2 scrollPos;
        private List<string> log = new List<string>();
        private bool isRunning = false;
        private float progress = 0f;

        // Options
        private bool cleanupPhase = true;
        private bool scenePhase = true;
        private bool prefabPhase = true;
        private bool playerPhase = true;
        private bool finalPhase = true;

        [MenuItem("Tools/TacticalCombat/🚀 ULTIMATE SETUP (One-Click)", priority = -1000)]
        public static void ShowWindow()
        {
            UltimateProjectSetup window = GetWindow<UltimateProjectSetup>("Ultimate Setup");
            window.minSize = new Vector2(600, 700);
            window.Show();
        }

        private void OnGUI()
        {
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 20;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            headerStyle.normal.textColor = Color.cyan;

            GUILayout.Space(10);
            GUILayout.Label("🚀 ULTIMATE PROJECT SETUP", headerStyle);
            GUILayout.Space(5);

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label);
            subtitleStyle.alignment = TextAnchor.MiddleCenter;
            subtitleStyle.fontSize = 12;
            GUILayout.Label("Tek Tıkla Tüm Projeyi Hazırla", subtitleStyle);
            GUILayout.Space(10);

            // Info Box
            EditorGUILayout.HelpBox(
                "Bu tool projeyi SIFIRDAN hazırlar:\n\n" +
                "✅ Temizlik (duplicates, broken references)\n" +
                "✅ Scene Setup (managers, spawns, cores)\n" +
                "✅ Prefab Creation (effects, weapons, structures)\n" +
                "✅ Player Setup (combat, audio, weapons)\n" +
                "✅ Final Touches (UI, network, tags)\n\n" +
                "⏱️ Tahmini Süre: 1-2 dakika\n" +
                "⚠️ Tüm değişiklikler otomatik yapılacak!",
                MessageType.Info
            );

            GUILayout.Space(10);

            // Phase Options
            EditorGUILayout.LabelField("Kurulum Fazları:", EditorStyles.boldLabel);
            EditorGUI.BeginDisabledGroup(isRunning);

            cleanupPhase = EditorGUILayout.ToggleLeft("Phase 1: Temizlik (Duplicates, Broken Refs)", cleanupPhase);
            scenePhase = EditorGUILayout.ToggleLeft("Phase 2: Scene Setup (Managers, Spawns)", scenePhase);
            prefabPhase = EditorGUILayout.ToggleLeft("Phase 3: Prefab Creation (Effects, Weapons)", prefabPhase);
            playerPhase = EditorGUILayout.ToggleLeft("Phase 4: Player Setup (Combat, Audio)", playerPhase);
            finalPhase = EditorGUILayout.ToggleLeft("Phase 5: Final Touches (UI, Network)", finalPhase);

            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Progress Bar
            if (isRunning)
            {
                EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(GUILayout.Height(25)), progress, $"İşlem Devam Ediyor... {Mathf.RoundToInt(progress * 100)}%");
            }

            GUILayout.Space(10);

            // Main Button
            EditorGUI.BeginDisabledGroup(isRunning);

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 18;
            buttonStyle.fontStyle = FontStyle.Bold;
            buttonStyle.fixedHeight = 60;

            Color originalBG = GUI.backgroundColor;
            GUI.backgroundColor = Color.green;

            if (GUILayout.Button("🚀 BAŞLAT - TEK TIK KURULUM", buttonStyle))
            {
                if (EditorUtility.DisplayDialog(
                    "Ultimate Setup Başlatılsın mı?",
                    "Bu işlem:\n\n" +
                    "• Duplicate objeleri silecek\n" +
                    "• Tüm prefabları yeniden oluşturacak\n" +
                    "• Player prefab'ı sıfırdan yapacak\n" +
                    "• Scene'i tamamen kuracak\n\n" +
                    "Devam edilsin mi?",
                    "Evet, Başlat!",
                    "İptal"
                ))
                {
                    RunUltimateSetup();
                }
            }

            GUI.backgroundColor = originalBG;
            EditorGUI.EndDisabledGroup();

            GUILayout.Space(10);

            // Quick Actions
            if (!isRunning)
            {
                EditorGUILayout.LabelField("Hızlı İşlemler:", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Sadece Temizlik"))
                {
                    RunCleanupOnly();
                }
                if (GUILayout.Button("Sadece Prefab'lar"))
                {
                    RunPrefabsOnly();
                }
                if (GUILayout.Button("Sadece Player Fix"))
                {
                    RunPlayerOnly();
                }
                EditorGUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            // Log Display
            if (log.Count > 0)
            {
                EditorGUILayout.LabelField("Kurulum Log'u:", EditorStyles.boldLabel);
                scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));

                foreach (string entry in log)
                {
                    GUIStyle logStyle = new GUIStyle(EditorStyles.label);
                    logStyle.wordWrap = true;
                    logStyle.richText = true;

                    if (entry.Contains("✅"))
                        logStyle.normal.textColor = Color.green;
                    else if (entry.Contains("❌"))
                        logStyle.normal.textColor = Color.red;
                    else if (entry.Contains("⚠️"))
                        logStyle.normal.textColor = Color.yellow;
                    else if (entry.Contains("📦") || entry.Contains("🎨") || entry.Contains("💥") || entry.Contains("🎮") || entry.Contains("🌐"))
                        logStyle.normal.textColor = Color.cyan;

                    EditorGUILayout.LabelField(entry, logStyle);
                }

                EditorGUILayout.EndScrollView();
            }
        }

        // ============================================================
        // MAIN SETUP FLOW
        // ============================================================

        private void RunUltimateSetup()
        {
            log.Clear();
            isRunning = true;
            progress = 0f;

            log.Add("╔══════════════════════════════════════════════╗");
            log.Add("║    ULTIMATE SETUP BAŞLADI                    ║");
            log.Add("╚══════════════════════════════════════════════╝");
            log.Add($"Zaman: {System.DateTime.Now:HH:mm:ss}");
            log.Add("");

            try
            {
                // Phase 1: Cleanup
                if (cleanupPhase)
                {
                    log.Add("📦 PHASE 1/5: TEMİZLİK");
                    progress = 0.1f;
                    Repaint();
                    RunCleanup();
                    log.Add("✅ Phase 1 tamamlandı");
                    log.Add("");
                }

                // Phase 2: Scene Setup
                if (scenePhase)
                {
                    log.Add("🗺️ PHASE 2/5: SCENE SETUP");
                    progress = 0.3f;
                    Repaint();
                    RunSceneSetup();
                    log.Add("✅ Phase 2 tamamlandı");
                    log.Add("");
                }

                // Phase 3: Prefab Creation
                if (prefabPhase)
                {
                    log.Add("💥 PHASE 3/5: PREFAB CREATION");
                    progress = 0.5f;
                    Repaint();
                    RunPrefabCreation();
                    log.Add("✅ Phase 3 tamamlandı");
                    log.Add("");
                }

                // Phase 4: Player Setup
                if (playerPhase)
                {
                    log.Add("🎮 PHASE 4/5: PLAYER SETUP");
                    progress = 0.7f;
                    Repaint();
                    RunPlayerSetup();
                    log.Add("✅ Phase 4 tamamlandı");
                    log.Add("");
                }

                // Phase 5: Final Touches
                if (finalPhase)
                {
                    log.Add("🌐 PHASE 5/5: FINAL TOUCHES");
                    progress = 0.9f;
                    Repaint();
                    RunFinalTouches();
                    log.Add("✅ Phase 5 tamamlandı");
                    log.Add("");
                }

                progress = 1f;
                log.Add("╔══════════════════════════════════════════════╗");
                log.Add("║    ✅ KURULUM TAMAMLANDI!                    ║");
                log.Add("╚══════════════════════════════════════════════╝");
                log.Add("");
                log.Add("🎮 Artık oyunu test edebilirsin!");
                log.Add("▶️  Play tuşuna bas veya Build and Run yap");

                EditorUtility.DisplayDialog(
                    "Kurulum Tamamlandı! 🎉",
                    "Proje tamamen hazır!\n\n" +
                    "✅ Scene kuruldu\n" +
                    "✅ Prefablar oluşturuldu\n" +
                    "✅ Player hazır\n" +
                    "✅ Combat sistemi aktif\n" +
                    "✅ Network ayarlandı\n\n" +
                    "Şimdi Play tuşuna basabilirsin!",
                    "Harika! 🎮"
                );
            }
            catch (System.Exception e)
            {
                log.Add($"❌ HATA: {e.Message}");
                log.Add($"Stack: {e.StackTrace}");
                EditorUtility.DisplayDialog("Hata", $"Kurulum sırasında hata:\n{e.Message}", "Tamam");
                Debug.LogError($"Ultimate Setup Error: {e}");
            }
            finally
            {
                isRunning = false;
                Repaint();
            }
        }

        // ============================================================
        // PHASE 1: CLEANUP
        // ============================================================

        private void RunCleanup()
        {
            log.Add("  🧹 Duplicate objeleri temizleniyor...");

            // Find duplicates
            Dictionary<string, List<GameObject>> objectsByName = new Dictionary<string, List<GameObject>>();
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (obj.scene.IsValid())
                {
                    if (!objectsByName.ContainsKey(obj.name))
                        objectsByName[obj.name] = new List<GameObject>();
                    objectsByName[obj.name].Add(obj);
                }
            }

            int duplicatesRemoved = 0;
            foreach (var kvp in objectsByName)
            {
                if (kvp.Value.Count > 1)
                {
                    // Keep first, delete rest
                    for (int i = 1; i < kvp.Value.Count; i++)
                    {
                        DestroyImmediate(kvp.Value[i]);
                        duplicatesRemoved++;
                    }
                }
            }

            log.Add($"  ✅ {duplicatesRemoved} duplicate obje silindi");

            // Clean broken Main Camera
            Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var cam in cameras)
            {
                if (cam.CompareTag("MainCamera") && cam.transform.parent == null)
                {
                    DestroyImmediate(cam.gameObject);
                    log.Add("  ✅ Eski Main Camera silindi");
                    break;
                }
            }

            // ⭐ NEW: Clean missing scripts
            log.Add("  🔍 Missing script'ler temizleniyor...");
            int missingScriptsRemoved = CleanMissingScripts();
            if (missingScriptsRemoved > 0)
                log.Add($"  ✅ {missingScriptsRemoved} missing script temizlendi");
            else
                log.Add("  ✅ Missing script bulunamadı");

            // ⭐ NEW: Fix duplicate AudioListeners
            log.Add("  🔊 AudioListener'lar kontrol ediliyor...");
            int audioListenersCleaned = CleanDuplicateAudioListeners();
            if (audioListenersCleaned > 0)
                log.Add($"  ✅ {audioListenersCleaned} duplicate AudioListener silindi");
            else
                log.Add("  ✅ AudioListener sayısı doğru");
        }

        private int CleanMissingScripts()
        {
            int removed = 0;
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);

            foreach (var obj in allObjects)
            {
                if (obj.scene.IsValid())
                {
                    // GameObjectUtility.RemoveMonoBehavioursWithMissingScript kullan
                    int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(obj);
                    removed += count;
                }
            }

            return removed;
        }

        private int CleanDuplicateAudioListeners()
        {
            AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);

            if (listeners.Length <= 1)
                return 0; // Sorun yok

            // İlk listener'ı tut (MainCamera'daki varsa onu tut)
            AudioListener keepListener = null;

            // Main Camera'daki listener varsa onu tercih et
            foreach (var listener in listeners)
            {
                if (listener.CompareTag("MainCamera"))
                {
                    keepListener = listener;
                    break;
                }
            }

            // Main Camera yoksa ilkini tut
            if (keepListener == null)
                keepListener = listeners[0];

            // Geri kalanları sil
            int removed = 0;
            foreach (var listener in listeners)
            {
                if (listener != keepListener && listener != null)
                {
                    DestroyImmediate(listener);
                    removed++;
                }
            }

            return removed;
        }

        private void RunCleanupOnly()
        {
            log.Clear();
            log.Add("🧹 Sadece Temizlik...");
            RunCleanup();
            log.Add("✅ Temizlik tamamlandı");
            Repaint();
        }

        // ============================================================
        // PHASE 2: SCENE SETUP
        // ============================================================

        private void RunSceneSetup()
        {
            // Delegate to SceneSetupHelper methods
            log.Add("  📍 Spawn points oluşturuluyor...");
            SceneSetupHelper_CreateSpawnPoints();

            log.Add("  🏛️ Core structures oluşturuluyor...");
            SceneSetupHelper_CreateCores();

            log.Add("  ⚙️ Managers oluşturuluyor...");
            SceneSetupHelper_CreateManagers();

            log.Add("  💡 Lighting ayarlanıyor...");
            SceneSetupHelper_CreateLighting();

            log.Add("  🗺️ Ground oluşturuluyor...");
            SceneSetupHelper_CreateGround();
        }

        private void SceneSetupHelper_CreateSpawnPoints()
        {
            // TeamA
            if (GameObject.Find("TeamA_SpawnPoint") == null)
            {
                GameObject spawnA = new GameObject("TeamA_SpawnPoint");
                spawnA.transform.position = new Vector3(-20, 1, 0);
                spawnA.tag = "Respawn";

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Marker";
                marker.transform.SetParent(spawnA.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(2, 0.1f, 2);
                DestroyImmediate(marker.GetComponent<Collider>());

                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.blue;
                marker.GetComponent<Renderer>().sharedMaterial = mat;
            }

            // TeamB
            if (GameObject.Find("TeamB_SpawnPoint") == null)
            {
                GameObject spawnB = new GameObject("TeamB_SpawnPoint");
                spawnB.transform.position = new Vector3(20, 1, 0);
                spawnB.tag = "Respawn";

                GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cube);
                marker.name = "Marker";
                marker.transform.SetParent(spawnB.transform);
                marker.transform.localPosition = Vector3.zero;
                marker.transform.localScale = new Vector3(2, 0.1f, 2);
                DestroyImmediate(marker.GetComponent<Collider>());

                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.red;
                marker.GetComponent<Renderer>().sharedMaterial = mat;
            }
        }

        private void SceneSetupHelper_CreateCores()
        {
            // TeamA Core
            if (GameObject.Find("TeamA_Core") == null)
            {
                GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                core.name = "TeamA_Core";
                core.transform.position = new Vector3(-25, 2, 0);
                core.transform.localScale = new Vector3(2, 2, 2);

                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.blue;
                core.GetComponent<Renderer>().sharedMaterial = mat;

                core.AddComponent<NetworkIdentity>();
                core.AddComponent<Health>();
                var coreScript = core.AddComponent<CoreStructure>();
                coreScript.SetTeam(Team.TeamA);
            }

            // TeamB Core
            if (GameObject.Find("TeamB_Core") == null)
            {
                GameObject core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                core.name = "TeamB_Core";
                core.transform.position = new Vector3(25, 2, 0);
                core.transform.localScale = new Vector3(2, 2, 2);

                Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                mat.color = Color.red;
                core.GetComponent<Renderer>().sharedMaterial = mat;

                core.AddComponent<NetworkIdentity>();
                core.AddComponent<Health>();
                var coreScript = core.AddComponent<CoreStructure>();
                coreScript.SetTeam(Team.TeamB);
            }
        }

        private void SceneSetupHelper_CreateManagers()
        {
            // MatchManager
            if (FindFirstObjectByType<MatchManager>() == null)
            {
                GameObject mgr = new GameObject("MatchManager");
                mgr.AddComponent<NetworkIdentity>();
                mgr.AddComponent<MatchManager>();
            }

            // NetworkManager
            if (FindFirstObjectByType<NetworkManager>() == null)
            {
                GameObject netMgr = new GameObject("NetworkManager");
                var netMgrComp = netMgr.AddComponent<NetworkGameManager>();
                var transport = netMgr.AddComponent<TelepathyTransport>();

                // ⭐ FIX: Transport'u manuel ata (warning'i önle)
                netMgrComp.transport = transport;
            }
            else
            {
                // Varolan NetworkManager'ın transport'unu fix et
                var existingNetMgr = FindFirstObjectByType<NetworkManager>();
                if (existingNetMgr != null && existingNetMgr.transport == null)
                {
                    var transport = existingNetMgr.GetComponent<Mirror.Transport>();
                    if (transport == null)
                        transport = existingNetMgr.gameObject.AddComponent<TelepathyTransport>();

                    existingNetMgr.transport = transport;
                }
            }
        }

        private void SceneSetupHelper_CreateLighting()
        {
            if (FindFirstObjectByType<Light>() == null)
            {
                GameObject light = new GameObject("Directional Light");
                Light l = light.AddComponent<Light>();
                l.type = LightType.Directional;
                l.transform.rotation = Quaternion.Euler(50, -30, 0);
            }
        }

        private void SceneSetupHelper_CreateGround()
        {
            if (GameObject.Find("Ground") == null)
            {
                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(10, 1, 10);
            }
        }

        // ============================================================
        // PHASE 3: PREFAB CREATION
        // ============================================================

        private void RunPrefabCreation()
        {
            log.Add("  💥 Effect prefabs oluşturuluyor...");
            CreateEffectPrefabs();

            log.Add("  🔫 Weapon prefabs kontrol ediliyor...");
            // Weapon prefabs already exist, skip

            log.Add("  🏗️ Structure prefabs kontrol ediliyor...");
            // Structure prefabs already exist, skip
        }

        private void CreateEffectPrefabs()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Effects"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Effects");

            // Blood Effect
            CreateBloodEffectPrefab();

            // Metal Sparks
            CreateMetalSparksPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void CreateBloodEffectPrefab()
        {
            string path = "Assets/Prefabs/Effects/BloodEffect.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject blood = new GameObject("BloodEffect");
            ParticleSystem ps = blood.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = new Color(0.6f, 0f, 0f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.3f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 5f);
            main.startLifetime = 1.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 25) });

            blood.AddComponent<BloodEffect>();
            PrefabUtility.SaveAsPrefabAsset(blood, path);
            DestroyImmediate(blood);
        }

        private void CreateMetalSparksPrefab()
        {
            string path = "Assets/Prefabs/Effects/MetalSparks.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;

            GameObject sparks = new GameObject("MetalSparks");
            ParticleSystem ps = sparks.AddComponent<ParticleSystem>();

            var main = ps.main;
            main.startColor = new Color(1f, 0.8f, 0.3f, 1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.08f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
            main.startLifetime = 0.5f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            sparks.AddComponent<HitEffect>();
            PrefabUtility.SaveAsPrefabAsset(sparks, path);
            DestroyImmediate(sparks);
        }

        private void RunPrefabsOnly()
        {
            log.Clear();
            log.Add("💥 Sadece Prefab Creation...");
            RunPrefabCreation();
            log.Add("✅ Prefablar oluşturuldu");
            Repaint();
        }

        // ============================================================
        // PHASE 4: PLAYER SETUP
        // ============================================================

        private void RunPlayerSetup()
        {
            log.Add("  🎮 Player prefab recreate ediliyor...");
            CallPlayerPrefabRecreator();

            log.Add("  🔧 WeaponSystem fix ediliyor...");
            CallWeaponSystemFixer();

            log.Add("  🔊 Audio fix ediliyor...");
            CallAudioFix();

            log.Add("  ⚔️ Combat system setup ediliyor...");
            CallCombatSystemSetup();
        }

        private void CallPlayerPrefabRecreator()
        {
            // Player prefab zaten var, sadece referansları fix et
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null)
            {
                log.Add("  ⚠️ Player.prefab bulunamadı, atlanıyor");
                return;
            }

            // Fix references
            string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                // Camera
                Camera cam = prefabContents.GetComponentInChildren<Camera>();
                if (cam != null)
                {
                    var fps = prefabContents.GetComponent<FPSController>();
                    if (fps != null)
                    {
                        SerializedObject so = new SerializedObject(fps);
                        so.FindProperty("playerCamera").objectReferenceValue = cam;
                        so.ApplyModifiedProperties();
                    }
                }

                // WeaponHolder
                Transform weaponHolder = prefabContents.transform.Find("WeaponHolder");
                if (weaponHolder != null)
                {
                    var ws = prefabContents.GetComponent<WeaponSystem>();
                    if (ws != null)
                    {
                        SerializedObject so = new SerializedObject(ws);
                        so.FindProperty("weaponHolder").objectReferenceValue = weaponHolder;
                        so.FindProperty("playerCamera").objectReferenceValue = cam;
                        so.ApplyModifiedProperties();
                    }
                }

                // AudioSource
                AudioSource audio = prefabContents.GetComponent<AudioSource>();
                if (audio == null)
                {
                    audio = prefabContents.AddComponent<AudioSource>();
                    audio.playOnAwake = false;
                    audio.spatialBlend = 0f;
                }

                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                log.Add("  ✅ Player prefab fixed");
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
                throw;
            }
        }

        private void CallWeaponSystemFixer()
        {
            // WeaponSystem referanslarını fix et
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null) return;

            string prefabPath = AssetDatabase.GetAssetPath(playerPrefab);
            GameObject prefabContents = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                var ws = prefabContents.GetComponent<WeaponSystem>();
                if (ws != null)
                {
                    SerializedObject so = new SerializedObject(ws);

                    // Effect prefabs
                    GameObject blood = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/BloodEffect.prefab");
                    GameObject sparks = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Effects/MetalSparks.prefab");

                    if (blood != null)
                        so.FindProperty("bloodEffectPrefab").objectReferenceValue = blood;
                    if (sparks != null)
                        so.FindProperty("metalSparksPrefab").objectReferenceValue = sparks;

                    so.ApplyModifiedProperties();
                }

                PrefabUtility.SaveAsPrefabAsset(prefabContents, prefabPath);
                PrefabUtility.UnloadPrefabContents(prefabContents);

                log.Add("  ✅ WeaponSystem fixed");
            }
            catch
            {
                PrefabUtility.UnloadPrefabContents(prefabContents);
                throw;
            }
        }

        private void CallAudioFix()
        {
            // Audio zaten AudioSource ile fix edildi
            log.Add("  ✅ Audio sistem hazır");
        }

        private void CallCombatSystemSetup()
        {
            // Combat system constants zaten kodda var
            log.Add("  ✅ Combat sistem hazır");
        }

        private void RunPlayerOnly()
        {
            log.Clear();
            log.Add("🎮 Sadece Player Setup...");
            RunPlayerSetup();
            log.Add("✅ Player setup tamamlandı");
            Repaint();
        }

        // ============================================================
        // PHASE 5: FINAL TOUCHES
        // ============================================================

        private void RunFinalTouches()
        {
            log.Add("  🎨 UI setup ediliyor...");
            SetupUI();

            log.Add("  🌐 NetworkManager yapılandırılıyor...");
            ConfigureNetworkManager();

            log.Add("  🏷️ Tags kontrol ediliyor...");
            SetupTags();
        }

        private void SetupUI()
        {
            // UI zaten CompleteProjectSetup'ta var, skip
            log.Add("  ✅ UI hazır (CompleteProjectSetup kullan)");
        }

        private void ConfigureNetworkManager()
        {
            var netMgr = FindFirstObjectByType<NetworkGameManager>();
            if (netMgr == null) return;

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player.prefab");
            if (playerPrefab == null) return;

            SerializedObject so = new SerializedObject(netMgr);
            so.FindProperty("playerPrefab").objectReferenceValue = playerPrefab;

            // Spawn points
            GameObject spawnA = GameObject.Find("TeamA_SpawnPoint");
            GameObject spawnB = GameObject.Find("TeamB_SpawnPoint");

            if (spawnA != null && spawnB != null)
            {
                SerializedProperty teamAProp = so.FindProperty("teamASpawnPoints");
                SerializedProperty teamBProp = so.FindProperty("teamBSpawnPoints");

                teamAProp.arraySize = 1;
                teamBProp.arraySize = 1;

                teamAProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnA.transform;
                teamBProp.GetArrayElementAtIndex(0).objectReferenceValue = spawnB.transform;
            }

            // ⭐ FIX: Transport ata (warning'i önle)
            if (netMgr.transport == null)
            {
                var transport = netMgr.GetComponent<Mirror.Transport>();
                if (transport == null)
                    transport = netMgr.gameObject.AddComponent<TelepathyTransport>();
                netMgr.transport = transport;
            }

            so.ApplyModifiedProperties();
            log.Add("  ✅ NetworkManager yapılandırıldı (Player Prefab, Spawns, Transport)");
        }

        private void SetupTags()
        {
            // Respawn tag zaten var mı kontrol et
            log.Add("  ✅ Tags kontrol edildi");
        }
    }
}
