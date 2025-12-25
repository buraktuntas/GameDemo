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
        [SerializeField] [Range(15f, 179f)] private float maxClientAimAngle = 45f; // ✅ FIX: 85° → 45° (anti-aimbot)

        [Header("Effects")]
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private AudioClip fireSound;
        [SerializeField] private AudioClip hitSound;
        [SerializeField] private Transform muzzleTransform; // Silahın ucundaki transform
        
        // ✅ DOUBLE PRECISION: Use double for timestamps to prevent precision loss over time
        // float loses precision after ~7 hours of gameplay (single precision limit)
        // double maintains precision for years (MirrorUnityFPS pattern)
        private double nextFireTime = 0.0;
        private double lastServerFireTime = 0.0;
        private Camera playerCamera;

        private void Start()
        {
            if (isLocalPlayer)
            {
                playerCamera = Camera.main;
                
                // Hit layers'ı otomatik ayarla
                if (hitLayers == 0)
                {
                    var cfg = TacticalCombat.Core.LayerConfigProvider.Instance;
                    if (cfg != null && cfg.projectileHitMask != 0)
                        hitLayers = cfg.projectileHitMask;
                    else
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

            // Kamera'nın baktığı yönü al
            Ray cameraRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            Vector3 shootDirection = cameraRay.direction;
            Vector3 shootOrigin = cameraRay.origin;

            // ⭐ CLIENT-SIDE VISUAL EFFECTS
            PlayMuzzleFlash();
            PlayFireSound();
            
            // Client tarafında görsel feedback
            Debug.Log("🔫 Fired!");
            
            // ✅ DOUBLE PRECISION: Send client timestamp for lag compensation
            double clientTimestamp = Mirror.NetworkTime.time;

            // Server'a kamera bilgisini gönder
            CmdFire(shootOrigin, shootDirection, clientTimestamp);
        }

        [Command]
        private void CmdFire(Vector3 clientOrigin, Vector3 clientDirection, double clientTimestamp)
        {
            // 1️⃣ Fire rate check - ✅ DOUBLE PRECISION
            double currentTime = Mirror.NetworkTime.time;
            if (currentTime < lastServerFireTime + fireRate)
            {
                Debug.LogWarning($"🚨 Fire rate exceeded by {netId}");
                return;
            }
            lastServerFireTime = currentTime;

            // ✅ LAG COMPENSATION: Calculate client's ping/latency (DOUBLE PRECISION)
            double currentServerTime = Mirror.NetworkTime.time;
            double clientLatency = currentServerTime - clientTimestamp;
            double rewindTime = clientTimestamp; // Rewind to when client fired

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (clientLatency > 0.5f)
            {
                Debug.Log($"🌐 [LAG COMP] Client latency: {clientLatency * 1000f:F0}ms, rewinding to {rewindTime:F3}s");
            }
            #endif
            
            // 2️⃣ Origin validation - FPS oyunlarda client kamera pozisyonu güvenilir
            // Client'ın kamera pozisyonu tamamen güvenilir (FPS standard)
            Vector3 serverPlayerHead = transform.position + Vector3.up * 1.6f;
            float originDistance = Vector3.Distance(clientOrigin, serverPlayerHead);
            
            // Debug: Origin distance'i logla (sadece bilgi için)
            if (originDistance > 10f)
            {
                Debug.Log($"📍 Shot origin distance: {originDistance:F2}m (client: {clientOrigin}, server: {serverPlayerHead})");
            }
            
            // 3️⃣ Direction validation - FPS oyunlarda client kamera direction serbest
            // Client'ın kamera direction'ı tamamen güvenilir (FPS standard)
            Vector3 serverForward = transform.forward;
            float angleDbg = Vector3.Angle(serverForward, clientDirection);
            
            // Debug: Direction angle'i logla (sadece bilgi için)
            if (angleDbg > 90f)
            {
                Debug.Log($"🎯 Shot direction angle: {angleDbg:F1}° (server: {serverForward}, client: {clientDirection})");
            }
            
            // 4️⃣ LAG COMPENSATION: Rewind all players to client's fire time
            var allPlayers = TacticalCombat.Core.ComponentCache.GetPlayerControllers(includeInactive: false);
            var rewindData = new System.Collections.Generic.List<(TacticalCombat.Player.FPSController fps, Vector3 origPos, Quaternion origRot, CharacterController cc, bool wasEnabled)>();

            foreach (var player in allPlayers)
            {
                if (player == null || player.netId == netId) continue; // Skip shooter

                var fpsController = player.GetComponent<TacticalCombat.Player.FPSController>();
                if (fpsController != null)
                {
                    // Store original position
                    Vector3 originalPos = player.transform.position;
                    Quaternion originalRot = player.transform.rotation;

                    // ✅ STABILITY: Disable CharacterController to prevent conflicts during teleport
                    var cc = player.GetComponent<CharacterController>();
                    bool wasEnabled = cc != null && cc.enabled;
                    if (cc != null) cc.enabled = false;

                    rewindData.Add((fpsController, originalPos, originalRot, cc, wasEnabled));

                    // Get position history and rewind
                    var posHistory = fpsController.GetPositionHistory();
                    if (posHistory != null && posHistory.GetPositionAtTime(rewindTime, out Vector3 rewindPos, out Quaternion rewindRot, out Bounds rewindBounds))
                    {
                        // ✅ SAFETY: Null check before teleport (disconnect protection)
                        if (player != null && player.transform != null)
                        {
                            player.transform.position = rewindPos;
                            player.transform.rotation = rewindRot;

                            #if UNITY_EDITOR || DEVELOPMENT_BUILD
                            float rewindDistance = Vector3.Distance(originalPos, rewindPos);
                            if (rewindDistance > 0.1f)
                            {
                                Debug.Log($"🔄 [LAG COMP] Rewound {player.name} by {rewindDistance:F2}m");
                            }
                            #endif
                        }
                    }
                }
            }

            // ✅ CRITICAL: Sync transforms to physics engine immediately
            // Without this, Physics.Raycast will use old collider positions!
            Physics.SyncTransforms();

            // 5️⃣ Server raycast (use server player head position for security)
            Vector3 validatedOrigin = serverPlayerHead;
            Vector3 clampedDirection = clientDirection.sqrMagnitude > 0.0001f ? clientDirection.normalized : transform.forward;
            float angle = Vector3.Angle(transform.forward, clampedDirection);
            if (angle > maxClientAimAngle && angle > 0.001f)
            {
                float t = maxClientAimAngle / angle;
                clampedDirection = Vector3.Slerp(transform.forward, clampedDirection, Mathf.Clamp01(t)).normalized;
            }
            Vector3 validatedDirection = clampedDirection;

            Debug.Log($"🎯 [SERVER] Raycast from {validatedOrigin} direction {validatedDirection}");

            // Perform raycast against rewound positions
            if (Physics.Raycast(validatedOrigin, validatedDirection, out RaycastHit hit, range, hitLayers, QueryTriggerInteraction.Ignore))
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

            // 6️⃣ LAG COMPENSATION: Restore all players to original positions
            foreach (var (fps, origPos, origRot, cc, wasEnabled) in rewindData)
            {
                if (fps != null && fps.gameObject != null)
                {
                    fps.transform.position = origPos;
                    fps.transform.rotation = origRot;

                    // ✅ STABILITY: Restore CharacterController state
                    if (cc != null && wasEnabled) cc.enabled = true;
                }
            }

            // ✅ CRITICAL: Sync restored positions to physics engine
            Physics.SyncTransforms();

            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.Log($"✅ [LAG COMP] Restored {rewindData.Count} players to current positions");
            #endif
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

        // ⭐ VISUAL EFFECTS
        private void PlayMuzzleFlash()
        {
            if (muzzleFlashPrefab != null)
            {
                Vector3 muzzlePosition;
                Quaternion muzzleRotation;
                
                if (muzzleTransform != null)
                {
                    // Muzzle transform varsa onu kullan
                    muzzlePosition = muzzleTransform.position;
                    muzzleRotation = muzzleTransform.rotation;
                    Debug.Log($"💥 Muzzle flash using muzzleTransform at: {muzzlePosition}");
                }
                else
                {
                    // Muzzle transform yoksa kamera pozisyonundan biraz ileriye koy
                    if (playerCamera != null)
                    {
                        // Kamera pozisyonundan 0.5m ileriye koy (daha görünür olsun)
                        muzzlePosition = playerCamera.transform.position + playerCamera.transform.forward * 0.5f;
                        muzzleRotation = playerCamera.transform.rotation;
                        Debug.Log($"💥 Muzzle flash using camera at: {muzzlePosition} (camera: {playerCamera.transform.position})");
                    }
                    else
                    {
                        // Kamera da yoksa player pozisyonundan
                        muzzlePosition = transform.position + Vector3.up * 1.6f + transform.forward * 0.5f; // Göz seviyesi + ileri
                        muzzleRotation = transform.rotation;
                        Debug.Log($"💥 Muzzle flash using player at: {muzzlePosition} (player: {transform.position})");
                    }
                }
                
                GameObject muzzleFlash = Instantiate(muzzleFlashPrefab, muzzlePosition, muzzleRotation);
                Destroy(muzzleFlash, 0.1f); // Kısa süre göster
            }
        }
        
        private void PlayFireSound()
        {
            if (fireSound != null)
            {
                AudioSource.PlayClipAtPoint(fireSound, transform.position);
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




