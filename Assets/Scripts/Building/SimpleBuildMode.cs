using UnityEngine;
using Unity.Profiling;
using Mirror;
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
        public KeyCode buildModeKey = KeyCode.B;
        public KeyCode rotateKey = KeyCode.R;
        public KeyCode cycleStructureKey = KeyCode.Tab;
        public float gridSize = 1f;
        
        [Header("Build Mode Behavior")]
        public bool allowCameraInBuildMode = true;
        public bool allowMovementInBuildMode = true;
        
        [Header("Structural Integrity Preview")]
        [SerializeField] private bool showStabilityPreview = true;
        [SerializeField] private float maxSupportDistance = 10f;
        
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

        // NonAlloc buffers
        private static readonly Collider[] stabilityBuffer = new Collider[256];
        private static readonly Collider[] overlapBoxBuffer = new Collider[64];

        // Profiling
        private static readonly ProfilerMarker marker_UpdateGhostPreview = new ProfilerMarker("Build.Simple.UpdateGhostPreview");
        private static readonly ProfilerMarker marker_FindSupport = new ProfilerMarker("Build.Simple.FindNearestSupport");
        
        private void Start()
        {
            if (!isLocalPlayer) return;
            
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
                Debug.LogWarning("⚠️ [SimpleBuildMode] InputManager not found! Creating one...");
                inputManager = gameObject.AddComponent<TacticalCombat.Player.InputManager>();
                Debug.Log("✅ [SimpleBuildMode] InputManager created and assigned");
            }
            
            // Get FPSController and camera
            var fpsController = GetComponent<FPSController>();
            if (fpsController != null)
            {
                playerCamera = fpsController.GetCamera();
            }
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }
            
            if (playerCamera == null)
            {
                Debug.LogError("❌ [SimpleBuildMode] Player camera not found!");
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
        }
        
        private void InitializeAvailableStructures()
        {
            Debug.Log($"🏗️ [SimpleBuildMode] Initializing structures...");
            Debug.Log($"  Wall: {(wallPrefab != null ? wallPrefab.name : "NULL")}");
            Debug.Log($"  Floor: {(floorPrefab != null ? floorPrefab.name : "NULL")}");
            Debug.Log($"  Roof: {(roofPrefab != null ? roofPrefab.name : "NULL")}");
            Debug.Log($"  Door: {(doorPrefab != null ? doorPrefab.name : "NULL")}");
            Debug.Log($"  Window: {(windowPrefab != null ? windowPrefab.name : "NULL")}");
            Debug.Log($"  Stairs: {(stairsPrefab != null ? stairsPrefab.name : "NULL")}");
            
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
                Debug.LogWarning("⚠️ [SimpleBuildMode] No structure prefabs assigned!");
            }
            else
            {
                Debug.Log($"✅ [SimpleBuildMode] {availableStructures.Length} structure types loaded");
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

            Debug.Log("✅ [SimpleBuildMode] URP materials created successfully");
        }
        
        private void Update()
        {
            if (!isLocalPlayer) return;
            
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
            if (Input.GetKeyDown(buildModeKey) && Time.time - lastToggleTime > TOGGLE_COOLDOWN)
            {
                isTogglingBuildMode = true;
                lastToggleTime = Time.time;
                Debug.Log($"🏗️ [SimpleBuildMode] B key pressed - Current state: {isBuildModeActive}");

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
                finally
                {
                    isTogglingBuildMode = false;
                }
            }

            // ESC also exits build mode (with same cooldown and guard)
            if (isBuildModeActive && Input.GetKeyDown(KeyCode.Escape) && Time.time - lastToggleTime > TOGGLE_COOLDOWN && !isTogglingBuildMode)
            {
                isTogglingBuildMode = true;
                lastToggleTime = Time.time;

                try
                {
                    ExitBuildMode();
                }
                finally
                {
                    isTogglingBuildMode = false;
                }
            }
        }
        
        private void HandleStructureSelection()
        {
            if (Input.GetKeyDown(cycleStructureKey))
            {
                CycleStructure();
            }
        }
        
        private void CycleStructure()
        {
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] Cannot cycle - no structures available");
                return;
            }
            
            currentStructureIndex = (currentStructureIndex + 1) % availableStructures.Length;
            
            if (ghostPreview != null)
            {
                DestroyGhostPreview();
                CreateGhostPreview();
            }
            
            string structureName = availableStructures[currentStructureIndex].name;
            Debug.Log($"🏗️ [SimpleBuildMode] Structure selected: {structureName}");
        }
        
        private void HandleRotation()
        {
            if (Input.GetKey(rotateKey))
            {
                currentRotationY += rotationSpeed * Time.deltaTime;
                currentRotationY = Mathf.Repeat(currentRotationY, 360f);
            }
        }
        
        private float lastPlacementTime = 0f;
        private const float PLACEMENT_COOLDOWN = 0.15f;  // 150ms minimum between placements

        private void HandlePlacement()
        {
            // ✅ FIX: Sadece build modunda placement'e izin ver
            if (!isBuildModeActive) return;

            // ✅ PERFORMANCE FIX: Prevent rapid placement spam (freeze fix)
            if (Input.GetMouseButtonDown(0) && canPlace)
            {
                float timeSinceLastPlacement = Time.time - lastPlacementTime;
                if (timeSinceLastPlacement >= PLACEMENT_COOLDOWN)
                {
                    PlaceStructure();
                    lastPlacementTime = Time.time;
                }
                else
                {
                    Debug.Log($"⏱️ [SimpleBuildMode] Placement on cooldown ({PLACEMENT_COOLDOWN - timeSinceLastPlacement:F2}s remaining)");
                }
            }
        }
        
        /// <summary>
        /// ✅ FIX: Build moduna giriş - silahı devre dışı bırak
        /// </summary>
        private void EnterBuildMode()
        {
            Debug.Log($"🏗️ [SimpleBuildMode] Entering build mode...");
            
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ [SimpleBuildMode] No structures available!");
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
                Debug.Log("🏗️ [SimpleBuildMode] Build mode: Valheim style (Movement + Camera + Hidden Cursor)");
            }
            
            // ✅ FIX: Silahı devre dışı bırak
            if (weaponSystem != null)
            {
                weaponSystem.DisableWeapon();
                Debug.Log("🔫 [SimpleBuildMode] Weapon disabled");
            }
            
            CreateGhostPreview();
            
            Debug.Log("🏗️ [SimpleBuildMode] BUILD MODE ACTIVE | Controls: LMB=Place | ESC=Exit | R=Rotate | Tab=Switch");
        }
        
        /// <summary>
        /// ✅ FIX: Build modundan çıkış - silahı aktif et
        /// </summary>
        private void ExitBuildMode()
        {
            Debug.Log("❌ [SimpleBuildMode] Exiting build mode...");
            
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
                Debug.Log("✅ [SimpleBuildMode] Weapon enabled");
            }
            
            DestroyGhostPreview();
            
            Debug.Log("✅ [SimpleBuildMode] BUILD MODE DEACTIVATED");
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
            if (ghostPreview == null || playerCamera == null) return;

            Ray ray = playerCamera.ScreenPointToRay(Input.mousePosition);

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
                
                if (overlap.GetComponent<Structure>() != null)
                    return false;
                
                if (overlap.GetComponent<NetworkIdentity>() != null)
                {
                    if (overlap.GetComponent<PlayerController>() == null)
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
            
            GameObject selectedStructure = availableStructures[currentStructureIndex];
            string structureName = selectedStructure.name;
            
            Debug.Log($"🏗️ [SimpleBuildMode] Placing {structureName} at {placementPosition}");
            CmdPlaceStructure(placementPosition, placementRotation, currentStructureIndex);
        }
        
        private float lastServerPlacementTime = 0f;
        private const float SERVER_PLACEMENT_COOLDOWN = 0.1f;  // Server-side cooldown

        [Command]
        private void CmdPlaceStructure(Vector3 position, Quaternion rotation, int structureIndex)
        {
            // ✅ ANTI-CHEAT: Server-side cooldown to prevent spam
            if (Time.time - lastServerPlacementTime < SERVER_PLACEMENT_COOLDOWN)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Rate limit exceeded from player {netId}");
                return;
            }
            lastServerPlacementTime = Time.time;

            // Distance check
            float distance = Vector3.Distance(transform.position, position);
            if (distance > placementDistance + 0.5f)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Invalid placement distance: {distance}m");
                return;
            }

            // Structure index validation (cheap check first)
            if (availableStructures == null || structureIndex >= availableStructures.Length)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Invalid structure index: {structureIndex}");
                return;
            }

            GameObject selectedStructure = availableStructures[structureIndex];
            if (selectedStructure == null)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Selected structure is null");
                return;
            }

            // Ground check
            if (!Physics.Raycast(position, Vector3.down, 2f, groundLayer))
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Not on ground");
                return;
            }

            // Overlap check (NonAlloc) - most expensive, do last
            int overlapCount = Physics.OverlapBoxNonAlloc(
                position,
                new Vector3(0.9f, 0.45f, 0.09f),
                overlapBoxBuffer,
                rotation,
                obstacleLayer,
                QueryTriggerInteraction.Ignore
            );
            if (overlapCount > 0)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] Overlapping with {overlapBoxBuffer[0].name}");
                return;
            }

            // Line of sight check (expensive, skip if other checks fail first)
            Vector3 playerEye = transform.position + Vector3.up * 1.6f;
            if (Physics.Linecast(playerEye, position, out RaycastHit hit, obstacleLayer, QueryTriggerInteraction.Ignore))
            {
                if (Vector3.Distance(hit.point, position) > 0.5f)
                {
                    Debug.LogWarning($"🚨 [SimpleBuildMode SERVER] No line of sight");
                    return;
                }
            }

            // Spawn structure
            GameObject structure = Instantiate(selectedStructure, position, rotation);
            NetworkServer.Spawn(structure);

            Debug.Log($"✅ [SimpleBuildMode SERVER] {selectedStructure.name} placed at {position}");
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

            Debug.Log("🗑️ [SimpleBuildMode] Cleanup completed");
        }

        public bool IsBuildModeActive() => isBuildModeActive;
        
        private void OnDrawGizmos()
        {
            if (!isBuildModeActive || ghostPreview == null) return;
            
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(placementPosition, placementRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.8f, 0.9f, 0.18f));
            
            if (showStabilityPreview)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                Gizmos.DrawWireSphere(placementPosition, maxSupportDistance);
            }
        }
    }
}
