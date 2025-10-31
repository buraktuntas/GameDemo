using UnityEngine;

namespace TacticalCombat.Player
{
    /// <summary>
    /// Example: How to attach prefabs to player
    /// Attach this to any object, assign player and prefabs, hit Play
    /// </summary>
    public class ExamplePrefabAttacher : MonoBehaviour
    {
        [Header("Test Setup")]
        [SerializeField] private GameObject playerObject;

        [Header("Prefabs to Attach")]
        [SerializeField] private GameObject weaponPrefab;
        [SerializeField] private GameObject helmetPrefab;
        [SerializeField] private GameObject backpackPrefab;

        [Header("Auto-attach on Start")]
        [SerializeField] private bool attachOnStart = true;

        private void Start()
        {
            if (attachOnStart)
            {
                AttachAllPrefabs();
            }
        }

        // Call this from Inspector button or code
        public void AttachAllPrefabs()
        {
            if (playerObject == null)
            {
                Debug.LogWarning("Player object not assigned!");
                return;
            }

            // Get PlayerComponents hub
            PlayerComponents pc = playerObject.GetComponent<PlayerComponents>();
            if (pc == null)
            {
                Debug.LogError("PlayerComponents not found on player! Add it to the player prefab.");
                return;
            }

            // Attach weapon
            if (weaponPrefab != null)
            {
                pc.AttachPrefabToWeapon(weaponPrefab);
                Debug.Log("✅ Weapon attached!");
            }

            // Attach helmet
            if (helmetPrefab != null)
            {
                pc.AttachPrefabToHead(helmetPrefab);
                Debug.Log("✅ Helmet attached!");
            }

            // Attach backpack
            if (backpackPrefab != null)
            {
                pc.AttachPrefabToBack(backpackPrefab);
                Debug.Log("✅ Backpack attached!");
            }
        }

        // Example: Access player components
        public void DamagePlayer(int amount)
        {
            if (playerObject == null) return;

            PlayerComponents pc = playerObject.GetComponent<PlayerComponents>();
            if (pc != null && pc.IsAlive)
            {
                pc.health.TakeDamage(amount);
                Debug.Log($"Player took {amount} damage!");
            }
        }

        // Example: Check player state
        public void LogPlayerInfo()
        {
            if (playerObject == null) return;

            PlayerComponents pc = playerObject.GetComponent<PlayerComponents>();
            if (pc != null)
            {
                Debug.Log($"=== PLAYER INFO ===");
                Debug.Log($"Team: {pc.Team}");
                Debug.Log($"Role: {pc.Role}");
                Debug.Log($"Is Local Player: {pc.IsLocalPlayer}");
                Debug.Log($"Is Alive: {pc.IsAlive}");
            }
        }
    }
}
