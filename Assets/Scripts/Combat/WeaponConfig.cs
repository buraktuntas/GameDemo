using UnityEngine;

namespace TacticalCombat.Combat
{
    [System.Serializable]
    public enum FireMode
    {
        Semi,
        Auto,
        Burst
    }

    [CreateAssetMenu(fileName = "New Weapon", menuName = "Tactical Combat/Weapon Config")]
    public class WeaponConfig : ScriptableObject
    {
        [Header("📊 STATS")]
        public string weaponName = "Assault Rifle";
        public float damage = 25f;
        public float range = 100f;
        public float fireRate = 10f;
        public FireMode fireMode = FireMode.Auto;

        [Header("🎯 ACCURACY")]
        public float hipSpread = 0.05f;
        public float aimSpread = 0.01f;
        public float recoilAmount = 2f;
        public float headshotMultiplier = 2f;

        [Header("📦 AMMO")]
        public int magazineSize = 30;
        public int maxAmmo = 120;
        public float reloadTime = 2f;

        [Header("🎯 TARGETING")]
        public LayerMask hitMask = ~0;
    }
}