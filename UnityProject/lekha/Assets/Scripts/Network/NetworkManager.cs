using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lekha.Core;

namespace Lekha.Network
{
    /// <summary>
    /// Network connection states
    /// </summary>
    public enum ConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        InLobby,
        InGame
    }

    /// <summary>
    /// Network message types - must match server MessageType
    /// </summary>
    public enum NetworkMessageType
    {
        // Connection
        Ping,
        Pong,
        Connected,
        Disconnected,
        Error,
        Reconnect,
        ReconnectSuccess,

        // Lobby
        CreateRoom,
        JoinRoom,
        LeaveRoom,
        RoomList,
        RoomJoined,
        RoomUpdated,
        PlayerJoined,
        PlayerLeft,
        PlayerDisconnected,
        PlayerReconnected,
        SelectPosition,
        PositionSelected,
        SetReady,
        StartGame,
        GameStarted,

        // Game
        CardDealt,
        PassCards,
        CardPlayed,
        TrickWon,
        RoundEnd,
        GameOver,
        GameState,
        BotReplaced,
        TurnUpdate,
        TurnTimeout,

        // Lobby (extended)
        JoinRoomByCode,

        // Social
        EmojiReaction,

        // Meta
        OnlineCount,

        // Spectator
        SpectateRoom,
        StopSpectating,
        SpectatorJoined,
        SpectatorLeft,
        LiveGames
    }

    /// <summary>
    /// Represents a network message
    /// </summary>
    [Serializable]
    public class NetworkMessage
    {
        public string Type;
        public string SenderId;
        public string Data;
        public long Timestamp;

        public NetworkMessage() { }

        public NetworkMessage(NetworkMessageType type, string data = "")
        {
            Type = type.ToString();
            Data = data;
            Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        public NetworkMessageType GetMessageType()
        {
            if (Enum.TryParse(Type, out NetworkMessageType result))
                return result;
            return NetworkMessageType.Error;
        }
    }

    /// <summary>
    /// Represents a player in the network
    /// </summary>
    [Serializable]
    public class NetworkPlayer
    {
        public string PlayerId;
        public string DisplayName;
        public string AvatarData;
        public bool IsReady;
        public bool IsHost;
        public string AssignedPosition;
        public bool IsMuted;
        public bool IsDisconnected;  // True if player is temporarily disconnected
        public long DisconnectTime;  // When they disconnected (for timeout display)

        [NonSerialized]
        private PlayerPosition? _position;

        public PlayerPosition? Position
        {
            get
            {
                if (_position == null && !string.IsNullOrEmpty(AssignedPosition))
                {
                    if (Enum.TryParse(AssignedPosition, out PlayerPosition pos))
                        _position = pos;
                }
                return _position;
            }
        }

        public NetworkPlayer() { }

        public NetworkPlayer(string id, string name)
        {
            PlayerId = id;
            DisplayName = name;
            IsReady = false;
            IsHost = false;
            AssignedPosition = null;
            IsMuted = false;
            IsDisconnected = false;
        }
    }

    /// <summary>
    /// Data for player disconnect event
    /// </summary>
    [Serializable]
    public class PlayerDisconnectInfo
    {
        public string PlayerId;
        public string Name;
        public string Position;
        public int ReconnectTimeout;  // Milliseconds until they're removed
    }

    /// <summary>
    /// Data for bot replacement event
    /// </summary>
    [Serializable]
    public class BotReplacedData
    {
        public string PlayerId;
        public string Name;
        public string Position;
    }

    /// <summary>
    /// Represents a game room
    /// </summary>
    [Serializable]
    public class GameRoom
    {
        public string RoomId;
        public string RoomName;
        public string HostId;
        public List<NetworkPlayer> Players;
        public int MaxPlayers;
        public bool IsPrivate;
        public string RoomCode;
        public bool GameInProgress;
        public bool CanStart;
        public int SpectatorCount;

        public GameRoom()
        {
            Players = new List<NetworkPlayer>();
        }

        public bool IsFull => Players != null && Players.Count >= MaxPlayers;
    }

    /// <summary>
    /// Live game info for spectating
    /// </summary>
    [Serializable]
    public class LiveGameInfo
    {
        public string RoomId;
        public string RoomName;
        public List<LiveGamePlayer> Players;
        public int SpectatorCount;
    }

    [Serializable]
    public class LiveGamePlayer
    {
        public string DisplayName;
        public string Position;
    }

    /// <summary>
    /// Central network manager for online multiplayer
    /// Uses WebSocket for real-time communication
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        // Server configuration
        [Header("Server Settings")]
        [SerializeField] private string serverUrl = "ws://95.179.255.32:8080";
        [SerializeField] private bool useLocalServer = false;
        [SerializeField] private string localServerUrl = "ws://localhost:8080";

        // Connection state
        public ConnectionState State { get; private set; } = ConnectionState.Disconnected;
        public string LocalPlayerId { get; private set; }
        public NetworkPlayer LocalPlayer { get; private set; }
        public GameRoom CurrentRoom { get; private set; }
        public bool IsSpectating { get; private set; }

        // Ping tracking
        public int PingMs { get; private set; } = 0;
        private float pingSentTime = 0f;
        private Coroutine pingCoroutine;

        // Events
        public event Action<ConnectionState> OnConnectionStateChanged;
        public event Action<List<GameRoom>> OnRoomListReceived;
        public event Action<List<LiveGameInfo>> OnLiveGamesReceived;
        public event Action<GameRoom> OnRoomJoined;
        public event Action<GameRoom> OnRoomUpdated;
        public event Action<NetworkPlayer> OnPlayerJoined;
        public event Action<NetworkPlayer> OnPlayerLeft;
        public event Action<PlayerDisconnectInfo> OnPlayerDisconnected;  // Player temporarily disconnected
        public event Action<NetworkPlayer> OnPlayerReconnected;  // Player reconnected
        public event Action<BotReplacedData> OnBotReplaced;  // Player replaced by bot after timeout
        public event Action<string, string> OnEmojiReceived;  // emoji, fromPosition
        public event Action<int> OnOnlineCountChanged;  // total players online
        public event Action<string, string, string> OnPositionSelected;  // playerId, newPosition, oldPosition
        public event Action<string> OnGameStarted;
        public event Action<NetworkMessage> OnMessageReceived;
        public event Action<string> OnError;
        public event Action<string, string> OnSpectatorJoined; // id, name
        public event Action<string, string> OnSpectatorLeft;
        public event Action OnReconnecting;  // Fired when attempting to reconnect
        public event Action<bool> OnReconnectResult;  // Fired with success/failure of reconnect

        // WebSocket
        private ClientWebSocket webSocket;
        private CancellationTokenSource cancellationTokenSource;
        private readonly object lockObject = new object();

        // Message queue (for main thread processing)
        private Queue<NetworkMessage> incomingMessages = new Queue<NetworkMessage>();
        private bool isConnecting = false;

        // Reconnection support
        private string lastRoomId;  // Room we were in before disconnect
        private string lastPlayerId;  // Our player ID before disconnect
        private bool wasInGame = false;  // Were we in a game when we disconnected?
        private bool isAttemptingReconnect = false;
        private const int MAX_RECONNECT_ATTEMPTS = 5;
        private int reconnectAttempts = 0;

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
            InitializeLocalPlayer();

            // Subscribe to profile changes to update display name
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.OnProfileChanged += OnProfileChanged;
            }
        }

        private void Update()
        {
            // Process incoming messages on main thread
            lock (lockObject)
            {
                while (incomingMessages.Count > 0)
                {
                    var message = incomingMessages.Dequeue();
                    ProcessMessage(message);
                }
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from profile changes
            if (PlayerProfileManager.Instance != null)
            {
                PlayerProfileManager.Instance.OnProfileChanged -= OnProfileChanged;
            }

            Disconnect();
        }

        private void OnProfileChanged(Lekha.Core.PlayerProfile profile)
        {
            // Update local player info when profile changes
            if (profile == null)
                profile = PlayerProfileManager.Instance?.CurrentProfile;
            if (profile != null && LocalPlayer != null)
            {
                string newName = profile.DisplayName ?? "Player";
                if (LocalPlayer.DisplayName != newName)
                {
                    Debug.Log($"[NetworkManager] Profile changed, updating name: {LocalPlayer.DisplayName} -> {newName}");
                    LocalPlayer.DisplayName = newName;

                    // Update avatar if changed
                    if (profile.HasCustomAvatar)
                    {
                        var tex = profile.GetAvatarTexture();
                        if (tex != null)
                        {
                            var thumbnail = CreateThumbnail(tex, 64);
                            LocalPlayer.AvatarData = Convert.ToBase64String(thumbnail.EncodeToPNG());
                            Destroy(thumbnail);
                        }
                    }
                    else
                    {
                        LocalPlayer.AvatarData = null;
                    }

                    // If connected, resend profile to server
                    if (State != ConnectionState.Disconnected && webSocket != null && webSocket.State == System.Net.WebSockets.WebSocketState.Open)
                    {
                        SendProfileInfo();
                    }
                }
            }
        }

        private void InitializeLocalPlayer()
        {
            // Get or create player ID
            LocalPlayerId = PlayerPrefs.GetString("NetworkPlayerId", "");
            if (string.IsNullOrEmpty(LocalPlayerId))
            {
                LocalPlayerId = Guid.NewGuid().ToString();
                PlayerPrefs.SetString("NetworkPlayerId", LocalPlayerId);
                PlayerPrefs.Save();
            }

            // Get profile info
            var profile = PlayerProfileManager.Instance?.CurrentProfile;
            string displayName = profile?.DisplayName ?? "Player";

            Debug.Log($"[NetworkManager] InitializeLocalPlayer - ProfileManager exists: {PlayerProfileManager.Instance != null}");
            Debug.Log($"[NetworkManager] InitializeLocalPlayer - Profile exists: {profile != null}");
            Debug.Log($"[NetworkManager] InitializeLocalPlayer - Profile.DisplayName: '{profile?.DisplayName}'");
            Debug.Log($"[NetworkManager] InitializeLocalPlayer - Using displayName: '{displayName}'");

            LocalPlayer = new NetworkPlayer(LocalPlayerId, displayName);

            // Encode avatar thumbnail if available
            if (profile != null && profile.HasCustomAvatar)
            {
                var tex = profile.GetAvatarTexture();
                if (tex != null)
                {
                    var thumbnail = CreateThumbnail(tex, 64);
                    LocalPlayer.AvatarData = Convert.ToBase64String(thumbnail.EncodeToPNG());
                    Destroy(thumbnail);
                }
            }
        }

        private Texture2D CreateThumbnail(Texture2D source, int maxSize)
        {
            int width = source.width;
            int height = source.height;

            if (width > maxSize || height > maxSize)
            {
                float ratio = Mathf.Min((float)maxSize / width, (float)maxSize / height);
                width = Mathf.RoundToInt(width * ratio);
                height = Mathf.RoundToInt(height * ratio);
            }

            RenderTexture rt = RenderTexture.GetTemporary(width, height);
            RenderTexture.active = rt;
            Graphics.Blit(source, rt);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGB24, false);
            result.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            result.Apply();

            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        /// <summary>
        /// Connect to the game server
        /// </summary>
        public void Connect()
        {
            if (State != ConnectionState.Disconnected || isConnecting)
                return;

            isConnecting = true;
            SetState(ConnectionState.Connecting);
            Debug.Log("[NetworkManager] Connecting to server...");

            StartCoroutine(ConnectAsync());
        }

        private IEnumerator ConnectAsync()
        {
            string url = useLocalServer ? localServerUrl : serverUrl;

            cancellationTokenSource = new CancellationTokenSource();
            webSocket = new ClientWebSocket();

            var connectTask = Task.Run(async () =>
            {
                try
                {
                    await webSocket.ConnectAsync(new Uri(url), cancellationTokenSource.Token);
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[NetworkManager] Connection failed: {ex.Message}");
                    return false;
                }
            });

            // Wait for connection with timeout
            float timeout = 10f;
            float elapsed = 0f;

            while (!connectTask.IsCompleted && elapsed < timeout)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            isConnecting = false;

            if (connectTask.IsCompleted && connectTask.Result)
            {
                Debug.Log("[NetworkManager] Connected to server");

                // Start receiving messages
                StartCoroutine(ReceiveMessages());

                // Start ping measurement
                if (pingCoroutine != null) StopCoroutine(pingCoroutine);
                pingCoroutine = StartCoroutine(PingCoroutine());

                // Send profile info
                SendProfileInfo();
            }
            else
            {
                Debug.LogError("[NetworkManager] Failed to connect");
                SetState(ConnectionState.Disconnected);
                OnError?.Invoke("Failed to connect to server");
            }
        }

        private void SendProfileInfo()
        {
            // Send our profile to the server
            Debug.Log($"[NetworkManager] SendProfileInfo - Sending DisplayName: '{LocalPlayer.DisplayName}'");

            var profileData = new ProfileInfoData
            {
                DisplayName = LocalPlayer.DisplayName,
                AvatarData = LocalPlayer.AvatarData
            };

            string json = JsonUtility.ToJson(profileData);
            Debug.Log($"[NetworkManager] SendProfileInfo - JSON: {json}");
            SendMessage(new NetworkMessage(NetworkMessageType.Connected, json));
        }

        private IEnumerator ReceiveMessages()
        {
            var buffer = new byte[8192];
            var messageBuffer = new StringBuilder();

            while (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                var receiveTask = Task.Run(async () =>
                {
                    try
                    {
                        var segment = new ArraySegment<byte>(buffer);
                        var result = await webSocket.ReceiveAsync(segment, cancellationTokenSource.Token);
                        return (result, buffer);
                    }
                    catch
                    {
                        return (null, null);
                    }
                });

                while (!receiveTask.IsCompleted)
                {
                    yield return null;
                }

                var (result, data) = receiveTask.Result;

                if (result == null || result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("[NetworkManager] Server closed connection");
                    break;
                }

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    messageBuffer.Append(Encoding.UTF8.GetString(data, 0, result.Count));

                    if (result.EndOfMessage)
                    {
                        string json = messageBuffer.ToString();
                        messageBuffer.Clear();

                        try
                        {
                            var message = JsonUtility.FromJson<NetworkMessage>(json);
                            lock (lockObject)
                            {
                                incomingMessages.Enqueue(message);
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[NetworkManager] Failed to parse message: {ex.Message}");
                        }
                    }
                }
            }

            // Connection lost
            if (State != ConnectionState.Disconnected)
            {
                // Check if we were in a game - if so, try to reconnect
                if (wasInGame && !string.IsNullOrEmpty(lastRoomId) && !string.IsNullOrEmpty(lastPlayerId))
                {
                    Debug.Log("[NetworkManager] Connection lost during game, attempting to reconnect...");
                    StartCoroutine(AttemptReconnection());
                }
                else
                {
                    SetState(ConnectionState.Disconnected);
                    OnError?.Invoke("Connection lost");
                }
            }
        }

        private IEnumerator AttemptReconnection()
        {
            if (isAttemptingReconnect) yield break;

            isAttemptingReconnect = true;
            OnReconnecting?.Invoke();

            while (reconnectAttempts < MAX_RECONNECT_ATTEMPTS)
            {
                reconnectAttempts++;
                Debug.Log($"[NetworkManager] Reconnection attempt {reconnectAttempts}/{MAX_RECONNECT_ATTEMPTS}...");

                // Wait a bit before trying (exponential backoff)
                float delay = Mathf.Min(2f * Mathf.Pow(1.5f, reconnectAttempts - 1), 10f);
                yield return new WaitForSeconds(delay);

                // Try to connect using Connect() which starts the coroutine
                Connect();

                // Wait for connection to establish (check state)
                float timeout = 10f;
                float elapsed = 0f;
                while (elapsed < timeout && State != ConnectionState.Connected && State != ConnectionState.InGame)
                {
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    // Connected! Now try to reconnect to the room
                    Debug.Log("[NetworkManager] Reconnected to server, sending reconnect request...");

                    yield return new WaitForSeconds(0.5f);  // Give server time to process

                    // Send reconnect request
                    AttemptRoomReconnection();

                    // Wait for response (handled by ReconnectSuccess message)
                    float reconnectTimeout = 5f;
                    float reconnectElapsed = 0f;
                    while (reconnectElapsed < reconnectTimeout && State != ConnectionState.InGame)
                    {
                        reconnectElapsed += Time.deltaTime;
                        yield return null;
                    }

                    // Check if we successfully reconnected to the game
                    if (State == ConnectionState.InGame)
                    {
                        Debug.Log("[NetworkManager] Successfully reconnected to game!");
                        isAttemptingReconnect = false;
                        reconnectAttempts = 0;
                        yield break;
                    }
                }
            }

            // Failed to reconnect after all attempts
            Debug.Log("[NetworkManager] Failed to reconnect after all attempts");
            isAttemptingReconnect = false;
            wasInGame = false;
            lastRoomId = null;
            reconnectAttempts = 0;
            SetState(ConnectionState.Disconnected);
            OnReconnectResult?.Invoke(false);
            OnError?.Invoke("Could not reconnect to game");
        }

        /// <summary>
        /// Attempt to reconnect to the last room we were in
        /// </summary>
        public void AttemptRoomReconnection()
        {
            if (string.IsNullOrEmpty(lastRoomId) || string.IsNullOrEmpty(lastPlayerId))
            {
                Debug.LogWarning("[NetworkManager] No room/player info to reconnect to");
                return;
            }

            var data = JsonUtility.ToJson(new ReconnectRequestData
            {
                PlayerId = lastPlayerId,
                RoomId = lastRoomId
            });

            SendMessage(new NetworkMessage(NetworkMessageType.Reconnect, data));
        }

        [Serializable]
        private class ReconnectRequestData
        {
            public string PlayerId;
            public string RoomId;
        }

        private void ProcessMessage(NetworkMessage message)
        {
            var msgType = message.GetMessageType();

            switch (msgType)
            {
                case NetworkMessageType.Connected:
                    HandleConnected(message);
                    break;

                case NetworkMessageType.Pong:
                    // Calculate round-trip latency
                    if (pingSentTime > 0f)
                    {
                        PingMs = Mathf.RoundToInt((Time.realtimeSinceStartup - pingSentTime) * 1000f);
                    }
                    break;

                case NetworkMessageType.Error:
                    OnError?.Invoke(message.Data);
                    break;

                case NetworkMessageType.RoomList:
                    HandleRoomList(message);
                    break;

                case NetworkMessageType.LiveGames:
                    HandleLiveGames(message);
                    break;

                case NetworkMessageType.RoomJoined:
                    HandleRoomJoined(message);
                    break;

                case NetworkMessageType.RoomUpdated:
                    HandleRoomUpdated(message);
                    break;

                case NetworkMessageType.PlayerJoined:
                    HandlePlayerJoined(message);
                    break;

                case NetworkMessageType.PlayerLeft:
                    HandlePlayerLeft(message);
                    break;

                case NetworkMessageType.PlayerDisconnected:
                    HandlePlayerDisconnected(message);
                    break;

                case NetworkMessageType.PlayerReconnected:
                    HandlePlayerReconnected(message);
                    break;

                case NetworkMessageType.BotReplaced:
                    HandleBotReplaced(message);
                    break;

                case NetworkMessageType.EmojiReaction:
                    HandleEmojiReaction(message);
                    break;

                case NetworkMessageType.OnlineCount:
                    HandleOnlineCount(message);
                    break;

                case NetworkMessageType.PositionSelected:
                    HandlePositionSelected(message);
                    break;

                case NetworkMessageType.ReconnectSuccess:
                    HandleReconnectSuccess(message);
                    break;

                case NetworkMessageType.GameStarted:
                    HandleGameStarted(message);
                    break;

                case NetworkMessageType.SpectatorJoined:
                    HandleSpectatorJoined(message);
                    break;

                case NetworkMessageType.SpectatorLeft:
                    HandleSpectatorLeft(message);
                    break;

                default:
                    // Forward to game sync
                    OnMessageReceived?.Invoke(message);
                    break;
            }
        }

        private void HandleConnected(NetworkMessage message)
        {
            try
            {
                Debug.Log($"[NetworkManager] Connected message Data: {message.Data}");
                var data = JsonUtility.FromJson<ConnectedData>(message.Data);
                if (!string.IsNullOrEmpty(data.PlayerId))
                {
                    LocalPlayerId = data.PlayerId;
                    LocalPlayer.PlayerId = data.PlayerId;
                }
                Debug.Log($"[NetworkManager] Connected: OnlineCount from server = {data.OnlineCount}");
                if (data.OnlineCount > 0)
                {
                    OnlinePlayerCount = data.OnlineCount;
                    OnOnlineCountChanged?.Invoke(data.OnlineCount);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse Connected data: {ex.Message}");
            }

            SetState(ConnectionState.Connected);
            Debug.Log($"[NetworkManager] Fully connected as {LocalPlayer.DisplayName}");
        }

        [Serializable]
        private class ConnectedData { public string PlayerId; public int OnlineCount; }

        [Serializable]
        private class ProfileInfoData
        {
            public string DisplayName;
            public string AvatarData;
        }

        private void HandleRoomList(NetworkMessage message)
        {
            try
            {
                Debug.Log($"[NetworkManager] Room list received: {message.Data}");
                var wrapper = JsonUtility.FromJson<RoomListWrapper>("{\"rooms\":" + message.Data + "}");
                Debug.Log($"[NetworkManager] Parsed {wrapper.rooms?.Count ?? 0} rooms");
                OnRoomListReceived?.Invoke(wrapper.rooms);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse room list: {ex.Message}");
            }
        }

        [Serializable]
        private class RoomListWrapper { public List<GameRoom> rooms; }

        private void HandleLiveGames(NetworkMessage message)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<LiveGamesWrapper>("{\"games\":" + message.Data + "}");
                OnLiveGamesReceived?.Invoke(wrapper.games);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse live games: {ex.Message}");
            }
        }

        [Serializable]
        private class LiveGamesWrapper { public List<LiveGameInfo> games; }

        private void HandleRoomJoined(NetworkMessage message)
        {
            try
            {
                CurrentRoom = JsonUtility.FromJson<GameRoom>(message.Data);

                // Find our player in the room
                foreach (var p in CurrentRoom.Players)
                {
                    if (p.PlayerId == LocalPlayerId)
                    {
                        LocalPlayer.IsHost = p.IsHost;
                        LocalPlayer.IsReady = p.IsReady;
                        LocalPlayer.AssignedPosition = p.AssignedPosition;
                        break;
                    }
                }

                SetState(ConnectionState.InLobby);
                OnRoomJoined?.Invoke(CurrentRoom);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse room joined: {ex.Message}");
            }
        }

        private void HandleRoomUpdated(NetworkMessage message)
        {
            try
            {
                CurrentRoom = JsonUtility.FromJson<GameRoom>(message.Data);

                // Update local player info
                foreach (var p in CurrentRoom.Players)
                {
                    if (p.PlayerId == LocalPlayerId)
                    {
                        LocalPlayer.IsHost = p.IsHost;
                        LocalPlayer.IsReady = p.IsReady;
                        LocalPlayer.AssignedPosition = p.AssignedPosition;
                        break;
                    }
                }

                OnRoomUpdated?.Invoke(CurrentRoom);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse room update: {ex.Message}");
            }
        }

        private void HandlePlayerJoined(NetworkMessage message)
        {
            try
            {
                var player = JsonUtility.FromJson<NetworkPlayer>(message.Data);
                OnPlayerJoined?.Invoke(player);
            }
            catch { }
        }

        private void HandlePlayerLeft(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<PlayerLeftData>(message.Data);
                var player = new NetworkPlayer(data.PlayerId, data.Name);
                OnPlayerLeft?.Invoke(player);
            }
            catch { }
        }

        [Serializable]
        private class PlayerLeftData { public string PlayerId; public string Name; public bool TimedOut; }

        private void HandlePlayerDisconnected(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<PlayerDisconnectInfo>(message.Data);
                Debug.Log($"[NetworkManager] Player disconnected: {data.Name} (timeout: {data.ReconnectTimeout}ms)");
                OnPlayerDisconnected?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse player disconnected: {ex.Message}");
            }
        }

        private void HandlePlayerReconnected(NetworkMessage message)
        {
            try
            {
                var player = JsonUtility.FromJson<NetworkPlayer>(message.Data);
                Debug.Log($"[NetworkManager] Player reconnected: {player.DisplayName}");
                OnPlayerReconnected?.Invoke(player);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse player reconnected: {ex.Message}");
            }
        }

        private void HandleBotReplaced(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<BotReplacedData>(message.Data);
                Debug.Log($"[NetworkManager] Player replaced by bot: {data.Name} at {data.Position}");
                OnBotReplaced?.Invoke(data);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse bot replaced: {ex.Message}");
            }
        }

        private void HandleEmojiReaction(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<EmojiReactionData>(message.Data);
                Debug.Log($"[NetworkManager] Emoji received: {data.Emoji} from {data.Position}");
                OnEmojiReceived?.Invoke(data.Emoji, data.Position);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse emoji reaction: {ex.Message}");
            }
        }

        /// <summary>
        /// Send an emoji reaction to all other players in the room
        /// </summary>
        public void SendEmojiReaction(string emoji, string position)
        {
            var data = new EmojiReactionData { Emoji = emoji, Position = position };
            SendGameAction(NetworkMessageType.EmojiReaction, JsonUtility.ToJson(data));
        }

        private void HandleOnlineCount(NetworkMessage message)
        {
            try
            {
                Debug.Log($"[NetworkManager] OnlineCount message received, Data: {message.Data}");
                var data = JsonUtility.FromJson<OnlineCountData>(message.Data);
                OnlinePlayerCount = data.Count;
                Debug.Log($"[NetworkManager] Online count updated to: {data.Count}");
                OnOnlineCountChanged?.Invoke(data.Count);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse online count: {ex.Message}");
            }
        }

        /// <summary>
        /// Current number of players connected to the server
        /// </summary>
        public int OnlinePlayerCount { get; private set; }

        [Serializable]
        private class OnlineCountData
        {
            public int Count;
        }

        [Serializable]
        private class EmojiReactionData
        {
            public string Emoji;
            public string Position;
        }

        [Serializable]
        private class PositionSelectedData
        {
            public string PlayerId;
            public string Position;
            public string OldPosition;
        }

        private void HandlePositionSelected(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<PositionSelectedData>(message.Data);
                Debug.Log($"[NetworkManager] Position selected: {data.PlayerId} -> {data.Position}");

                // Update local player's position if it's us
                if (data.PlayerId == LocalPlayerId && LocalPlayer != null)
                {
                    LocalPlayer.AssignedPosition = data.Position;
                }

                OnPositionSelected?.Invoke(data.PlayerId, data.Position, data.OldPosition);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse position selected: {ex.Message}");
            }
        }

        private void HandleReconnectSuccess(NetworkMessage message)
        {
            try
            {
                Debug.Log("[NetworkManager] Reconnection successful!");
                var data = JsonUtility.FromJson<ReconnectSuccessData>(message.Data);

                CurrentRoom = data.Room;
                isAttemptingReconnect = false;
                reconnectAttempts = 0;

                // Restore our state
                SetState(ConnectionState.InGame);
                OnReconnectResult?.Invoke(true);
                OnRoomJoined?.Invoke(CurrentRoom);

                // If there's game state, forward it
                if (!string.IsNullOrEmpty(data.GameState))
                {
                    var gameStateMsg = new NetworkMessage(NetworkMessageType.GameState, data.GameState);
                    OnMessageReceived?.Invoke(gameStateMsg);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to parse reconnect success: {ex.Message}");
                OnReconnectResult?.Invoke(false);
            }
        }

        [Serializable]
        private class ReconnectSuccessData
        {
            public GameRoom Room;
            public string GameState;
        }

        private void HandleGameStarted(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<GameStartedData>(message.Data);

                // Save state for potential reconnection
                lastRoomId = CurrentRoom?.RoomId;
                lastPlayerId = LocalPlayerId;
                wasInGame = true;

                SetState(ConnectionState.InGame);
                OnGameStarted?.Invoke(data.RoomId);
            }
            catch { }
        }

        [Serializable]
        private class GameStartedData { public string RoomId; }

        private void HandleSpectatorJoined(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<SpectatorData>(message.Data);
                OnSpectatorJoined?.Invoke(data.PlayerId, data.Name);
            }
            catch { }
        }

        private void HandleSpectatorLeft(NetworkMessage message)
        {
            try
            {
                var data = JsonUtility.FromJson<SpectatorData>(message.Data);
                OnSpectatorLeft?.Invoke(data.PlayerId, data.Name);
            }
            catch { }
        }

        [Serializable]
        private class SpectatorData { public string PlayerId; public string Name; }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        public void Disconnect()
        {
            if (State == ConnectionState.Disconnected)
                return;

            Debug.Log("[NetworkManager] Disconnecting...");

            cancellationTokenSource?.Cancel();

            if (webSocket != null && webSocket.State == WebSocketState.Open)
            {
                try
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Disconnecting", CancellationToken.None);
                }
                catch { }
            }

            webSocket = null;
            CurrentRoom = null;
            IsSpectating = false;

            SetState(ConnectionState.Disconnected);
        }

        /// <summary>
        /// Request room list from server
        /// </summary>
        public void RequestRoomList()
        {
            Debug.Log("[NetworkManager] Requesting room list...");
            SendMessage(new NetworkMessage(NetworkMessageType.RoomList));
        }

        /// <summary>
        /// Request live games for spectating
        /// </summary>
        public void RequestLiveGames()
        {
            SendMessage(new NetworkMessage(NetworkMessageType.LiveGames));
        }

        /// <summary>
        /// Create a new game room
        /// </summary>
        public void CreateRoom(string roomName, bool isPrivate = false)
        {
            if (State != ConnectionState.Connected)
            {
                OnError?.Invoke("Not connected to server");
                return;
            }

            var data = JsonUtility.ToJson(new CreateRoomData
            {
                RoomName = roomName,
                IsPrivate = isPrivate
            });

            SendMessage(new NetworkMessage(NetworkMessageType.CreateRoom, data));
        }

        [Serializable]
        private class CreateRoomData { public string RoomName; public bool IsPrivate; }

        /// <summary>
        /// Join a room by ID
        /// </summary>
        public void JoinRoom(string roomId)
        {
            if (State != ConnectionState.Connected)
            {
                OnError?.Invoke("Not connected to server");
                return;
            }

            SendMessage(new NetworkMessage(NetworkMessageType.JoinRoom, roomId));
        }

        /// <summary>
        /// Leave current room
        /// </summary>
        public void LeaveRoom()
        {
            if (CurrentRoom == null && !IsSpectating)
                return;

            if (IsSpectating)
            {
                SendMessage(new NetworkMessage(NetworkMessageType.StopSpectating));
                IsSpectating = false;
            }
            else
            {
                SendMessage(new NetworkMessage(NetworkMessageType.LeaveRoom));
            }

            // Clear reconnection state - user intentionally left
            wasInGame = false;
            lastRoomId = null;
            lastPlayerId = null;

            CurrentRoom = null;
            LocalPlayer.IsHost = false;
            LocalPlayer.IsReady = false;
            LocalPlayer.AssignedPosition = null;

            SetState(ConnectionState.Connected);
        }

        /// <summary>
        /// Set ready status
        /// </summary>
        public void SetReady(bool ready)
        {
            if (CurrentRoom == null)
                return;

            LocalPlayer.IsReady = ready;
            SendMessage(new NetworkMessage(NetworkMessageType.SetReady, ready.ToString().ToLower()));
            Debug.Log($"[NetworkManager] SetReady sent: {ready}");
        }

        /// <summary>
        /// Select a position in the room (North, South, East, West)
        /// </summary>
        public void SelectPosition(PlayerPosition position)
        {
            if (CurrentRoom == null)
                return;

            SendMessage(new NetworkMessage(NetworkMessageType.SelectPosition, position.ToString()));
            Debug.Log($"[NetworkManager] SelectPosition sent: {position}");
        }

        /// <summary>
        /// Start the game (host only)
        /// </summary>
        public void StartGame()
        {
            if (CurrentRoom == null || !LocalPlayer.IsHost)
                return;

            SendMessage(new NetworkMessage(NetworkMessageType.StartGame));
        }

        /// <summary>
        /// Spectate a live game
        /// </summary>
        public void SpectateRoom(string roomId)
        {
            if (State != ConnectionState.Connected)
            {
                OnError?.Invoke("Not connected to server");
                return;
            }

            IsSpectating = true;
            SendMessage(new NetworkMessage(NetworkMessageType.SpectateRoom, roomId));
        }

        /// <summary>
        /// Send a game action
        /// </summary>
        public void SendGameAction(NetworkMessageType type, string data)
        {
            var message = new NetworkMessage(type, data);
            message.SenderId = LocalPlayerId;
            SendMessage(message);
        }

        private void SendMessage(NetworkMessage message)
        {
            if (webSocket == null || webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[NetworkManager] Cannot send - not connected");
                return;
            }

            try
            {
                string json = JsonUtility.ToJson(message);
                byte[] bytes = Encoding.UTF8.GetBytes(json);

                Task.Run(async () =>
                {
                    try
                    {
                        await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cancellationTokenSource.Token);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[NetworkManager] Send failed: {ex.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"[NetworkManager] Failed to send message: {ex.Message}");
            }
        }

        private void SetState(ConnectionState newState)
        {
            if (State == newState)
                return;

            State = newState;
            OnConnectionStateChanged?.Invoke(newState);
        }

        private IEnumerator PingCoroutine()
        {
            while (State != ConnectionState.Disconnected)
            {
                yield return new WaitForSeconds(3f);
                if (webSocket != null && webSocket.State == WebSocketState.Open)
                {
                    pingSentTime = Time.realtimeSinceStartup;
                    SendMessage(new NetworkMessage { Type = NetworkMessageType.Ping.ToString() });
                }
            }
            PingMs = 0;
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && State != ConnectionState.Disconnected)
            {
                // Could implement reconnection logic here
                Debug.Log("[NetworkManager] App paused - connection may be lost");
            }
        }
    }
}
