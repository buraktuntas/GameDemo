using UnityEngine;
using System.Collections.Generic;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Vision
{
    public class ControlPoint : NetworkBehaviour
    {
        [Header("Control Point Settings")]
        [SerializeField] private float captureRadius = 5f;
        [SerializeField] private float captureTime = GameConstants.MID_CAPTURE_TIME;
        
        [SyncVar(hook = nameof(OnControllingTeamChanged))]
        private Team controllingTeam = Team.None;
        
        [SyncVar]
        private float captureProgress = 0f; // -1 to 1, negative = TeamA, positive = TeamB

        [Header("Vision Settings")]
        [SerializeField] private float visionPulseInterval = GameConstants.VISION_PULSE_INTERVAL;
        [SerializeField] private float visionPulseRadius = GameConstants.VISION_PULSE_RADIUS;

        private float lastPulseTime;
        private List<Player.PlayerController> playersInZone = new List<Player.PlayerController>();

        // ✅ PERFORMANCE FIX: Cache player health components
        private Dictionary<Player.PlayerController, Combat.Health> playerHealthCache = new Dictionary<Player.PlayerController, Combat.Health>();

        // ✅ PERFORMANCE FIX: NonAlloc buffers
        private static readonly Collider[] visionBuffer = new Collider[32];
        private readonly Vector3[] positionBuffer = new Vector3[32];

        public System.Action<Team> OnControlChanged;

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;

            // ✅ PERFORMANCE FIX: Use TryGetComponent
            if (other.TryGetComponent<Player.PlayerController>(out var player))
            {
                if (!playersInZone.Contains(player))
                {
                    playersInZone.Add(player);

                    // ✅ PERFORMANCE FIX: Cache health component
                    if (!playerHealthCache.ContainsKey(player))
                    {
                        if (player.TryGetComponent<Combat.Health>(out var health))
                        {
                            playerHealthCache[player] = health;
                        }
                    }
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isServer) return;

            // ✅ PERFORMANCE FIX: Use TryGetComponent
            if (other.TryGetComponent<Player.PlayerController>(out var player))
            {
                if (playersInZone.Contains(player))
                {
                    playersInZone.Remove(player);
                    // Note: Keep health cache in case player returns
                }
            }
        }

        private void Update()
        {
            if (!isServer) return;

            // Only active during Combat phase
            if (MatchManager.Instance == null)
            {
                Debug.LogWarning("⚠️ MatchManager.Instance is null in ControlPoint!");
                return;
            }
            
            if (MatchManager.Instance.GetCurrentPhase() != Phase.Combat)
                return;

            ServerTickCapture();
            ServerTickVisionPulse();
        }

        [Server]
        private void ServerTickCapture()
        {
            // Count players in zone by team
            int teamACount = 0;
            int teamBCount = 0;

            // Remove dead players and clean up cache
            playersInZone.RemoveAll(p => {
                if (p == null)
                {
                    return true;
                }
                return false;
            });

            foreach (var player in playersInZone)
            {
                // ✅ PERFORMANCE FIX: Use cached health component instead of GetComponent
                if (!playerHealthCache.TryGetValue(player, out var health))
                {
                    // Cache miss - get and cache it
                    if (player.TryGetComponent<Combat.Health>(out health))
                    {
                        playerHealthCache[player] = health;
                    }
                }

                if (health != null && !health.IsDead())
                {
                    if (player.team == Team.TeamA)
                        teamACount++;
                    else if (player.team == Team.TeamB)
                        teamBCount++;
                }
            }

            // Calculate capture direction
            float captureRate = 0f;
            if (teamACount > 0 && teamBCount == 0)
            {
                captureRate = -1f / captureTime; // TeamA captures (negative)
            }
            else if (teamBCount > 0 && teamACount == 0)
            {
                captureRate = 1f / captureTime; // TeamB captures (positive)
            }
            // If both teams present, no progress (contested)

            // Update progress
            captureProgress += captureRate * Time.deltaTime;
            captureProgress = Mathf.Clamp(captureProgress, -1f, 1f);

            // Check for control change
            Team newControllingTeam = Team.None;
            if (captureProgress <= -1f)
            {
                newControllingTeam = Team.TeamA;
            }
            else if (captureProgress >= 1f)
            {
                newControllingTeam = Team.TeamB;
            }

            if (newControllingTeam != Team.None && newControllingTeam != controllingTeam)
            {
                controllingTeam = newControllingTeam;
                Debug.Log($"{controllingTeam} captured the control point!");
            }
        }

        [Server]
        private void ServerTickVisionPulse()
        {
            if (controllingTeam == Team.None)
                return;

            if (Time.time >= lastPulseTime + visionPulseInterval)
            {
                lastPulseTime = Time.time;
                ApplyVisionPulse();
            }
        }

        [Server]
        private void ApplyVisionPulse()
        {
            int hitCount = Physics.OverlapSphereNonAlloc(
                transform.position,
                visionPulseRadius,
                visionBuffer
            );

            int revealCount = 0;

            for (int i = 0; i < hitCount; i++)
            {
                Collider hit = visionBuffer[i];

                if (hit.TryGetComponent<Player.PlayerController>(out var player))
                {
                    if (player.team != controllingTeam)
                    {
                        if (!playerHealthCache.TryGetValue(player, out var health))
                        {
                            if (player.TryGetComponent<Combat.Health>(out health))
                            {
                                playerHealthCache[player] = health;
                            }
                        }

                        if (health != null && !health.IsDead())
                        {
                            if (revealCount < positionBuffer.Length)
                            {
                                positionBuffer[revealCount] = player.transform.position;
                                revealCount++;
                            }
                        }
                    }
                }
            }

            if (revealCount > 0)
            {
                RpcRevealEnemies(positionBuffer, revealCount, controllingTeam);
            }
        }

        [ClientRpc]
        private void RpcRevealEnemies(Vector3[] positions, int count, Team revealingTeam)
        {
            for (int i = 0; i < count; i++)
            {
                CreateRevealMarker(positions[i]);
            }
        }

        private void CreateRevealMarker(Vector3 position)
        {
            // This would create a temporary visual marker
            Debug.Log($"Enemy revealed at {position}");
        }

        private void OnControllingTeamChanged(Team oldTeam, Team newTeam)
        {
            OnControlChanged?.Invoke(newTeam);
            Debug.Log($"Control point now controlled by {newTeam}");
        }

        public Team GetControllingTeam() => controllingTeam;
        public float GetCaptureProgress() => captureProgress;

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, captureRadius);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, visionPulseRadius);
        }
    }
}

