using UnityEngine;
using UnityEngine.UI;

namespace TacticalCombat.Debugging
{
    /// <summary>
    /// AGGRESSIVE crosshair disabler - runs every frame to find and hide crosshairs
    /// Attach to MainMenu GameObject
    /// </summary>
    public class ForceDisableCrosshair : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private bool runOnStart = true;
        [SerializeField] private bool runEveryFrame = true;

        private void Start()
        {
            if (runOnStart)
            {
                Debug.Log("🎯 ForceDisableCrosshair: Starting aggressive crosshair search...");
                DisableAllCrosshairs();
            }
        }

        private void Update()
        {
            if (runEveryFrame)
            {
                DisableAllCrosshairs();
            }
        }

        [ContextMenu("Force Disable All Crosshairs NOW")]
        public void DisableAllCrosshairs()
        {
            int disabledCount = 0;

            // Method 1: Find by name
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject obj in allObjects)
            {
                string name = obj.name.ToLower();
                if (name.Contains("crosshair") || name.Contains("reticle") || name.Contains("nişan"))
                {
                    if (obj.activeSelf)
                    {
                        obj.SetActive(false);
                        disabledCount++;
                        Debug.Log($"✅ Disabled by name: {GetPath(obj)}");
                    }
                }
            }

            // Method 2: Find all Images in center of screen and disable raycast
            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (Canvas canvas in canvases)
            {
                Image[] images = canvas.GetComponentsInChildren<Image>(true);
                foreach (Image img in images)
                {
                    // Check if image is near screen center
                    RectTransform rt = img.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 anchoredPos = rt.anchoredPosition;
                        float distanceFromCenter = anchoredPos.magnitude;

                        // If within 100 pixels of center
                        if (distanceFromCenter < 100f)
                        {
                            string objName = img.gameObject.name.ToLower();
                            if (objName.Contains("cross") || objName.Contains("aim") ||
                                objName.Contains("reticle") || objName.Contains("center") ||
                                objName.Contains("dot"))
                            {
                                // Disable raycast AND hide it
                                img.raycastTarget = false;
                                img.enabled = false;
                                img.gameObject.SetActive(false);
                                disabledCount++;
                                Debug.Log($"✅ Disabled center image: {GetPath(img.gameObject)} (pos: {anchoredPos})");
                            }
                        }
                    }
                }
            }

            // Method 3: Find by component type
            var crosshairControllers = FindObjectsByType<TacticalCombat.UI.CrosshairController>(FindObjectsSortMode.None);
            foreach (var controller in crosshairControllers)
            {
                if (controller.gameObject.activeSelf)
                {
                    controller.gameObject.SetActive(false);
                    disabledCount++;
                    Debug.Log($"✅ Disabled CrosshairController: {GetPath(controller.gameObject)}");
                }
            }

            var simpleCrosshairs = FindObjectsByType<TacticalCombat.UI.SimpleCrosshair>(FindObjectsSortMode.None);
            foreach (var crosshair in simpleCrosshairs)
            {
                if (crosshair.gameObject.activeSelf)
                {
                    crosshair.gameObject.SetActive(false);
                    disabledCount++;
                    Debug.Log($"✅ Disabled SimpleCrosshair: {GetPath(crosshair.gameObject)}");
                }
            }

            // Method 4: Disable entire CombatUI
            var combatUIs = FindObjectsByType<TacticalCombat.UI.CombatUI>(FindObjectsSortMode.None);
            foreach (var combatUI in combatUIs)
            {
                if (combatUI.gameObject.activeSelf)
                {
                    combatUI.gameObject.SetActive(false);
                    disabledCount++;
                    Debug.Log($"✅ Disabled CombatUI: {GetPath(combatUI.gameObject)}");
                }
            }

            if (disabledCount > 0)
            {
                Debug.Log($"🎯 Total crosshairs/UI disabled: {disabledCount}");
            }
        }

        private string GetPath(GameObject obj)
        {
            string path = obj.name;
            Transform parent = obj.transform.parent;

            while (parent != null)
            {
                path = parent.name + "/" + path;
                parent = parent.parent;
            }

            return path;
        }

        [ContextMenu("List All Center UI Elements")]
        public void ListCenterUIElements()
        {
            Debug.Log("═══════════════════════════════════════");
            Debug.Log("🔍 SEARCHING FOR CENTER UI ELEMENTS");
            Debug.Log("═══════════════════════════════════════");

            Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            int foundCount = 0;

            foreach (Canvas canvas in canvases)
            {
                Debug.Log($"\n📐 Canvas: {canvas.name}");

                Image[] images = canvas.GetComponentsInChildren<Image>(true);
                foreach (Image img in images)
                {
                    RectTransform rt = img.GetComponent<RectTransform>();
                    if (rt != null)
                    {
                        Vector2 anchoredPos = rt.anchoredPosition;
                        float distanceFromCenter = anchoredPos.magnitude;

                        // If within 150 pixels of center
                        if (distanceFromCenter < 150f)
                        {
                            foundCount++;
                            Debug.Log($"   🎯 {GetPath(img.gameObject)}");
                            Debug.Log($"      Position: {anchoredPos}, Distance: {distanceFromCenter:F1}");
                            Debug.Log($"      Active: {img.gameObject.activeSelf}, Enabled: {img.enabled}");
                            Debug.Log($"      RaycastTarget: {img.raycastTarget}");
                        }
                    }
                }
            }

            Debug.Log($"\n✅ Found {foundCount} UI elements near screen center");
            Debug.Log("═══════════════════════════════════════");
        }
    }
}
