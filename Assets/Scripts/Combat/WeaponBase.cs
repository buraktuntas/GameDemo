using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    public abstract class WeaponBase : NetworkBehaviour
    {
        [Header("Base Settings")]
        [SerializeField] protected int damage = 50;
        [SerializeField] protected float cooldown = 1f;
        
        protected float lastFireTime = -999f;
        protected Team ownerTeam;
        protected ulong ownerId;

        public virtual void Initialize(Team team, ulong playerId)
        {
            ownerTeam = team;
            ownerId = playerId;
        }

        public virtual bool CanFire()
        {
            return Time.time >= lastFireTime + cooldown;
        }

        public abstract void Fire(bool special = false);

        public virtual void Equip()
        {
            gameObject.SetActive(true);
        }

        public virtual void Unequip()
        {
            gameObject.SetActive(false);
        }
    }
}



