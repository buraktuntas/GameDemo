using UnityEngine;
using Mirror;
using System.Collections.Generic;

namespace TacticalCombat.Building
{
    /// <summary>
    /// Valheim-style structural integrity system
    /// Yapılar zemin veya güçlü desteklerden uzaklaştıkça zayıflar
    /// </summary>
    public class StructuralIntegrity : NetworkBehaviour
    {
        [Header("Stability Settings")]
        [SerializeField] private float maxStability = 100f; // Zemine bağlı yapılar
        [SerializeField] private float stabilityLossPerMeter = 10f; // Her metre için kayıp
        [SerializeField] private float minStability = 10f; // Bu değerin altında yıkılır
        [SerializeField] private float checkRadius = 2.5f; // Komşu yapıları kontrol için
        
        [Header("Visual Feedback")]
        [SerializeField] private Renderer structureRenderer;
        [SerializeField] private bool showStabilityColor = true;
        
        [SyncVar(hook = nameof(OnStabilityChanged))]
        private float currentStability = 100f;
        
        [SyncVar]
        private bool isGrounded = false; // Zemine bağlı mı?
        
        private static List<StructuralIntegrity> allStructures = new List<StructuralIntegrity>();
        
        // Stability renk kodu (Valheim tarzı)
        private static Color blueStability = new Color(0.3f, 0.6f, 1f);    // 100-80: Çok sağlam
        private static Color greenStability = new Color(0.3f, 1f, 0.3f);   // 80-60: Sağlam
        private static Color yellowStability = new Color(1f, 1f, 0.3f);    // 60-40: Orta
        private static Color orangeStability = new Color(1f, 0.6f, 0.2f);  // 40-20: Zayıf
        private static Color redStability = new Color(1f, 0.2f, 0.2f);     // 20-0: Yıkılmak üzere

        private void Awake()
        {
            if (structureRenderer == null)
                structureRenderer = GetComponentInChildren<Renderer>();
        }

        private void Start()
        {
            allStructures.Add(this);
            
            if (isServer)
            {
                // Server'da stability hesapla
                Invoke(nameof(CalculateStability), 0.5f);
            }
        }

        private void OnDestroy()
        {
            allStructures.Remove(this);
        }

        [Server]
        private void CalculateStability()
        {
            // 1. Zemine bağlı mı kontrol et
            isGrounded = CheckGrounded();
            
            if (isGrounded)
            {
                // Zemine direkt bağlı = maksimum stabilite
                currentStability = maxStability;
            }
            else
            {
                // Zemine bağlı değilse, en yakın destek noktasını bul
                float supportDistance = FindNearestSupport();
                
                if (supportDistance < 0)
                {
                    // Hiç destek yok! Yıkılmalı
                    currentStability = 0f;
                }
                else
                {
                    // Destek mesafesine göre stabilite hesapla
                    float stabilityLoss = supportDistance * stabilityLossPerMeter;
                    currentStability = Mathf.Max(minStability, maxStability - stabilityLoss);
                }
            }
            
            // Çok düşük stabilitede yıkıl
            if (currentStability < minStability)
            {
                CollapseStructure();
            }
            else
            {
                // Komşu yapıları da güncelle (zincirleme etki)
                UpdateNeighborStabilities();
            }
        }

        [Server]
        private bool CheckGrounded()
        {
            // Zemin kontrolü (raycast aşağı)
            Vector3 origin = transform.position;
            float checkDistance = 1.5f;
            
            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, checkDistance))
            {
                // Zemin layer'ına çarptı mı?
                if (hit.collider.gameObject.layer == LayerMask.NameToLayer("Default") ||
                    hit.collider.CompareTag("Ground"))
                {
                    return true;
                }
            }
            
            return false;
        }

        [Server]
        private float FindNearestSupport()
        {
            float nearestDistance = float.MaxValue;
            bool foundSupport = false;
            
            // Yakındaki tüm yapıları tara
            foreach (var other in allStructures)
            {
                if (other == this || other == null) continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                
                // Komşu mu? (checkRadius içinde)
                if (distance <= checkRadius)
                {
                    // Komşu yapı zeminde veya sağlam mı?
                    if (other.isGrounded || other.currentStability > 50f)
                    {
                        foundSupport = true;
                        
                        // En yakın destek mesafesini hesapla
                        if (other.isGrounded)
                        {
                            // Zemine bağlı yapıdan mesafe
                            nearestDistance = Mathf.Min(nearestDistance, distance);
                        }
                        else
                        {
                            // Başka bir destekten gelen mesafe (zincirleme)
                            float chainDistance = distance + (maxStability - other.currentStability) / stabilityLossPerMeter;
                            nearestDistance = Mathf.Min(nearestDistance, chainDistance);
                        }
                    }
                }
            }
            
            return foundSupport ? nearestDistance : -1f;
        }

        [Server]
        private void UpdateNeighborStabilities()
        {
            // Yakındaki yapıları güncelle
            foreach (var other in allStructures)
            {
                if (other == this || other == null) continue;
                
                float distance = Vector3.Distance(transform.position, other.transform.position);
                
                if (distance <= checkRadius)
                {
                    // Komşu yapının stabilitesini yeniden hesapla
                    other.Invoke(nameof(CalculateStability), 0.1f);
                }
            }
        }

        [Server]
        private void CollapseStructure()
        {
            Debug.Log($"💥 Structure collapsed due to low stability: {gameObject.name}");
            
            // Yıkılma efekti (opsiyonel: partikül, ses)
            // TODO: Partikül efekti ekle
            
            // Komşu yapıları güncelle (zincirleme yıkılma)
            UpdateNeighborStabilities();
            
            // Yapıyı yok et
            NetworkServer.Destroy(gameObject);
        }

        // Client-side görsel feedback
        private void OnStabilityChanged(float oldStability, float newStability)
        {
            if (showStabilityColor && structureRenderer != null)
            {
                UpdateVisualFeedback(newStability);
            }
        }

        private void UpdateVisualFeedback(float stability)
        {
            Color stabilityColor = GetStabilityColor(stability);
            
            // Materyal rengini güncelle
            if (structureRenderer.material != null)
            {
                structureRenderer.material.color = stabilityColor;
            }
        }

        private Color GetStabilityColor(float stability)
        {
            float normalizedStability = stability / maxStability;
            
            if (normalizedStability > 0.8f) return blueStability;      // 100-80
            if (normalizedStability > 0.6f) return greenStability;     // 80-60
            if (normalizedStability > 0.4f) return yellowStability;    // 60-40
            if (normalizedStability > 0.2f) return orangeStability;    // 40-20
            return redStability;                                       // 20-0
        }

        // Debug görselleştirme
        private void OnDrawGizmosSelected()
        {
            // Komşu yapı arama yarıçapı
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, checkRadius);
            
            // Zemin kontrolü
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position, transform.position + Vector3.down * 1.5f);
        }

        // Public getter'lar
        public float GetCurrentStability() => currentStability;
        public bool IsGrounded() => isGrounded;
    }
}


