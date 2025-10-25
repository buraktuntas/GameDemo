using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// Basit raycast tabanlı silah - Test için
    /// Mouse sol tık = ateş
    /// </summary>
    public class SimpleGun : NetworkBehaviour
    {
        [Header("Gun Settings")]
        [SerializeField] private int damage = 10;
        [SerializeField] private float range = 100f;
        [SerializeField] private float fireRate = 0.5f; // Saniyede 2 atış
        [SerializeField] private LayerMask hitLayers;

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        
        private float nextFireTime = 0f;
        private float lastServerFireTime = 0f;
        private Camera playerCamera;

        private void Start()
        {
            if (isLocalPlayer)
            {
                playerCamera = Camera.main;
                
                // Hit layers'ı otomatik ayarla
                if (hitLayers == 0)
                {
                    hitLayers = LayerMask.GetMask("Default", "Player", "Structure", "Trap");
                    Debug.Log($"🎯 SimpleGun hit layers set to: {hitLayers}");
                }
            }
        }

        private void Update()
        {
            if (!isLocalPlayer) return;

            // Mouse sol tık = ateş
            if (Input.GetButton("Fire1") && Time.time >= nextFireTime)
            {
                nextFireTime = Time.time + fireRate;
                Fire();
            }
        }

        private void Fire()
        {
            if (playerCamera == null) return;

            // ⭐ Client-side fire rate kontrolü
            if (Time.time < nextFireTime)
            {
                Debug.Log($"🔫 Fire rate exceeded on client: {Time.time - nextFireTime + fireRate:F2}s remaining");
                return;
            }

            // Kamera'nın baktığı yönü al
            Ray cameraRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 shootDirection = cameraRay.direction;
            Vector3 shootOrigin = cameraRay.origin;

            // Client tarafında fire rate güncelle
            nextFireTime = Time.time + fireRate;

            // Client tarafında görsel feedback
            Debug.Log("🔫 Fired!");
            
            // Server'a kamera bilgisini gönder
            CmdFire(shootOrigin, shootDirection);
        }

        [Command]
        private void CmdFire(Vector3 origin, Vector3 direction)
        {
            // ⚠️ GÜVENLİK: Server-side fire rate kontrolü
            if (Time.time < lastServerFireTime + fireRate)
            {
                Debug.LogWarning($"🚨 Server fire rate exceeded by {netId}: {Time.time - lastServerFireTime:F2}s < {fireRate:F2}s");
                return;
            }
            
            // Server fire time güncelle
            lastServerFireTime = Time.time;
            
            // Server'da client'ın gönderdiği ray ile raycast yap
            Ray ray = new Ray(origin, direction);
            
            Debug.Log($"🎯 [SERVER] Raycast from {origin} direction {direction} with layers {hitLayers}");
            
            if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
            {
                Debug.Log($"🎯 [SERVER] Hit: {hit.collider.name} at {hit.point}");
                
                // Hasar uygula
                var health = hit.collider.GetComponent<Health>();
                if (health != null)
                {
                    // Critical hit kontrolü
                    bool isCritical = Random.Range(0f, 1f) < 0.1f; // %10 critical chance
                    int finalDamage = isCritical ? Mathf.RoundToInt(damage * 2f) : Mathf.RoundToInt(damage);
                    
                    health.TakeDamage(finalDamage);
                    
                    // Hit effects
                    RpcPlayHitEffects(hit.point, isCritical, HitType.Normal);
                    
                    // Damage numbers
                    RpcShowDamageNumbers(hit.collider.transform, finalDamage, isCritical);
                    
                    Debug.Log($"💥 [SERVER] Dealt {finalDamage} damage to {hit.collider.name}. Remaining HP: {health.GetCurrentHealth()} {(isCritical ? "CRITICAL!" : "")}");
                }
                else
                {
                    // Metal hit effect
                    RpcPlayHitEffects(hit.point, false, HitType.Metal);
                }

                // Tüm clientlara hit efekti göster
                RpcShowHitEffect(hit.point, hit.normal);
            }
            else
            {
                Debug.Log("❌ [SERVER] Missed!");
            }
        }

        [ClientRpc]
        private void RpcShowHitEffect(Vector3 position, Vector3 normal)
        {
            // Basit hit marker
            Debug.Log($"💥 Hit effect at {position}");
            
            // Opsiyonel: Particle effect spawn
            if (hitEffectPrefab != null)
            {
                GameObject effect = Instantiate(hitEffectPrefab, position, Quaternion.LookRotation(normal));
                Destroy(effect, 2f);
            }
        }
        
        [ClientRpc]
        private void RpcPlayHitEffects(Vector3 hitPosition, bool isCritical, HitType hitType)
        {
            Debug.Log($"🎯 [CLIENT] RpcPlayHitEffects called at {hitPosition}");
            if (HitEffects.Instance != null)
            {
                HitEffects.Instance.PlayHitEffect(hitPosition, isCritical ? HitType.Critical : hitType);
                Debug.Log($"🎯 [CLIENT] HitEffects played");
            }
            else
            {
                Debug.LogWarning("⚠️ [CLIENT] HitEffects.Instance is null!");
            }
        }
        
        [ClientRpc]
        private void RpcShowDamageNumbers(Transform target, float damage, bool isCritical)
        {
            Debug.Log($"🎯 [CLIENT] RpcShowDamageNumbers called for {target?.name} damage {damage}");
            if (DamageNumbers.Instance != null && target != null)
            {
                DamageNumbers.Instance.ShowDamageAtTransform(target, damage, isCritical);
                Debug.Log($"🎯 [CLIENT] DamageNumbers shown");
            }
            else
            {
                Debug.LogWarning($"⚠️ [CLIENT] DamageNumbers.Instance: {DamageNumbers.Instance != null}, Target: {target != null}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            // Silah menzilini göster
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * range);
        }
    }
}

