using UnityEngine;
using Mirror;
using TacticalCombat.Core;

namespace TacticalCombat.Combat
{
    /// <summary>
    /// AAA Weapon Hit Processor
    /// Handles server-side hit validation (anti-cheat) and damage calculation
    /// Extracted from WeaponSystem to improve maintainability
    /// </summary>
    public static class WeaponHitProcessor
    {
        public struct HitValidationContext
        {
            public uint ShooterNetId;
            public Team ShooterTeam;
            public WeaponConfig Weapon;
            public float NextFireTime;
            public int CurrentAmmo;
            public Vector3 ShooterPosition;
            public Vector3 ShooterForward;
        }

        public struct HitRequest
        {
            public Vector3 HitPoint;
            public Vector3 HitNormal;
            public float Distance;
            public Collider HitCollider;
        }

        /// <summary>
        /// Validates a hit request from a client (Anti-Cheat)
        /// </summary>
        public static bool IsHitValid(HitValidationContext ctx, HitRequest req, out string failureReason)
        {
            failureReason = string.Empty;

            // 1. Validate Weapon
            if (ctx.Weapon == null)
            {
                failureReason = "No weapon assigned";
                return false;
            }

            // 2. Rate Limit (Note: Time.time is used, so ensure this is called on server)
            if (Time.time < ctx.NextFireTime)
            {
                failureReason = "Rate limit violation";
                return false;
            }

            // 3. Ammo Check
            if (ctx.CurrentAmmo <= 0)
            {
                failureReason = "Ammo cheat attempt";
                return false;
            }

            // 4. Distance Check
            if (req.Distance > ctx.Weapon.range + 1f) // 1m buffer for latency
            {
                failureReason = $"Distance cheat: {req.Distance}m > {ctx.Weapon.range}m";
                return false;
            }

            // 5. Angle Check
            Vector3 hitDirection = (req.HitPoint - ctx.ShooterPosition).normalized;
            float angle = Vector3.Angle(ctx.ShooterForward, hitDirection);
            if (angle > 95f) // 95 degree cone
            {
                failureReason = $"Impossible angle: {angle:F1}°";
                return false;
            }

            // 6. LOS Check (Line of Sight)
            if (req.HitCollider != null)
            {
                if (!CheckLineOfSight(ctx.ShooterPosition, req.HitPoint, req.HitCollider, ctx.Weapon.hitMask))
                {
                    failureReason = "LOS violation - wall blocking";
                    return false;
                }
            }

            return true;
        }

        private static bool CheckLineOfSight(Vector3 origin, Vector3 target, Collider targetCollider, LayerMask mask)
        {
            float distance = Vector3.Distance(origin, target);
            Vector3 direction = (target - origin).normalized;

            RaycastHit[] losHits = new RaycastHit[8];
            int count = Physics.RaycastNonAlloc(origin, direction, losHits, distance, mask);

            for (int i = 0; i < count; i++)
            {
                if (losHits[i].collider == targetCollider) return true;

                // If we hit a wall/structure before the target, LOS is blocked
                if (losHits[i].collider.CompareTag("Structure") || losHits[i].collider.CompareTag("Wall"))
                {
                    if (losHits[i].distance < distance - 0.1f) return false;
                }
            }

            return true;
        }

        public static DamageInfo CalculateDamage(HitValidationContext ctx, HitRequest req, out bool isCritical, out bool isBodyHit)
        {
            isCritical = false;
            isBodyHit = false;
            float damage = ctx.Weapon.damage;

            // Hitbox calculation
            if (req.HitCollider.TryGetComponent<Hitbox>(out var hitbox))
            {
                damage = hitbox.CalculateDamage(Mathf.RoundToInt(damage));
                isCritical = hitbox.IsCritical();
                isBodyHit = true;
            }
            else if (req.HitCollider.TryGetComponent<Health>(out _))
            {
                isBodyHit = true;
            }

            // Distance Falloff
            float distanceFactor = Mathf.Clamp01(1f - (req.Distance / ctx.Weapon.range));
            damage *= distanceFactor;

            return new DamageInfo(
                Mathf.RoundToInt(damage),
                ctx.ShooterNetId,
                DamageType.Bullet,
                req.HitPoint,
                req.HitNormal,
                0f,
                isCritical
            );
        }
    }
}
