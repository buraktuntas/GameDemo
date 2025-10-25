using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using TacticalCombat.Core;
using TacticalCombat.Player;

namespace TacticalCombat.Building
{
    public class BuildPlacementController : NetworkBehaviour
    {
        [Header("Build Settings")]
        [SerializeField] private float placementRange = GameConstants.BUILD_PLACEMENT_RANGE;
        [SerializeField] private LayerMask placementSurface;
        [SerializeField] private LayerMask obstacleMask;

        [Header("Ghost Prefabs")]
        [SerializeField] private GameObject wallGhostPrefab;
        [SerializeField] private GameObject platformGhostPrefab;
        [SerializeField] private GameObject rampGhostPrefab;

        [Header("Structure Prefabs")]
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject platformPrefab;
        [SerializeField] private GameObject rampPrefab;

        private PlayerController playerController;
        private Camera mainCamera;
        private BuildGhost currentGhost;
        private StructureType currentStructureType = StructureType.Wall;
        private bool isPlacementValid = false;
        private Vector3 ghostPosition;
        private Quaternion ghostRotation;

        private InputAction placeAction;
        private InputAction rotateAction;
        private InputAction selectWallAction;
        private InputAction selectPlatformAction;
        private InputAction selectRampAction;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            
            // Unity 6: Cache Camera.main for performance
            mainCamera = Camera.main;

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
                selectWallAction.performed += ctx => SelectStructure(StructureType.Wall);
                selectPlatformAction.performed += ctx => SelectStructure(StructureType.Platform);
                selectRampAction.performed += ctx => SelectStructure(StructureType.Ramp);
            }

            // Create initial ghost
            CreateGhost(currentStructureType);
        }

        private void Update()
        {
            if (!isLocalPlayer || playerController == null) return;

            if (playerController.IsInBuildMode() && currentGhost != null)
            {
                UpdateGhostPosition();
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
            
            if (Physics.Raycast(ray, out RaycastHit hit, placementRange, placementSurface))
            {
                ghostPosition = hit.point;
                ghostRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                
                // Validate placement
                isPlacementValid = ValidatePlacement(ghostPosition, ghostRotation);
                
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
            Collider[] overlaps = Physics.OverlapSphere(position, 0.5f, obstacleMask);
            if (overlaps.Length > 0)
            {
                return false;
            }

            // Additional checks can be added here
            return true;
        }

        private void OnPlace(InputAction.CallbackContext context)
        {
            if (!playerController.IsInBuildMode() || !isPlacementValid) return;

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
            if (!playerController.IsInBuildMode()) return;

            // Rotate ghost 90 degrees
            ghostRotation *= Quaternion.Euler(0, 90, 0);
        }

        private void SelectStructure(StructureType type)
        {
            if (!playerController.IsInBuildMode()) return;

            currentStructureType = type;
            CreateGhost(type);
        }

        private void CreateGhost(StructureType type)
        {
            // Destroy old ghost
            if (currentGhost != null)
            {
                Destroy(currentGhost.gameObject);
            }

            // Create new ghost
            GameObject ghostPrefab = GetGhostPrefab(type);
            if (ghostPrefab != null)
            {
                GameObject ghostObj = Instantiate(ghostPrefab);
                currentGhost = ghostObj.GetComponent<BuildGhost>();
                if (currentGhost == null)
                {
                    currentGhost = ghostObj.AddComponent<BuildGhost>();
                }
                currentGhost.Show(false);
            }
        }

        private GameObject GetGhostPrefab(StructureType type)
        {
            return type switch
            {
                StructureType.Wall => wallGhostPrefab,
                StructureType.Platform => platformGhostPrefab,
                StructureType.Ramp => rampGhostPrefab,
                _ => wallGhostPrefab
            };
        }

        private GameObject GetStructurePrefab(StructureType type)
        {
            return type switch
            {
                StructureType.Wall => wallPrefab,
                StructureType.Platform => platformPrefab,
                StructureType.Ramp => rampPrefab,
                _ => wallPrefab
            };
        }

        [Command]
        private void CmdRequestPlace(BuildRequest request)
        {
            // Unity 6: Use FindFirstObjectByType
            BuildValidator validator = FindFirstObjectByType<BuildValidator>();
            if (validator == null)
            {
                Debug.LogWarning("No BuildValidator found!");
                return;
            }

            if (validator.ValidateAndPlace(request, playerController.team))
            {
                Debug.Log($"Structure {request.type} placed successfully");
            }
            else
            {
                Debug.Log($"Structure {request.type} placement failed validation");
            }
        }

        private void OnDisable()
        {
            if (placeAction != null)
            {
                placeAction.performed -= OnPlace;
            }
            if (rotateAction != null)
            {
                rotateAction.performed -= OnRotate;
            }
        }
    }
}

