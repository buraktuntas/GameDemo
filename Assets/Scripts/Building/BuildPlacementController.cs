using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Profiling;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;
using System.Collections.Generic;

namespace TacticalCombat.Building
{
    public class BuildPlacementController : NetworkBehaviour
    {
        [Header("Build Settings")]
        [SerializeField] private float placementRange = GameConstants.BUILD_PLACEMENT_RANGE;
        [SerializeField] private LayerMask placementSurface;
        [SerializeField] private LayerMask obstacleMask;
        [SerializeField] private bool verboseLogging = false;

        [Header("Ghost Prefabs")]
        [SerializeField] private GameObject wallGhostPrefab;
        [SerializeField] private GameObject platformGhostPrefab;
        [SerializeField] private GameObject rampGhostPrefab;

        [Header("Structure Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private GameObject rampPrefab;

        [Header("Dependencies (Server)")]
        [SerializeField] private BuildValidator buildValidator;

        private PlayerController playerController;
        private SimpleBuildMode buildMode;
        private Camera mainCamera;
        private BuildGhost currentGhost;
        private StructureType currentStructureType = StructureType.WoodWall; // Default to WoodWall
        private bool isPlacementValid = false;
        private Vector3 ghostPosition;
        private Quaternion ghostRotation;
        private readonly Dictionary<StructureType, BuildGhost> ghostCache = new Dictionary<StructureType, BuildGhost>();

        private InputAction placeAction;
        private InputAction rotateAction;
        private InputAction selectWallAction;
        private InputAction selectPlatformAction;
        private InputAction selectRampAction;
        private System.Action<InputAction.CallbackContext> selectWallHandler;
        private System.Action<InputAction.CallbackContext> selectPlatformHandler;
        private System.Action<InputAction.CallbackContext> selectRampHandler;

        // NonAlloc buffers
        private static readonly Collider[] overlapBuffer = new Collider[64];

        // Profiling
        private static readonly ProfilerMarker marker_UpdateGhost = new ProfilerMarker("BuildPlacement.UpdateGhost");
        private static readonly ProfilerMarker marker_Validate = new ProfilerMarker("BuildPlacement.ValidatePlacement");

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
            buildMode = GetComponent<SimpleBuildMode>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            // ✅ PERFORMANCE FIX: Get camera from FPSController instead of Camera.main
            var fpsController = GetComponent<FPSController>();
            if (fpsController != null)
            {
                mainCamera = fpsController.GetCamera();
            }

            if (mainCamera == null)
            {
                Debug.LogWarning("⚠️ [BuildPlacementController] FPSController camera not found - trying child camera");
                mainCamera = GetComponentInChildren<Camera>();
            }

            if (mainCamera == null)
            {
                Debug.LogError("❌ [BuildPlacementController] No camera found!");
            }

            var layerCfg = TacticalCombat.Core.LayerConfigProvider.Instance;
            if (layerCfg != null)
            {
                if (placementSurface == 0) placementSurface = layerCfg.placementSurface;
                if (obstacleMask == 0) obstacleMask = layerCfg.obstacleMask;
            }
            else
            {
                if (placementSurface == 0)
                {
                    placementSurface = LayerMask.GetMask("Default", "Structure");
                }
                if (obstacleMask == 0)
                {
                    obstacleMask = LayerMask.GetMask("Structure", "Player", "BuildObstruction");
                }
            }

            // Setup input
            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                var buildMap = playerInput.actions.FindActionMap("Build");
                placeAction = buildMap.FindAction("Place");
                rotateAction = buildMap.FindAction("Rotate");
                selectWallAction = buildMap.FindAction("SelectWall");
                selectPlatformAction = buildMap.FindAction("SelectPlatform");
                selectRampAction = buildMap.FindAction("SelectRamp");

                placeAction.performed += OnPlace;
                rotateAction.performed += OnRotate;
                selectWallHandler = ctx => SelectStructure(StructureType.WoodWall); // Default to WoodWall
                selectPlatformHandler = ctx => SelectStructure(StructureType.Platform);
                selectRampHandler = ctx => SelectStructure(StructureType.Ramp);

                selectWallAction.performed += selectWallHandler;
                selectPlatformAction.performed += selectPlatformHandler;
                selectRampAction.performed += selectRampHandler;
            }

            // Create initial ghost
            CreateGhost(currentStructureType);
        }

        private void OnDisable()
        {
            // Unsubscribe input actions to prevent duplicate handlers on re-enable
            if (placeAction != null) placeAction.performed -= OnPlace;
            if (rotateAction != null) rotateAction.performed -= OnRotate;
            if (selectWallAction != null && selectWallHandler != null) selectWallAction.performed -= selectWallHandler;
            if (selectPlatformAction != null && selectPlatformHandler != null) selectPlatformAction.performed -= selectPlatformHandler;
            if (selectRampAction != null && selectRampHandler != null) selectRampAction.performed -= selectRampHandler;
            selectWallHandler = null;
            selectPlatformHandler = null;
            selectRampHandler = null;
        }

        private void Update()
        {
            if (!isLocalPlayer || playerController == null) return;

            if (buildMode != null && buildMode.IsBuildModeActive() && currentGhost != null)
            {
                marker_UpdateGhost.Begin();
                UpdateGhostPosition();
                marker_UpdateGhost.End();
                currentGhost.Show(true);
            }
            else if (currentGhost != null)
            {
                currentGhost.Show(false);
            }
        }

        private void UpdateGhostPosition()
        {
            Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
            
            if (Physics.Raycast(ray, out RaycastHit hit, placementRange, placementSurface, QueryTriggerInteraction.Ignore))
            {
                ghostPosition = hit.point;
                ghostRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                
                // Validate placement
                marker_Validate.Begin();
                isPlacementValid = ValidatePlacement(ghostPosition, ghostRotation);
                marker_Validate.End();
                
                currentGhost.transform.position = ghostPosition;
                currentGhost.transform.rotation = ghostRotation;
                currentGhost.SetValid(isPlacementValid);
            }
            else
            {
                isPlacementValid = false;
                currentGhost.SetValid(false);
            }
        }

        private bool ValidatePlacement(Vector3 position, Quaternion rotation)
        {
            // Check if too close to other structures
            int count = Physics.OverlapSphereNonAlloc(position, 0.5f, overlapBuffer, obstacleMask, QueryTriggerInteraction.Ignore);
            if (count > 0)
            {
                return false;
            }

            // Additional checks can be added here
            return true;
        }

        private void OnPlace(InputAction.CallbackContext context)
        {
            if (buildMode == null || !buildMode.IsBuildModeActive() || !isPlacementValid) return;

            // Request placement from server
            BuildRequest request = new BuildRequest(
                ghostPosition,
                ghostRotation,
                currentStructureType,
                playerController.playerId
            );

            CmdRequestPlace(request);
        }

        private void OnRotate(InputAction.CallbackContext context)
        {
            if (buildMode == null || !buildMode.IsBuildModeActive()) return;

            // Rotate ghost 90 degrees
            ghostRotation *= Quaternion.Euler(0, 90, 0);
        }

        private void SelectStructure(StructureType type)
        {
            if (buildMode == null || !buildMode.IsBuildModeActive()) return;

            currentStructureType = type;
            CreateGhost(type);
        }

        private void CreateGhost(StructureType type)
        {
            // hide current
            if (currentGhost != null) currentGhost.Show(false);

            // reuse cached ghost if available
            if (!ghostCache.TryGetValue(type, out var ghost) || ghost == null)
            {
                var ghostPrefab = GetGhostPrefab(type);
                if (ghostPrefab != null)
                {
                    var ghostObj = Instantiate(ghostPrefab);
                    ghost = ghostObj.GetComponent<BuildGhost>();
                    if (ghost == null)
                    {
                        ghost = ghostObj.AddComponent<BuildGhost>();
                    }
                    ghostCache[type] = ghost;
                }
            }

            currentGhost = ghost;
            if (currentGhost != null)
            {
                currentGhost.Show(false);
            }
        }

        private GameObject GetGhostPrefab(StructureType type)
        {
            return type switch
            {
                StructureType.WoodWall => wallGhostPrefab,
                StructureType.MetalWall => wallGhostPrefab, // TODO: Add metalWallGhostPrefab when available
                StructureType.Platform => platformGhostPrefab,
                StructureType.Ramp => rampGhostPrefab,
                _ => wallGhostPrefab // Default fallback
            };
        }

        private GameObject GetStructurePrefab(StructureType type)
        {
            return type switch
            {
                StructureType.WoodWall => wallPrefab,
                StructureType.MetalWall => wallPrefab, // TODO: Add metalWallPrefab when available
                StructureType.Platform => platformPrefab,
                StructureType.Ramp => rampPrefab,
                _ => wallPrefab // Default fallback
            };
        }

        [Command]
        private void CmdRequestPlace(BuildRequest request)
        {
            // ✅ PERFORMANCE FIX: Cache BuildValidator (avoid FindFirstObjectByType on every placement)
            if (buildValidator == null)
            {
                // First try: Use singleton pattern (recommended)
                buildValidator = BuildValidator.Instance;

                // Fallback: Find once and cache (only on first placement)
                if (buildValidator == null)
                {
                    buildValidator = FindFirstObjectByType<BuildValidator>();
                }

                if (buildValidator == null)
                {
                    Debug.LogWarning("❌ [BuildPlacementController] No BuildValidator found on server! Assign in Inspector or use singleton.");
                    return;
                }
            }

            if (buildValidator.ValidateAndPlace(request, playerController.team))
            {
                if (verboseLogging) Debug.Log($"✅ Structure {request.type} placed successfully");
            }
            else
            {
                if (verboseLogging) Debug.Log($"⚠️ Structure {request.type} placement failed validation");
            }
        }

        
    }
}
