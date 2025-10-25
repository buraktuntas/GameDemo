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
            
            Debug.Log("✅ Damage Numbers system initialized");
        }
        
        private void CreateDamageTextPrefab()
        {
            GameObject textGO = new GameObject("DamageText");
            textGO.transform.SetParent(damageCanvas.transform);
            
            Text text = textGO.AddComponent<Text>();
            
            // Unity 6 font fix - Dynamic font creation
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 24);
            if (text.font == null)
            {
                text.font = Resources.Load<Font>("Arial");
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
            
            // Damage text oluştur
            if (damageTextPrefab == null)
            {
                Debug.LogError("❌ DamageTextPrefab is null! Cannot show damage numbers.");
                yield break;
            }
            
            GameObject damageTextGO = Instantiate(damageTextPrefab, damageCanvas.transform);
            damageTextGO.SetActive(true);
            
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
            
            // Temizle
            Destroy(damageTextGO);
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
