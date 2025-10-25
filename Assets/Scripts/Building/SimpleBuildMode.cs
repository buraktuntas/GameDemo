using UnityEngine;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Valheim-style build system with structural integrity preview
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
        public Material validPlacementMaterial;   // Green ghost
        public Material invalidPlacementMaterial; // Red ghost
        
        [Header("Settings")]
        public LayerMask groundLayer;
        public float placementDistance = 5f;
        public float rotationSpeed = 90f; // Degrees per second
        public KeyCode buildModeKey = KeyCode.B;
        public KeyCode rotateKey = KeyCode.R;
        public KeyCode cycleStructureKey = KeyCode.Tab;
        public float gridSize = 1f;
        
        [Header("Build Mode Behavior")]
        [Tooltip("Valheim style: Camera rotates freely while building")]
        public bool allowCameraInBuildMode = true;
        [Tooltip("Fortnite style: Movement allowed while building")]
        public bool allowMovementInBuildMode = true; // Grid snapping size
        
        [Header("Structure Selection")]
        public int currentStructureIndex = 0;
        private GameObject[] availableStructures;
        
        
        [Header("Structural Integrity Preview")]
        [SerializeField] private bool showStabilityPreview = true;
        [SerializeField] private float maxSupportDistance = 10f;
        
        // State
        private bool isBuildModeActive = false;
        private GameObject ghostPreview;
        private Camera playerCamera;
        private bool canPlace = false;
        private Vector3 placementPosition;
        private Quaternion placementRotation;
        private float currentRotationY = 0f;
        private Material lastMaterial; // Cache material to avoid reassignment
        
        // ⭐ PERFORMANCE OPTIMIZATION: Cache renderers and materials
        private Renderer[] ghostRenderers;
        private Material[] ghostOriginalMaterials;
        private bool lastCanPlaceState = false;
        private Color lastStabilityColor;

        private void Start()
        {
            if (!isLocalPlayer) return;
            
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
                Debug.LogError("❌ Player camera not found!");
            }
            
            CreateDefaultMaterials();
            InitializeAvailableStructures();
        }
        
        private void InitializeAvailableStructures()
        {
            // Debug: Prefab durumlarını kontrol et
            Debug.Log($"🏗️ Prefab durumları:");
            Debug.Log($"  Wall: {(wallPrefab != null ? wallPrefab.name : "NULL")}");
            Debug.Log($"  Floor: {(floorPrefab != null ? floorPrefab.name : "NULL")}");
            Debug.Log($"  Roof: {(roofPrefab != null ? roofPrefab.name : "NULL")}");
            Debug.Log($"  Door: {(doorPrefab != null ? doorPrefab.name : "NULL")}");
            Debug.Log($"  Window: {(windowPrefab != null ? windowPrefab.name : "NULL")}");
            Debug.Log($"  Stairs: {(stairsPrefab != null ? stairsPrefab.name : "NULL")}");
            
            // Mevcut yapıları listele
            availableStructures = new GameObject[]
            {
                wallPrefab,
                floorPrefab,
                roofPrefab,
                doorPrefab,
                windowPrefab,
                stairsPrefab
            };
            
            // Null olanları filtrele
            int validCount = 0;
            for (int i = 0; i < availableStructures.Length; i++)
            {
                if (availableStructures[i] != null)
                {
                    availableStructures[validCount] = availableStructures[i];
                    validCount++;
                }
            }
            
            // Array'i yeniden boyutlandır
            System.Array.Resize(ref availableStructures, validCount);
            
            if (availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ Hiç yapı prefab'ı atanmamış!");
            }
            else
            {
                Debug.Log($"✅ {availableStructures.Length} yapı türü yüklendi");
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
            if (!isLocalPlayer) 
            {
                // Debug: isLocalPlayer false ise log at
                if (Time.frameCount % 300 == 0) // Her 5 saniyede bir
                {
                    Debug.Log($"🏗️ SimpleBuildMode Update - isLocalPlayer: {isLocalPlayer}");
                }
                return;
            }
            
            HandleBuildModeToggle();
            
            if (isBuildModeActive)
            {
                HandleStructureSelection();
                UpdateGhostPreview();
                HandleRotation();
                HandlePlacement();
            }
            
            // Debug: Tab tuşu her zaman çalışsın
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                Debug.Log("🏗️ Tab tuşu algılandı - Build mode: " + isBuildModeActive);
            }
        }
        
        private void HandleBuildModeToggle()
        {
            if (Input.GetKeyDown(buildModeKey))
            {
                Debug.Log($"🏗️ B tuşu basıldı - Mevcut durum: {isBuildModeActive}");
                
                if (isBuildModeActive)
                {
                    Debug.Log("🏗️ Build mode kapatılıyor...");
                    ExitBuildMode();
                }
                else
                {
                    Debug.Log("🏗️ Build mode açılıyor...");
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
                Debug.Log("🏗️ Tab tuşu basıldı - Yapı değiştiriliyor...");
                CycleStructure();
            }
        }
        
        private void CycleStructure()
        {
            if (availableStructures == null || availableStructures.Length == 0) 
            {
                Debug.LogWarning("⚠️ availableStructures is null or empty! Cannot cycle structures.");
                return;
            }
            
            Debug.Log($"🏗️ Önceki index: {currentStructureIndex}, Toplam yapı: {availableStructures.Length}");
            
            currentStructureIndex = (currentStructureIndex + 1) % availableStructures.Length;
            
            Debug.Log($"🏗️ Yeni index: {currentStructureIndex}");
            
            // Ghost preview'ı yeniden oluştur
            if (ghostPreview != null)
            {
                DestroyGhostPreview();
                CreateGhostPreview();
            }
            
            string structureName = availableStructures[currentStructureIndex].name;
            Debug.Log($"🏗️ Yapı seçildi: {structureName}");
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
            if (Input.GetMouseButtonDown(0) && canPlace) // Left click
            {
                PlaceStructure();
            }
        }

        private void EnterBuildMode()
        {
            Debug.Log($"🏗️ EnterBuildMode çağrıldı - wallPrefab: {(wallPrefab != null ? wallPrefab.name : "NULL")}");
            
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ No structures available!");
                return;
            }
            
            isBuildModeActive = true;
            currentRotationY = 0f;
            
            // ✅ Configure input based on build mode behavior
            if (InputManager.Instance != null)
            {
                InputManager.Instance.IsInBuildMode = true;
                
                // ⭐ KEY DECISION POINT
                if (allowCameraInBuildMode && allowMovementInBuildMode)
                {
                    // VALHEIM STYLE: Full movement + camera
                    Cursor.lockState = CursorLockMode.Confined;
                    Cursor.visible = true;
                    InputManager.Instance.BlockCameraInput = false;
                    InputManager.Instance.BlockMovementInput = false;
                    
                    Debug.Log("🏗️ BUILD MODE (Valheim): Movement + Camera enabled");
                }
                else if (allowMovementInBuildMode && !allowCameraInBuildMode)
                {
                    // FORTNITE STYLE: Movement but no camera
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    InputManager.Instance.BlockCameraInput = true;
                    InputManager.Instance.BlockMovementInput = false;
                    
                    Debug.Log("🏗️ BUILD MODE (Fortnite): Movement only");
                }
                else
                {
                    // STATIC BUILD: No movement, no camera
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                    InputManager.Instance.BlockCameraInput = true;
                    InputManager.Instance.BlockMovementInput = true;
                    
                    Debug.Log("🏗️ BUILD MODE (Static): No movement");
                }
            }
            
            CreateGhostPreview();
            
            Debug.Log("🏗️ BUILD MODE: ON | LMB=Place | ESC=Exit | R=Rotate | Tab=Change");
        }
        
        private void ExitBuildMode()
        {
            isBuildModeActive = false;
            
            // ✅ Restore FPS mode
            if (InputManager.Instance != null)
            {
                InputManager.Instance.IsInBuildMode = false;
                InputManager.Instance.SetCursorMode(InputManager.CursorMode.Locked);
            }
            
            DestroyGhostPreview();
            
            Debug.Log("❌ BUILD MODE: OFF");
        }

        private void CreateGhostPreview()
        {
            if (availableStructures == null || availableStructures.Length == 0)
            {
                Debug.LogWarning("⚠️ Hiç yapı prefab'ı atanmamış!");
                return;
            }
            
            if (currentStructureIndex >= availableStructures.Length)
            {
                Debug.LogWarning("⚠️ Geçersiz yapı indeksi!");
                return;
            }
            
            GameObject selectedStructure = availableStructures[currentStructureIndex];
            if (selectedStructure == null)
            {
                Debug.LogWarning("⚠️ Seçili yapı prefab'ı null!");
                return;
            }

            ghostPreview = Instantiate(selectedStructure);
            
            // Disable colliders
            foreach (var collider in ghostPreview.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            
            // ⭐ CACHE RENDERERS FOR PERFORMANCE
            ghostRenderers = ghostPreview.GetComponentsInChildren<Renderer>();
            ghostOriginalMaterials = new Material[ghostRenderers.Length];
            
            // Initialize with valid material
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
                // Grid snapping
                Vector3 snappedPosition = SnapToGrid(hit.point);
                snappedPosition.y = 0.5f; // Wall height/2
                
                placementPosition = snappedPosition;
                placementRotation = Quaternion.Euler(0, currentRotationY, 0);
                
                ghostPreview.transform.position = placementPosition;
                ghostPreview.transform.rotation = placementRotation;
                
                // Validation
                canPlace = IsValidPlacement(placementPosition, placementRotation);
                
                // ⭐ UPDATE MATERIALS ONLY IF STATE CHANGED
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
        
        /// <summary>
        /// ⭐ OPTIMIZED: Only update materials when state changes
        /// </summary>
        private void UpdateGhostMaterials()
        {
            if (ghostRenderers == null) return;
            
            if (canPlace)
            {
                // Get stability color
                Color stabilityColor = showStabilityPreview 
                    ? GetStabilityPreviewColor(placementPosition) 
                    : validPlacementMaterial.color;
                
                // ⭐ Only update if color changed
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
                // ⭐ Only update if state changed
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
            // Distance check
            float distanceToPlayer = Vector3.Distance(transform.position, position);
            if (distanceToPlayer > placementDistance)
                return false;
            
            // Overlap check
            Vector3 boxSize = new Vector3(1.8f, 0.9f, 0.18f);
            Collider[] overlaps = Physics.OverlapBox(position, boxSize / 2f, rotation);
            
            foreach (var overlap in overlaps)
            {
                if (overlap.transform == transform) continue;
                if (overlap.gameObject.layer == LayerMask.NameToLayer("Default")) continue;
                
                // Structure check
                if (overlap.GetComponent<Structure>() != null)
                    return false;
                
                // Networked object check
                if (overlap.GetComponent<NetworkIdentity>() != null)
                {
                    if (overlap.GetComponent<PlayerController>() == null)
                        return false;
                }
            }
            
            return true;
        }
        
        // Overload for GameObject parameter
        private bool IsValidPlacement(Vector3 position, GameObject structure)
        {
            if (structure == null) return false;
            
            // Use default rotation for GameObject check
            Quaternion rotation = Quaternion.identity;
            return IsValidPlacement(position, rotation);
        }

        private void PlaceStructure()
        {
            if (availableStructures == null || currentStructureIndex >= availableStructures.Length) return;
            
            GameObject selectedStructure = availableStructures[currentStructureIndex];
            string structureName = selectedStructure.name;
            
            Debug.Log($"🏗️ Placing {structureName} at {placementPosition}");
            CmdPlaceStructure(placementPosition, placementRotation, currentStructureIndex);
        }

        [Command]
        private void CmdPlaceStructure(Vector3 position, Quaternion rotation, int structureIndex)
        {
            // ⚠️ GÜVENLİK KONTROLÜ: Client pozisyon manipülasyonu önleme
            float distance = Vector3.Distance(transform.position, position);
            float maxBuildDistance = placementDistance + 1f; // Tolerance
            
            if (distance > maxBuildDistance)
            {
                Debug.LogWarning($"🚨 Invalid build position from {netId}: {distance}m > {maxBuildDistance}m");
                return;
            }
            
            if (availableStructures == null || structureIndex >= availableStructures.Length) return;
            
            GameObject selectedStructure = availableStructures[structureIndex];
            if (selectedStructure == null) return;

            // ⚠️ GÜVENLİK: Geçerli yerleştirme kontrolü
            if (!IsValidPlacement(position, selectedStructure))
            {
                Debug.LogWarning($"🚨 Invalid placement attempt from {netId} at {position}");
                return;
            }

            GameObject structure = Instantiate(selectedStructure, position, rotation);
            NetworkServer.Spawn(structure);
            
            Debug.Log($"✅ [SERVER] {selectedStructure.name} placed at {position} by {netId}");
        }

        // ═══════════════════════════════════════════════════════════
        // STRUCTURAL INTEGRITY PREVIEW SYSTEM
        // ═══════════════════════════════════════════════════════════
        
        // Performance optimization - cache stability calculations
        private float lastStabilityCheckTime;
        private Color cachedStabilityColor;
        private Vector3 lastStabilityPosition;
        
        /// <summary>
        /// Calculates stability color based on support distance
        /// Blue = 100% stable (grounded)
        /// Green = 80%+ stable
        /// Yellow = 60%+ stable
        /// Orange = 40%+ stable
        /// Red = <40% or no support
        /// </summary>
        private Color GetStabilityPreviewColor(Vector3 position)
        {
            // ⚡ PERFORMANCE: Cache stability calculations - 0.1 saniyede bir hesapla
            if (Time.time - lastStabilityCheckTime < 0.1f && 
                Vector3.Distance(position, lastStabilityPosition) < 0.5f)
            {
                return cachedStabilityColor;
            }
            
            lastStabilityCheckTime = Time.time;
            lastStabilityPosition = position;
            
            // Check if grounded
            if (IsGrounded(position))
            {
                cachedStabilityColor = new Color(0.3f, 0.6f, 1f, 0.5f); // Blue - 100% stable
                return cachedStabilityColor;
            }
            
            // Find nearest support
            float supportDistance = FindNearestSupportDistance(position);
            
            if (supportDistance < 0 || supportDistance > maxSupportDistance)
            {
                cachedStabilityColor = new Color(1f, 0.2f, 0.2f, 0.5f); // Red - will collapse
                return cachedStabilityColor;
            }
            
            // Calculate stability based on distance
            float stabilityPercent = 1f - (supportDistance / maxSupportDistance);
            
            if (stabilityPercent > 0.8f) cachedStabilityColor = new Color(0.3f, 0.6f, 1f, 0.5f);    // Blue
            else if (stabilityPercent > 0.6f) cachedStabilityColor = new Color(0.3f, 1f, 0.3f, 0.5f);    // Green
            else if (stabilityPercent > 0.4f) cachedStabilityColor = new Color(1f, 1f, 0.3f, 0.5f);      // Yellow
            else if (stabilityPercent > 0.2f) cachedStabilityColor = new Color(1f, 0.6f, 0.2f, 0.5f);    // Orange
            else cachedStabilityColor = new Color(1f, 0.2f, 0.2f, 0.5f);                                 // Red
            
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
                
                // Check if structure is stable enough to provide support
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
                InputManager.Instance?.ExitBuildMode();
            }
            
            DestroyGhostPreview();
        }
        
        // ═══════════════════════════════════════════════════════════
        // DEBUG GIZMOS
        // ═══════════════════════════════════════════════════════════
        
        private void OnDrawGizmos()
        {
            if (!isBuildModeActive || ghostPreview == null) return;
            
            // Draw placement box
            Gizmos.color = canPlace ? Color.green : Color.red;
            Gizmos.matrix = Matrix4x4.TRS(placementPosition, placementRotation, Vector3.one);
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(1.8f, 0.9f, 0.18f));
            
            // Draw support radius
            if (showStabilityPreview)
            {
                Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
                Gizmos.DrawWireSphere(placementPosition, maxSupportDistance);
            }
        }
    }
}