using UnityEngine;
using System.Collections.Generic;

namespace Lekha.Audio
{
    /// <summary>
    /// Manages game sound effects using procedurally generated audio
    /// </summary>
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        private AudioSource audioSource;
        private AudioSource specialCardAudioSource; // Second source for layering special card sounds
        private Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

        // Volume settings
        private float masterVolume = 1f;
        private float sfxVolume = 0.7f;

        // Deterministic noise generator (avoids Random.value issues on Android IL2CPP)
        private int noiseSeed = 12345;

        private float NextNoise()
        {
            noiseSeed = (noiseSeed * 1103515245 + 12345) & 0x7fffffff;
            return (noiseSeed / (float)0x7fffffff) - 0.5f; // returns -0.5 to 0.5
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            specialCardAudioSource = gameObject.AddComponent<AudioSource>();
            specialCardAudioSource.playOnAwake = false;

            GenerateSoundEffects();
        }

        private void GenerateSoundEffects()
        {
            // Generate simple procedural sound effects
            clips["card_play"] = GenerateCardPlaySound();
            clips["card_deal"] = GenerateCardDealSound();
            clips["trick_win"] = GenerateTrickWinSound();
            clips["round_end"] = GenerateRoundEndSound();
            clips["game_win"] = GenerateGameWinSound();
            clips["game_lose"] = GenerateGameLoseSound();
            clips["button_click"] = GenerateButtonClickSound();
            clips["card_select"] = GenerateCardSelectSound();
            clips["card_hover"] = GenerateCardHoverSound();
            clips["special_impact"] = GenerateSpecialImpactSound();
            clips["special_impact_intense"] = GenerateSpecialImpactSoundIntense();
            clips["special_bass_boom"] = GenerateSpecialBassBoom(false);
            clips["special_bass_boom_intense"] = GenerateSpecialBassBoom(true);

            Debug.Log($"SoundManager: Generated {clips.Count} sound effects");
        }

        public void PlayCardPlay()
        {
            PlaySound("card_play");
        }

        public void PlayCardDeal()
        {
            PlaySound("card_deal", 0.5f);
        }

        public void PlayTrickWin()
        {
            PlaySound("trick_win");
        }

        public void PlayRoundEnd()
        {
            PlaySound("round_end");
        }

        public void PlayGameWin()
        {
            PlaySound("game_win");
        }

        public void PlayGameLose()
        {
            PlaySound("game_lose");
        }

        public void PlayButtonClick()
        {
            PlaySound("button_click");
        }

        public void PlayCardSelect()
        {
            PlaySound("card_select", 0.3f);
        }

        public void PlayCardHover()
        {
            PlaySound("card_hover", 0.15f);
        }

        /// <summary>
        /// Play dramatic impact sound for special cards (Queen of Spades, 10 of Diamonds)
        /// Layers main impact + bass boom on separate audio sources for maximum drama
        /// </summary>
        public void PlaySpecialCardImpact(bool intense = false)
        {
            float mainVolume = intense ? 2.0f : 1.6f;
            PlaySound(intense ? "special_impact_intense" : "special_impact", mainVolume);

            // Layer bass boom on second audio source for extra weight
            string boomKey = intense ? "special_bass_boom_intense" : "special_bass_boom";
            if (clips.TryGetValue(boomKey, out AudioClip boomClip))
            {
                float boomVolume = intense ? 1.8f : 1.4f;
                specialCardAudioSource.PlayOneShot(boomClip, masterVolume * sfxVolume * boomVolume);
            }
        }

        private void PlaySound(string soundName, float volumeMultiplier = 1f)
        {
            if (clips.TryGetValue(soundName, out AudioClip clip))
            {
                audioSource.PlayOneShot(clip, masterVolume * sfxVolume * volumeMultiplier);
            }
        }

        public void SetMasterVolume(float volume)
        {
            masterVolume = Mathf.Clamp01(volume);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
        }

        // Procedural sound generation using simple waveforms
        private AudioClip GenerateCardPlaySound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 8; // 0.125 seconds - solid card slap
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Quick attack, fast decay - serious card slap on table
                float envelope = Mathf.Exp(-t * 15f); // Fast exponential decay

                // Low frequency thump (table impact)
                float lowThump = Mathf.Sin(2 * Mathf.PI * 80 * i / sampleRate) * envelope * 0.5f;

                // Mid frequency body (card material)
                float midBody = Mathf.Sin(2 * Mathf.PI * 200 * i / sampleRate) * envelope * 0.3f;

                // High frequency snap (card edge)
                float highSnap = Mathf.Sin(2 * Mathf.PI * 400 * i / sampleRate) * Mathf.Exp(-t * 25f) * 0.2f;

                // Filtered noise for realistic texture
                float noise = 0;
                if (t < 0.1f) // Only at the start
                {
                    noise = NextNoise() * (1 - t * 10) * 0.4f;
                }

                data[i] = lowThump + midBody + highSnap + noise;

                // Soft clip for warmth
                data[i] = Mathf.Clamp(data[i], -0.8f, 0.8f);
            }

            return CreateClip("card_play", data, sampleRate);
        }

        private AudioClip GenerateCardDealSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 20; // 0.05 seconds
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1 - t) * (1 - t);
                // Quick swoosh sound
                data[i] = NextNoise() * envelope * 0.4f;
            }

            return CreateClip("card_deal", data, sampleRate);
        }

        private AudioClip GenerateTrickWinSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 4; // 0.25 seconds
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Sin(t * Mathf.PI); // Bell curve
                // Two-tone rising sound
                float freq1 = 440 + 220 * t;
                float freq2 = 660 + 330 * t;
                data[i] = (Mathf.Sin(2 * Mathf.PI * freq1 * i / sampleRate) * 0.5f +
                          Mathf.Sin(2 * Mathf.PI * freq2 * i / sampleRate) * 0.3f) * envelope * 0.3f;
            }

            return CreateClip("trick_win", data, sampleRate);
        }

        private AudioClip GenerateRoundEndSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 2; // 0.5 seconds
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = (1 - t);
                // Chord sound
                float freq1 = 262; // C
                float freq2 = 330; // E
                float freq3 = 392; // G
                data[i] = (Mathf.Sin(2 * Mathf.PI * freq1 * i / sampleRate) +
                          Mathf.Sin(2 * Mathf.PI * freq2 * i / sampleRate) +
                          Mathf.Sin(2 * Mathf.PI * freq3 * i / sampleRate)) * envelope * 0.15f;
            }

            return CreateClip("round_end", data, sampleRate);
        }

        private AudioClip GenerateGameWinSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate; // 1 second
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Sin(t * Mathf.PI);

                // Ascending arpeggio
                float noteT = t * 4; // 4 notes
                int noteIndex = Mathf.Min((int)noteT, 3);
                float[] freqs = { 262, 330, 392, 523 }; // C major arpeggio
                float freq = freqs[noteIndex];

                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.25f;
            }

            return CreateClip("game_win", data, sampleRate);
        }

        private AudioClip GenerateGameLoseSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate; // 1 second
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = 1 - t;

                // Descending minor sound
                float freq = 400 - 200 * t;
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.2f;
                // Add wobble
                data[i] *= 1 + 0.3f * Mathf.Sin(2 * Mathf.PI * 6 * t);
            }

            return CreateClip("game_lose", data, sampleRate);
        }

        private AudioClip GenerateButtonClickSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 20; // 0.05 seconds
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = 1 - t;
                data[i] = Mathf.Sin(2 * Mathf.PI * 1000 * i / sampleRate) * envelope * envelope * 0.3f;
            }

            return CreateClip("button_click", data, sampleRate);
        }

        private AudioClip GenerateCardSelectSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 15; // ~0.07 seconds
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Sin(t * Mathf.PI);
                float freq = 600 + 200 * t; // Rising pitch
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.2f;
            }

            return CreateClip("card_select", data, sampleRate);
        }

        private AudioClip GenerateCardHoverSound()
        {
            int sampleRate = 44100;
            int samples = sampleRate / 30; // ~0.033 seconds - very short
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float envelope = Mathf.Sin(t * Mathf.PI) * (1 - t); // Quick fade
                float freq = 800 + 400 * t; // Quick rising pitch
                data[i] = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * envelope * 0.15f;
            }

            return CreateClip("card_hover", data, sampleRate);
        }

        private AudioClip CreateClip(string name, float[] data, int sampleRate)
        {
            AudioClip clip = AudioClip.Create(name, data.Length, 1, sampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Generate a deep bass boom to layer on top of the special card impact
        /// </summary>
        private AudioClip GenerateSpecialBassBoom(bool intense)
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * (intense ? 1.0f : 0.7f));
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Descending bass sweep - starts low, goes lower
                float startFreq = intense ? 60f : 70f;
                float endFreq = intense ? 15f : 20f;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float bass = Mathf.Sin(2 * Mathf.PI * freq * i / sampleRate) * Mathf.Exp(-t * (intense ? 1.5f : 2f)) * 0.7f;

                // Sub-harmonic reinforcement
                float subFreq = freq * 0.5f;
                float sub = Mathf.Sin(2 * Mathf.PI * subFreq * i / sampleRate) * Mathf.Exp(-t * (intense ? 1.2f : 1.8f)) * 0.4f;

                // Pressure thump at the start
                float thump = 0;
                if (t < 0.05f)
                {
                    thump = Mathf.Sin(2 * Mathf.PI * 30 * i / sampleRate) * (1 - t / 0.05f) * 0.8f;
                }

                data[i] = bass + sub + thump;
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }

            string clipName = intense ? "special_bass_boom_intense" : "special_bass_boom";
            return CreateClip(clipName, data, sampleRate);
        }

        /// <summary>
        /// Generate impact/crack sound for special cards (10 of Diamonds)
        /// </summary>
        private AudioClip GenerateSpecialImpactSound()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 0.8f); // 0.8 seconds (was 0.5)
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Initial impact crack - heavier
                float impact = 0;
                if (t < 0.07f)
                {
                    float impactT = t / 0.07f;
                    impact = NextNoise() * 2.5f * (1 - impactT);
                    impact += Mathf.Sin(2 * Mathf.PI * 45 * i / sampleRate) * (1 - impactT) * 1.0f;
                }

                // Deep rumble - lower pitch, longer decay
                float rumble = Mathf.Sin(2 * Mathf.PI * 30 * i / sampleRate) * Mathf.Exp(-t * 3f) * 0.5f;
                rumble += Mathf.Sin(2 * Mathf.PI * 50 * i / sampleRate) * Mathf.Exp(-t * 3.5f) * 0.3f;

                // Crack/breaking texture - extended
                float crack = 0;
                if (t < 0.3f)
                {
                    crack = NextNoise() * Mathf.Exp(-t * 10f) * 0.7f;
                }

                // Resonance - lower pitch
                float resonance = Mathf.Sin(2 * Mathf.PI * 100 * i / sampleRate) * Mathf.Exp(-t * 4f) * 0.35f;

                // Sub-bass throb
                float subBass = Mathf.Sin(2 * Mathf.PI * 25 * i / sampleRate) * Mathf.Exp(-t * 2.5f) * 0.3f;

                data[i] = impact + rumble + crack + resonance + subBass;
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }

            return CreateClip("special_impact", data, sampleRate);
        }

        /// <summary>
        /// Generate more intense impact sound for Queen of Spades - DEVASTATING
        /// </summary>
        private AudioClip GenerateSpecialImpactSoundIntense()
        {
            int sampleRate = 44100;
            int samples = (int)(sampleRate * 1.2f); // 1.2 seconds (was 0.7) - much longer tail
            float[] data = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;

                // Massive initial impact - wider and louder
                float impact = 0;
                if (t < 0.1f)
                {
                    float impactT = t / 0.1f;
                    impact = NextNoise() * 3.0f * (1 - impactT);
                    impact += Mathf.Sin(2 * Mathf.PI * 35 * i / sampleRate) * (1 - impactT) * 1.2f;
                }

                // Deep bass rumble - lower frequencies, slower decay
                float rumble = Mathf.Sin(2 * Mathf.PI * 22 * i / sampleRate) * Mathf.Exp(-t * 2f) * 0.6f;
                rumble += Mathf.Sin(2 * Mathf.PI * 40 * i / sampleRate) * Mathf.Exp(-t * 2.5f) * 0.4f;
                rumble += Mathf.Sin(2 * Mathf.PI * 55 * i / sampleRate) * Mathf.Exp(-t * 3f) * 0.3f;

                // Multiple crack layers - extended
                float crack = 0;
                if (t < 0.4f)
                {
                    crack = NextNoise() * Mathf.Exp(-t * 8f) * 0.8f;
                    // Secondary cracks
                    if (t > 0.1f && t < 0.25f)
                    {
                        crack += NextNoise() * 0.5f;
                    }
                    // Tertiary micro-cracks
                    if (t > 0.2f && t < 0.35f)
                    {
                        crack += NextNoise() * 0.3f * Mathf.Exp(-(t - 0.2f) * 20f);
                    }
                }

                // Ominous low tone - deeper
                float ominous = Mathf.Sin(2 * Mathf.PI * 60 * i / sampleRate) * Mathf.Exp(-t * 2f) * 0.35f;
                ominous += Mathf.Sin(2 * Mathf.PI * 90 * i / sampleRate) * Mathf.Exp(-t * 3f) * 0.2f;

                // Shattering glass texture
                float shatter = 0;
                if (t < 0.15f)
                {
                    shatter = Mathf.Sin(2 * Mathf.PI * 2000 * i / sampleRate * (1 - t)) * Mathf.Exp(-t * 30f) * 0.25f;
                }

                // Sub-bass pressure wave
                float subBass = Mathf.Sin(2 * Mathf.PI * 18 * i / sampleRate) * Mathf.Exp(-t * 1.5f) * 0.4f;

                data[i] = impact + rumble + crack + ominous + shatter + subBass;
                data[i] = Mathf.Clamp(data[i], -1f, 1f);
            }

            return CreateClip("special_impact_intense", data, sampleRate);
        }
    }
}
