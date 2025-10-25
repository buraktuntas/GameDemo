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
        private Camera playerCamera;

        private void Start()
        {
            if (isLocalPlayer)
            {
                playerCamera = Camera.main;
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

            // Kamera'nın baktığı yönü al
            Ray cameraRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 shootDirection = cameraRay.direction;
            Vector3 shootOrigin = cameraRay.origin;

            // Client tarafında görsel feedback
            Debug.Log("🔫 Fired!");
            
            // Server'a kamera bilgisini gönder
            CmdFire(shootOrigin, shootDirection);
        }

        [Command]
        private void CmdFire(Vector3 origin, Vector3 direction)
        {
            // Server'da client'ın gönderdiği ray ile raycast yap
            Ray ray = new Ray(origin, direction);
            
            if (Physics.Raycast(ray, out RaycastHit hit, range, hitLayers))
            {
                Debug.Log($"🎯 [SERVER] Hit: {hit.collider.name} at {hit.point}");
                
                // Hasar uygula
                var health = hit.collider.GetComponent<Health>();
                if (health != null)
                {
                    health.TakeDamage(damage);
                    Debug.Log($"💥 [SERVER] Dealt {damage} damage to {hit.collider.name}. Remaining HP: {health.GetCurrentHealth()}");
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

        private void OnDrawGizmosSelected()
        {
            // Silah menzilini göster
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, transform.forward * range);
        }
    }
}

