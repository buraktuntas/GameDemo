using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Info Tower - Hackable tower that reveals enemy bases on minimap
    /// </summary>
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Collider))]
    public class InfoTower : NetworkBehaviour
    {
        [Header("Info Tower Settings")]
        [SerializeField] private float hackRange = 3f;
        [SerializeField] private float revealRadius = GameConstants.INFO_TOWER_REVEAL_RADIUS;
        [SerializeField] private GameObject hackProgressUI;

        [SyncVar]
        private Team ownerTeam;

        [SyncVar]
        private bool isHacked = false;

        [SyncVar]
        private ulong hackerId;

        [SyncVar]
        private float hackProgress = 0f;

        private float hackStartTime = 0f;
        private ulong currentHackerId = 0;
        private bool isHacking = false;
        private System.Collections.IEnumerator revealCoroutine; // ✅ FIX: Track coroutine to prevent memory leak

        // ✅ CRITICAL PERFORMANCE FIX: Use trigger collider instead of Physics.OverlapSphere every frame
        private System.Collections.Generic.HashSet<Player.PlayerController> playersInRange = new System.Collections.Generic.HashSet<Player.PlayerController>();

        // ✅ PERFORMANCE FIX: Static buffer for RevealEnemyBases coroutine
        private static Collider[] revealColliderBuffer = new Collider[100];

        public override void OnStartServer()
        {
            base.OnStartServer();
            GetComponent<Collider>().isTrigger = true;
        }

        public void Initialize(Team team)
        {
            ownerTeam = team;
            isHacked = false;
            hackProgress = 0f;
        }

        private void Update()
        {
            if (!isServer) return;

            // ✅ PERFORMANCE FIX: Only check hacking if we have players in range
            if (!isHacked && playersInRange.Count > 0)
            {
                CheckForHackers();
            }
            else if (isHacking && playersInRange.Count == 0)
            {
                // All hackers left the area
                StopHacking();
            }
        }

        // ✅ CRITICAL PERFORMANCE FIX: Use trigger collider events instead of Physics.OverlapSphere
        [Server]
        private void OnTriggerEnter(Collider other)
        {
            if (isHacked) return;

            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && player.team != ownerTeam)
            {
                playersInRange.Add(player);
            }
        }

        [Server]
        private void OnTriggerExit(Collider other)
        {
            var player = other.GetComponent<Player.PlayerController>();
            if (player != null)
            {
                playersInRange.Remove(player);
            }
        }

        [Server]
        private void CheckForHackers()
        {
            // Find first valid hacker in range
            Player.PlayerController validHacker = null;
            foreach (var player in playersInRange)
            {
                if (player != null && player.team != ownerTeam)
                {
                    validHacker = player;
                    break;
                }
            }

            if (validHacker != null)
            {
                // Start or continue hacking
                if (!isHacking || currentHackerId != validHacker.netId)
                {
                    StartHacking(validHacker.netId);
                }
                else
                {
                    ContinueHacking();
                }
            }
            else if (isHacking)
            {
                // No valid hackers - stop hacking
                StopHacking();
            }
        }

        [Server]
        private void StartHacking(ulong hackerId)
        {
            isHacking = true;
            currentHackerId = hackerId;
            hackStartTime = Time.time;
            RpcOnHackStarted(hackerId);
        }

        [Server]
        private void ContinueHacking()
        {
            float elapsed = Time.time - hackStartTime;
            hackProgress = elapsed / GameConstants.INFO_TOWER_HACK_TIME;

            if (hackProgress >= 1f)
            {
                CompleteHack();
            }
            else
            {
                RpcUpdateHackProgress(hackProgress);
            }
        }

        [Server]
        private void StopHacking()
        {
            isHacking = false;
            currentHackerId = 0;
            hackProgress = 0f;
            hackStartTime = 0f;
            RpcOnHackStopped();
        }

        [Server]
        private void CompleteHack()
        {
            isHacked = true;
            hackerId = currentHackerId;
            hackProgress = 1f;
            isHacking = false;

            // Start reveal effect
            revealCoroutine = RevealEnemyBases();
            StartCoroutine(revealCoroutine);

            RpcOnHackCompleted(hackerId);
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[InfoTower] Hacked by player {hackerId}!");
            #endif
        }

        [Server]
        private System.Collections.IEnumerator RevealEnemyBases()
        {
            float elapsed = 0f;
            float duration = GameConstants.INFO_TOWER_REVEAL_DURATION;

            while (elapsed < duration)
            {
                // ✅ PERFORMANCE FIX: Use OverlapSphereNonAlloc to avoid GC allocation every second
                int count = Physics.OverlapSphereNonAlloc(transform.position, revealRadius, revealColliderBuffer);

                for (int i = 0; i < count; i++)
                {
                    var col = revealColliderBuffer[i];

                    // Reveal enemy structures
                    var structure = col.GetComponent<Structure>();
                    if (structure != null && structure.team != ownerTeam)
                    {
                        RpcRevealStructure(structure.netId);
                    }

                    // Reveal enemy players
                    var player = col.GetComponent<Player.PlayerController>();
                    if (player != null && player.team != ownerTeam)
                    {
                        RpcRevealPlayer(player.netId);
                    }
                }

                elapsed += 1f; // Check every second
                yield return new WaitForSeconds(1f);
            }

            // Hack expires
            isHacked = false;
            hackProgress = 0f;
            hackerId = 0;
            revealCoroutine = null;
            RpcOnHackExpired();
        }

        private void OnDestroy()
        {
            // ✅ CRITICAL FIX: Stop coroutine on destroy to prevent memory leak
            if (revealCoroutine != null)
            {
                StopCoroutine(revealCoroutine);
                revealCoroutine = null;
            }
        }

        [ClientRpc]
        private void RpcOnHackStarted(ulong hackerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Info Tower hack started by player {hackerId}");
            #endif

            // ✅ IMPLEMENTED: Show hack progress UI
            var gameHUD = UI.GameHUD.Instance;
            if (gameHUD != null)
            {
                gameHUD.ShowInfoTowerHackProgress("HACKING INFO TOWER...");
            }
        }

        [ClientRpc]
        private void RpcUpdateHackProgress(float progress)
        {
            // ✅ IMPLEMENTED: Update hack progress UI
            var gameHUD = UI.GameHUD.Instance;
            if (gameHUD != null)
            {
                gameHUD.UpdateInfoTowerHackProgress(progress);
            }
        }

        [ClientRpc]
        private void RpcOnHackStopped()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Client] Info Tower hack stopped");
            #endif

            // ✅ IMPLEMENTED: Hide hack progress UI
            var gameHUD = UI.GameHUD.Instance;
            if (gameHUD != null)
            {
                gameHUD.HideInfoTowerHackProgress();
            }
        }

        [ClientRpc]
        private void RpcOnHackCompleted(ulong hackerId)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Info Tower hacked by player {hackerId}!");
            #endif

            // ✅ IMPLEMENTED: Show hack complete effect
            var gameHUD = UI.GameHUD.Instance;
            if (gameHUD != null)
            {
                gameHUD.ShowInfoTowerHackComplete();
            }
        }

        [ClientRpc]
        private void RpcRevealStructure(uint structureId)
        {
            // ✅ FIX: Integrate with MinimapManager
            var minimapManager = UI.MinimapManager.Instance;
            if (minimapManager != null)
            {
                minimapManager.RevealStructure(structureId, GameConstants.INFO_TOWER_REVEAL_DURATION);
            }
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Structure {structureId} revealed on minimap");
            #endif
        }

        [ClientRpc]
        private void RpcRevealPlayer(ulong playerId)
        {
            // ✅ FIX: Integrate with MinimapManager
            var minimapManager = UI.MinimapManager.Instance;
            if (minimapManager != null)
            {
                minimapManager.RevealPlayer(playerId, GameConstants.INFO_TOWER_REVEAL_DURATION);
            }
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"[Client] Player {playerId} revealed on minimap");
            #endif
        }

        [ClientRpc]
        private void RpcOnHackExpired()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log("[Client] Info Tower hack expired");
            #endif
            // ✅ FIX: Hide revealed structures/players via MinimapManager
            var minimapManager = UI.MinimapManager.Instance;
            if (minimapManager != null)
            {
                // Clear all reveals when hack expires
                minimapManager.ClearAllReveals();
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Draw hack range
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, hackRange);

            // Draw reveal radius
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, revealRadius);
        }
    }
}

