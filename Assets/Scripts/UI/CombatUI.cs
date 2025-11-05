using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TMPro;
using System.Collections;

namespace TacticalCombat.UI
{
    /// <summary>
    /// COMBAT UI - Professional FPS UI
    /// - Hit markers
    /// - Crosshair feedback
    /// - Ammo counter
    /// - Reload indicator
    /// </summary>
    public class CombatUI : MonoBehaviour
    {
        private static CombatUI _instance;
        public static CombatUI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<CombatUI>();
                }
                return _instance;
            }
        }
        
        [Header("🎯 CROSSHAIR")]
        [SerializeField] private Image crosshairImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private Color headshotColor = new Color(1f, 0.5f, 0f);
        [SerializeField] private float crosshairExpandAmount = 10f;
        
        [Header("💥 HIT MARKER")]
        [SerializeField] private Image hitMarker;
        [SerializeField] private Image headshotMarker;
        [SerializeField] private float hitMarkerDuration = 0.2f;
        
        [Header("📊 AMMO")]
        [SerializeField] public TextMeshProUGUI ammoText;
        [SerializeField] public TextMeshProUGUI reserveAmmoText;
        [SerializeField] public Image reloadProgressBar;
        
        [Header("ℹ️ INFO")]
        [SerializeField] private TextMeshProUGUI weaponNameText;
        [SerializeField] private TextMeshProUGUI fireModText;
        
        private Vector2 originalCrosshairSize;
        private Coroutine hitMarkerCoroutine;
        private Coroutine flashCrosshairCoroutine;
        private Coroutine expandCrosshairCoroutine;
        private Combat.WeaponSystem weaponSystem;
        
        private void Awake()
        {
            _instance = this;
            
            EnsureCanvasAndCrosshair();
            
            // Hide reload bar initially
            if (reloadProgressBar != null)
                reloadProgressBar.fillAmount = 0f;
        }
        
        private void Start()
        {
            // Find local player's weapon system
            StartCoroutine(FindLocalPlayerWeapon());

            // Ensure Canvas targets the local player's camera if needed
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                // Try to find local player's camera
                var controllers = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
                foreach (var ctrl in controllers)
                {
                    if (ctrl != null && ctrl.isLocalPlayer)
                    {
                        var cam = ctrl.GetCamera();
                        if (cam != null)
                        {
                            canvas.worldCamera = cam;
                            // Ensure canvas is enabled and on top
                            canvas.sortingOrder = Mathf.Max(canvas.sortingOrder, 100);
                        }
                        break;
                    }
                }
            }

            // As a final safety, ensure crosshair GameObject is active if found
            if (crosshairImage != null)
            {
                crosshairImage.gameObject.SetActive(true);
                crosshairImage.enabled = true;
            }
        }

        private void EnsureCanvasAndCrosshair()
        {
            // Ensure we have a Canvas in parents; otherwise create one
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                var canvasGO = new GameObject("Canvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
                canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
                transform.SetParent(canvasGO.transform, false);
            }

            // If crosshair already assigned, cache its size
            if (crosshairImage != null)
            {
                originalCrosshairSize = crosshairImage.rectTransform.sizeDelta;
                return;
            }

            // Try to find existing crosshair Image in children by name
            var images = GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var img in images)
            {
                var n = img.name.ToLower();
                if (n.Contains("cross") || n.Contains("reticle") || n.Contains("aim"))
                {
                    crosshairImage = img;
                    originalCrosshairSize = crosshairImage.rectTransform.sizeDelta;
                    return;
                }
            }

            // Create a minimal crosshair if none found
            var go = new GameObject("Crosshair");
            go.transform.SetParent(canvas.transform, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(16, 16);

            crosshairImage = go.AddComponent<Image>();
            crosshairImage.raycastTarget = false;
            crosshairImage.color = normalColor;

            // Create a simple white sprite from built-in texture
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            crosshairImage.sprite = sprite;

            // Shape crosshair as plus: use child lines
            CreateCrosshairLines(go.transform, tex);

            originalCrosshairSize = crosshairImage.rectTransform.sizeDelta;
        }

        private void CreateCrosshairLines(Transform parent, Texture2D tex)
        {
            // Horizontal
            var h = new GameObject("H");
            h.transform.SetParent(parent, false);
            var hImg = h.AddComponent<Image>();
            hImg.raycastTarget = false;
            hImg.color = normalColor;
            hImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            var hrt = h.GetComponent<RectTransform>();
            hrt.sizeDelta = new Vector2(24, 2);
            hrt.anchoredPosition = Vector2.zero;

            // Vertical
            var v = new GameObject("V");
            v.transform.SetParent(parent, false);
            var vImg = v.AddComponent<Image>();
            vImg.raycastTarget = false;
            vImg.color = normalColor;
            vImg.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            var vrt = v.GetComponent<RectTransform>();
            vrt.sizeDelta = new Vector2(2, 24);
            vrt.anchoredPosition = Vector2.zero;
        }
        
        private System.Collections.IEnumerator FindLocalPlayerWeapon()
        {
            // Wait for local player to spawn
            yield return new WaitForSeconds(0.5f);

            // Prefer weapon under local player's hierarchy
            var controllers = FindObjectsByType<TacticalCombat.Player.FPSController>(FindObjectsSortMode.None);
            foreach (var ctrl in controllers)
            {
                if (ctrl != null && ctrl.isLocalPlayer)
                {
                    var weapon = ctrl.GetComponentInChildren<Combat.WeaponSystem>(true);
                    if (weapon != null)
                    {
                        BindWeapon(weapon);
                        yield break;
                    }
                }
            }

            // Fallback: bind weapon associated with our canvas camera if any
            var weapons = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            var canvas = GetComponentInParent<Canvas>();
            Camera cam = canvas != null ? canvas.worldCamera : null;
            if (cam != null)
            {
                foreach (var w in weapons)
                {
                    if (w == null) continue;
                    var owningCam = w.GetComponentInParent<Camera>(true);
                    if (owningCam == cam)
                    {
                        BindWeapon(w);
                        yield break;
                    }
                }
            }

            // Final fallback: first available weapon
            if (weapons.Length > 0 && weapons[0] != null)
            {
                BindWeapon(weapons[0]);
            }
        }

        private void BindWeapon(Combat.WeaponSystem weapon)
        {
            weaponSystem = weapon;

            weapon.OnAmmoChanged += UpdateAmmoDisplay;
            weapon.OnWeaponFired += OnWeaponFired;
            weapon.OnReloadStarted += OnReloadStarted;
            weapon.OnReloadComplete += OnReloadComplete;

            Debug.Log("✅ [CombatUI] Bound to local weapon system");
        }
        
        // ═══════════════════════════════════════════════════════════
        // CROSSHAIR
        // ═══════════════════════════════════════════════════════════
        
        public void ShowHitFeedback(bool isHeadshot = false)
        {
            if (crosshairImage == null) return;
            
            // Change color briefly
            Color targetColor = isHeadshot ? headshotColor : hitColor;
            
            // ✅ PERFORMANCE FIX: Track coroutine to prevent leaks
            if (flashCrosshairCoroutine != null)
                StopCoroutine(flashCrosshairCoroutine);
            flashCrosshairCoroutine = StartCoroutine(FlashCrosshair(targetColor));
            
            // Show hit marker
            ShowHitMarker(isHeadshot);
        }
        
        private IEnumerator FlashCrosshair(Color flashColor)
        {
            if (crosshairImage == null)
            {
                flashCrosshairCoroutine = null;
                yield break;
            }
            
            crosshairImage.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            crosshairImage.color = normalColor;
            flashCrosshairCoroutine = null; // ✅ Cleanup reference
        }
        
        private void OnWeaponFired()
        {
            // Expand crosshair on fire
            if (crosshairImage == null) return;
            
            // ✅ PERFORMANCE FIX: Stop specific coroutines instead of StopAllCoroutines (prevents killing other coroutines)
            if (flashCrosshairCoroutine != null)
                StopCoroutine(flashCrosshairCoroutine);
            if (expandCrosshairCoroutine != null)
                StopCoroutine(expandCrosshairCoroutine);
            if (hitMarkerCoroutine != null)
                StopCoroutine(hitMarkerCoroutine);
                
            expandCrosshairCoroutine = StartCoroutine(ExpandCrosshair());
        }
        
        private IEnumerator ExpandCrosshair()
        {
            if (crosshairImage == null)
            {
                expandCrosshairCoroutine = null;
                yield break;
            }
            
            Vector2 expandedSize = originalCrosshairSize + Vector2.one * crosshairExpandAmount;
            
            // Expand
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 10f;
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(originalCrosshairSize, expandedSize, t);
                yield return null;
            }
            
            // Contract
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime * 5f;
                crosshairImage.rectTransform.sizeDelta = Vector2.Lerp(expandedSize, originalCrosshairSize, t);
                yield return null;
            }
            
            crosshairImage.rectTransform.sizeDelta = originalCrosshairSize;
            expandCrosshairCoroutine = null; // ✅ Cleanup reference
        }
        
        // ═══════════════════════════════════════════════════════════
        // HIT MARKER
        // ═══════════════════════════════════════════════════════════
        
        public void ShowHitMarker(bool isHeadshot = false)
        {
            if (hitMarkerCoroutine != null)
                StopCoroutine(hitMarkerCoroutine);
                
            hitMarkerCoroutine = StartCoroutine(ShowHitMarkerCoroutine(isHeadshot));
        }
        
        private IEnumerator ShowHitMarkerCoroutine(bool isHeadshot)
        {
            Image marker = isHeadshot ? headshotMarker : hitMarker;
            if (marker == null)
            {
                hitMarkerCoroutine = null;
                yield break;
            }
            
            // Show
            marker.gameObject.SetActive(true);
            Color startColor = marker.color;
            startColor.a = 1f;
            marker.color = startColor;
            
            // Fade out
            float elapsed = 0f;
            while (elapsed < hitMarkerDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = 1f - (elapsed / hitMarkerDuration);
                Color color = marker.color;
                color.a = alpha;
                marker.color = color;
                yield return null;
            }
            
            marker.gameObject.SetActive(false);
            hitMarkerCoroutine = null; // ✅ Cleanup reference
        }
        
        // ✅ PERFORMANCE FIX: Cleanup coroutines on disable
        private void OnDisable()
        {
            if (hitMarkerCoroutine != null)
            {
                StopCoroutine(hitMarkerCoroutine);
                hitMarkerCoroutine = null;
            }
            if (flashCrosshairCoroutine != null)
            {
                StopCoroutine(flashCrosshairCoroutine);
                flashCrosshairCoroutine = null;
            }
            if (expandCrosshairCoroutine != null)
            {
                StopCoroutine(expandCrosshairCoroutine);
                expandCrosshairCoroutine = null;
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // AMMO DISPLAY
        // ═══════════════════════════════════════════════════════════
        
        private void UpdateAmmoDisplay(int current, int reserve)
        {
            Debug.Log($"📊 UpdateAmmoDisplay called: {current}/{reserve}");
            
            if (ammoText != null)
            {
                ammoText.text = current.ToString();
                Debug.Log($"📊 Ammo text updated to: {current}");
                
                // Color feedback
                if (current <= 0)
                    ammoText.color = Color.red;
                else if (current <= 5)
                    ammoText.color = Color.yellow;
                else
                    ammoText.color = Color.white;
            }
            else
            {
                // Try to find ammo text if not assigned
                if (ammoText == null)
                {
                    ammoText = GameObject.Find("AmmoText")?.GetComponent<TextMeshProUGUI>();
                    if (ammoText != null)
                    {
                        Debug.Log("✅ [CombatUI] Found ammoText automatically!");
                        UpdateAmmoDisplay(current, reserve); // Recursive call with found text
                        return;
                    }
                }
                Debug.LogWarning("⚠️ ammoText is null! Please assign ammo text UI element.");
            }
            
            if (reserveAmmoText != null)
            {
                reserveAmmoText.text = $"/ {reserve}";
            }
        }
        
        private void OnReloadStarted()
        {
            if (reloadProgressBar != null)
            {
                reloadProgressBar.gameObject.SetActive(true);
                StartCoroutine(AnimateReload());
            }
        }
        
        private void OnReloadComplete()
        {
            if (reloadProgressBar != null)
            {
                reloadProgressBar.gameObject.SetActive(false);
                reloadProgressBar.fillAmount = 0f;
            }
        }
        
        private IEnumerator AnimateReload()
        {
            if (reloadProgressBar == null) yield break;
            
            float duration = 2f; // Should match weapon reload time
            float elapsed = 0f;
            
            while (elapsed < duration && weaponSystem != null && weaponSystem.IsReloading())
            {
                elapsed += Time.deltaTime;
                reloadProgressBar.fillAmount = elapsed / duration;
                yield return null;
            }
            
            reloadProgressBar.fillAmount = 1f;
        }
        
        // ═══════════════════════════════════════════════════════════
        // WEAPON INFO
        // ═══════════════════════════════════════════════════════════
        
        public void SetWeaponInfo(string weaponName, string fireMode)
        {
            if (weaponNameText != null)
                weaponNameText.text = weaponName;
                
            if (fireModText != null)
                fireModText.text = fireMode;
        }
    }
}
