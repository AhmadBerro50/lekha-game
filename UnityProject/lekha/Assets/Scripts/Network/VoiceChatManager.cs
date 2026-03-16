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
        private bool isJoining = false;
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

                // Optimize for voice chat — clear speech with noise suppression
                rtcEngine.SetAudioProfile(AUDIO_PROFILE_TYPE.AUDIO_PROFILE_SPEECH_STANDARD);
                rtcEngine.SetParameters("{\"che.audio.enable.ns\":true}");   // Noise suppression
                rtcEngine.SetParameters("{\"che.audio.enable.agc\":true}");  // Auto gain control

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
        /// Uses position-based uid so we can map speakers to player panels.
        /// South=1, East=2, North=3, West=4
        /// </summary>
        public void JoinChannel(string roomId, string position = null)
        {
            if (string.IsNullOrEmpty(roomId))
            {
                Debug.LogWarning("[VoiceChatManager] JoinChannel called with null/empty roomId");
                return;
            }

            if (!isInitialized)
            {
                if (!Initialize()) return;
            }

            // If already in this exact channel and connected, skip
            if (isInChannel && currentChannelName == roomId)
            {
                Debug.Log($"[VoiceChatManager] Already in channel {roomId}, skipping join");
                return;
            }

            // Leave current channel first (handles isJoining stuck state too)
            if (isJoining || isInChannel)
            {
                LeaveChannel();
            }

            currentChannelName = roomId;
            lastRoomId = roomId;
            lastPosition = position;
            uint uid = PositionToUid(position);
            isJoining = true;

            int result = rtcEngine.JoinChannel("", roomId, "", uid);
            if (result != 0)
            {
                // CRITICAL: Reset isJoining on failure so we can retry
                isJoining = false;
                currentChannelName = null;
                Debug.LogError($"[VoiceChatManager] JoinChannel failed with code {result}. Will retry on next call.");
                OnVoiceChatError?.Invoke($"Join failed: {result}");
            }
            else
            {
                Debug.Log($"[VoiceChatManager] Joining channel: {roomId}, uid: {uid} (position: {position})");
            }
        }

        private static uint PositionToUid(string position)
        {
            return position switch
            {
                "South" => 1,
                "East" => 2,
                "North" => 3,
                "West" => 4,
                _ => 0
            };
        }

        public static string UidToPosition(uint uid)
        {
            return uid switch
            {
                1 => "South",
                2 => "East",
                3 => "North",
                4 => "West",
                _ => null
            };
        }

        /// <summary>
        /// Leave the current voice channel
        /// </summary>
        public void LeaveChannel()
        {
            if ((!isInChannel && !isJoining) || rtcEngine == null) return;

            rtcEngine.LeaveChannel();
            isInChannel = false;
            isJoining = false;
            currentChannelName = null;
            speakingVolumes.Clear();
            uidToPlayerId.Clear();
            localSpeakingVolume = 0;

            rtcEngine?.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);

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
            isJoining = false;
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
            isInChannel = false;
            isJoining = false;
            OnVoiceChatError?.Invoke($"Error {err}: {msg}");
        }

        // Auto-rejoin tracking
        private string lastRoomId;
        private string lastPosition;
        private float rejoinCooldown = 0f;

        internal void HandleConnectionLost()
        {
            Debug.LogWarning("[VoiceChatManager] Connection lost — will auto-rejoin");
            isInChannel = false;
            isJoining = false;
            // Store for rejoin
            lastRoomId = currentChannelName;
            rejoinCooldown = 3f; // Wait 3 seconds before rejoin
        }

        private void Update()
        {
            if (rejoinCooldown > 0)
            {
                rejoinCooldown -= Time.deltaTime;
                if (rejoinCooldown <= 0 && !isInChannel && !isJoining && !string.IsNullOrEmpty(lastRoomId))
                {
                    Debug.Log($"[VoiceChatManager] Auto-rejoining channel: {lastRoomId}");
                    JoinChannel(lastRoomId, lastPosition);
                    lastRoomId = null;
                }
            }
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

        public override void OnConnectionLost(RtcConnection connection)
        {
            Debug.LogWarning("[VoiceChatManager] OnConnectionLost callback");
            manager.HandleConnectionLost();
        }

        public override void OnConnectionStateChanged(RtcConnection connection, CONNECTION_STATE_TYPE state, CONNECTION_CHANGED_REASON_TYPE reason)
        {
            Debug.Log($"[VoiceChatManager] Connection state: {state}, reason: {reason}");
            if (state == CONNECTION_STATE_TYPE.CONNECTION_STATE_FAILED ||
                state == CONNECTION_STATE_TYPE.CONNECTION_STATE_DISCONNECTED)
            {
                manager.HandleConnectionLost();
            }
        }
    }
}
