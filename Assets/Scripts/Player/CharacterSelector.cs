using UnityEngine;
using Mirror;

namespace TacticalCombat.Player
{
    /// <summary>
    /// ✅ CHARACTER SELECTOR
    /// Player prefab'a karakter seçimi ve spawn işlevselliği ekler
    /// </summary>
    public class CharacterSelector : NetworkBehaviour
    {
        [Header("Character Prefabs")]
        [SerializeField] private GameObject maleCharacterPrefab;
        [SerializeField] private GameObject femaleCharacterPrefab;
        
        [Header("Settings")]
        [SerializeField] private bool randomizeCharacter = true;
        [SerializeField] private bool useSpawnPoints = true;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // Current character
        private GameObject currentCharacter;
        private bool isCharacterSpawned = false;
        private int characterIndex = 0; // 0 = male, 1 = female
        
        // Events
        public System.Action<GameObject> OnCharacterSpawned;
        public System.Action<GameObject> OnCharacterChanged;
        
        private void Start()
        {
            if (isLocalPlayer)
            {
                // Local player - spawn character immediately
                SpawnCharacter();
            }
        }
        
        /// <summary>
        /// Spawn character for this player
        /// </summary>
        public void SpawnCharacter()
        {
            if (isCharacterSpawned)
            {
                Debug.LogWarning("⚠️ [CharacterSelector] Character already spawned!");
                return;
            }
            
            GameObject selectedPrefab = SelectCharacterPrefab();
            if (selectedPrefab == null)
            {
                Debug.LogError("❌ [CharacterSelector] No character prefab selected!");
                return;
            }
            
            // Spawn character
            Vector3 spawnPosition = GetSpawnPosition();
            Quaternion spawnRotation = GetSpawnRotation();
            
            if (isServer)
            {
                // Server spawns the character
                SpawnCharacterOnServer(characterIndex, spawnPosition, spawnRotation);
            }
            else
            {
                // Client requests server to spawn
                CmdSpawnCharacter(characterIndex, spawnPosition, spawnRotation);
            }
        }
        
        /// <summary>
        /// Select which character prefab to use
        /// </summary>
        private GameObject SelectCharacterPrefab()
        {
            if (randomizeCharacter)
            {
                // Random selection
                bool useMale = Random.Range(0f, 1f) > 0.5f;
                characterIndex = useMale ? 0 : 1;
                GameObject selectedPrefab = useMale ? maleCharacterPrefab : femaleCharacterPrefab;
                
                if (showDebugInfo)
                {
                    Debug.Log($"🎲 [CharacterSelector] Random character selected: {(useMale ? "Male" : "Female")}");
                }
                
                return selectedPrefab;
            }
            else
            {
                // Default to male
                characterIndex = 0;
                return maleCharacterPrefab;
            }
        }
        
        /// <summary>
        /// Get spawn position
        /// </summary>
        private Vector3 GetSpawnPosition()
        {
            if (useSpawnPoints)
            {
                // Find spawn points
                GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
                if (spawnPoints.Length > 0)
                {
                    // Select random spawn point
                    GameObject randomSpawn = spawnPoints[Random.Range(0, spawnPoints.Length)];
                    Vector3 spawnPos = randomSpawn.transform.position;
                    
                    // Add some random offset
                    spawnPos += new Vector3(
                        Random.Range(-2f, 2f),
                        0,
                        Random.Range(-2f, 2f)
                    );
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"📍 [CharacterSelector] Spawning at spawn point: {randomSpawn.name}");
                    }
                    
                    return spawnPos;
                }
            }
            
            // Fallback to player position
            Vector3 fallbackPos = transform.position;
            fallbackPos.y = 0; // Ground level
            
            if (showDebugInfo)
            {
                Debug.Log("📍 [CharacterSelector] Using fallback spawn position");
            }
            
            return fallbackPos;
        }
        
        /// <summary>
        /// Get spawn rotation
        /// </summary>
        private Quaternion GetSpawnRotation()
        {
            // Random Y rotation
            float randomY = Random.Range(0f, 360f);
            return Quaternion.Euler(0, randomY, 0);
        }
        
        [Command]
        private void CmdSpawnCharacter(int charIndex, Vector3 position, Quaternion rotation)
        {
            SpawnCharacterOnServer(charIndex, position, rotation);
        }
        
        [Server]
        private void SpawnCharacterOnServer(int charIndex, Vector3 position, Quaternion rotation)
        {
            if (currentCharacter != null)
            {
                // Remove existing character
                DestroyImmediate(currentCharacter);
            }
            
            // Get prefab based on index
            GameObject prefab = charIndex == 0 ? maleCharacterPrefab : femaleCharacterPrefab;
            if (prefab == null)
            {
                Debug.LogError($"❌ [CharacterSelector] Character prefab is null for index {charIndex}");
                return;
            }
            
            // Spawn new character (local only, not networked)
            currentCharacter = Instantiate(prefab, position, rotation);
            
            // Set as child of player
            currentCharacter.transform.SetParent(transform);
            
            // Configure character
            ConfigureCharacter(currentCharacter);
            
            isCharacterSpawned = true;
            
            // Notify clients with character info instead of GameObject reference
            RpcOnCharacterSpawned(charIndex, position, rotation);
            
            if (showDebugInfo)
            {
                Debug.Log($"✅ [CharacterSelector] Character spawned: {prefab.name} at {position}");
            }
        }
        
        [ClientRpc]
        private void RpcOnCharacterSpawned(int charIndex, Vector3 position, Quaternion rotation)
        {
            if (isLocalPlayer)
            {
                // Spawn character locally for this client
                GameObject prefab = charIndex == 0 ? maleCharacterPrefab : femaleCharacterPrefab;
                if (prefab != null)
                {
                    GameObject localCharacter = Instantiate(prefab, position, rotation);
                    localCharacter.transform.SetParent(transform);
                    ConfigureCharacter(localCharacter);
                    
                    OnCharacterSpawned?.Invoke(localCharacter);
                    
                    if (showDebugInfo)
                    {
                        Debug.Log($"🎭 [CharacterSelector] Local player character spawned: {prefab.name}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Configure spawned character
        /// </summary>
        private void ConfigureCharacter(GameObject character)
        {
            // Add PlayerVisuals component if not present
            PlayerVisuals playerVisuals = character.GetComponent<PlayerVisuals>();
            if (playerVisuals == null)
            {
                playerVisuals = character.AddComponent<PlayerVisuals>();
            }
            
            // Set team color
            var playerController = GetComponent<PlayerController>();
            if (playerController != null)
            {
                playerVisuals.UpdateTeamColor(playerController.GetPlayerTeam());
            }
            
            // Add any other character-specific components
            // (e.g., animation controller, audio source, etc.)
        }
        
        /// <summary>
        /// Change character (for future use)
        /// </summary>
        public void ChangeCharacter(bool useMale)
        {
            if (!isCharacterSpawned) return;
            
            GameObject newPrefab = useMale ? maleCharacterPrefab : femaleCharacterPrefab;
            if (newPrefab == null) return;
            
            Vector3 currentPos = currentCharacter.transform.position;
            Quaternion currentRot = currentCharacter.transform.rotation;
            
            if (isServer)
            {
                SpawnCharacterOnServer(characterIndex, currentPos, currentRot);
            }
            else
            {
                CmdSpawnCharacter(characterIndex, currentPos, currentRot);
            }
            
            OnCharacterChanged?.Invoke(newPrefab);
        }
        
        /// <summary>
        /// Get current character
        /// </summary>
        public GameObject GetCurrentCharacter()
        {
            return currentCharacter;
        }
        
        /// <summary>
        /// Check if character is spawned
        /// </summary>
        public bool IsCharacterSpawned()
        {
            return isCharacterSpawned;
        }
        
        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (currentCharacter != null)
            {
                // Since character is not networked, just destroy normally
                DestroyImmediate(currentCharacter);
            }
        }
        
        /// <summary>
        /// Debug info
        /// </summary>
        private void OnGUI()
        {
            if (!showDebugInfo || !isLocalPlayer) return;
            
            GUILayout.BeginArea(new Rect(10, 100, 300, 100));
            GUILayout.Label($"Character: {(currentCharacter != null ? currentCharacter.name : "None")}");
            GUILayout.Label($"Spawned: {isCharacterSpawned}");
            GUILayout.EndArea();
        }
    }
}