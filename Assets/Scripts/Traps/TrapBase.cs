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

        public virtual void Initialize(Team team)
        {
            ownerTeam = team;
            armingTime = Time.time + armingDelay;
        }

        protected virtual void Update()
        {
            if (!isServer) return;

            if (!isArmed && Time.time >= armingTime)
            {
                Arm();
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
            // Visual feedback that trap is armed
            Debug.Log($"{gameObject.name} armed");
        }

        protected virtual void OnTriggerEnter(Collider other)
        {
            if (!isServer || !isArmed || isTriggered) return;

            // Check if enemy player
            var player = other.GetComponent<Player.PlayerController>();
            if (player != null && player.team != ownerTeam)
            {
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
    }
}

