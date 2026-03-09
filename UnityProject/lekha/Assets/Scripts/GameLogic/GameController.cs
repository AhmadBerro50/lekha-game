using UnityEngine;
using System.Collections.Generic;
using Lekha.Core;
using Lekha.UI;
using Lekha.Animation;
using Lekha.Audio;
using Lekha.Effects;
using Lekha.Network;

namespace Lekha.GameLogic
{
    /// <summary>
    /// High-level game flow controller - manages transitions between menu and game
    /// </summary>
    public class GameController : MonoBehaviour
    {
        public static GameController Instance { get; private set; }

        private PremiumMainMenu premiumMenu;

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
            // Initialize player profile manager
            InitializeProfileManager();

            // Initialize voice chat manager (Agora)
            InitializeVoiceChatManager();

            // Initialize network game sync (for online multiplayer)
            InitializeNetworkGameSync();

            // Initialize Barteyyeh manager (best of 3)
            InitializeBarteyyehManager();

            // Create premium main menu
            CreateMainMenu();

            // Subscribe to game over event
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnGameOver += OnGameOver;
            }
        }

        private void InitializeProfileManager()
        {
            if (PlayerProfileManager.Instance == null)
            {
                GameObject profileManagerObj = new GameObject("PlayerProfileManager");
                profileManagerObj.transform.SetParent(transform);
                profileManagerObj.AddComponent<PlayerProfileManager>();
            }
        }

        private void InitializeVoiceChatManager()
        {
            if (VoiceChatManager.Instance == null)
            {
                GameObject voiceChatObj = new GameObject("VoiceChatManager");
                voiceChatObj.transform.SetParent(transform);
                voiceChatObj.AddComponent<VoiceChatManager>();
                Debug.Log("[GameController] VoiceChatManager initialized");
            }
        }

        private void InitializeNetworkGameSync()
        {
            if (NetworkGameSync.Instance == null)
            {
                GameObject syncObj = new GameObject("NetworkGameSync");
                syncObj.transform.SetParent(transform);
                syncObj.AddComponent<NetworkGameSync>();
                Debug.Log("[GameController] NetworkGameSync initialized");
            }
        }

        private void InitializeBarteyyehManager()
        {
            if (BarteyyehManager.Instance == null)
            {
                GameObject barteyyehObj = new GameObject("BarteyyehManager");
                barteyyehObj.transform.SetParent(transform);
                barteyyehObj.AddComponent<BarteyyehManager>();
                Debug.Log("[GameController] BarteyyehManager initialized");
            }
        }

        private void CreateMainMenu()
        {
            GameObject menuObj = new GameObject("PremiumMainMenu");
            menuObj.transform.SetParent(transform);
            premiumMenu = menuObj.AddComponent<PremiumMainMenu>();
            premiumMenu.OnPlayClicked += OnPlayClicked;
        }

        private void OnPlayClicked()
        {
            if (GameManager.Instance == null) return;

            // Reinitialize GameUI (recreates canvas and re-subscribes events)
            if (GameUI.Instance != null)
            {
                GameUI.Instance.Reinitialize();
            }

            // Check if this is an online game by looking at NetworkManager state
            // This is more reliable than NetworkGameSync.IsOnlineGame due to event timing
            bool isOnlineGame = NetworkManager.Instance != null &&
                                NetworkManager.Instance.State == ConnectionState.InGame &&
                                NetworkManager.Instance.CurrentRoom != null;

            bool isHost = NetworkManager.Instance?.LocalPlayer?.IsHost ?? false;

            Debug.Log($"[GameController] OnPlayClicked - isOnlineGame: {isOnlineGame}, isHost: {isHost}");

            if (isOnlineGame)
            {
                // Initialize NetworkGameSync for online play if not already done
                var localPlayer = NetworkManager.Instance.LocalPlayer;
                string positionStr = localPlayer?.AssignedPosition ?? "South";

                if (NetworkGameSync.Instance != null)
                {
                    NetworkGameSync.Instance.InitializeOnlineGame(positionStr, isHost);
                }

                // Configure GameManager for online play
                if (System.Enum.TryParse<Lekha.Core.PlayerPosition>(positionStr, out var localPosition))
                {
                    GameManager.Instance.ConfigureForOnlineGame(localPosition);
                }

                // Show ping display for online games
                if (PingDisplay.Instance != null)
                    PingDisplay.Instance.Show();

                // Ensure network events are subscribed (may be null during Start())
                EnsureNetworkEventSubscription();

                // Set up player names from network room data
                SetupNetworkPlayerNames();

                // In online game, only the host starts the game and syncs to all clients
                // Non-host clients wait for game state from network
                if (isHost)
                {
                    Debug.Log("[GameController] Online game - Host starting and syncing game");
                    GameManager.Instance.StartGame();
                    // Host will deal cards and NetworkGameSync will broadcast to all clients
                }
                else
                {
                    Debug.Log("[GameController] Online game - Client waiting for host to sync game state");
                    // Don't start local game - wait for CardDealt messages from host
                    // Just prepare the UI for the game
                    GameManager.Instance.PrepareForOnlineGame();
                }

                // Join Agora voice channel and show UI
                JoinVoiceChat();
                CreateInGameVoiceChatUI();
            }
            else
            {
                // Local game with bots - start normally
                Debug.Log("[GameController] Local game - Starting with bots");
                GameManager.Instance.StartGame();
            }
        }

        private void SetupNetworkPlayerNames()
        {
            var room = NetworkManager.Instance?.CurrentRoom;
            if (room?.Players == null || GameManager.Instance == null) return;

            var playerNames = new Dictionary<Lekha.Core.PlayerPosition, string>();

            foreach (var netPlayer in room.Players)
            {
                if (netPlayer.Position.HasValue)
                {
                    string displayName = !string.IsNullOrEmpty(netPlayer.DisplayName) ? netPlayer.DisplayName : "Player";
                    playerNames[netPlayer.Position.Value] = displayName;
                    Debug.Log($"[GameController] Network player: {netPlayer.Position.Value} = {displayName}");
                }
            }

            GameManager.Instance.SetNetworkPlayerNames(playerNames);
        }

        private void JoinVoiceChat()
        {
            var roomId = NetworkManager.Instance?.CurrentRoom?.RoomId;
            if (string.IsNullOrEmpty(roomId)) return;

            if (VoiceChatManager.Instance != null)
            {
                string position = NetworkManager.Instance?.LocalPlayer?.AssignedPosition ?? "South";
                VoiceChatManager.Instance.JoinChannel(roomId, position);
                Debug.Log($"[GameController] Joining voice channel for room: {roomId}, position: {position}");
            }
        }

        private void CreateInGameVoiceChatUI()
        {
            if (Lekha.UI.InGameVoiceChatUI.Instance == null)
            {
                GameObject voiceUIObj = new GameObject("InGameVoiceChatUI");
                voiceUIObj.AddComponent<Lekha.UI.InGameVoiceChatUI>();
                Debug.Log("[GameController] InGameVoiceChatUI created");
            }

            Lekha.UI.InGameVoiceChatUI.Instance?.Show();
        }

        /// <summary>
        /// Ensure GameManager is subscribed to network events
        /// Called after NetworkGameSync is confirmed initialized
        /// </summary>
        private void EnsureNetworkEventSubscription()
        {
            // This triggers the subscription if it wasn't done in Start()
            // due to initialization order issues
            if (NetworkGameSync.Instance != null && GameManager.Instance != null)
            {
                // The subscription is handled in GameManager via a public method
                // We call it here to ensure it happens after NetworkGameSync is ready
                GameManager.Instance.EnsureNetworkSubscription();
            }
        }

        private void OnGameOver(Team winningTeam)
        {
            // Record Barteyyeh win
            BarteyyehManager.Instance?.RecordGameWin(winningTeam);

            // Record game result in profile
            bool playerWon = false;
            int playerPoints = 0;

            if (GameManager.Instance != null)
            {
                var humanPlayer = GameManager.Instance.GetHumanPlayer();
                if (humanPlayer != null)
                {
                    playerWon = humanPlayer.Team == winningTeam;
                    playerPoints = humanPlayer.TotalPoints;
                }
            }

            PlayerProfileManager.Instance?.RecordGameResult(playerWon, playerPoints);

            // Stop effects
            if (ParticleEffects.Instance != null)
            {
                ParticleEffects.Instance.StopSparkles();

                if (playerWon)
                {
                    ParticleEffects.Instance.PlayGameWin(Vector3.zero);
                }
            }

            // GameOverScreen handles navigation (Play Again / Main Menu)
            // No auto-return to main menu
        }

        private void ShowMainMenuDelayed()
        {
            if (premiumMenu != null)
            {
                premiumMenu.Show();
            }
        }

        /// <summary>
        /// Start the next game in the Barteyyeh series.
        /// For online: goes back to ready screen. For local: starts new game directly.
        /// </summary>
        public void StartNextBarteyyehGame()
        {
            Debug.Log("[GameController] StartNextBarteyyehGame called");

            // Hide game over screen
            if (GameOverScreen.Instance != null)
            {
                GameOverScreen.Instance.Hide();
            }

            bool isOnlineGame = NetworkManager.Instance != null &&
                                (NetworkManager.Instance.State == ConnectionState.InLobby ||
                                 NetworkManager.Instance.State == ConnectionState.InGame) &&
                                NetworkManager.Instance.CurrentRoom != null;

            // Reset game state but keep Barteyyeh and network connection
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState_Public(GameState.WaitingToStart);
            }

            // Cleanup GameUI (unsubscribes events and destroys canvas)
            GameUI.Instance?.Cleanup();

            // Stop effects
            if (ParticleEffects.Instance != null)
            {
                ParticleEffects.Instance.StopSparkles();
                ParticleEffects.Instance.StopGlow();
            }

            if (isOnlineGame)
            {
                // Online: go back to lobby/ready screen (players stay in room)
                LobbyUI lobbyUI = Object.FindFirstObjectByType<LobbyUI>();
                if (lobbyUI != null)
                {
                    lobbyUI.Show();
                    Debug.Log("[GameController] Showing lobby for next Barteyyeh game");
                }
                else
                {
                    // Fallback: show main menu if lobby is unavailable
                    Debug.LogWarning("[GameController] LobbyUI not available, showing main menu");
                    if (premiumMenu != null) premiumMenu.Show();
                }
            }
            else
            {
                // Local game: start new game directly
                Debug.Log("[GameController] Local game - starting next Barteyyeh game");
                OnPlayClicked();
            }
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[GameController] ReturnToMainMenu called");

            // Cancel any pending delayed calls
            CancelInvoke();

            // Restore time scale (pause menu sets it to 0)
            Time.timeScale = 1f;

            // Stop any ongoing effects
            if (ParticleEffects.Instance != null)
            {
                ParticleEffects.Instance.StopSparkles();
                ParticleEffects.Instance.StopGlow();
            }

            // Hide pause menu
            if (PauseMenu.Instance != null)
            {
                PauseMenu.Instance.Hide();
            }

            // Hide game over screen
            if (GameOverScreen.Instance != null)
            {
                GameOverScreen.Instance.Hide();
            }

            // Hide voice chat UI and leave channel
            if (InGameVoiceChatUI.Instance != null)
            {
                InGameVoiceChatUI.Instance.Hide();
            }

            if (VoiceChatManager.Instance != null)
            {
                VoiceChatManager.Instance.LeaveChannel();
            }

            // Disconnect from online room if connected
            if (NetworkManager.Instance != null &&
                (NetworkManager.Instance.State == ConnectionState.InLobby ||
                 NetworkManager.Instance.State == ConnectionState.InGame))
            {
                NetworkManager.Instance.LeaveRoom();
                Debug.Log("[GameController] Left online room");
            }

            // Reset online game config
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResetOnlineConfig();
                GameManager.Instance.SetState_Public(GameState.WaitingToStart);
            }

            // Reset network game sync
            if (NetworkGameSync.Instance != null)
            {
                NetworkGameSync.Instance.ResetOnlineState();
            }

            // Reset Barteyyeh
            BarteyyehManager.Instance?.ResetBarteyyeh();

            // Cleanup GameUI (unsubscribes events and destroys canvas)
            GameUI.Instance?.Cleanup();

            // Show main menu
            if (premiumMenu != null)
            {
                premiumMenu.Show();
            }

            Debug.Log("[GameController] Returned to main menu");
        }
    }
}
