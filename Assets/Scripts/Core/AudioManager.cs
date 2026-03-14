using UnityEngine;
using System.Collections.Generic;

namespace Albia.Core
{
    /// <summary>
    /// Central audio manager for creature sounds, UI, ambient
    /// MVP: Placeholder system ready for clips
    /// Full: 3D spatial audio, mixing, dynamic music
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }
        
        [Header("Sources")]
        [SerializeField] private AudioSource ambientSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource musicSource;
        
        [Header("Clips")]
        [SerializeField] private List<AudioClip> eatSounds;
        [SerializeField] private List<AudioClip> hurtSounds;
        [SerializeField] private List<AudioClip> dieSounds;
        [SerializeField] private List<AudioClip> birthSounds;
        
        void Awake() => Instance = this;
        
        void Start()
        {
            // Subscribe to events
            // TODO: Connect to PopulationRegistry events
        }
        
        public void PlayEat(Vector3 position)
        {
            PlayClipAt(eatSounds, position);
        }
        
        public void PlayHurt(Vector3 position)
        {
            PlayClipAt(hurtSounds, position);
        }
        
        public void PlayDie(Vector3 position)
        {
            PlayClipAt(dieSounds, position);
        }
        
        public void PlayBirth(Vector3 position)
        {
            PlayClipAt(birthSounds, position);
        }
        
        void PlayClipAt(List<AudioClip> clips, Vector3 position)
        {
            if (clips == null || clips.Count == 0) return;
            
            // MVP: Simple 2D playback
            // Full: AudioSource.PlayClipAtPoint with 3D rolloff
            AudioClip clip = clips[Random.Range(0, clips.Count)];
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip);
            }
        }
        
        public void SetMasterVolume(float volume) => AudioListener.volume = volume;
        public void SetAmbientVolume(float volume) { if (ambientSource != null) ambientSource.volume = volume; }
        public void SetSFXVolume(float volume) { if (sfxSource != null) sfxSource.volume = volume; }
        public void SetMusicVolume(float volume) { if (musicSource != null) musicSource.volume = volume; }
    }
}