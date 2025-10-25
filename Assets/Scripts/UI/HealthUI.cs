using UnityEngine;
using UnityEngine.UI;
using Mirror;
using TacticalCombat.Combat;

namespace TacticalCombat.UI
{
    /// <summary>
    /// Health bar ve UI elementleri
    /// </summary>
    public class HealthUI : NetworkBehaviour
    {
        [Header("UI References")]
        public Slider healthSlider;
        public Image healthFill;
        public Text healthText;
        public Text playerNameText;
        
        [Header("Colors")]
        public Color healthyColor = Color.green;
        public Color damagedColor = Color.yellow;
        public Color criticalColor = Color.red;
        
        [Header("Settings")]
        public bool showHealthText = true;
        public bool showPlayerName = true;
        public float updateSpeed = 5f;
        
        private Health playerHealth;
        private float targetHealthValue;
        private bool isInitialized = false;
        
        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            InitializeHealthUI();
        }
        
        private void InitializeHealthUI()
        {
            // Health component'i bul
            playerHealth = GetComponent<Health>();
            if (playerHealth == null)
            {
                Debug.LogError("❌ Health component not found!");
                return;
            }
            
            // UI elementlerini bul
            if (healthSlider == null)
            {
                healthSlider = FindFirstObjectByType<Slider>();
            }
            
            if (healthFill == null && healthSlider != null)
            {
                healthFill = healthSlider.fillRect.GetComponent<Image>();
            }
            
            if (healthText == null)
            {
                healthText = GameObject.Find("HealthText")?.GetComponent<Text>();
            }
            
            if (playerNameText == null)
            {
                playerNameText = GameObject.Find("PlayerNameText")?.GetComponent<Text>();
            }
            
            // Health değerini başlat
            if (healthSlider != null)
            {
                healthSlider.maxValue = playerHealth.MaxHealth;
                healthSlider.value = playerHealth.CurrentHealth;
                targetHealthValue = playerHealth.CurrentHealth;
            }
            
            // Player name'i ayarla
            if (playerNameText != null && showPlayerName)
            {
                playerNameText.text = $"Player {netId}";
            }
            
            isInitialized = true;
            Debug.Log("✅ Health UI initialized");
        }
        
        private void Update()
        {
            if (!isLocalPlayer || !isInitialized) return;
            
            UpdateHealthDisplay();
        }
        
        private void UpdateHealthDisplay()
        {
            if (playerHealth == null) return;
            
            // Smooth health bar animation
            targetHealthValue = playerHealth.CurrentHealth;
            
            if (healthSlider != null)
            {
                healthSlider.value = Mathf.Lerp(healthSlider.value, targetHealthValue, updateSpeed * Time.deltaTime);
                
                // Health bar rengini güncelle
                if (healthFill != null)
                {
                    float healthPercentage = healthSlider.value / healthSlider.maxValue;
                    healthFill.color = GetHealthColor(healthPercentage);
                }
            }
            
            // Health text'i güncelle
            if (healthText != null && showHealthText)
            {
                healthText.text = $"{Mathf.RoundToInt(targetHealthValue)}/{playerHealth.MaxHealth}";
            }
        }
        
        private Color GetHealthColor(float healthPercentage)
        {
            if (healthPercentage > 0.6f)
                return healthyColor;
            else if (healthPercentage > 0.3f)
                return damagedColor;
            else
                return criticalColor;
        }
        
        // Health değişikliklerini dinle
        private void OnHealthChanged(float newHealth, float maxHealth)
        {
            if (!isLocalPlayer) return;
            
            targetHealthValue = newHealth;
            
            // Health bar'ı güncelle
            if (healthSlider != null)
            {
                healthSlider.maxValue = maxHealth;
            }
        }
        
        // Health component'ten event dinle
        private void OnEnable()
        {
            if (playerHealth != null)
            {
                // Health component'te event varsa dinle
                // playerHealth.OnHealthChanged += OnHealthChanged;
            }
        }
        
        private void OnDisable()
        {
            if (playerHealth != null)
            {
                // playerHealth.OnHealthChanged -= OnHealthChanged;
            }
        }
        
        // Debug için OnGUI
        private void OnGUI()
        {
            if (!isLocalPlayer || !isInitialized) return;
            
            // Health bilgilerini ekranda göster
            GUILayout.BeginArea(new Rect(10, 10, 200, 100));
            GUILayout.Label($"Health: {Mathf.RoundToInt(targetHealthValue)}/{playerHealth.MaxHealth}");
            GUILayout.Label($"Player: {netId}");
            GUILayout.EndArea();
        }
    }
}
