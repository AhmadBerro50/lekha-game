using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Lekha.Core;
using Lekha.GameLogic;
using Lekha.UI;

namespace Lekha.Network
{
    /// <summary>
    /// Serializable game state for network transmission
    /// </summary>
    [Serializable]
    public class NetworkGameState
    {
        public string GameId;
        public int CurrentRound;
        public int CurrentTrick;
        public string CurrentPlayerPosition;
        public string LeadingPosition;
        public List<NetworkCardData> CurrentTrickCards;
        public Dictionary<string, int> TeamScores;
        public bool HeartsbrokenBroken;
        public long Timestamp;
    }

    /// <summary>
    /// Serializable card data for network transmission
    /// </summary>
    [Serializable]
    public class NetworkCardData
    {
        public string Suit;
        public string Rank;
        public string PlayedByPosition;

        public NetworkCardData() { }

        public NetworkCardData(Card card, PlayerPosition position)
        {
            Suit = card.Suit.ToString();
            Rank = card.Rank.ToString();
            PlayedByPosition = position.ToString();
        }

        public Card ToCard()
        {
            Suit suit = (Suit)Enum.Parse(typeof(Suit), Suit);
            Rank rank = (Rank)Enum.Parse(typeof(Rank), Rank);
            return new Card(suit, rank);
        }
    }

    /// <summary>
    /// Serializable hand data for network transmission
    /// </summary>
    [Serializable]
    public class NetworkHandData
    {
        public List<NetworkCardData> Cards;

        public NetworkHandData() { }

        public NetworkHandData(List<Card> cards)
        {
            Cards = new List<NetworkCardData>();
            foreach (var card in cards)
            {
                Cards.Add(new NetworkCardData { Suit = card.Suit.ToString(), Rank = card.Rank.ToString() });
            }
        }

        public List<Card> ToCardList()
        {
            List<Card> cards = new List<Card>();
            foreach (var data in Cards)
            {
                cards.Add(data.ToCard());
            }
            return cards;
        }
    }

    /// <summary>
    /// Pass cards action data
    /// </summary>
    [Serializable]
    public class PassCardsData
    {
        public string FromPosition;
        public string ToPosition;
        public List<NetworkCardData> Cards;
    }

    /// <summary>
    /// Card played action data
    /// </summary>
    [Serializable]
    public class CardPlayedData
    {
        public string Position;
        public NetworkCardData Card;
        public int TrickIndex;
    }

    /// <summary>
    /// Trick won action data
    /// </summary>
    [Serializable]
    public class TrickWonData
    {
        public string WinnerPosition;
        public int PointsWon;
        public List<NetworkCardData> TrickCards;
    }

    /// <summary>
    /// Round end data
    /// </summary>
    [Serializable]
    public class RoundEndData
    {
        public int RoundNumber;
        public int NorthSouthPoints;
        public int EastWestPoints;
        public int NorthSouthTotal;
        public int EastWestTotal;
        public bool ShotTheMoon;
        public string ShotByPosition;
    }

    /// <summary>
    /// Handles game state synchronization for online multiplayer
    /// </summary>
    public class NetworkGameSync : MonoBehaviour
    {
        public static NetworkGameSync Instance { get; private set; }

        // Network state
        private bool isOnlineGame = false;
        private bool isHost = false;
        private string localPosition;
        private NetworkGameState currentNetworkState;

        // Events
        public event Action<NetworkHandData> OnHandReceived;
        public event Action<PassCardsData> OnPassCardsReceived;
        public event Action<CardPlayedData> OnCardPlayedReceived;
        public event Action<TrickWonData> OnTrickWonReceived;
        public event Action<RoundEndData> OnRoundEndReceived;
        public event Action<Team> OnGameOverReceived;

        // Pending actions queue
        private Queue<Action> pendingActions = new Queue<Action>();

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
            // Subscribe to network messages
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived += OnNetworkMessageReceived;
                NetworkManager.Instance.OnGameStarted += OnGameStarted;
            }
        }

        private void OnDestroy()
        {
            if (NetworkManager.Instance != null)
            {
                NetworkManager.Instance.OnMessageReceived -= OnNetworkMessageReceived;
                NetworkManager.Instance.OnGameStarted -= OnGameStarted;
            }
        }

        private void Update()
        {
            // Process pending actions on main thread
            while (pendingActions.Count > 0)
            {
                var action = pendingActions.Dequeue();
                action?.Invoke();
            }
        }

        /// <summary>
        /// Initialize for an online game
        /// </summary>
        public void InitializeOnlineGame(string position, bool hosting)
        {
            isOnlineGame = true;
            isHost = hosting;
            localPosition = position;

            Debug.Log($"[NetworkGameSync] Initialized online game - Position: {position}, Host: {hosting}");
        }

        /// <summary>
        /// Check if this is an online game
        /// </summary>
        public bool IsOnlineGame => isOnlineGame;

        /// <summary>
        /// Check if local player is the host
        /// </summary>
        public bool IsHost => isHost;

        /// <summary>
        /// Get local player's position
        /// </summary>
        public string LocalPosition => localPosition;

        private void OnGameStarted(string roomId)
        {
            var room = NetworkManager.Instance?.CurrentRoom;
            var localPlayer = NetworkManager.Instance?.LocalPlayer;

            if (room != null && localPlayer != null)
            {
                string position = localPlayer.AssignedPosition?.ToString() ?? PlayerPosition.South.ToString();
                InitializeOnlineGame(position, localPlayer.IsHost);

                // Set player names in GameManager from network data
                SetNetworkPlayerNamesInGameManager(room);

                // Create and show in-game voice chat UI for online games
                CreateInGameVoiceChatUI();
            }
        }

        /// <summary>
        /// Create the in-game voice chat UI for online multiplayer
        /// </summary>
        private void CreateInGameVoiceChatUI()
        {
            // Voice chat is disabled - don't create UI
            if (VoiceChatManager.VOICE_CHAT_DISABLED)
            {
                return;
            }

            if (InGameVoiceChatUI.Instance == null)
            {
                GameObject voiceUIObj = new GameObject("InGameVoiceChatUI");
                voiceUIObj.AddComponent<InGameVoiceChatUI>();
                Debug.Log("[NetworkGameSync] InGameVoiceChatUI created");
            }

            // Show the voice chat UI
            InGameVoiceChatUI.Instance?.Show();
        }

        /// <summary>
        /// Set player names in GameManager from network room data
        /// </summary>
        private void SetNetworkPlayerNamesInGameManager(GameRoom room)
        {
            if (room?.Players == null || GameManager.Instance == null) return;

            var playerNames = new Dictionary<PlayerPosition, string>();

            foreach (var netPlayer in room.Players)
            {
                if (netPlayer.Position.HasValue)
                {
                    string displayName = !string.IsNullOrEmpty(netPlayer.DisplayName) ? netPlayer.DisplayName : "Player";
                    playerNames[netPlayer.Position.Value] = displayName;
                    Debug.Log($"[NetworkGameSync] Network player: {netPlayer.Position.Value} = {displayName}");
                }
            }

            GameManager.Instance.SetNetworkPlayerNames(playerNames);

            // Refresh the UI to show updated names
            if (GameUI.Instance != null)
            {
                GameUI.Instance.RefreshPlayerPanelNames();
            }
        }

        private void OnNetworkMessageReceived(NetworkMessage message)
        {
            switch (message.GetMessageType())
            {
                case NetworkMessageType.CardDealt:
                    HandleCardDealt(message);
                    break;

                case NetworkMessageType.PassCards:
                    HandlePassCards(message);
                    break;

                case NetworkMessageType.CardPlayed:
                    HandleCardPlayed(message);
                    break;

                case NetworkMessageType.TrickWon:
                    HandleTrickWon(message);
                    break;

                case NetworkMessageType.RoundEnd:
                    HandleRoundEnd(message);
                    break;

                case NetworkMessageType.GameOver:
                    HandleGameOver(message);
                    break;

                case NetworkMessageType.GameState:
                    HandleGameStateSync(message);
                    break;
            }
        }

        // Host functions - send game actions to all players

        /// <summary>
        /// Host: Deal cards to a player
        /// </summary>
        public void SendDealCards(PlayerPosition position, List<Card> cards, PlayerPosition startingPosition, int roundNumber)
        {
            if (!isOnlineGame || !isHost) return;

            var wrapper = new DealtDataWrapper
            {
                Position = position.ToString(),
                Hand = new NetworkHandData(cards),
                StartingPosition = startingPosition.ToString(),
                RoundNumber = roundNumber
            };
            string data = JsonUtility.ToJson(wrapper);

            NetworkManager.Instance?.SendGameAction(NetworkMessageType.CardDealt, data);
            Debug.Log($"[NetworkGameSync] Sent deal to {position}: {cards.Count} cards, starts: {startingPosition}, round: {roundNumber}");
        }

        /// <summary>
        /// Send pass cards action
        /// </summary>
        public void SendPassCards(PlayerPosition from, PlayerPosition to, List<Card> cards)
        {
            if (!isOnlineGame) return;

            var passData = new PassCardsData
            {
                FromPosition = from.ToString(),
                ToPosition = to.ToString(),
                Cards = new List<NetworkCardData>()
            };

            foreach (var card in cards)
            {
                passData.Cards.Add(new NetworkCardData(card, from));
            }

            string data = JsonUtility.ToJson(passData);
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.PassCards, data);
            Debug.Log($"[NetworkGameSync] Sent pass cards from {from} to {to}");
        }

        /// <summary>
        /// Send card played action
        /// </summary>
        public void SendCardPlayed(PlayerPosition position, Card card, int trickIndex)
        {
            if (!isOnlineGame) return;

            var playData = new CardPlayedData
            {
                Position = position.ToString(),
                Card = new NetworkCardData(card, position),
                TrickIndex = trickIndex
            };

            string data = JsonUtility.ToJson(playData);
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.CardPlayed, data);
            Debug.Log($"[NetworkGameSync] Sent card played: {card} by {position}");
        }

        /// <summary>
        /// Host: Send trick won notification
        /// </summary>
        public void SendTrickWon(PlayerPosition winner, int points, List<Card> trickCards)
        {
            if (!isOnlineGame || !isHost) return;

            var wonData = new TrickWonData
            {
                WinnerPosition = winner.ToString(),
                PointsWon = points,
                TrickCards = new List<NetworkCardData>()
            };

            foreach (var card in trickCards)
            {
                wonData.TrickCards.Add(new NetworkCardData { Suit = card.Suit.ToString(), Rank = card.Rank.ToString() });
            }

            string data = JsonUtility.ToJson(wonData);
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.TrickWon, data);
            Debug.Log($"[NetworkGameSync] Sent trick won by {winner}, {points} points");
        }

        /// <summary>
        /// Host: Send round end notification
        /// </summary>
        public void SendRoundEnd(int round, int nsPoints, int ewPoints, int nsTotal, int ewTotal, bool shotMoon, PlayerPosition? shotBy)
        {
            if (!isOnlineGame || !isHost) return;

            var roundData = new RoundEndData
            {
                RoundNumber = round,
                NorthSouthPoints = nsPoints,
                EastWestPoints = ewPoints,
                NorthSouthTotal = nsTotal,
                EastWestTotal = ewTotal,
                ShotTheMoon = shotMoon,
                ShotByPosition = shotBy?.ToString() ?? ""
            };

            string data = JsonUtility.ToJson(roundData);
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.RoundEnd, data);
            Debug.Log($"[NetworkGameSync] Sent round {round} end - NS: {nsTotal}, EW: {ewTotal}");
        }

        /// <summary>
        /// Host: Send game over notification
        /// </summary>
        public void SendGameOver(Team losingTeam)
        {
            if (!isOnlineGame || !isHost) return;

            string data = losingTeam.ToString();
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.GameOver, data);
            Debug.Log($"[NetworkGameSync] Sent game over - Losing team: {losingTeam}");
        }

        /// <summary>
        /// Host: Send full game state for sync
        /// </summary>
        public void SendGameStateSync(NetworkGameState state)
        {
            if (!isOnlineGame || !isHost) return;

            state.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            string data = JsonUtility.ToJson(state);
            NetworkManager.Instance?.SendGameAction(NetworkMessageType.GameState, data);
        }

        // Message handlers

        private void HandleCardDealt(NetworkMessage message)
        {
            try
            {
                // Parse position and hand
                var parsed = JsonUtility.FromJson<DealtDataWrapper>(message.Data);

                Debug.Log($"[NetworkGameSync] Received CardDealt for position: {parsed.Position}, local position: {localPosition}, starting: {parsed.StartingPosition}, round: {parsed.RoundNumber}");

                // Only process if this is for local player's position
                if (parsed.Position == localPosition)
                {
                    pendingActions.Enqueue(() => {
                        // Convert network hand data to actual cards and give to player
                        var cards = parsed.Hand.ToCardList();
                        Debug.Log($"[NetworkGameSync] Received {cards.Count} cards for local player at {localPosition}");

                        // Find the player at the local position and give them the cards
                        if (GameLogic.GameManager.Instance != null)
                        {
                            // Parse the position enum
                            if (System.Enum.TryParse<PlayerPosition>(localPosition, out PlayerPosition pos))
                            {
                                var player = GameLogic.GameManager.Instance.GetPlayerAtPosition(pos);
                                if (player != null)
                                {
                                    player.ReceiveCards(cards);
                                    Debug.Log($"[NetworkGameSync] Cards applied to player at {pos}");

                                    // Set starting position if provided
                                    if (!string.IsNullOrEmpty(parsed.StartingPosition) &&
                                        System.Enum.TryParse<PlayerPosition>(parsed.StartingPosition, out PlayerPosition startPos))
                                    {
                                        GameLogic.GameManager.Instance.SetStartingPlayer(startPos, parsed.RoundNumber);
                                    }

                                    // Notify UI
                                    GameLogic.GameManager.Instance.NotifyCardsDealt();
                                }
                                else
                                {
                                    Debug.LogError($"[NetworkGameSync] No player found at position {pos}");
                                }
                            }
                            else
                            {
                                Debug.LogError($"[NetworkGameSync] Failed to parse position: {localPosition}");
                            }
                        }

                        OnHandReceived?.Invoke(parsed.Hand);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing card dealt: {e.Message}");
            }
        }

        private void HandlePassCards(NetworkMessage message)
        {
            try
            {
                var passData = JsonUtility.FromJson<PassCardsData>(message.Data);

                // Process if we're the recipient
                if (passData.ToPosition == localPosition)
                {
                    pendingActions.Enqueue(() => {
                        OnPassCardsReceived?.Invoke(passData);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing pass cards: {e.Message}");
            }
        }

        private void HandleCardPlayed(NetworkMessage message)
        {
            try
            {
                var playData = JsonUtility.FromJson<CardPlayedData>(message.Data);

                // Don't process our own plays (we already know about them)
                if (playData.Position != localPosition)
                {
                    pendingActions.Enqueue(() => {
                        OnCardPlayedReceived?.Invoke(playData);
                    });
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing card played: {e.Message}");
            }
        }

        private void HandleTrickWon(NetworkMessage message)
        {
            try
            {
                var wonData = JsonUtility.FromJson<TrickWonData>(message.Data);

                pendingActions.Enqueue(() => {
                    OnTrickWonReceived?.Invoke(wonData);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing trick won: {e.Message}");
            }
        }

        private void HandleRoundEnd(NetworkMessage message)
        {
            try
            {
                var roundData = JsonUtility.FromJson<RoundEndData>(message.Data);

                pendingActions.Enqueue(() => {
                    OnRoundEndReceived?.Invoke(roundData);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing round end: {e.Message}");
            }
        }

        private void HandleGameOver(NetworkMessage message)
        {
            try
            {
                Team losingTeam = (Team)Enum.Parse(typeof(Team), message.Data);

                pendingActions.Enqueue(() => {
                    OnGameOverReceived?.Invoke(losingTeam);
                });
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing game over: {e.Message}");
            }
        }

        private void HandleGameStateSync(NetworkMessage message)
        {
            try
            {
                var state = JsonUtility.FromJson<NetworkGameState>(message.Data);
                currentNetworkState = state;

                Debug.Log($"[NetworkGameSync] Received game state sync - Round: {state.CurrentRound}, Trick: {state.CurrentTrick}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[NetworkGameSync] Error parsing game state: {e.Message}");
            }
        }

        /// <summary>
        /// Reset online state (called when returning to main menu)
        /// </summary>
        public void ResetOnlineState()
        {
            Reset();
        }

        /// <summary>
        /// Reset for new game
        /// </summary>
        public void Reset()
        {
            isOnlineGame = false;
            isHost = false;
            localPosition = null;
            currentNetworkState = null;
            pendingActions.Clear();
        }

        /// <summary>
        /// Leave online game
        /// </summary>
        public void LeaveGame()
        {
            Reset();
            NetworkManager.Instance?.LeaveRoom();
        }

        // Helper class for JSON parsing
        [Serializable]
        private class DealtDataWrapper
        {
            public string Position;
            public NetworkHandData Hand;
            public string StartingPosition; // Who leads the first trick
            public int RoundNumber;
        }
    }
}
