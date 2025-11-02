using UnityEngine;
using Mirror;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// NetworkManagerHUD'ı otomatik olarak yok eder
    /// MainMenu veya custom UI kullanırken NetworkManager'ın default HUD'ını gizler
    /// </summary>
    [RequireComponent(typeof(NetworkManager))]
    [DefaultExecutionOrder(-1000)] // Start()'dan önce çalıştır
    public class DisableNetworkManagerHUD : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool destroyOnAwake = true;
        [Tooltip("If true, destroys NetworkManagerHUD in Awake. If false, only disables it.")]
        [SerializeField] private bool completelyDestroy = true;

        private void Awake()
        {
            if (!destroyOnAwake) return;

            RemoveNetworkManagerHUD();
        }

        private void Start()
        {
            // Double check on Start too
            RemoveNetworkManagerHUD();
        }

        [ContextMenu("Remove NetworkManagerHUD Now")]
        public void RemoveNetworkManagerHUD()
        {
            bool foundAny = false;

            // 1. Try to find Mirror's NetworkManagerHUD
            var hud = GetComponent<NetworkManagerHUD>();
            if (hud != null)
            {
                if (completelyDestroy)
                {
                    DestroyImmediate(hud);
                    Debug.Log("🚫 NetworkManagerHUD DESTROYED (DestroyImmediate)");
                }
                else
                {
                    hud.enabled = false;
                    Debug.Log("🚫 NetworkManagerHUD DISABLED");
                }
                foundAny = true;
            }

            // 2. Also check on NetworkManager itself
            var nm = GetComponent<NetworkManager>();
            if (nm != null)
            {
                hud = nm.GetComponent<NetworkManagerHUD>();
                if (hud != null)
                {
                    if (completelyDestroy)
                    {
                        DestroyImmediate(hud);
                        Debug.Log("🚫 NetworkManagerHUD DESTROYED from NetworkManager (DestroyImmediate)");
                    }
                    else
                    {
                        hud.enabled = false;
                        Debug.Log("🚫 NetworkManagerHUD DISABLED on NetworkManager");
                    }
                    foundAny = true;
                }
            }

            // 3. ✅ CRITICAL: Also remove SimpleNetworkHUD (custom HUD)
            var simpleHud = GetComponent<TacticalCombat.Network.SimpleNetworkHUD>();
            if (simpleHud != null)
            {
                if (completelyDestroy)
                {
                    DestroyImmediate(simpleHud);
                    Debug.Log("🚫 SimpleNetworkHUD DESTROYED (This was showing LAN HOST/CLIENT buttons!)");
                }
                else
                {
                    simpleHud.enabled = false;
                    Debug.Log("🚫 SimpleNetworkHUD DISABLED");
                }
                foundAny = true;
            }

            if (!foundAny)
            {
                Debug.Log("✅ No NetworkManagerHUD or SimpleNetworkHUD found (already clean)");
            }
        }

        // Editor'de test için
        #if UNITY_EDITOR
        [ContextMenu("Check HUD Status")]
        public void CheckHUDStatus()
        {
            var hud = GetComponent<NetworkManagerHUD>();
            if (hud == null)
            {
                Debug.Log("✅ NetworkManagerHUD component does NOT exist");
            }
            else
            {
                Debug.Log($"⚠️ NetworkManagerHUD EXISTS - Enabled: {hud.enabled}");
            }

            var simpleHud = GetComponent<TacticalCombat.Network.SimpleNetworkHUD>();
            if (simpleHud == null)
            {
                Debug.Log("✅ SimpleNetworkHUD component does NOT exist");
            }
            else
            {
                Debug.Log($"⚠️ SimpleNetworkHUD EXISTS - Enabled: {simpleHud.enabled} (This shows LAN HOST/CLIENT!)");
            }
        }
        #endif
    }
}
