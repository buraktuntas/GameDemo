using UnityEngine;
using Unity.Profiling;
using Mirror;
using UnityEngine.InputSystem;
using TacticalCombat.Core;
using TacticalCombat.Player;
using TacticalCombat.Combat;
using System.Collections.Generic;

namespace TacticalCombat.Building
{
    /// <summary>
    /// ✅ BUG FIX VERSION - Input çakışması çözüldü
    /// Build moduna girişte silah devre dışı kalıyor
    /// </summary>
    public class SimpleBuildMode : NetworkBehaviour
    {
        [Header("Prefabs")]
        public GameObject wallPrefab;
        public GameObject floorPrefab;
        public GameObject roofPrefab;
        public GameObject doorPrefab;
        public GameObject windowPrefab;
        public GameObject stairsPrefab;
        
        [Header("Materials")]
        public Material validPlacementMaterial;
        public Material invalidPlacementMaterial;
        
        [Header("Settings")]
        public LayerMask groundLayer;
        public LayerMask obstacleLayer;
        [SerializeField] private LayerMask structureLayer; // for stability/support checks
        public float placementDistance = 5f;
        public float rotationSpeed = 90f;
        public Key buildModeKey = Key.B;
        public Key rotateKey = Key.R;
        public Key cycleStructureKey = Key.Tab;
        public float gridSize = 1f;
        
        [Header("Build Mode Behavior")]
        public bool allowCameraInBuildMode = true;
        public bool allowMovementInBuildMode = true;
        
        [Header("Structural Integrity Preview")]
        [SerializeField] private bool showStabilityPreview = true;
        [SerializeField] private float maxSupportDistance = 10f;

        // ✅ REMOVED: Structure costs are now managed by StructureDatabase
        // Old fields (kept for reference, but no longer used):
        // - wallCost, floorCost, roofCost, doorCost, windowCost, stairsCost
        // Use StructureDatabase.Instance.GetCost(StructureType) instead
        
        // ✅ FIX: Weapon system reference
        private WeaponSystem weaponSystem;
        
        // ✅ FIX: InputManager reference - her player'ın kendi InputManager'ı var
        private TacticalCombat.Player.InputManager inputManager;
        
        // State
        private bool isBuildModeActive = false;
        private GameObject ghostPreview;
        private Camera playerCamera;
        private bool canPlace = false;
        private Vector3 placementPosition;
        private Quaternion placementRotation;
        private float currentRotationY = 0f;
        private Material lastMaterial;
        
        // Performance optimization
        private Renderer[] ghostRenderers;
        private Material[] ghostOriginalMaterials;
        private bool lastCanPlaceState = false;
        private Color lastStabilityColor;

        // ✅ FIX: Material pooling to prevent memory leak
        private Material[] ghostMaterialInstances; // One material per renderer
        
        [Header("Structure Selection")]
        public int currentStructureIndex = 0;
        private GameObject[] availableStructures;
        
        // Performance throttling
        private float lastUpdateTime;
        private const float UPDATE_INTERVAL = 0.05f;

        // Stability caching
        private float lastStabilityCheckTime;
        private Color cachedStabilityColor;
        private Vector3 lastStabilityPosition;

        // ✅ FIX: Toggle cooldown and reentrant guard to prevent race condition
        private float lastToggleTime = 0f;
        private const float TOGGLE_COOLDOWN = 0.3f;
        private bool isTogglingBuildMode = false; // Prevents recursive toggle
        
        // ✅ FIX: Track initialization state
        private bool isInitialized = false;

        // NonAlloc buffers
        private static readonly Collider[] stabilityBuffer = new Collider[256];
        private static readonly Collider[] overlapBoxBuffer = new Collider[64];

        // Profiling
        private static readonly ProfilerMarker marker_UpdateGhostPreview = new ProfilerMarker("Build.Simple.UpdateGhostPreview");
        private static readonly ProfilerMarker marker_FindSupport = new ProfilerMarker("Build.Simple.FindNearestSupport");
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            Debug.Log($"[SimpleBuildMode] OnStartLocalPlayer() called - GameObject: {gameObject.name}");
            InitializeBuildMode();
        }
        
        private void Start()
        {
            // ✅ DEBUG: Always log Start() to verify component is active
            Debug.Log($"[SimpleBuildMode] Start() called - isLocalPlayer: {isLocalPlayer}, GameObject: {gameObject.name}");
            
            // ✅ FIX: Don't initialize here - wait for OnStartLocalPlayer()
            // NetworkBehaviour.isLocalPlayer is only set after OnStartLocalPlayer() is called
            if (isLocalPlayer)
            {
                InitializeBuildMode();
            }
            else
            {
                Debug.LogWarning($"[SimpleBuildMode] Not local player in Start() - will wait for OnStartLocalPlayer()");
            }
        }
        
        private void InitializeBuildMode()
        {
            // ✅ FIX: Prevent double initialization using flag
            if (isInitialized)
            {
                Debug.LogWarning("[SimpleBuildMode] Already initialized - skipping");
                return;
            }
            
            Debug.Log("[SimpleBuildMode] Initializing build mode...");
            
            // ✅ FIX: Get weapon system reference
            weaponSystem = GetComponent<WeaponSystem>();
            if (weaponSystem == null)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] WeaponSystem not found!");
            }
            
            // ✅ FIX: Get InputManager reference - her player'ın kendi InputManager'ı var
            inputManager = GetComponent<TacticalCombat.Player.InputManager>();
            if (inputManager == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [SimpleBuildMode] InputManager not found! Creating one...");
                #endif
                inputManager = gameObject.AddComponent<TacticalCombat.Player.InputManager>();
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("✅ [SimpleBuildMode] InputManager created and assigned");
                #endif
            }
            
            // ✅ PERFORMANCE FIX: Get camera from FPSController (NEVER use Camera.main)
            var fpsController = GetComponent<FPSController>();
            if (fpsController != null)
            {
                playerCamera = fpsController.GetCamera();
            }

            if (playerCamera == null)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] FPSController camera not found - trying child camera");
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (playerCamera == null)
            {
                Debug.LogError("❌ [SimpleBuildMode] No camera found! Camera.main usage is BANNED in Unity 6 for performance.");
            }
            
            if (structureLayer == 0)
            {
                structureLayer = LayerMask.GetMask("Structure");
            }

            var layerCfg = TacticalCombat.Core.LayerConfigProvider.Instance;
            if (structureLayer == 0 && layerCfg != null)
            {
                structureLayer = layerCfg.structureLayer;
            }

            CreateDefaultMaterials();
            InitializeAvailableStructures();
            
            // ✅ FIX: Send prefabs to BuildValidator on server
            if (isServer && BuildValidator.Instance != null)
            {
                BuildValidator.Instance.SetPrefabs(wallPrefab, floorPrefab, stairsPrefab);
            }
            
            // ✅ CRITICAL FIX: Mark as initialized AFTER everything is set up
            isInitialized = true;
            Debug.Log("[SimpleBuildMode] ✅ Build mode initialized successfully!");
        }
        
        private void InitializeAvailableStructures()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🏗️ [SimpleBuildMode] Initializing structures...");
            Debug.Log($"  Wall: {(wallPrefab != null ? wallPrefab.name : "NULL")}");
            Debug.Log($"  Floor: {(floorPrefab != null ? floorPrefab.name : "NULL")}");
            Debug.Log($"  Roof: {(roofPrefab != null ? roofPrefab.name : "NULL")}");
            Debug.Log($"  Door: {(doorPrefab != null ? doorPrefab.name : "NULL")}");
            Debug.Log($"  Window: {(windowPrefab != null ? windowPrefab.name : "NULL")}");
            Debug.Log($"  Stairs: {(stairsPrefab != null ? stairsPrefab.name : "NULL")}");
            #endif
            
            availableStructures = new GameObject[]
            {
                wallPrefab,
                floorPrefab,
                roofPrefab,
                doorPrefab,
                windowPrefab,
                stairsPrefab
            };
            
            // Filter null entries
            int validCount = 0;
            for (int i = 0; i < availableStructures.Length; i++)
            {
                if (availableStructures[i] != null)
                {
                    availableStructures[validCount] = availableStructures[i];
                    validCount++;
                }
            }
            
            System.Array.Resize(ref availableStructures, validCount);
            
            if (availableStructures.Length == 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [SimpleBuildMode] No structure prefabs assigned!");
                #endif
            }
            else
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"✅ [SimpleBuildMode] {availableStructures.Length} structure types loaded");
                #endif
            }
        }
        
        private void CreateDefaultMaterials()
        {
            if (validPlacementMaterial == null)
            {
                // ✅ FIX: Use URP/Lit shader for Unity 6 URP
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null)
                {
                    // Fallback to Standard if URP not available
                    Debug.LogWarning("⚠️ [SimpleBuildMode] URP shader not found, using Standard");
                    urpShader = Shader.Find("Standard");
                }

                validPlacementMaterial = new Material(urpShader);
                validPlacementMaterial.color = new Color(0, 1, 0, 0.7f); // Green with alpha

                // URP transparency setup
                validPlacementMaterial.SetFloat("_Surface", 1f); // 0 = Opaque, 1 = Transparent
                validPlacementMaterial.SetFloat("_Blend", 0f); // 0 = Alpha, 1 = Premultiply
                validPlacementMaterial.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                validPlacementMaterial.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                validPlacementMaterial.SetFloat("_ZWrite", 0f);
                validPlacementMaterial.SetFloat("_AlphaClip", 0f);
                validPlacementMaterial.renderQueue = 3000;
                validPlacementMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                validPlacementMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            if (invalidPlacementMaterial == null)
            {
                // ✅ FIX: Use URP/Lit shader for Unity 6 URP
                Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
                if (urpShader == null)
                {
                    urpShader = Shader.Find("Standard");
                }

                invalidPlacementMaterial = new Material(urpShader);
                invalidPlacementMaterial.color = new Color(1, 0, 0, 0.7f); // Red with alpha

                // URP transparency setup
                invalidPlacementMaterial.SetFloat("_Surface", 1f);
                invalidPlacementMaterial.SetFloat("_Blend", 0f);
                invalidPlacementMaterial.SetFloat("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                invalidPlacementMaterial.SetFloat("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                invalidPlacementMaterial.SetFloat("_ZWrite", 0f);
                invalidPlacementMaterial.SetFloat("_AlphaClip", 0f);
                invalidPlacementMaterial.renderQueue = 3000;
                invalidPlacementMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                invalidPlacementMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("✅ [SimpleBuildMode] URP materials created successfully");
            #endif
        }
        
        private void Update()
        {
            // ✅ DEBUG: Always log first 20 frames to verify Update() is being called
            if (Time.frameCount <= 20)
            {
                Debug.Log($"[SimpleBuildMode] Update() frame {Time.frameCount} - isLocalPlayer: {isLocalPlayer}, isInitialized: {isInitialized}, GameObject: {gameObject.name}, enabled: {enabled}");
            }
            
            // ✅ CRITICAL FIX: Check isLocalPlayer - it should be true after OnStartLocalPlayer()
            if (!isLocalPlayer)
            {
                if (Time.frameCount <= 20)
                {
                    Debug.LogWarning($"[SimpleBuildMode] Update() skipped - not local player: {gameObject.name}");
                }
                return;
            }
            
            // ✅ CRITICAL FIX: Ensure initialization happened
            if (!isInitialized)
            {
                if (Time.frameCount <= 20)
                {
                    Debug.LogWarning("[SimpleBuildMode] Not initialized yet - calling InitializeBuildMode()");
                }
                InitializeBuildMode();
                return; // Skip this frame, wait for next frame after initialization
            }
            
            HandleBuildModeToggle();
            
            if (isBuildModeActive)
            {
                HandleStructureSelection();
                HandleRotation();
                HandlePlacement();
                
                // Throttled update
                if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
                {
                    marker_UpdateGhostPreview.Begin();
                    UpdateGhostPreview();
                    marker_UpdateGhostPreview.End();
                    lastUpdateTime = Time.time;
                }
            }
        }
        
        private void HandleBuildModeToggle()
        {
            // ✅ FIX: Reentrant guard prevents state corruption from recursive calls
            if (isTogglingBuildMode)
            {
                return;
            }

            // ✅ FIX: Add cooldown to prevent rapid toggle causing state corruption
            // Check keyboard validity
            if (Keyboard.current == null)
            {
                return;
            }

            // ✅ CRITICAL FIX: Check for B key press DIRECTLY from InputSystem
            // This bypasses any InputManager blocking logic for the toggle itself
            bool togglePressed = Keyboard.current[buildModeKey].wasPressedThisFrame;
            
            // ESC also exits build mode
            bool escapePressed = Keyboard.current.escapeKey.wasPressedThisFrame;

            if ((togglePressed || (isBuildModeActive && escapePressed)) && Time.time - lastToggleTime > TOGGLE_COOLDOWN)
            {
                // ✅ CRITICAL: Only allow entering build mode in Build Phase (or if no MatchManager exists for testing)
                if (!isBuildModeActive && MatchManager.Instance != null)
                {
                    Phase currentPhase = MatchManager.Instance.GetCurrentPhase();
                    if (currentPhase != Phase.Build)
                    {
                        // Optional: Allow in Combat phase if design permits, but for now restrict to Build
                        // If user wants to build in combat, remove this check
                        #if UNITY_EDITOR || DEVELOPMENT_BUILD
                        Debug.Log($"⚠️ [SimpleBuildMode] Cannot enter build mode in {currentPhase} phase");
                        #endif
                        return;
                    }
                }

                isTogglingBuildMode = true;
                lastToggleTime = Time.time;
                
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log($"🏗️ [SimpleBuildMode] Toggle Input Detected! Current State: {isBuildModeActive}, Key: {(togglePressed ? "B" : "ESC")}");
                #endif

                try
                {
                    if (isBuildModeActive)
                    {
                        ExitBuildMode();
                    }
                    else
                    {
                        EnterBuildMode();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"❌ [SimpleBuildMode] Error toggling build mode: {e.Message}\n{e.StackTrace}");
                    // Force exit on error to prevent getting stuck
                    if (isBuildModeActive) ExitBuildMode();
                }
                finally
                {
                    isTogglingBuildMode = false;
                }
            }
        }
        
        private void HandleStructureSelection()
        {
            if (Keyboard.current != null && Keyboard.current[cycleStructureKey].wasPressedThisFrame)
            {
                CycleStructure();
            }
        }
        
        private void CycleStructure()
        {
            if (availableStructures == null || availableStructures.Length == 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [SimpleBuildMode] Cannot cycle - no structures available");
                #endif
                return;
            }
            
            currentStructureIndex = (currentStructureIndex + 1) % availableStructures.Length;
            
            if (ghostPreview != null)
            {
                DestroyGhostPreview();
                CreateGhostPreview();
            }
            else
            {
                // Update cost display if ghost already exists
                UpdateCostDisplay();
            }

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            string structureName = availableStructures[currentStructureIndex].name;
            Debug.Log($"🏗️ [SimpleBuildMode] Structure selected: {structureName} - Cost: {GetCurrentStructureCost()}₺");
            #endif
        }
        
        private void HandleRotation()
        {
            if (Keyboard.current != null && Keyboard.current[rotateKey].isPressed)
            {
                currentRotationY += rotationSpeed * Time.deltaTime;
                currentRotationY = Mathf.Repeat(currentRotationY, 360f);
            }
        }
        
        private float lastPlacementTime = 0f;
        private const float PLACEMENT_COOLDOWN = 0.3f;  // 300ms minimum between placements (prevents spam freeze)

        private void HandlePlacement()
        {
            // ✅ FIX: Sadece build modunda placement'e izin ver
            if (!isBuildModeActive) return;

            // ✅ PERFORMANCE FIX: Prevent rapid placement spam (freeze fix)
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && canPlace)
            {
                float timeSinceLastPlacement = Time.time - lastPlacementTime;
                if (timeSinceLastPlacement >= PLACEMENT_COOLDOWN)
                {
                    PlaceStructure();
                    lastPlacementTime = Time.time;
                }
                else
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.Log($"⏱️ [SimpleBuildMode] Placement on cooldown ({PLACEMENT_COOLDOWN - timeSinceLastPlacement:F2}s remaining)");
                    #endif
                }
            }
        }
        
        /// <summary>
        /// ✅ FIX: Build moduna giriş - silahı devre dışı bırak
        /// </summary>
        private void EnterBuildMode()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"🏗️ [SimpleBuildMode] Entering build mode...");
            #endif
            
            if (availableStructures == null || availableStructures.Length == 0)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning("⚠️ [SimpleBuildMode] No structures available!");
                #endif
                return;
            }
            
            isBuildModeActive = true;
            currentRotationY = 0f;
            
            // ✅ FIX: InputManager'ı override etme, kendi state'ini kullan
            if (inputManager != null)
            {
                inputManager.IsInBuildMode = true;
                inputManager.BlockShootInput = true; // ← Silah kullanımını engelle
                
                // Valheim tarzı: Hareket + Kamera çalışsın, ama cursor gizli olsun
                Cursor.lockState = CursorLockMode.Locked; // ← Cursor gizli
                Cursor.visible = false; // ← Cursor gizli
                inputManager.BlockCameraInput = false;  // ← Kamera çalışsın
                inputManager.BlockMovementInput = false; // ← Hareket çalışsın
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("🏗️ [SimpleBuildMode] Build mode: Valheim style (Movement + Camera + Hidden Cursor)");
                #endif
            }
            
            // ✅ FIX: Silahı devre dışı bırak
            if (weaponSystem != null)
            {
                weaponSystem.DisableWeapon();
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("🔫 [SimpleBuildMode] Weapon disabled");
                #endif
            }
            
            CreateGhostPreview();
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("🏗️ [SimpleBuildMode] BUILD MODE ACTIVE | Controls: LMB=Place | ESC=Exit | R=Rotate | Tab=Switch");
            #endif
        }
        
        /// <summary>
        /// ✅ FIX: Build modundan çıkış - silahı aktif et
        /// </summary>
        private void ExitBuildMode()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("❌ [SimpleBuildMode] Exiting build mode...");
            #endif
            
            isBuildModeActive = false;
            
            // ✅ FIX: InputManager restoration
            if (inputManager != null)
            {
                inputManager.IsInBuildMode = false;
                inputManager.BlockShootInput = false; // ← Silah kullanımını aç
                inputManager.SetCursorMode(InputManager.CursorMode.Locked);
            }
            
            // ✅ FIX: Silahı aktif et
            if (weaponSystem != null)
            {
                weaponSystem.EnableWeapon();
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.Log("✅ [SimpleBuildMode] Weapon enabled");
                #endif
            }
            
            DestroyGhostPreview();
            
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("✅ [SimpleBuildMode] BUILD MODE DEACTIVATED");
            #endif
        }
        
        private void CreateGhostPreview()
        {
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] No structures available for ghost!");
                return;
            }
            
            if (currentStructureIndex >= availableStructures.Length)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] Invalid structure index!");
                return;
            }
            
            GameObject selectedStructure = availableStructures[currentStructureIndex];
            if (selectedStructure == null)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] Selected structure is null!");
                return;
            }
            
            ghostPreview = Instantiate(selectedStructure);
            
            // Disable colliders
            foreach (var collider in ghostPreview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            
            // Cache renderers
            ghostRenderers = ghostPreview.GetComponentsInChildren<Renderer>();
            ghostOriginalMaterials = new Material[ghostRenderers.Length];

            // ✅ FIX: Create material instances ONCE to prevent memory leak
            ghostMaterialInstances = new Material[ghostRenderers.Length];

            for (int i = 0; i < ghostRenderers.Length; i++)
            {
                ghostOriginalMaterials[i] = ghostRenderers[i].sharedMaterial;

                // Create ONE material instance per renderer
                ghostMaterialInstances[i] = new Material(validPlacementMaterial);
                // ✅ FIX: Use sharedMaterial to avoid Unity creating instances
                ghostRenderers[i].sharedMaterial = ghostMaterialInstances[i];
            }

            lastCanPlaceState = true;
            lastStabilityColor = Color.clear;

            // Add cost display
            var costDisplay = ghostPreview.AddComponent<TacticalCombat.UI.BuildCostDisplay>();
            UpdateCostDisplay();
        }
        
        private void DestroyGhostPreview()
        {
            if (ghostPreview != null)
            {
                // ✅ FIX: Cleanup material instances to prevent memory leak
                if (ghostMaterialInstances != null)
                {
                    foreach (var mat in ghostMaterialInstances)
                    {
                        if (mat != null)
                        {
                            Destroy(mat);
                        }
                    }
                    ghostMaterialInstances = null;
                }

                Destroy(ghostPreview);
                ghostPreview = null;
                ghostRenderers = null;
                ghostOriginalMaterials = null;
            }
        }
        
        private Vector3 lastGhostPosition = Vector3.zero;
        private float lastGhostRotation = 0f;

        private void UpdateGhostPreview()
        {
            if (ghostPreview == null || playerCamera == null || Mouse.current == null) return;

            Ray ray = playerCamera.ScreenPointToRay(Mouse.current.position.ReadValue());

            if (Physics.Raycast(ray, out RaycastHit hit, placementDistance, groundLayer))
            {
                Vector3 snappedPosition = SnapToGrid(hit.point);
                snappedPosition.y = 0.5f;

                placementPosition = snappedPosition;
                placementRotation = Quaternion.Euler(0, currentRotationY, 0);

                ghostPreview.transform.position = placementPosition;
                ghostPreview.transform.rotation = placementRotation;

                // ✅ PERFORMANCE FIX: Only validate if position or rotation changed
                bool positionChanged = Vector3.Distance(lastGhostPosition, placementPosition) > 0.01f;
                bool rotationChanged = Mathf.Abs(lastGhostRotation - currentRotationY) > 1f;

                if (positionChanged || rotationChanged)
                {
                    canPlace = IsValidPlacement(placementPosition, placementRotation);
                    lastGhostPosition = placementPosition;
                    lastGhostRotation = currentRotationY;
                }

                UpdateGhostMaterials();

                if (!ghostPreview.activeSelf)
                {
                    ghostPreview.SetActive(true);
                }
            }
            else
            {
                if (ghostPreview.activeSelf)
                {
                    ghostPreview.SetActive(false);
                }
                canPlace = false;
            }
        }
        
        private void UpdateGhostMaterials()
        {
            if (ghostRenderers == null || ghostMaterialInstances == null) return;

            if (canPlace)
            {
                Color stabilityColor = showStabilityPreview
                    ? GetStabilityPreviewColor(placementPosition)
                    : validPlacementMaterial.color;

                // ✅ FIX: Only update colors if changed, DON'T create new materials!
                if (lastCanPlaceState != canPlace || !ColorsEqual(lastStabilityColor, stabilityColor))
                {
                    for (int i = 0; i < ghostRenderers.Length; i++)
                    {
                        // Reuse existing material instance, just change color
                        ghostMaterialInstances[i].color = stabilityColor;
                        // ✅ FIX: Use sharedMaterial to avoid creating new instances
                        ghostRenderers[i].sharedMaterial = ghostMaterialInstances[i];
                    }

                    lastStabilityColor = stabilityColor;
                }
            }
            else
            {
                if (lastCanPlaceState != canPlace)
                {
                    foreach (var renderer in ghostRenderers)
                    {
                        // ✅ FIX: Use sharedMaterial to avoid creating new instances
                        renderer.sharedMaterial = invalidPlacementMaterial;
                    }
                }
            }

            lastCanPlaceState = canPlace;
        }
        
        private bool ColorsEqual(Color a, Color b, float threshold = 0.01f)
        {
            return Mathf.Abs(a.r - b.r) < threshold &&
                   Mathf.Abs(a.g - b.g) < threshold &&
                   Mathf.Abs(a.b - b.b) < threshold &&
                   Mathf.Abs(a.a - b.a) < threshold;
        }
        
        private Vector3 SnapToGrid(Vector3 position)
        {
            float x = Mathf.Round(position.x / gridSize) * gridSize;
            float z = Mathf.Round(position.z / gridSize) * gridSize;
            return new Vector3(x, position.y, z);
        }
        
        private bool IsValidPlacement(Vector3 position, Quaternion rotation)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, position);
            if (distanceToPlayer > placementDistance)
                return false;
            
            Vector3 boxSize = new Vector3(1.8f, 0.9f, 0.18f);
            int overlapCountLocal = Physics.OverlapBoxNonAlloc(position, boxSize / 2f, overlapBoxBuffer, rotation, obstacleLayer, QueryTriggerInteraction.Ignore);
            
            for (int k = 0; k < overlapCountLocal; k++)
            {
                var overlap = overlapBoxBuffer[k];
                if (overlap == null) continue;
                if (overlap.transform == transform) continue;
                if (overlap.gameObject.layer == LayerMask.NameToLayer("Default")) continue;
                
                // ✅ HIGH PRIORITY: Use TryGetComponent instead of GetComponent (no GC allocation)
                if (overlap.TryGetComponent<Structure>(out _))
                    return false;
                
                if (overlap.TryGetComponent<NetworkIdentity>(out _))
                {
                    // ✅ HIGH PRIORITY: Use TryGetComponent instead of GetComponent
                    if (!overlap.TryGetComponent<PlayerController>(out _))
                        return false;
                }
            }
            
            return true;
        }
        
        private bool IsValidPlacement(Vector3 position, GameObject structure)
        {
            if (structure == null) return false;
            return IsValidPlacement(position, Quaternion.identity);
        }
        
        private void PlaceStructure()
        {
            if (availableStructures == null || currentStructureIndex >= availableStructures.Length) return;

            // ✅ CRITICAL FIX: Check affordability before sending placement command
            if (!CanAffordStructure())
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                int cost = GetCurrentStructureCost();
                Debug.LogWarning($"⚠️ [SimpleBuildMode] Cannot afford structure (Cost: {cost})");
                #endif

                // Show UI feedback to player
                var gameHUD = TacticalCombat.UI.GameHUD.Instance;
                if (gameHUD != null)
                {
                    gameHUD.ShowBuildFeedback(false, "Insufficient budget!");
                }
                return;
            }

            GameObject selectedStructure = availableStructures[currentStructureIndex];
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            string structureName = selectedStructure.name;
            Debug.Log($"🏗️ [SimpleBuildMode] Placing {structureName} at {placementPosition}");
            #endif
            CmdPlaceStructure(placementPosition, placementRotation, currentStructureIndex);
        }
        
        // ✅ CRITICAL FIX: Per-player rate limiting (prevents budget bypass exploit)
        private static Dictionary<ulong, Queue<float>> playerPlacementTimes = new Dictionary<ulong, Queue<float>>();
        private const float RATE_LIMIT_WINDOW = 5f; // 5 second window
        private const int MAX_PLACEMENTS_PER_WINDOW = 10; // Max 10 structures per 5 seconds
        private const float SERVER_PLACEMENT_COOLDOWN = 0.25f; // 250ms minimum between placements

        [Command]
        private void CmdPlaceStructure(Vector3 position, Quaternion rotation, int structureIndex)
        {
            // ✅ CRITICAL FIX: Per-player rate limiting (prevents spam exploit)
            if (!playerPlacementTimes.ContainsKey(netId))
            {
                playerPlacementTimes[netId] = new Queue<float>();
            }

            Queue<float> placementTimes = playerPlacementTimes[netId];

            // Remove old placements outside the time window
            while (placementTimes.Count > 0 && Time.time - placementTimes.Peek() > RATE_LIMIT_WINDOW)
            {
                placementTimes.Dequeue();
            }

            // Check rate limit
            if (placementTimes.Count >= MAX_PLACEMENTS_PER_WINDOW)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Player {netId} exceeded rate limit: {placementTimes.Count}/{MAX_PLACEMENTS_PER_WINDOW} in {RATE_LIMIT_WINDOW}s");
                #endif
                RpcPlacementRejected($"Rate limit: Max {MAX_PLACEMENTS_PER_WINDOW} placements per {RATE_LIMIT_WINDOW}s");
                return;
            }

            // Add this placement to history
            placementTimes.Enqueue(Time.time);

            // ✅ MEDIUM PRIORITY: Snap point validation (ensure client sent snapped position)
            Vector3 snappedPosition = SnapToGrid(position);
            if (Vector3.Distance(position, snappedPosition) > 0.1f)
            {
                // Client sent non-snapped position, use server's snap
                position = snappedPosition;
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Client sent non-snapped position, using server snap");
                #endif
            }

            // Distance check (quick validation before BuildValidator)
            float distance = Vector3.Distance(transform.position, position);
            if (distance > placementDistance + 0.5f)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Invalid placement distance: {distance}m");
                #endif
                RpcPlacementRejected("Placement distance too far");
                return;
            }

            // Structure index validation (cheap check first)
            if (availableStructures == null || structureIndex >= availableStructures.Length)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Invalid structure index: {structureIndex}");
                #endif
                RpcPlacementRejected("Invalid structure index");
                return;
            }

            GameObject selectedStructure = availableStructures[structureIndex];
            if (selectedStructure == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Selected structure is null");
                #endif
                RpcPlacementRejected("Selected structure is null");
                return;
            }

            // ✅ HIGH PRIORITY: Combat lockout check (prevent building during combat)
            var health = GetComponent<Combat.Health>();
            if (health != null && health.IsInCombat())
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode] Cannot build during combat (took damage {health.GetTimeSinceLastDamage():F1}s ago)");
                #endif
                RpcPlacementRejected("Cannot build during combat");
                return;
            }

            // ✅ CRITICAL FIX: Convert structure index to StructureType
            StructureType structureType = GetStructureTypeFromIndex(structureIndex);
            
            // ✅ CRITICAL FIX: Use BuildValidator for all placements (prevents budget bypass exploit)
            BuildValidator validator = BuildValidator.Instance;
            
            // Fallback: If singleton not initialized, try to find it
            if (validator == null)
            {
                validator = FindFirstObjectByType<BuildValidator>();
                if (validator != null)
                {
                    #if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogWarning("⚠️ [SimpleBuildMode] BuildValidator.Instance was null, but found via FindFirstObjectByType. Make sure BuildValidator GameObject exists in scene!");
                    #endif
                }
            }
            
            if (validator == null)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("❌ [SimpleBuildMode] BuildValidator not found! Please add BuildValidator GameObject to scene.");
                #endif
                RpcPlacementRejected("BuildValidator not found");
                return;
            }

            // ✅ FIX: Ensure prefabs are set in BuildValidator (lazy initialization)
            if (isServer && (validator.GetStructurePrefab(StructureType.WoodWall) == null || 
                           validator.GetStructurePrefab(StructureType.Platform) == null ||
                           validator.GetStructurePrefab(StructureType.Ramp) == null))
            {
                validator.SetPrefabs(wallPrefab, floorPrefab, stairsPrefab);
            }

            // Get player controller for team
            var playerController = GetComponent<PlayerController>();
            if (playerController == null)
                {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogError("❌ [SimpleBuildMode] PlayerController not found!");
                #endif
                RpcPlacementRejected("PlayerController not found");
                    return;
                }

            // Create BuildRequest
            BuildRequest request = new BuildRequest(position, rotation, structureType, netId);

            // Use BuildValidator (centralized validation + budget check)
            if (!validator.ValidateAndPlace(request, playerController.team))
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"🚨 [SimpleBuildMode] Placement rejected by BuildValidator");
                #endif
                RpcPlacementRejected("Validation failed");
                return;
            }

            // Success - structure spawned by BuildValidator
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [SimpleBuildMode SERVER] Structure placed at {position} via BuildValidator");
            #endif
        }

        /// <summary>
        /// ✅ CRITICAL FIX: Map structure index to StructureType
        /// </summary>
        private StructureType GetStructureTypeFromIndex(int index)
        {
            if (availableStructures == null || index >= availableStructures.Length || index < 0)
                return StructureType.WoodWall; // Default fallback

            GameObject structure = availableStructures[index];
            if (structure == null) return StructureType.WoodWall;

            // Map prefab name to StructureType
            string name = structure.name.ToLower();
            if (name.Contains("wall")) return StructureType.WoodWall; // Default to WoodWall
            if (name.Contains("metal")) return StructureType.MetalWall; // Metal wall
            if (name.Contains("floor") || name.Contains("platform")) return StructureType.Platform;
            if (name.Contains("roof")) return StructureType.Platform; // Roof is elevation
            if (name.Contains("door")) return StructureType.WoodWall; // Door is wall type (default to wood)
            if (name.Contains("window")) return StructureType.WoodWall; // Window is wall type (default to wood)
            if (name.Contains("stair") || name.Contains("ramp")) return StructureType.Ramp;

            return StructureType.WoodWall; // Default fallback
        }

        [ClientRpc]
        private void RpcPlacementRejected(string reason)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning($"🚨 [SimpleBuildMode CLIENT] Placement rejected: {reason}");
            #endif
            // TODO: Could show UI message to player
        }
        
        // Stability preview system
        private Color GetStabilityPreviewColor(Vector3 position)
        {
            if (Time.time - lastStabilityCheckTime < 0.1f && 
                Vector3.Distance(position, lastStabilityPosition) < 0.5f)
            {
                return cachedStabilityColor;
            }
            
            lastStabilityCheckTime = Time.time;
            lastStabilityPosition = position;
            
            if (IsGrounded(position))
            {
                cachedStabilityColor = new Color(0.3f, 0.6f, 1f, 0.5f);
                return cachedStabilityColor;
            }
            
            float supportDistance = FindNearestSupportDistance(position);
            
            if (supportDistance < 0 || supportDistance > maxSupportDistance)
            {
                cachedStabilityColor = new Color(1f, 0.2f, 0.2f, 0.5f);
                return cachedStabilityColor;
            }
            
            float stabilityPercent = 1f - (supportDistance / maxSupportDistance);
            
            if (stabilityPercent > 0.8f) cachedStabilityColor = new Color(0.3f, 0.6f, 1f, 0.5f);
            else if (stabilityPercent > 0.6f) cachedStabilityColor = new Color(0.3f, 1f, 0.3f, 0.5f);
            else if (stabilityPercent > 0.4f) cachedStabilityColor = new Color(1f, 1f, 0.3f, 0.5f);
            else if (stabilityPercent > 0.2f) cachedStabilityColor = new Color(1f, 0.6f, 0.2f, 0.5f);
            else cachedStabilityColor = new Color(1f, 0.2f, 0.2f, 0.5f);
            
            return cachedStabilityColor;
        }
        
        private bool IsGrounded(Vector3 position)
        {
            Vector3 origin = position;
            float checkDistance = 1.5f;
            
            return Physics.Raycast(origin, Vector3.down, checkDistance, groundLayer);
        }
        
        private float FindNearestSupportDistance(Vector3 position)
        {
            marker_FindSupport.Begin();
            int count = Physics.OverlapSphereNonAlloc(position, maxSupportDistance, stabilityBuffer, structureLayer, QueryTriggerInteraction.Ignore);
            
            float nearestDistance = float.MaxValue;
            bool foundSupport = false;
            
            for (int i = 0; i < count; i++)
            {
                var col = stabilityBuffer[i];
                if (col == null) continue;
                var structure = col.GetComponent<Structure>();
                if (structure == null) continue;
                
                var integrity = structure.GetComponent<StructuralIntegrity>();
                if (integrity == null) continue;
                
                if (integrity.IsGrounded() || integrity.GetCurrentStability() > 50f)
                {
                    float distance = Vector3.Distance(position, col.transform.position);
                    if (distance < nearestDistance)
                    {
                        nearestDistance = distance;
                        foundSupport = true;
                    }
                }
            }
            marker_FindSupport.End();
            return foundSupport ? nearestDistance : -1f;
        }
        
        private void OnDisable()
        {
            if (isBuildModeActive)
            {
                ExitBuildMode();
            }

            DestroyGhostPreview();
        }

        /// <summary>
        /// ✅ FIX: Cleanup on destroy to prevent memory leaks
        /// </summary>
        private void OnDestroy()
        {
            // Cleanup materials
            if (ghostMaterialInstances != null)
            {
                foreach (var mat in ghostMaterialInstances)
                {
                    if (mat != null)
                    {
                        Destroy(mat);
                    }
                }
                ghostMaterialInstances = null;
            }

            // Destroy ghost preview
            DestroyGhostPreview();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("🗑️ [SimpleBuildMode] Cleanup completed");
            #endif
        }

        public bool IsBuildModeActive() => isBuildModeActive;

        /// <summary>
        /// Get current structure cost
        /// </summary>
        private int GetCurrentStructureCost()
        {
            if (availableStructures == null || currentStructureIndex >= availableStructures.Length)
                return 0;

            StructureType type = GetStructureTypeFromIndex(currentStructureIndex);
            // ✅ FIX: Null check for StructureDatabase
            if (StructureDatabase.Instance != null)
            {
                return StructureDatabase.Instance.GetCost(type);
            }
            return 1; // Default fallback
        }

        /// <summary>
        /// Get current structure name
        /// </summary>
        private string GetCurrentStructureName()
        {
            if (availableStructures == null || currentStructureIndex >= availableStructures.Length)
                return "Structure";

            GameObject structure = availableStructures[currentStructureIndex];
            if (structure == null) return "Structure";

            // Clean up name (remove prefixes like "Wall_Prefab" -> "Wall")
            string name = structure.name.Replace("Prefab", "").Replace("_", " ").Trim();
            return name;
        }

        /// <summary>
        /// Check if player can afford current structure
        /// </summary>
        private bool CanAffordStructure()
        {
            // ✅ IMPLEMENTED: Get player budget from MatchManager
            var matchManager = MatchManager.Instance;
            if (matchManager == null) return false;

            var playerState = matchManager.GetPlayerState(netId);
            if (playerState == null) return false;

            int cost = GetCurrentStructureCost();
            StructureType structureType = GetStructureTypeFromIndex(currentStructureIndex);

            // Check budget category based on structure type
            switch (structureType)
            {
                case StructureType.WoodWall:
                case StructureType.MetalWall:
                case StructureType.CoreStructure:
                    return playerState.budget.wallPoints >= cost;

                case StructureType.Platform:
                case StructureType.Ramp:
                    return playerState.budget.elevationPoints >= cost;

                case StructureType.TrapSpike:
                case StructureType.TrapGlue:
                case StructureType.TrapSpringboard:
                case StructureType.TrapDartTurret:
                    return playerState.budget.trapPoints >= cost;

                case StructureType.UtilityGate:
                    return playerState.budget.utilityPoints >= cost;

                default:
                    return false;
            }
        }

        /// <summary>
        /// Update cost display on ghost preview
        /// </summary>
        private void UpdateCostDisplay()
        {
            if (ghostPreview == null) return;

            var costDisplay = ghostPreview.GetComponent<TacticalCombat.UI.BuildCostDisplay>();
            if (costDisplay != null)
            {
                string structureName = GetCurrentStructureName();
                int cost = GetCurrentStructureCost();
                bool canAfford = CanAffordStructure();

                costDisplay.UpdateCost(structureName, cost, canAfford);
            }
        }
        
        #if UNITY_EDITOR
        // ✅ FIX: Gizmos sadece Editor'da görünsün, oyun içinde görünmesin
        private void OnDrawGizmos()
        {
            // Sadece Scene View'da görünsün, Game View'da değil
            if (!UnityEditor.EditorApplication.isPlaying || !isBuildModeActive || ghostPreview == null) return;
            
            // ✅ FIX: Validate Quaternion before using it (prevents "Quaternion To Matrix conversion failed" error)
            Quaternion validRotation = placementRotation;
            if (validRotation.x == 0 && validRotation.y == 0 && validRotation.z == 0 && validRotation.w == 0)
            {
                validRotation = Quaternion.identity; // Use identity if invalid
            }
            
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(placementPosition, validRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.8f, 0.9f, 0.18f));
            
            if (showStabilityPreview)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                Gizmos.DrawWireSphere(placementPosition, maxSupportDistance);
            }
        }
        #endif
    }
}
