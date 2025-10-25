using UnityEngine;
using System.Collections.Generic;

namespace TacticalCombat.Audio
{
    /// <summary>
    /// Merkezi ses yönetimi sistemi
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Audio Sources")]
        public AudioSource musicSource;
        public AudioSource sfxSource;
        public AudioSource ambientSource;
        
        [Header("Music")]
        public AudioClip backgroundMusic;
        public AudioClip buildModeMusic;
        public AudioClip combatMusic;
        
        [Header("SFX - Movement")]
        public AudioClip[] footstepSounds;
        public AudioClip jumpSound;
        public AudioClip landSound;
        
        [Header("SFX - Combat")]
        public AudioClip gunshotSound;
        public AudioClip hitSound;
        public AudioClip reloadSound;
        
        [Header("SFX - Building")]
        public AudioClip buildSound;
        public AudioClip demolishSound;
        public AudioClip rotateSound;
        
        [Header("SFX - UI")]
        public AudioClip buttonClickSound;
        public AudioClip buildModeEnterSound;
        public AudioClip buildModeExitSound;
        
        [Header("Settings")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.7f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float ambientVolume = 0.5f;
        
        private Dictionary<string, AudioClip> audioClips;
        private bool isInitialized = false;
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeAudioManager();
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        private void InitializeAudioManager()
        {
            // Audio source'ları oluştur
            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.loop = true;
                musicSource.playOnAwake = false;
            }
            
            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
            }
            
            if (ambientSource == null)
            {
                ambientSource = gameObject.AddComponent<AudioSource>();
                ambientSource.loop = true;
                ambientSource.playOnAwake = false;
            }
            
            // Audio clip dictionary'sini oluştur
            audioClips = new Dictionary<string, AudioClip>();
            PopulateAudioClips();
            
            // Background music'i başlat
            PlayBackgroundMusic();
            
            isInitialized = true;
            Debug.Log("✅ AudioManager initialized");
        }
        
        private void PopulateAudioClips()
        {
            // Music
            if (backgroundMusic != null) audioClips["background_music"] = backgroundMusic;
            if (buildModeMusic != null) audioClips["build_mode_music"] = buildModeMusic;
            if (combatMusic != null) audioClips["combat_music"] = combatMusic;
            
            // Movement
            if (jumpSound != null) audioClips["jump"] = jumpSound;
            if (landSound != null) audioClips["land"] = landSound;
            
            // Combat
            if (gunshotSound != null) audioClips["gunshot"] = gunshotSound;
            if (hitSound != null) audioClips["hit"] = hitSound;
            if (reloadSound != null) audioClips["reload"] = reloadSound;
            
            // Building
            if (buildSound != null) audioClips["build"] = buildSound;
            if (demolishSound != null) audioClips["demolish"] = demolishSound;
            if (rotateSound != null) audioClips["rotate"] = rotateSound;
            
            // UI
            if (buttonClickSound != null) audioClips["button_click"] = buttonClickSound;
            if (buildModeEnterSound != null) audioClips["build_mode_enter"] = buildModeEnterSound;
            if (buildModeExitSound != null) audioClips["build_mode_exit"] = buildModeExitSound;
        }
        
        // ═══════════════════════════════════════════════════════════
        // PUBLIC AUDIO METHODS
        // ═══════════════════════════════════════════════════════════
        
        public void PlaySFX(string clipName, float volume = 1f)
        {
            if (!isInitialized || !audioClips.ContainsKey(clipName)) return;
            
            AudioClip clip = audioClips[clipName];
            sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }
        
        public void PlaySFX(AudioClip clip, float volume = 1f)
        {
            if (!isInitialized || clip == null) return;
            
            sfxSource.PlayOneShot(clip, volume * sfxVolume * masterVolume);
        }
        
        public void PlayRandomFootstep()
        {
            if (footstepSounds == null || footstepSounds.Length == 0) return;
            
            AudioClip randomFootstep = footstepSounds[Random.Range(0, footstepSounds.Length)];
            PlaySFX(randomFootstep, 0.7f);
        }
        
        public void PlayBackgroundMusic()
        {
            if (backgroundMusic != null && musicSource != null)
            {
                musicSource.clip = backgroundMusic;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }
        
        public void PlayBuildModeMusic()
        {
            if (buildModeMusic != null && musicSource != null)
            {
                musicSource.clip = buildModeMusic;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }
        
        public void PlayCombatMusic()
        {
            if (combatMusic != null && musicSource != null)
            {
                musicSource.clip = combatMusic;
                musicSource.volume = musicVolume * masterVolume;
                musicSource.Play();
            }
        }
        
        public void StopMusic()
        {
            if (musicSource != null)
            {
                musicSource.Stop();
            }
        }
        
        // ═══════════════════════════════════════════════════════════
        // VOLUME CONTROL
        // ═══════════════════════════════════════════════════════════
        
        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
            UpdateAllVolumes();
        }
        
        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            if (musicSource != null)
            {
                musicSource.volume = musicVolume * masterVolume;
            }
        }
        
        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }
        
        public void SetAmbientVolume(float volume)
        {
            ambientVolume = Mathf.Clamp01(volume);
            if (ambientSource != null)
            {
                ambientSource.volume = ambientVolume * masterVolume;
            }
        }
        
        private void UpdateAllVolumes()
        {
            if (musicSource != null)
                musicSource.volume = musicVolume * masterVolume;
            
            if (ambientSource != null)
                ambientSource.volume = ambientVolume * masterVolume;
        }
        
        // ═══════════════════════════════════════════════════════════
        // UTILITY METHODS
        // ═══════════════════════════════════════════════════════════
        
        public bool IsPlaying(string clipName)
        {
            if (!audioClips.ContainsKey(clipName)) return false;
            
            AudioClip clip = audioClips[clipName];
            return sfxSource.isPlaying && sfxSource.clip == clip;
        }
        
        public void FadeOutMusic(float duration = 1f)
        {
            StartCoroutine(FadeOutCoroutine(musicSource, duration));
        }
        
        public void FadeInMusic(float duration = 1f)
        {
            StartCoroutine(FadeInCoroutine(musicSource, duration));
        }
        
        private System.Collections.IEnumerator FadeOutCoroutine(AudioSource source, float duration)
        {
            float startVolume = source.volume;
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, 0f, timer / duration);
                yield return null;
            }
            
            source.Stop();
            source.volume = startVolume;
        }
        
        private System.Collections.IEnumerator FadeInCoroutine(AudioSource source, float duration)
        {
            float targetVolume = musicVolume * masterVolume;
            source.volume = 0f;
            source.Play();
            
            float timer = 0f;
            
            while (timer < duration)
            {
                timer += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, targetVolume, timer / duration);
                yield return null;
            }
            
            source.volume = targetVolume;
        }
    }
}
