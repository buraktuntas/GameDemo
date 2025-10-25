using UnityEngine;
using UnityEngine.UI;
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
        private Combat.WeaponSystem weaponSystem;
        
        private void Awake()
        {
            _instance = this;
            
            if (crosshairImage != null)
            {
                originalCrosshairSize = crosshairImage.rectTransform.sizeDelta;
            }
            
            // Hide reload bar initially
            if (reloadProgressBar != null)
                reloadProgressBar.fillAmount = 0f;
        }
        
        private void Start()
        {
            // Find local player's weapon system
            StartCoroutine(FindLocalPlayerWeapon());
        }
        
        private System.Collections.IEnumerator FindLocalPlayerWeapon()
        {
            // Wait for local player to spawn
            yield return new WaitForSeconds(1f);
            
            // Find all weapon systems
            var weapons = FindObjectsByType<Combat.WeaponSystem>(FindObjectsSortMode.None);
            
            foreach (var weapon in weapons)
            {
                if (weapon.isLocalPlayer)
                {
                    weaponSystem = weapon;
                    
                    // Subscribe to events
                    weapon.OnAmmoChanged += UpdateAmmoDisplay;
                    weapon.OnWeaponFired += OnWeaponFired;
                    weapon.OnReloadStarted += OnReloadStarted;
                    weapon.OnReloadComplete += OnReloadComplete;
                    
                    Debug.Log("✅ Combat UI connected to local player weapon");
                    break;
                }
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // CROSSHAIR
        // ═══════════════════════════════════════════════════════════
        
        public void ShowHitFeedback(bool isHeadshot = false)
        {
            if (crosshairImage == null) return;
            
            // Change color briefly
            Color targetColor = isHeadshot ? headshotColor : hitColor;
            StartCoroutine(FlashCrosshair(targetColor));
            
            // Show hit marker
            ShowHitMarker(isHeadshot);
        }
        
        private IEnumerator FlashCrosshair(Color flashColor)
        {
            if (crosshairImage == null) yield break;
            
            crosshairImage.color = flashColor;
            yield return new WaitForSeconds(0.1f);
            crosshairImage.color = normalColor;
        }
        
        private void OnWeaponFired()
        {
            // Expand crosshair on fire
            if (crosshairImage == null) return;
            
            StopAllCoroutines();
            StartCoroutine(ExpandCrosshair());
        }
        
        private IEnumerator ExpandCrosshair()
        {
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
            if (marker == null) yield break;
            
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
