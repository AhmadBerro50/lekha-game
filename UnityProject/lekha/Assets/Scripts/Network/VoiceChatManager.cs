using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace Lekha.Network
{
    /// <summary>
    /// Voice chat settings
    /// </summary>
    [Serializable]
    public class VoiceChatSettings
    {
        // Optimized for low bandwidth (was 16000Hz, 100ms = 32KB/s per player)
        // Now 8000Hz, 250ms = ~4KB/s per player (8x reduction)
        public int SampleRate = 8000;
        public int RecordingFrequency = 8000;
        public int ChunkDurationMs = 250;
        public float VoiceActivationThreshold = 0.02f; // Higher threshold to filter more silence
        public bool PushToTalk = false;
    }

    /// <summary>
    /// Manages voice chat for online multiplayer
    /// Handles microphone input, audio streaming, and playback
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; }

        // Feature flag - set to true to completely disable voice chat
        // TODO: Enable when we have a better server infrastructure
        public static bool VOICE_CHAT_DISABLED = true;

        // Settings
        public VoiceChatSettings Settings { get; private set; } = new VoiceChatSettings();

        // State
        private bool isInitialized = false;
        private bool isMicrophoneMuted = false;
        private bool isSpeakerMuted = false;
        private bool isRecording = false;
        private bool isVoiceChatEnabled = true; // Master toggle
        private string selectedMicrophone;

        // Public property for voice chat enabled state
        public bool IsVoiceChatEnabled => isVoiceChatEnabled;

        // Audio components
        private AudioClip microphoneClip;
        private int lastSamplePosition = 0;
        private float[] sampleBuffer;

        // Playback for remote players
        private Dictionary<string, AudioSource> playerAudioSources = new Dictionary<string, AudioSource>();
        private Dictionary<string, Queue<float[]>> playerAudioQueues = new Dictionary<string, Queue<float[]>>();

        // Events
        public event Action<bool> OnMicrophoneMuteChanged;
        public event Action<bool> OnSpeakerMuteChanged;
        public event Action<string, bool> OnPlayerSpeaking;
        public event Action<string> OnVoiceChatError;

        // Voice activity detection
        private Dictionary<string, float> playerSpeakingLevels = new Dictionary<string, float>();
        private float localSpeakingLevel = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // Don't subscribe to anything if voice chat is disabled
            if (VOICE_CHAT_DISABLED)
            {
                return;
            }

            // Subscribe to network voice data
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived += OnNetworkMessageReceived;
            }
        }

        private void OnDestroy()
        {
            StopRecording();

            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= OnNetworkMessageReceived;
            }

            // Clean up audio sources
            foreach (var source in playerAudioSources.Values)
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }
            playerAudioSources.Clear();
        }

        /// <summary>
        /// Initialize voice chat system
        /// </summary>
        public bool Initialize()
        {
            // Voice chat is disabled - don't initialize
            if (VOICE_CHAT_DISABLED)
            {
                Debug.Log("[VoiceChatManager] Voice chat is disabled");
                return false;
            }

            if (isInitialized)
            {
                return true;
            }

            // Check microphone availability
            if (Microphone.devices.Length == 0)
            {
                Debug.LogWarning("[VoiceChatManager] No microphone devices found");
                OnVoiceChatError?.Invoke("No microphone found");
                return false;
            }

            // Select first available microphone
            selectedMicrophone = Microphone.devices[0];
            Debug.Log($"[VoiceChatManager] Selected microphone: {selectedMicrophone}");

            // Calculate buffer size
            int samplesPerChunk = (Settings.SampleRate * Settings.ChunkDurationMs) / 1000;
            sampleBuffer = new float[samplesPerChunk];

            isInitialized = true;
            Debug.Log("[VoiceChatManager] Initialized successfully");

            return true;
        }

        /// <summary>
        /// Start recording from microphone
        /// </summary>
        public void StartRecording()
        {
            if (!isInitialized)
            {
                if (!Initialize())
                {
                    return;
                }
            }

            if (isRecording)
            {
                return;
            }

            // Start microphone recording (loop mode, 1 second buffer)
            microphoneClip = Microphone.Start(selectedMicrophone, true, 1, Settings.RecordingFrequency);

            if (microphoneClip == null)
            {
                Debug.LogError("[VoiceChatManager] Failed to start microphone");
                OnVoiceChatError?.Invoke("Failed to start microphone");
                return;
            }

            // Wait for microphone to start
            while (Microphone.GetPosition(selectedMicrophone) <= 0) { }

            lastSamplePosition = 0;
            isRecording = true;

            // Start processing coroutine
            StartCoroutine(ProcessMicrophoneInput());

            Debug.Log("[VoiceChatManager] Started recording");
        }

        /// <summary>
        /// Stop recording
        /// </summary>
        public void StopRecording()
        {
            if (!isRecording)
            {
                return;
            }

            isRecording = false;
            StopCoroutine(ProcessMicrophoneInput());

            if (Microphone.IsRecording(selectedMicrophone))
            {
                Microphone.End(selectedMicrophone);
            }

            microphoneClip = null;
            Debug.Log("[VoiceChatManager] Stopped recording");
        }

        /// <summary>
        /// Toggle microphone mute
        /// </summary>
        public void SetMicrophoneMuted(bool muted)
        {
            isMicrophoneMuted = muted;
            OnMicrophoneMuteChanged?.Invoke(muted);
            Debug.Log($"[VoiceChatManager] Microphone muted: {muted}");
        }

        /// <summary>
        /// Toggle speaker mute
        /// </summary>
        public void SetSpeakerMuted(bool muted)
        {
            isSpeakerMuted = muted;

            // Mute/unmute all player audio sources
            foreach (var source in playerAudioSources.Values)
            {
                if (source != null)
                {
                    source.mute = muted;
                }
            }

            OnSpeakerMuteChanged?.Invoke(muted);
            Debug.Log($"[VoiceChatManager] Speaker muted: {muted}");
        }

        /// <summary>
        /// Check if microphone is muted
        /// </summary>
        public bool IsMicrophoneMuted => isMicrophoneMuted;

        /// <summary>
        /// Check if speaker is muted
        /// </summary>
        public bool IsSpeakerMuted => isSpeakerMuted;

        /// <summary>
        /// Get local speaking level (0-1)
        /// </summary>
        public float LocalSpeakingLevel => localSpeakingLevel;

        /// <summary>
        /// Get speaking level for a remote player
        /// </summary>
        public float GetPlayerSpeakingLevel(string playerId)
        {
            return playerSpeakingLevels.TryGetValue(playerId, out float level) ? level : 0f;
        }

        /// <summary>
        /// Mute a specific player
        /// </summary>
        public void MutePlayer(string playerId, bool muted)
        {
            if (playerAudioSources.TryGetValue(playerId, out AudioSource source))
            {
                source.mute = muted;
            }

            // Notify network
            if (NetworkManager.Instance != null)
            {
                var msgType = muted ? NetworkMessageType.MutePlayer : NetworkMessageType.UnmutePlayer;
                NetworkManager.Instance.SendGameAction(msgType, playerId);
            }

            Debug.Log($"[VoiceChatManager] Player {playerId} muted: {muted}");
        }

        private IEnumerator ProcessMicrophoneInput()
        {
            while (isRecording)
            {
                int currentPosition = Microphone.GetPosition(selectedMicrophone);

                if (currentPosition < 0 || microphoneClip == null)
                {
                    yield return null;
                    continue;
                }

                // Calculate how many samples we have
                int samplesAvailable;
                if (currentPosition < lastSamplePosition)
                {
                    // Wrapped around
                    samplesAvailable = (microphoneClip.samples - lastSamplePosition) + currentPosition;
                }
                else
                {
                    samplesAvailable = currentPosition - lastSamplePosition;
                }

                // Process in chunks
                while (samplesAvailable >= sampleBuffer.Length)
                {
                    // Get samples
                    microphoneClip.GetData(sampleBuffer, lastSamplePosition);

                    // Calculate RMS level for voice activity detection
                    float sum = 0f;
                    for (int i = 0; i < sampleBuffer.Length; i++)
                    {
                        sum += sampleBuffer[i] * sampleBuffer[i];
                    }
                    float rms = Mathf.Sqrt(sum / sampleBuffer.Length);
                    localSpeakingLevel = rms;

                    // Only send if enabled, not muted, and above threshold
                    if (isVoiceChatEnabled && !isMicrophoneMuted && rms > Settings.VoiceActivationThreshold)
                    {
                        SendVoiceData(sampleBuffer);
                    }

                    // Update position
                    lastSamplePosition = (lastSamplePosition + sampleBuffer.Length) % microphoneClip.samples;
                    samplesAvailable -= sampleBuffer.Length;
                }

                // Wait for next chunk
                yield return new WaitForSeconds(Settings.ChunkDurationMs / 1000f);
            }
        }

        private void SendVoiceData(float[] samples)
        {
            // Convert float samples to bytes (16-bit PCM)
            byte[] audioData = new byte[samples.Length * 2];

            for (int i = 0; i < samples.Length; i++)
            {
                short pcmValue = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767);
                byte[] bytes = BitConverter.GetBytes(pcmValue);
                audioData[i * 2] = bytes[0];
                audioData[i * 2 + 1] = bytes[1];
            }

            // Send via network
            NetworkManager.Instance?.SendVoiceData(audioData);
        }

        private void OnNetworkMessageReceived(NetworkMessage message)
        {
            // Ignore voice data if voice chat is disabled
            if (!isVoiceChatEnabled) return;

            if (message.GetMessageType() == NetworkMessageType.VoiceData)
            {
                ProcessReceivedVoiceData(message.SenderId, message.Data);
            }
        }

        /// <summary>
        /// Enable or disable voice chat completely
        /// </summary>
        public void SetVoiceChatEnabled(bool enabled)
        {
            isVoiceChatEnabled = enabled;
            Debug.Log($"[VoiceChatManager] Voice chat {(enabled ? "enabled" : "disabled")}");

            if (!enabled)
            {
                // Stop all playback when disabling
                foreach (var source in playerAudioSources.Values)
                {
                    if (source != null && source.isPlaying)
                    {
                        source.Stop();
                    }
                }
                // Clear queues
                foreach (var queue in playerAudioQueues.Values)
                {
                    queue.Clear();
                }
            }
        }

        /// <summary>
        /// Toggle voice chat on/off
        /// </summary>
        public void ToggleVoiceChat()
        {
            SetVoiceChatEnabled(!isVoiceChatEnabled);
        }

        private void ProcessReceivedVoiceData(string senderId, string base64Data)
        {
            // Don't play our own voice
            if (senderId == NetworkManager.Instance?.LocalPlayerId)
            {
                return;
            }

            if (isSpeakerMuted)
            {
                return;
            }

            try
            {
                // Decode base64 to bytes
                byte[] audioData = Convert.FromBase64String(base64Data);

                // Convert bytes back to float samples
                float[] samples = new float[audioData.Length / 2];
                for (int i = 0; i < samples.Length; i++)
                {
                    short pcmValue = BitConverter.ToInt16(audioData, i * 2);
                    samples[i] = pcmValue / 32767f;
                }

                // Calculate speaking level
                float sum = 0f;
                for (int i = 0; i < samples.Length; i++)
                {
                    sum += samples[i] * samples[i];
                }
                float rms = Mathf.Sqrt(sum / samples.Length);
                playerSpeakingLevels[senderId] = rms;

                // Notify that player is speaking
                OnPlayerSpeaking?.Invoke(senderId, rms > Settings.VoiceActivationThreshold);

                // Queue for playback
                QueueAudioForPlayback(senderId, samples);
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceChatManager] Error processing voice data: {e.Message}");
            }
        }

        private void QueueAudioForPlayback(string playerId, float[] samples)
        {
            // Get or create audio source for this player
            if (!playerAudioSources.TryGetValue(playerId, out AudioSource source))
            {
                source = CreatePlayerAudioSource(playerId);
                playerAudioSources[playerId] = source;
            }

            // Get or create queue
            if (!playerAudioQueues.TryGetValue(playerId, out Queue<float[]> queue))
            {
                queue = new Queue<float[]>();
                playerAudioQueues[playerId] = queue;
            }

            // Add samples to queue
            queue.Enqueue(samples);

            // Start playback if not already playing
            if (!source.isPlaying && queue.Count > 2) // Buffer a few chunks before starting
            {
                StartCoroutine(PlaybackAudioQueue(playerId));
            }
        }

        private AudioSource CreatePlayerAudioSource(string playerId)
        {
            GameObject audioObj = new GameObject($"VoiceAudio_{playerId}");
            audioObj.transform.SetParent(transform);

            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.spatialBlend = 0f; // 2D audio
            source.volume = 1f;
            source.mute = isSpeakerMuted;
            source.playOnAwake = false;

            return source;
        }

        private IEnumerator PlaybackAudioQueue(string playerId)
        {
            if (!playerAudioSources.TryGetValue(playerId, out AudioSource source))
            {
                yield break;
            }

            if (!playerAudioQueues.TryGetValue(playerId, out Queue<float[]> queue))
            {
                yield break;
            }

            while (queue.Count > 0)
            {
                float[] samples = queue.Dequeue();

                // Create audio clip from samples
                AudioClip clip = AudioClip.Create($"Voice_{playerId}", samples.Length, 1, Settings.SampleRate, false);
                clip.SetData(samples, 0);

                // Play
                source.clip = clip;
                source.Play();

                // Wait for playback
                yield return new WaitForSeconds((float)samples.Length / Settings.SampleRate);

                // Clean up
                Destroy(clip);
            }

            // Clear speaking level after playback ends
            playerSpeakingLevels[playerId] = 0f;
            OnPlayerSpeaking?.Invoke(playerId, false);
        }

        /// <summary>
        /// Get available microphone devices
        /// </summary>
        public string[] GetAvailableMicrophones()
        {
            return Microphone.devices;
        }

        /// <summary>
        /// Select a specific microphone
        /// </summary>
        public void SelectMicrophone(string deviceName)
        {
            if (isRecording)
            {
                StopRecording();
            }

            selectedMicrophone = deviceName;
            Debug.Log($"[VoiceChatManager] Selected microphone: {deviceName}");
        }

        /// <summary>
        /// Reset voice chat state
        /// </summary>
        public void Reset()
        {
            StopRecording();

            foreach (var source in playerAudioSources.Values)
            {
                if (source != null)
                {
                    Destroy(source.gameObject);
                }
            }
            playerAudioSources.Clear();
            playerAudioQueues.Clear();
            playerSpeakingLevels.Clear();

            isMicrophoneMuted = false;
            isSpeakerMuted = false;
            localSpeakingLevel = 0f;
        }
    }
}
