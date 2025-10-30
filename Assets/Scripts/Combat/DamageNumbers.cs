using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Damage number display system
    /// </summary>
    public class DamageNumbers : MonoBehaviour
    {
        [Header("UI References")]
        public Canvas damageCanvas;
        public GameObject damageTextPrefab;
        
        [Header("Settings")]
        public float displayDuration = 2f;
        public float floatSpeed = 2f;
        public float fadeSpeed = 1f;
        public Vector3 randomOffset = new Vector3(0.5f, 0, 0.5f);
        
        [Header("Colors")]
        public Color normalDamageColor = Color.white;
        public Color criticalDamageColor = Color.red;
        public Color healColor = Color.green;

        [Header("Performance Settings")]
        public int poolSize = 20;  // ✅ PERFORMANCE FIX: Object pool size

        // ✅ PERFORMANCE FIX: Object pooling to avoid Instantiate spam
        private System.Collections.Generic.Queue<GameObject> damageTextPool;

        private static DamageNumbers instance;
        public static DamageNumbers Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<DamageNumbers>();
                    if (instance == null)
                    {
                        GameObject go = new GameObject("DamageNumbers");
                        instance = go.AddComponent<DamageNumbers>();
                    }
                }
                return instance;
            }
        }
        
        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeDamageSystem();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeDamageSystem()
        {
            // Canvas oluştur
            if (damageCanvas == null)
            {
                GameObject canvasGO = new GameObject("DamageCanvas");
                damageCanvas = canvasGO.AddComponent<Canvas>();
                damageCanvas.renderMode = RenderMode.WorldSpace;
                damageCanvas.sortingOrder = 100;

                // Canvas Scaler ekle
                CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                // Graphic Raycaster ekle
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            // Damage text prefab oluştur
            if (damageTextPrefab == null)
            {
                CreateDamageTextPrefab();
            }

            // ✅ PERFORMANCE FIX: Initialize object pool
            InitializePool();

            Debug.Log("✅ Damage Numbers system initialized");
        }

        /// <summary>
        /// ✅ PERFORMANCE FIX: Initialize damage text object pool
        /// </summary>
        private void InitializePool()
        {
            damageTextPool = new System.Collections.Generic.Queue<GameObject>();

            for (int i = 0; i < poolSize; i++)
            {
                GameObject textGO = Instantiate(damageTextPrefab, damageCanvas.transform);
                textGO.SetActive(false);
                damageTextPool.Enqueue(textGO);
            }

            Debug.Log($"✅ Damage text pool initialized with {poolSize} objects");
        }

        /// <summary>
        /// ✅ PERFORMANCE FIX: Get damage text from pool instead of Instantiate
        /// </summary>
        private GameObject GetPooledDamageText()
        {
            if (damageTextPool == null || damageTextPool.Count == 0)
            {
                // Fallback: create new if pool exhausted
                GameObject newTextGO = Instantiate(damageTextPrefab, damageCanvas.transform);
                Debug.LogWarning("⚠️ Damage text pool exhausted, creating new instance");
                return newTextGO;
            }

            GameObject textGO = damageTextPool.Dequeue();
            textGO.SetActive(true);
            return textGO;
        }

        /// <summary>
        /// ✅ PERFORMANCE FIX: Return damage text to pool instead of Destroy
        /// </summary>
        private void ReturnToPool(GameObject textGO)
        {
            if (textGO == null) return;

            textGO.SetActive(false);

            // Reset to default state
            Text text = textGO.GetComponent<Text>();
            if (text != null)
            {
                text.fontSize = 24;
                text.color = normalDamageColor;
            }

            damageTextPool.Enqueue(textGO);
        }
        
        private void CreateDamageTextPrefab()
        {
            GameObject textGO = new GameObject("DamageText");
            textGO.transform.SetParent(damageCanvas.transform);
            
            Text text = textGO.AddComponent<Text>();
            
            // Unity 6 font fix - Robust font fallback chain
            text.font = null;
            try
            {
                text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            }
            catch {}
            if (text.font == null)
            {
                // Try built-in Arial
                try { text.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); }
                catch { }
            }
            if (text.font == null)
            {
                // Last resort: use any available font in Resources
                text.font = Resources.Load<Font>("Arial");
            }
            if (text.font == null)
            {
                Debug.LogWarning("⚠️ [DamageNumbers] No font found. Using default dynamic font.");
                text.font = Font.CreateDynamicFontFromOSFont("Verdana", 24);
            }
            
            text.fontSize = 24;
            text.color = normalDamageColor;
            text.alignment = TextAnchor.MiddleCenter;
            text.text = "100";
            
            // Outline ekle
            Outline outline = textGO.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, 2);
            
            damageTextPrefab = textGO;
            damageTextPrefab.SetActive(false);
            
            Debug.Log("✅ DamageTextPrefab created successfully");
        }
        
        public void ShowDamage(Vector3 worldPosition, float damage, bool isCritical = false, bool isHeal = false)
        {
            StartCoroutine(DisplayDamageCoroutine(worldPosition, damage, isCritical, isHeal));
        }
        
        private IEnumerator DisplayDamageCoroutine(Vector3 worldPosition, float damage, bool isCritical, bool isHeal)
        {
            // Random offset ekle
            Vector3 offset = new Vector3(
                Random.Range(-randomOffset.x, randomOffset.x),
                Random.Range(-randomOffset.y, randomOffset.y),
                Random.Range(-randomOffset.z, randomOffset.z)
            );

            Vector3 startPosition = worldPosition + offset;

            // ✅ PERFORMANCE FIX: Get from pool instead of Instantiate
            if (damageTextPrefab == null)
            {
                Debug.LogError("❌ DamageTextPrefab is null! Cannot show damage numbers.");
                yield break;
            }

            GameObject damageTextGO = GetPooledDamageText();
            if (damageTextGO == null)
            {
                Debug.LogError("❌ Failed to get pooled damage text!");
                yield break;
            }

            Text damageText = damageTextGO.GetComponent<Text>();
            damageText.text = Mathf.RoundToInt(damage).ToString();

            // Renk ayarla
            if (isHeal)
            {
                damageText.color = healColor;
                damageText.text = "+" + damageText.text;
            }
            else if (isCritical)
            {
                damageText.color = criticalDamageColor;
                damageText.fontSize = 32;
                damageText.text = "CRIT! " + damageText.text;
            }
            else
            {
                damageText.color = normalDamageColor;
            }

            // Pozisyon ayarla
            damageTextGO.transform.position = startPosition;

            // Animasyon
            float timer = 0f;
            Vector3 startPos = startPosition;
            Vector3 endPos = startPosition + Vector3.up * 2f;
            Color startColor = damageText.color;
            Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f);

            while (timer < displayDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / displayDuration;

                // Pozisyon animasyonu
                damageTextGO.transform.position = Vector3.Lerp(startPos, endPos, progress);

                // Fade animasyonu
                damageText.color = Color.Lerp(startColor, endColor, progress * fadeSpeed);

                yield return null;
            }

            // ✅ PERFORMANCE FIX: Return to pool instead of Destroy
            ReturnToPool(damageTextGO);
        }
        
        // Utility methods
        public void ShowDamageAtTransform(Transform target, float damage, bool isCritical = false, bool isHeal = false)
        {
            if (target != null)
            {
                ShowDamage(target.position + Vector3.up, damage, isCritical, isHeal);
            }
        }
        
        public void ShowDamageAtPlayer(Transform player, float damage, bool isCritical = false, bool isHeal = false)
        {
            if (player != null)
            {
                // Player'ın baş seviyesinde göster
                Vector3 headPosition = player.position + Vector3.up * 1.8f;
                ShowDamage(headPosition, damage, isCritical, isHeal);
            }
        }
    }
}
