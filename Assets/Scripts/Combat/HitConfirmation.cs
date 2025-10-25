using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Hit Confirmation System
    /// Oyuncuya vurduğunu gösterir - kritik UX
    /// - Visual hit marker
    /// - Audio feedback
    /// - Haptic feedback (optional)
    /// - Damage numbers integration
    /// </summary>
    public class HitConfirmation : MonoBehaviour
    {
        [Header("🎯 Hit Marker")]
        [SerializeField] private Image hitMarkerImage;
        [SerializeField] private Color normalHitColor = Color.white;
        [SerializeField] private Color headshotColor = Color.red;
        [SerializeField] private Color criticalHitColor = Color.yellow;
        [SerializeField] private float hitMarkerDuration = 0.2f;
        [SerializeField] private float hitMarkerScale = 1.2f;
        
        [Header("🔊 Audio")]
        [SerializeField] private AudioClip normalHitSound;
        [SerializeField] private AudioClip headshotSound;
        [SerializeField] private AudioClip criticalHitSound;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float hitSoundVolume = 0.5f;
        
        [Header("📳 Haptic Feedback")]
        [SerializeField] private bool useHapticFeedback = true;
        
        [Header("🎨 Visual Effects")]
        [SerializeField] private bool showDamageNumbers = true;
        [SerializeField] private GameObject damageNumberPrefab;
        [SerializeField] private Canvas worldCanvas;
        
        [Header("📊 Stats")]
        [SerializeField] private bool showDebug = false;
        
        // State
        private Vector3 originalHitMarkerScale;
        private Coroutine currentHitMarkerCoroutine;
        
        // Stats tracking
        private int totalHits = 0;
        private int headshots = 0;
        private int criticalHits = 0;
        
        private void Awake()
        {
            // Setup audio source
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
                audioSource.spatialBlend = 0f; // 2D sound
            }
            
            // Setup hit marker
            if (hitMarkerImage != null)
            {
                hitMarkerImage.enabled = false;
                originalHitMarkerScale = hitMarkerImage.transform.localScale;
            }
            else
            {
                CreateDefaultHitMarker();
            }
            
            // Find world canvas for damage numbers
            if (worldCanvas == null && showDamageNumbers)
            {
                GameObject canvasGO = new GameObject("HitConfirmationCanvas");
                worldCanvas = canvasGO.AddComponent<Canvas>();
                worldCanvas.renderMode = RenderMode.WorldSpace;
            }
        }
        
        private void CreateDefaultHitMarker()
        {
            // Create hit marker UI
            GameObject hitMarkerGO = new GameObject("HitMarker");
            hitMarkerGO.transform.SetParent(transform);
            
            // Add Canvas if not exists
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("HitConfirmationCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                
                hitMarkerGO.transform.SetParent(canvas.transform);
            }
            
            // Create hit marker image
            hitMarkerImage = hitMarkerGO.AddComponent<Image>();
            hitMarkerImage.color = normalHitColor;
            hitMarkerImage.enabled = false;
            
            // Set position to center
            RectTransform rect = hitMarkerImage.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(50, 50);
            
            originalHitMarkerScale = rect.localScale;
            
            Debug.Log("✅ [HitConfirmation] Default hit marker created");
        }
        
        /// <summary>
        /// Show hit confirmation - ana metod
        /// </summary>
        public void ShowHit(bool isHeadshot = false, bool isCritical = false)
        {
            totalHits++;
            
            if (isHeadshot)
            {
                headshots++;
                ShowHeadshotConfirmation();
            }
            else if (isCritical)
            {
                criticalHits++;
                ShowCriticalHitConfirmation();
            }
            else
            {
                ShowNormalHitConfirmation();
            }
            
            if (showDebug)
            {
                Debug.Log($"💥 [HitConfirmation] Hit confirmed! Total: {totalHits}, Headshots: {headshots}, Crits: {criticalHits}");
            }
        }
        
        /// <summary>
        /// Normal hit confirmation
        /// </summary>
        private void ShowNormalHitConfirmation()
        {
            ShowHitMarker(normalHitColor);
            PlayHitSound(normalHitSound);
            
            if (useHapticFeedback)
            {
                TriggerHaptic(HapticType.Light);
            }
        }
        
        /// <summary>
        /// Headshot confirmation - extra emphasis
        /// </summary>
        private void ShowHeadshotConfirmation()
        {
            ShowHitMarker(headshotColor, 1.5f);
            PlayHitSound(headshotSound);
            
            if (useHapticFeedback)
            {
                TriggerHaptic(HapticType.Heavy);
            }
            
            if (showDebug)
            {
                Debug.Log("🎯 [HitConfirmation] HEADSHOT!");
            }
        }
        
        /// <summary>
        /// Critical hit confirmation
        /// </summary>
        private void ShowCriticalHitConfirmation()
        {
            ShowHitMarker(criticalHitColor, 1.3f);
            PlayHitSound(criticalHitSound);
            
            if (useHapticFeedback)
            {
                TriggerHaptic(HapticType.Medium);
            }
        }
        
        /// <summary>
        /// Visual hit marker display
        /// </summary>
        private void ShowHitMarker(Color color, float scaleMultiplier = 1f)
        {
            if (hitMarkerImage == null) return;
            
            // Cancel previous animation
            if (currentHitMarkerCoroutine != null)
            {
                StopCoroutine(currentHitMarkerCoroutine);
            }
            
            // Start new animation
            currentHitMarkerCoroutine = StartCoroutine(HitMarkerAnimation(color, scaleMultiplier));
        }
        
        private IEnumerator HitMarkerAnimation(Color color, float scaleMultiplier)
        {
            if (hitMarkerImage == null) yield break;
            
            // Enable and set color
            hitMarkerImage.enabled = true;
            hitMarkerImage.color = color;
            
            // Scale up
            Vector3 targetScale = originalHitMarkerScale * hitMarkerScale * scaleMultiplier;
            hitMarkerImage.transform.localScale = targetScale;
            
            // Wait
            yield return new WaitForSeconds(hitMarkerDuration);
            
            // Fade out
            float fadeTime = 0.1f;
            float elapsed = 0f;
            
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeTime);
                hitMarkerImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }
            
            // Disable
            hitMarkerImage.enabled = false;
            hitMarkerImage.transform.localScale = originalHitMarkerScale;
        }
        
        /// <summary>
        /// Play hit sound
        /// </summary>
        private void PlayHitSound(AudioClip clip)
        {
            if (audioSource == null || clip == null) return;
            
            audioSource.PlayOneShot(clip, hitSoundVolume);
        }
        
        /// <summary>
        /// Trigger haptic feedback (mobile/controller)
        /// </summary>
        private void TriggerHaptic(HapticType type)
        {
            // TODO: Implement platform-specific haptics
            // For PC: Controller rumble
            // For Mobile: Vibration
            
            switch (type)
            {
                case HapticType.Light:
                    // Light tap
                    break;
                case HapticType.Medium:
                    // Medium rumble
                    break;
                case HapticType.Heavy:
                    // Strong rumble
                    break;
            }
        }
        
        /// <summary>
        /// Show damage number at world position
        /// </summary>
        public void ShowDamageNumber(Vector3 worldPosition, float damage, bool isCritical = false)
        {
            if (!showDamageNumbers || damageNumberPrefab == null) return;
            
            // Use DamageNumbers system if available
            if (DamageNumbers.Instance != null)
            {
                DamageNumbers.Instance.ShowDamage(worldPosition, damage, isCritical);
            }
        }
        
        // Public API
        public void ShowHitWithDamage(Vector3 worldPosition, float damage, bool isHeadshot = false, bool isCritical = false)
        {
            ShowHit(isHeadshot, isCritical);
            ShowDamageNumber(worldPosition, damage, isCritical || isHeadshot);
        }
        
        public int GetTotalHits() => totalHits;
        public int GetHeadshots() => headshots;
        public int GetCriticalHits() => criticalHits;
        public float GetHeadshotPercentage() => totalHits > 0 ? (float)headshots / totalHits * 100f : 0f;
        
        public void ResetStats()
        {
            totalHits = 0;
            headshots = 0;
            criticalHits = 0;
        }
        
        // Debug UI
        private void OnGUI()
        {
            if (!showDebug) return;
            
            GUILayout.BeginArea(new Rect(10, 400, 300, 150));
            GUILayout.BeginVertical("box");
            
            GUILayout.Label("<b>Hit Confirmation Stats</b>");
            GUILayout.Label($"Total Hits: {totalHits}");
            GUILayout.Label($"Headshots: {headshots} ({GetHeadshotPercentage():F1}%)");
            GUILayout.Label($"Critical Hits: {criticalHits}");
            
            if (GUILayout.Button("Reset Stats"))
            {
                ResetStats();
            }
            
            GUILayout.EndVertical();
            GUILayout.EndArea();
        }
    }
    
    /// <summary>
    /// Haptic feedback types
    /// </summary>
    public enum HapticType
    {
        Light,
        Medium,
        Heavy
    }
}
