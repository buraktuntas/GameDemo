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
        
        [Header("Materials")]
        public Material validPlacementMaterial;   // Green ghost
        public Material invalidPlacementMaterial; // Red ghost
        
        [Header("Settings")]
        public LayerMask groundLayer;
        public float placementDistance = 5f;
        public float rotationSpeed = 90f; // Degrees per second
        public KeyCode buildModeKey = KeyCode.B;
        public KeyCode rotateKey = KeyCode.R;
        public float gridSize = 1f; // Grid snapping size
        
        
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
                UpdateGhostPreview();
                HandleRotation();
                HandlePlacement();
            }
        }
        
        private void HandleBuildModeToggle()
        {
            if (Input.GetKeyDown(buildModeKey))
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
            
            // ESC also exits build mode
            if (isBuildModeActive && Input.GetKeyDown(KeyCode.Escape))
            {
                ExitBuildMode();
            }
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
            if (wallPrefab == null)
            {
                Debug.LogError("❌ Wall prefab not assigned!");
                return;
            }
            
            isBuildModeActive = true;
            currentRotationY = 0f;
            
            Debug.Log("🏗️ BUILD MODE: ON | LMB=Place | ESC=Exit | R=Rotate");
            CreateGhostPreview();
            
            // InputManager'ın yeni API'sini kullan
            if (InputManager.Instance != null)
            {
                InputManager.Instance.EnterBuildMode();
            }
        }
        
        private void ExitBuildMode()
        {
            isBuildModeActive = false;
            
            Debug.Log("❌ BUILD MODE: OFF");
            DestroyGhostPreview();
            
            // InputManager'ın yeni API'sini kullan
            if (InputManager.Instance != null)
            {
                InputManager.Instance.ExitBuildMode();
            }
        }

        private void CreateGhostPreview()
        {
            if (wallPrefab == null)
            {
                Debug.LogWarning("⚠️ Wall prefab not assigned!");
                return;
            }

            ghostPreview = Instantiate(wallPrefab);
            
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

        private void PlaceStructure()
        {
            Debug.Log($"🏗️ Placing wall at {placementPosition}");
            CmdPlaceStructure(placementPosition, placementRotation);
        }

        [Command]
        private void CmdPlaceStructure(Vector3 position, Quaternion rotation)
        {
            if (wallPrefab == null) return;

            GameObject structure = Instantiate(wallPrefab, position, rotation);
            NetworkServer.Spawn(structure);
            
            Debug.Log($"✅ [SERVER] Wall placed at {position}");
        }

        // ═══════════════════════════════════════════════════════════
        // STRUCTURAL INTEGRITY PREVIEW SYSTEM
        // ═══════════════════════════════════════════════════════════
        
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
            // Check if grounded
            if (IsGrounded(position))
            {
                return new Color(0.3f, 0.6f, 1f, 0.5f); // Blue - 100% stable
            }
            
            // Find nearest support
            float supportDistance = FindNearestSupportDistance(position);
            
            if (supportDistance < 0 || supportDistance > maxSupportDistance)
            {
                return new Color(1f, 0.2f, 0.2f, 0.5f); // Red - will collapse
            }
            
            // Calculate stability based on distance
            float stabilityPercent = 1f - (supportDistance / maxSupportDistance);
            
            if (stabilityPercent > 0.8f) return new Color(0.3f, 0.6f, 1f, 0.5f);    // Blue
            if (stabilityPercent > 0.6f) return new Color(0.3f, 1f, 0.3f, 0.5f);    // Green
            if (stabilityPercent > 0.4f) return new Color(1f, 1f, 0.3f, 0.5f);      // Yellow
            if (stabilityPercent > 0.2f) return new Color(1f, 0.6f, 0.2f, 0.5f);    // Orange
            return new Color(1f, 0.2f, 0.2f, 0.5f);                                 // Red
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