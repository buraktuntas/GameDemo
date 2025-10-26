using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;
using TacticalCombat.Combat;

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
                validPlacementMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                validPlacementMaterial.color = new Color(0, 1, 0, 0.5f);
            }
            
            if (invalidPlacementMaterial == null)
            {
                invalidPlacementMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                invalidPlacementMaterial.color = new Color(1, 0, 0, 0.5f);
            }
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
                    UpdateGhostPreview();
                    lastUpdateTime = Time.time;
                }
            }
        }
        
        private void HandleBuildModeToggle()
        {
            if (Input.GetKeyDown(buildModeKey))
            {
                Debug.Log($"🏗️ [SimpleBuildMode] B key pressed - Current state: {isBuildModeActive}");
                
                if (isBuildModeActive)
                {
                    ExitBuildMode();
                }
                else
                {
                    EnterBuildMode();
                }
            }
            
            // ESC also exits build mode
            if (isBuildModeActive && Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
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
        
        private void HandlePlacement()
        {
            // ✅ FIX: Sadece build modunda placement'e izin ver
            if (!isBuildModeActive) return;
            
            if (Input.GetMouseButtonDown(0) && canPlace)
            {
                PlaceStructure();
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
            
            for (int i = 0; i < ghostRenderers.Length; i++)
            {
                ghostOriginalMaterials[i] = ghostRenderers[i].material;
                ghostRenderers[i].material = validPlacementMaterial;
            }
            
            lastCanPlaceState = true;
            lastStabilityColor = Color.clear;
        }
        
        private void DestroyGhostPreview()
        {
            if (ghostPreview != null)
            {
                Destroy(ghostPreview);
                ghostPreview = null;
                ghostRenderers = null;
                ghostOriginalMaterials = null;
            }
        }
        
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
                
                canPlace = IsValidPlacement(placementPosition, placementRotation);
                
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
            if (ghostRenderers == null) return;
            
            if (canPlace)
            {
                Color stabilityColor = showStabilityPreview 
                    ? GetStabilityPreviewColor(placementPosition) 
                    : validPlacementMaterial.color;
                
                if (lastCanPlaceState != canPlace || !ColorsEqual(lastStabilityColor, stabilityColor))
                {
                    foreach (var renderer in ghostRenderers)
                    {
                        Material mat = new Material(validPlacementMaterial);
                        mat.color = stabilityColor;
                        renderer.material = mat;
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
                        renderer.material = invalidPlacementMaterial;
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
            Collider[] overlaps = Physics.OverlapBox(position, boxSize / 2f, rotation);
            
            foreach (var overlap in overlaps)
            {
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
        
        [Command]
        private void CmdPlaceStructure(Vector3 position, Quaternion rotation, int structureIndex)
        {
            // Distance check
            float distance = Vector3.Distance(transform.position, position);
            if (distance > placementDistance + 0.5f)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode] Invalid placement distance: {distance}m");
                return;
            }
            
            // Line of sight check
            Vector3 playerEye = transform.position + Vector3.up * 1.6f;
            if (Physics.Linecast(playerEye, position, out RaycastHit hit, obstacleLayer))
            {
                if (Vector3.Distance(hit.point, position) > 0.5f)
                {
                    Debug.LogWarning($"🚨 [SimpleBuildMode] No line of sight");
                    return;
                }
            }
            
            // Ground check
            if (!Physics.Raycast(position, Vector3.down, 2f, groundLayer))
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode] Not on ground");
                return;
            }
            
            // Structure index validation
            if (availableStructures == null || structureIndex >= availableStructures.Length)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode] Invalid structure index: {structureIndex}");
                return;
            }
            
            GameObject selectedStructure = availableStructures[structureIndex];
            if (selectedStructure == null)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode] Selected structure is null");
                return;
            }
            
            // Overlap check
            Collider[] overlaps = Physics.OverlapBox(
                position,
                new Vector3(0.9f, 0.45f, 0.09f),
                rotation,
                obstacleLayer
            );
            
            if (overlaps.Length > 0)
            {
                Debug.LogWarning($"🚨 [SimpleBuildMode] Overlapping with {overlaps[0].name}");
                return;
            }
            
            // Spawn structure
            GameObject structure = Instantiate(selectedStructure, position, rotation);
            NetworkServer.Spawn(structure);
            
            Debug.Log($"✅ [SimpleBuildMode] {selectedStructure.name} placed at {position}");
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
            Collider[] nearbyStructures = Physics.OverlapSphere(position, maxSupportDistance);
            
            float nearestDistance = float.MaxValue;
            bool foundSupport = false;
            
            foreach (var col in nearbyStructures)
            {
                Structure structure = col.GetComponent<Structure>();
                if (structure == null) continue;
                
                StructuralIntegrity integrity = structure.GetComponent<StructuralIntegrity>();
                if (integrity == null) continue;
                
                if (integrity.IsGrounded() || integrity.GetCurrentStability() > 50f)
                {
                    float distance = Vector3.Distance(position, col.transform.position);
                    nearestDistance = Mathf.Min(nearestDistance, distance);
                    foundSupport = true;
                }
            }
            
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