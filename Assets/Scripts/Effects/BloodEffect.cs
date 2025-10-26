using UnityEngine;

namespace TacticalCombat.Effects
{
    /// <summary>
    /// Kan efekti - hasar alındığında spawn olur
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class BloodEffect : MonoBehaviour
    {
        [SerializeField] private float lifetime = 3f;

        private void Start()
        {
            GetComponent<ParticleSystem>()?.Play();
            Destroy(gameObject, lifetime);
        }
    }
}
