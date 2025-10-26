using UnityEngine;

namespace TacticalCombat.Effects
{
    /// <summary>
    /// Genel hit efekti - metal kıvılcımları, bullet hole vb.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class HitEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 5f;

        private void Start()
        {
            GetComponent<ParticleSystem>()?.Play();
            Destroy(gameObject, lifetime);
        }
    }
}
