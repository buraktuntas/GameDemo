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

        public System.Action<Team> OnControlChanged;

        private void OnTriggerEnter(Collider other)
        {
            if (!isServer) return;

            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && !playersInZone.Contains(player))
            {
                playersInZone.Add(player);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!isServer) return;

            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && playersInZone.Contains(player))
            {
                playersInZone.Remove(player);
            }
        }

        private void Update()
        {
            if (!isServer) return;

            // Only active during Combat phase
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

            // Remove dead players
            playersInZone.RemoveAll(p => p == null);

            foreach (var player in playersInZone)
            {
                var health = player.GetComponent<Combat.Health>();
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
            // Find all enemy players in radius
            Collider[] hits = Physics.OverlapSphere(transform.position, visionPulseRadius);
            List<Vector3> revealedPositions = new List<Vector3>();

            foreach (var hit in hits)
            {
                var player = hit.GetComponent<Player.PlayerController>();
                if (player != null && player.team != controllingTeam)
                {
                    var health = player.GetComponent<Combat.Health>();
                    if (health != null && !health.IsDead())
                    {
                        revealedPositions.Add(player.transform.position);
                    }
                }
            }

            if (revealedPositions.Count > 0)
            {
                RpcRevealEnemies(revealedPositions.ToArray(), controllingTeam);
            }
        }

        [ClientRpc]
        private void RpcRevealEnemies(Vector3[] positions, Team revealingTeam)
        {
            // Show revealed positions on minimap/UI for the controlling team
            Debug.Log($"{revealingTeam} vision pulse revealed {positions.Length} enemies");
            
            foreach (var pos in positions)
            {
                // Create temporary marker or ping
                CreateRevealMarker(pos);
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

