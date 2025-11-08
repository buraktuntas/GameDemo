using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Traps
{
    public abstract class TrapBase : NetworkBehaviour
    {
        [Header("Trap Settings")]
        [SerializeField] protected TrapType trapType;
        
        [SyncVar]
        protected Team ownerTeam;
        
        [SyncVar]
        protected bool isArmed = false;
        
        [SyncVar]
        protected bool isTriggered = false;

        [SerializeField] protected float armingDelay = 2f;
        protected float armingTime;

        // ✅ MEDIUM PRIORITY FIX: Prevent double-initialization
        private bool isInitialized = false;

        public virtual void Initialize(Team team)
        {
            if (isInitialized)
            {
                #if UNITY_EDITOR || DEVELOPMENT_BUILD
                Debug.LogWarning($"Trap {gameObject.name} already initialized!");
                #endif
                return;
            }
            
            ownerTeam = team;
            isInitialized = true;
            
            if (isServer)
            {
                Invoke(nameof(Arm), armingDelay);
            }
        }

        [Server]
        protected virtual void Arm()
        {
            isArmed = true;
            RpcOnArmed();
        }

        [ClientRpc]
        protected virtual void RpcOnArmed()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"{gameObject.name} armed");
            #endif
            
            // ✅ HIGH PRIORITY FIX: Visual feedback that trap is armed
            // Option 1: Change material color (use sharedMaterial to avoid material leak)
            var renderer = GetComponent<Renderer>();
            if (renderer != null && renderer.sharedMaterial != null)
            {
                var mat = renderer.sharedMaterial;
                if (mat.HasProperty("_EmissionColor"))
                {
                    // Note: sharedMaterial is read-only, so we can't modify it
                    // If material modification is needed, create instance once and cache it
                    // For now, skip material modification to avoid material leak
                    // TODO: Implement material instance caching if visual feedback needed
                }
            }
            
            // Option 2: Enable particle effect
            var particles = GetComponentInChildren<ParticleSystem>();
            if (particles != null)
            {
                particles.Play();
            }
            
            // Option 3: Play sound
            var audioSource = GetComponent<AudioSource>();
            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        // ✅ MEDIUM PRIORITY FIX: Rate limit triggering
        private float lastTriggerTime = 0f;
        private const float TRIGGER_COOLDOWN = 0.5f;

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isServer || !isArmed || isTriggered) return;

            // ✅ MEDIUM PRIORITY FIX: Rate limit triggering (prevent spam)
            if (Time.time - lastTriggerTime < TRIGGER_COOLDOWN) return;

            // ✅ PERFORMANCE FIX: Use TryGetComponent instead of GetComponent (faster, no GC)
            // Check if enemy player
            if (other.TryGetComponent<Player.PlayerController>(out var player) && player.team != ownerTeam)
            {
                lastTriggerTime = Time.time;
                Trigger(player.gameObject);
            }
        }

        // Abstract method - derived classes should add [Server] attribute to their implementation
        protected abstract void Trigger(GameObject target);

        [Server]
        protected virtual void MarkAsTriggered()
        {
            isTriggered = true;
        }

        // ✅ CRITICAL FIX: Cancel Invoke on destroy to prevent memory leaks
        private void OnDestroy()
        {
            CancelInvoke(nameof(Arm)); // Cancel Arm Invoke if trap destroyed before arming
        }
    }
}

