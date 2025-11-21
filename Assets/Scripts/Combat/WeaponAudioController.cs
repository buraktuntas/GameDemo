using UnityEngine;

namespace TacticalCombat.Combat
{
    [RequireComponent(typeof(AudioSource))]
    public class WeaponAudioController : MonoBehaviour
    {
        [Header("🔊 AUDIO")]
        [SerializeField] private AudioClip[] fireSounds;
        [SerializeField] private AudioClip reloadSound;
        [SerializeField] private AudioClip emptySound;
        [SerializeField] private AudioClip[] hitSounds;

        private AudioSource audioSource;

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
            
            // AudioSource settings
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound for local player
            audioSource.volume = 1f;
            audioSource.priority = 128;
        }

        public void PlayFireSound(bool isLocalPlayer)
        {
            if (fireSounds == null || fireSounds.Length == 0) return;
            
            AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
            if (clip == null) return;

            if (isLocalPlayer)
            {
                // Play 2D for local player
                audioSource.PlayOneShot(clip);
            }
            else
            {
                // Should be handled by PlayFireSoundAt for remote players
                // But if called directly, play 2D
                audioSource.PlayOneShot(clip);
            }
        }

        public void PlayFireSoundAt(Vector3 position, bool useSpatialAudio)
        {
            if (fireSounds == null || fireSounds.Length == 0) return;
            
            AudioClip clip = fireSounds[Random.Range(0, fireSounds.Length)];
            if (clip == null) return;
            
            if (useSpatialAudio)
            {
                // Create temporary AudioSource at position for 3D sound
                GameObject tempAudio = new GameObject("TempFireSound");
                tempAudio.transform.position = position;
                
                AudioSource source = tempAudio.AddComponent<AudioSource>();
                source.clip = clip;
                source.spatialBlend = 1f; // 3D sound
                source.minDistance = 2f;
                source.maxDistance = 50f;
                source.rolloffMode = AudioRolloffMode.Linear;
                source.volume = 0.8f;
                source.Play();
                
                Destroy(tempAudio, clip.length + 0.1f);
            }
            else
            {
                // Play 2D
                audioSource.PlayOneShot(clip);
            }
        }

        public void PlayReloadSound()
        {
            if (reloadSound != null)
            {
                audioSource.PlayOneShot(reloadSound);
            }
        }

        public void PlayEmptySound()
        {
            if (emptySound != null)
            {
                audioSource.PlayOneShot(emptySound);
            }
        }

        public void PlayHitSound()
        {
            if (hitSounds != null && hitSounds.Length > 0)
            {
                AudioClip clip = hitSounds[Random.Range(0, hitSounds.Length)];
                if (clip != null)
                {
                    audioSource.PlayOneShot(clip);
                }
            }
        }
    }
}
