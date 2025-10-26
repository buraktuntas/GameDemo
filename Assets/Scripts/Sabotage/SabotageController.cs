using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Sabotage
{
    public class SabotageController : NetworkBehaviour
    {
        [Header("Sabotage Settings")]
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private float interactDuration = GameConstants.SABOTAGE_INTERACT_TIME;
        [SerializeField] private LayerMask sabotageTargetMask;

        private Player.PlayerController playerController;
        private SabotageTarget currentTarget;
        private bool isInteracting = false;
        private float interactProgress = 0f;

        private InputAction interactAction;

        // ✅ PERFORMANCE FIX: Throttle target scanning + NonAlloc buffer
        private float lastScanTime = 0f;
        private const float SCAN_INTERVAL = 0.3f;  // Scan every 300ms instead of every frame
        private static readonly Collider[] scanBuffer = new Collider[8];  // NonAlloc buffer

        public System.Action<float> OnSabotageProgress; // 0-1
        public System.Action<bool> OnSabotageResult; // success/fail

        private void Awake()
        {
            playerController = GetComponent<Player.PlayerController>();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            var playerInput = GetComponent<PlayerInput>();
            if (playerInput != null)
            {
                var playerMap = playerInput.actions.FindActionMap("Player");
                interactAction = playerMap.FindAction("Interact");
                
                interactAction.started += OnInteractStarted;
                interactAction.canceled += OnInteractCanceled;
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // ✅ PERFORMANCE FIX: Throttled target scanning (was every frame)
            if (Time.time - lastScanTime >= SCAN_INTERVAL)
            {
                lastScanTime = Time.time;
                FindNearbyTarget();
            }

            // Update sabotage progress
            if (isInteracting && currentTarget != null)
            {
                interactProgress += Time.deltaTime / interactDuration;
                OnSabotageProgress?.Invoke(interactProgress);

                if (interactProgress >= 1f)
                {
                    CompleteSabotage();
                }
            }
        }

        private void FindNearbyTarget()
        {
            if (isInteracting) return;

            // ✅ PERFORMANCE FIX: Use NonAlloc to avoid GC allocations
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                interactRange,
                scanBuffer,
                sabotageTargetMask
            );

            SabotageTarget closestTarget = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = scanBuffer[i];

                // ✅ PERFORMANCE FIX: Use TryGetComponent (faster than GetComponent)
                if (hit.TryGetComponent<SabotageTarget>(out var target))
                {
                    if (target.CanBeSabotaged(playerController.team))
                    {
                        float distance = Vector3.Distance(transform.position, hit.transform.position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestTarget = target;
                        }
                    }
                }
            }

            currentTarget = closestTarget;
        }

        private void OnInteractStarted(InputAction.CallbackContext context)
        {
            // Only saboteur can sabotage
            if (playerController.role != RoleId.Saboteur)
            {
                Debug.Log("Only Saboteur can perform sabotage");
                return;
            }

            // Only in Combat phase
            if (MatchManager.Instance.GetCurrentPhase() != Phase.Combat)
            {
                return;
            }

            if (currentTarget != null)
            {
                StartSabotage();
            }
        }

        private void OnInteractCanceled(InputAction.CallbackContext context)
        {
            if (isInteracting)
            {
                CancelSabotage();
            }
        }

        private void StartSabotage()
        {
            isInteracting = true;
            interactProgress = 0f;
            Debug.Log("Starting sabotage...");
        }

        private void CancelSabotage()
        {
            if (interactProgress > 0.3f) // If canceled after 30% progress, reveal player
            {
                CmdSabotageFailedReveal();
            }

            isInteracting = false;
            interactProgress = 0f;
            OnSabotageProgress?.Invoke(0f);
        }

        private void CompleteSabotage()
        {
            isInteracting = false;
            interactProgress = 0f;
            OnSabotageProgress?.Invoke(0f);

            if (currentTarget != null)
            {
                CmdSabotageSuccess(currentTarget.GetComponent<NetworkIdentity>().netId);
                OnSabotageResult?.Invoke(true);
            }

            currentTarget = null;
        }

        [Command]
        private void CmdSabotageSuccess(uint targetNetId)
        {
            if (NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity identity))
            {
                var target = identity.GetComponent<SabotageTarget>();
                if (target != null)
                {
                    target.Disable(GameConstants.SABOTAGE_DISABLE_DURATION);
                    Debug.Log("Sabotage successful!");
                }
            }
        }

        [Command]
        private void CmdSabotageFailedReveal()
        {
            // Reveal the saboteur to enemy team
            RpcRevealSaboteur(transform.position, playerController.team);
        }

        [ClientRpc]
        private void RpcRevealSaboteur(Vector3 position, Team sabotagingTeam)
        {
            Debug.Log($"Saboteur from {sabotagingTeam} revealed at {position}!");
            OnSabotageResult?.Invoke(false);
            // This would show a ping/marker on the UI for enemy team
        }

        public SabotageTarget GetCurrentTarget() => currentTarget;
        public bool IsInteracting() => isInteracting;
        public float GetProgress() => interactProgress;

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.started -= OnInteractStarted;
                interactAction.canceled -= OnInteractCanceled;
            }
        }
    }
}



