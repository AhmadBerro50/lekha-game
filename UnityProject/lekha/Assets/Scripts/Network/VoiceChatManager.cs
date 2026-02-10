using UnityEngine;
using System;
using System.Collections.Generic;
using Agora.Rtc;

namespace Lekha.Network
{
    /// <summary>
    /// Manages voice chat using Agora RTC SDK.
    /// Handles joining/leaving voice channels, mute controls, and speaking detection.
    /// </summary>
    public class VoiceChatManager : MonoBehaviour
    {
        public static VoiceChatManager Instance { get; private set; }

        private const string AGORA_APP_ID = "20f6a6b31dce4650a3b7ec2f2f6a8670";

        // Agora engine
        private IRtcEngine rtcEngine;
        private bool isInitialized = false;
        private bool isInChannel = false;
        private string currentChannelName;

        // State
        private bool isMicrophoneMuted = false;
        private bool isSpeakerMuted = false;

        // Speaking detection (uid -> volume 0-255)
        private Dictionary<uint, int> speakingVolumes = new Dictionary<uint, int>();
        private int localSpeakingVolume = 0;

        // Map Agora uid to player ID for speaking indicators
        private Dictionary<uint, string> uidToPlayerId = new Dictionary<uint, string>();

        // Events
        public event Action<bool> OnMicrophoneMuteChanged;
        public event Action<bool> OnSpeakerMuteChanged;
        public event Action<uint, bool> OnPlayerSpeaking; // uid, isSpeaking
        public event Action<string> OnVoiceChatError;
        public event Action OnJoinedChannel;
        public event Action OnLeftChannel;

        // Public properties
        public bool IsMicrophoneMuted => isMicrophoneMuted;
        public bool IsSpeakerMuted => isSpeakerMuted;
        public bool IsInChannel => isInChannel;
        public int LocalSpeakingVolume => localSpeakingVolume;

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

        private void OnDestroy()
        {
            LeaveChannel();
            DisposeEngine();

            if (Instance == this)
                Instance = null;
        }

        /// <summary>
        /// Initialize the Agora RTC engine
        /// </summary>
        public bool Initialize()
        {
            if (isInitialized) return true;

            // Request microphone permission on Android before initializing
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
            {
                UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
            }
#endif

            try
            {
                rtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngine();

                RtcEngineContext context = new RtcEngineContext();
                context.appId = AGORA_APP_ID;
                context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_COMMUNICATION;
                context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_CHATROOM;

                int result = rtcEngine.Initialize(context);
                if (result != 0)
                {
                    Debug.LogError($"[VoiceChatManager] Agora Initialize failed: {result}");
                    OnVoiceChatError?.Invoke($"Agora init failed: {result}");
                    return false;
                }

                // Set up event handler
                rtcEngine.InitEventHandler(new VoiceChatEventHandler(this));

                // Enable audio
                rtcEngine.EnableAudio();
                rtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

                // Enable volume indication for speaking detection (every 200ms)
                rtcEngine.EnableAudioVolumeIndication(200, 3, true);

                // Optimize for voice chat
                rtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_SPEECH_STANDARD);

                // Route audio to loudspeaker instead of earpiece
                rtcEngine.SetDefaultAudioRouteToSpeakerphone(true);
                rtcEngine.SetEnableSpeakerphone(true);

                isInitialized = true;
                Debug.Log("[VoiceChatManager] Agora initialized successfully");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[VoiceChatManager] Agora initialization error: {e.Message}");
                OnVoiceChatError?.Invoke($"Init error: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Join the voice channel for the current game room.
        /// Uses the room ID as the channel name so all players in the same room hear each other.
        /// </summary>
        public void JoinChannel(string roomId)
        {
            if (!isInitialized)
            {
                if (!Initialize()) return;
            }

            if (isInChannel)
            {
                if (currentChannelName == roomId) return;
                LeaveChannel();
            }

            currentChannelName = roomId;

            // Join with uid=0 (auto-assign)
            int result = rtcEngine.JoinChannel("", roomId, "", 0);
            if (result != 0)
            {
                Debug.LogError($"[VoiceChatManager] JoinChannel failed: {result}");
                OnVoiceChatError?.Invoke($"Join failed: {result}");
            }
            else
            {
                Debug.Log($"[VoiceChatManager] Joining channel: {roomId}");
            }
        }

        /// <summary>
        /// Leave the current voice channel
        /// </summary>
        public void LeaveChannel()
        {
            if (!isInChannel || rtcEngine == null) return;

            rtcEngine.LeaveChannel();
            isInChannel = false;
            currentChannelName = null;
            speakingVolumes.Clear();
            uidToPlayerId.Clear();
            localSpeakingVolume = 0;

            Debug.Log("[VoiceChatManager] Left voice channel");
            OnLeftChannel?.Invoke();
        }

        /// <summary>
        /// Toggle microphone mute
        /// </summary>
        public void SetMicrophoneMuted(bool muted)
        {
            isMicrophoneMuted = muted;
            rtcEngine?.MuteLocalAudioStream(muted);
            OnMicrophoneMuteChanged?.Invoke(muted);
            Debug.Log($"[VoiceChatManager] Microphone muted: {muted}");
        }

        /// <summary>
        /// Toggle speaker mute (mute all remote audio)
        /// </summary>
        public void SetSpeakerMuted(bool muted)
        {
            isSpeakerMuted = muted;
            rtcEngine?.MuteAllRemoteAudioStreams(muted);
            OnSpeakerMuteChanged?.Invoke(muted);
            Debug.Log($"[VoiceChatManager] Speaker muted: {muted}");
        }

        /// <summary>
        /// Get speaking volume for a remote user (0-255)
        /// </summary>
        public int GetSpeakingVolume(uint uid)
        {
            return speakingVolumes.TryGetValue(uid, out int vol) ? vol : 0;
        }

        /// <summary>
        /// Reset voice chat state
        /// </summary>
        public void Reset()
        {
            LeaveChannel();
            isMicrophoneMuted = false;
            isSpeakerMuted = false;
        }

        private void DisposeEngine()
        {
            if (rtcEngine != null)
            {
                rtcEngine.InitEventHandler(null);
                rtcEngine.Dispose();
                rtcEngine = null;
                isInitialized = false;
            }
        }

        // Called by event handler
        internal void HandleJoinChannelSuccess(uint uid)
        {
            isInChannel = true;
            Debug.Log($"[VoiceChatManager] Joined channel successfully, uid: {uid}");
            OnJoinedChannel?.Invoke();
        }

        internal void HandleUserJoined(uint uid)
        {
            Debug.Log($"[VoiceChatManager] Remote user joined: {uid}");
        }

        internal void HandleUserOffline(uint uid)
        {
            speakingVolumes.Remove(uid);
            uidToPlayerId.Remove(uid);
            OnPlayerSpeaking?.Invoke(uid, false);
            Debug.Log($"[VoiceChatManager] Remote user left: {uid}");
        }

        internal void HandleAudioVolumeIndication(AudioVolumeInfo[] speakers, int totalVolume)
        {
            foreach (var speaker in speakers)
            {
                if (speaker.uid == 0)
                {
                    // Local user
                    localSpeakingVolume = (int)speaker.volume;
                }
                else
                {
                    speakingVolumes[speaker.uid] = (int)speaker.volume;
                    OnPlayerSpeaking?.Invoke(speaker.uid, speaker.volume > 10);
                }
            }
        }

        internal void HandleError(int err, string msg)
        {
            Debug.LogError($"[VoiceChatManager] Agora error {err}: {msg}");
            OnVoiceChatError?.Invoke($"Error {err}: {msg}");
        }
    }

    /// <summary>
    /// Agora RTC event handler
    /// </summary>
    internal class VoiceChatEventHandler : IRtcEngineEventHandler
    {
        private readonly VoiceChatManager manager;

        internal VoiceChatEventHandler(VoiceChatManager manager)
        {
            this.manager = manager;
        }

        public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
        {
            manager.HandleJoinChannelSuccess(connection.localUid);
        }

        public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
        {
            manager.HandleUserJoined(uid);
        }

        public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
        {
            manager.HandleUserOffline(uid);
        }

        public override void OnAudioVolumeIndication(RtcConnection connection, AudioVolumeInfo[] speakers, uint speakerNumber, int totalVolume)
        {
            manager.HandleAudioVolumeIndication(speakers, totalVolume);
        }

        public override void OnError(int err, string msg)
        {
            manager.HandleError(err, msg);
        }

        public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
        {
            Debug.Log("[VoiceChatManager] OnLeaveChannel callback");
        }
    }
}
