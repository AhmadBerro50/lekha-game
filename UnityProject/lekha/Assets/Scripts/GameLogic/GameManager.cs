using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lekha.Core;
using Lekha.Network;

namespace Lekha.GameLogic
{
    /// <summary>
    /// Possible states of the game
    /// </summary>
    public enum GameState
    {
        WaitingToStart,
        Dealing,
        PassingCards,
        PlayingTricks,
        RoundEnd,
        GameOver
    }

    /// <summary>
    /// Main game controller - manages game flow and state
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.WaitingToStart;
        public GameState CurrentState => currentState;

        [Header("Players")]
        private Player[] players = new Player[4];
        public Player[] Players => players;

        private int currentPlayerIndex;
        public Player CurrentPlayer => players[currentPlayerIndex];

        [Header("Round Info")]
        private int roundNumber = 0;
        public int RoundNumber => roundNumber;

        private int startingPlayerIndex = 0; // Who starts the round

        [Header("Trick Info")]
        private List<Card> currentTrick = new List<Card>(4);
        public IReadOnlyList<Card> CurrentTrick => currentTrick;
        private int trickLeaderIndex;
        public PlayerPosition TrickLeaderPosition => players[trickLeaderIndex].Position;
        private Suit? ledSuit;
        public Suit? LedSuit => ledSuit;
        private int tricksPlayedThisRound = 0;
        private Player queenOfSpadesTaker = null; // Track who took the Queen this round

        // Network sync flag - prevents re-sending when processing remote plays
        private bool isProcessingRemotePlay = false;

        [Header("Game Components")]
        private Deck deck;

        // Online game state
        private bool isOnlineGame = false;
        private PlayerPosition? localPlayerPosition = null;

        // Disconnect tracking for online games
        private HashSet<PlayerPosition> disconnectedPositions = new HashSet<PlayerPosition>();
        private HashSet<PlayerPosition> botReplacedPositions = new HashSet<PlayerPosition>();

        // Points needed to lose
        private const int LOSING_SCORE = 101;

        // Events for UI updates
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<Player, Card> OnCardPlayed;
        public System.Action<Player, List<Card>> OnTrickWon;
        public System.Action<Player[]> OnRoundEnded;
        public System.Action<Team> OnGameOver;
        public System.Action OnCardsDealt;
        public System.Action OnPassPhaseComplete;
        public System.Action<PlayerPosition, List<Card>> OnPassCardsReceived; // fromPosition, cards
        public System.Action<Player> OnTrickStarted; // Fired when a new trick starts, with the leading player

        private void Awake()
        {
            // Singleton pattern
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            deck = new Deck();
        }

        private void Start()
        {
            InitializePlayers();
            SubscribeToNetworkEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromNetworkEvents();
        }

        private bool networkEventsSubscribed = false;

        private void SubscribeToNetworkEvents()
        {
            if (networkEventsSubscribed) return;

            if (NetworkGameSync.Instance != null)
            {
                NetworkGameSync.Instance.OnCardPlayedReceived += HandleRemoteCardPlayed;
                NetworkGameSync.Instance.OnPassCardsReceived += HandleRemotePassCards;
                NetworkGameSync.Instance.OnTrickWonReceived += HandleRemoteTrickWon;
                NetworkGameSync.Instance.OnRoundEndReceived += HandleRemoteRoundEnd;
                NetworkGameSync.Instance.OnGameOverReceived += HandleRemoteGameOver;
                networkEventsSubscribed = true;
                Debug.Log("[GameManager] Subscribed to network events");
            }
            else
            {
                Debug.Log("[GameManager] NetworkGameSync.Instance is null, will retry subscription later");
            }
        }

        /// <summary>
        /// Ensure network events are subscribed (called when NetworkGameSync is ready)
        /// </summary>
        public void EnsureNetworkSubscription()
        {
            if (!networkEventsSubscribed)
            {
                SubscribeToNetworkEvents();
            }
        }

        private void UnsubscribeFromNetworkEvents()
        {
            if (NetworkGameSync.Instance != null)
            {
                NetworkGameSync.Instance.OnCardPlayedReceived -= HandleRemoteCardPlayed;
                NetworkGameSync.Instance.OnPassCardsReceived -= HandleRemotePassCards;
                NetworkGameSync.Instance.OnTrickWonReceived -= HandleRemoteTrickWon;
                NetworkGameSync.Instance.OnRoundEndReceived -= HandleRemoteRoundEnd;
                NetworkGameSync.Instance.OnGameOverReceived -= HandleRemoteGameOver;
            }
        }

        /// <summary>
        /// Handle card played by a remote player
        /// </summary>
        private void HandleRemoteCardPlayed(Network.CardPlayedData playData)
        {
            if (!System.Enum.TryParse<PlayerPosition>(playData.Position, out PlayerPosition pos))
                return;

            // Guard: don't process if not in trick-playing state
            if (currentState != GameState.PlayingTricks)
            {
                Debug.LogWarning($"[GameManager] Ignoring remote card play — not in PlayingTricks (state={currentState})");
                return;
            }

            // Guard: don't process if already handling a remote play (duplicate message)
            if (isProcessingRemotePlay)
            {
                Debug.LogWarning($"[GameManager] Ignoring duplicate remote card play from {pos}");
                return;
            }

            // Guard: check if trick already has 4 cards (stale message)
            if (currentTrick.Count >= 4)
            {
                Debug.LogWarning($"[GameManager] Ignoring remote card play — trick already complete ({currentTrick.Count} cards)");
                return;
            }

            Player player = GetPlayerAtPosition(pos);
            Card card = playData.Card.ToCard();

            Debug.Log($"[GameManager] Remote player {pos} played {card}");

            isProcessingRemotePlay = true;
            try
            {
                // For non-host clients, remote players' hands may be empty
                bool cardWasInHand = player.Hand.Any(c => c.Equals(card));
                if (!cardWasInHand)
                {
                    player.AddPassedCards(new List<Card> { card });
                }

                PlayCard(player, card);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[GameManager] Exception in HandleRemoteCardPlayed: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isProcessingRemotePlay = false;
            }
        }

        // Track pass phase state for online games
        private HashSet<string> receivedPassFrom = new HashSet<string>();
        private bool localPassSubmitted = false;
        private Network.PassCardsData bufferedPassCards = null;
        private Coroutine passPhaseTimeoutCoroutine = null;
        private const float PASS_PHASE_TIMEOUT = 30f; // Max seconds to wait for pass cards

        /// <summary>
        /// Called by GameUI when the local player submits their pass cards.
        /// This ensures we don't complete the pass phase until both:
        /// 1. Local player has submitted their pass
        /// 2. We've received pass cards from the server
        /// </summary>
        public void NotifyLocalPassSubmitted()
        {
            localPassSubmitted = true;
            Debug.Log("[GameManager] Local player submitted pass cards");

            // If we already received remote pass cards while waiting, apply them now
            if (bufferedPassCards != null)
            {
                Debug.Log("[GameManager] Applying buffered pass cards");
                ApplyReceivedPassCards(bufferedPassCards);
                bufferedPassCards = null;
            }
        }

        /// <summary>
        /// Handle pass cards from remote player
        /// </summary>
        private void HandleRemotePassCards(Network.PassCardsData passData)
        {
            Debug.Log($"[GameManager] Received pass cards from {passData.FromPosition} to {passData.ToPosition}");

            if (System.Enum.TryParse<PlayerPosition>(passData.ToPosition, out PlayerPosition toPos))
            {
                if (isOnlineGame && localPlayerPosition.HasValue && toPos == localPlayerPosition.Value)
                {
                    // If local player hasn't submitted their pass yet, buffer the received cards
                    if (!localPassSubmitted)
                    {
                        Debug.Log("[GameManager] Buffering pass cards - local player hasn't passed yet");
                        bufferedPassCards = passData;
                        return;
                    }

                    ApplyReceivedPassCards(passData);
                }
            }
        }

        /// <summary>
        /// Apply received pass cards to local player's hand and complete pass phase
        /// </summary>
        private void ApplyReceivedPassCards(Network.PassCardsData passData)
        {
            if (System.Enum.TryParse<PlayerPosition>(passData.ToPosition, out PlayerPosition toPos))
            {
                Player recipient = GetPlayerAtPosition(toPos);
                List<Card> cards = new List<Card>();
                foreach (var cardData in passData.Cards)
                {
                    cards.Add(cardData.ToCard());
                }
                recipient.AddPassedCards(cards);

                receivedPassFrom.Add(passData.FromPosition);
                Debug.Log($"[GameManager] Local player received {cards.Count} cards from {passData.FromPosition}");

                // Notify UI to show received cards with "P" badge
                if (System.Enum.TryParse<PlayerPosition>(passData.FromPosition, out PlayerPosition fromPos))
                {
                    OnPassCardsReceived?.Invoke(fromPos, cards);
                }

                if (currentState == GameState.PassingCards)
                {
                    CompleteOnlinePassPhase();
                }
            }
        }

        /// <summary>
        /// Complete pass phase for online games
        /// </summary>
        private void CompleteOnlinePassPhase()
        {
            Debug.Log("[GameManager] Online pass phase complete");
            CancelPassPhaseTimeout();
            receivedPassFrom.Clear();
            localPassSubmitted = false;
            bufferedPassCards = null;

            OnPassPhaseComplete?.Invoke();
            StartTrickPhase();
        }

        /// <summary>
        /// Start a timeout coroutine for the pass phase — if it takes too long, force completion
        /// </summary>
        public void StartPassPhaseTimeout()
        {
            CancelPassPhaseTimeout();
            if (isOnlineGame)
            {
                passPhaseTimeoutCoroutine = StartCoroutine(PassPhaseTimeoutCoroutine());
            }
        }

        private void CancelPassPhaseTimeout()
        {
            if (passPhaseTimeoutCoroutine != null)
            {
                StopCoroutine(passPhaseTimeoutCoroutine);
                passPhaseTimeoutCoroutine = null;
            }
        }

        private System.Collections.IEnumerator PassPhaseTimeoutCoroutine()
        {
            yield return new WaitForSeconds(PASS_PHASE_TIMEOUT);

            if (currentState == GameState.PassingCards)
            {
                Debug.LogError($"[GameManager] Pass phase timeout after {PASS_PHASE_TIMEOUT}s — forcing completion");
                localPassSubmitted = true; // Prevent buffered cards from re-triggering
                bufferedPassCards = null;
                // Skip directly to trick phase — don't fire OnPassPhaseComplete twice
                CancelPassPhaseTimeout();
                receivedPassFrom.Clear();
                Debug.LogWarning("[GameManager] Forcing transition to trick phase");
                OnPassPhaseComplete?.Invoke();
                StartTrickPhase();
            }
            passPhaseTimeoutCoroutine = null;
        }

        /// <summary>
        /// Handle trick won notification from host (for non-host clients)
        /// </summary>
        private void HandleRemoteTrickWon(Network.TrickWonData wonData)
        {
            // Non-host clients use this to sync their state
            Debug.Log($"[GameManager] Remote trick won by {wonData.WinnerPosition}");
            // The actual trick logic is handled locally, this is just for verification
        }

        /// <summary>
        /// Handle round end notification from host
        /// </summary>
        private void HandleRemoteRoundEnd(Network.RoundEndData roundData)
        {
            Debug.Log($"[GameManager] Remote round {roundData.RoundNumber} ended");
            // For non-host, this ensures score sync
        }

        /// <summary>
        /// Handle game over notification from host
        /// </summary>
        private void HandleRemoteGameOver(Team losingTeam)
        {
            Debug.Log($"[GameManager] Remote game over - {losingTeam} lost");
            // Sync game over state
        }

        /// <summary>
        /// Create all 4 players
        /// Turn order is left to right (clockwise): South → East → North → West
        /// </summary>
        private void InitializePlayers()
        {
            // Players array indexed by turn order: 0=South, 1=East, 2=North, 3=West
            players[0] = new Player(0, "You", PlayerPosition.South, isHuman: true);
            players[1] = new Player(1, "East AI", PlayerPosition.East, isHuman: false);
            players[2] = new Player(2, "Partner", PlayerPosition.North, isHuman: false);
            players[3] = new Player(3, "West AI", PlayerPosition.West, isHuman: false);

            Debug.Log("Players initialized (turn order: South → East → North → West):");
            foreach (var player in players)
            {
                Debug.Log($"  {player}");
            }
        }

        /// <summary>
        /// Start a new game
        /// </summary>
        public void StartGame()
        {
            roundNumber = 0;

            // Random starting player for first round
            startingPlayerIndex = Random.Range(0, 4);

            // Clear disconnect/bot tracking from previous game
            disconnectedPositions.Clear();
            botReplacedPositions.Clear();

            // Reset all player scores
            foreach (var player in players)
            {
                // Reset scores when starting a new game
                player.ResetForNewGame();
            }

            Debug.Log("=== GAME STARTED ===");
            Debug.Log($"Random starting player: {players[startingPlayerIndex].PlayerName}");
            StartNewRound();
        }

        /// <summary>
        /// Prepare for online game (for non-host clients)
        /// Sets up game state without dealing cards - waits for host to sync
        /// </summary>
        public void PrepareForOnlineGame()
        {
            roundNumber = 0;

            // Reset all player scores
            foreach (var player in players)
            {
                player.ResetForNewGame();
            }

            Debug.Log("=== ONLINE GAME - WAITING FOR HOST ===");
            SetState(GameState.Dealing);
            // Cards will be received via NetworkGameSync.HandleCardDealt
        }

        /// <summary>
        /// Notify that cards have been dealt (called by NetworkGameSync for non-host clients)
        /// </summary>
        public void NotifyCardsDealt()
        {
            // Reset pass state for new round
            localPassSubmitted = false;
            bufferedPassCards = null;
            receivedPassFrom.Clear();

            // Reset round state for non-host clients (host resets these in StartNewRound)
            tricksPlayedThisRound = 0;
            queenOfSpadesTaker = null;
            isProcessingRemotePlay = false; // Safety reset between rounds
            Lekha.AI.AIPlayer.ResetRoundTracking();

            Debug.Log($"[GameManager] NotifyCardsDealt: Reset round state (tricksPlayed=0, isProcessingRemotePlay=false)");

            OnCardsDealt?.Invoke();
            SetState(GameState.PassingCards);
            StartPassPhaseTimeout();
        }

        /// <summary>
        /// Set the starting player for a round (called by NetworkGameSync for non-host clients)
        /// </summary>
        public void SetStartingPlayer(PlayerPosition position, int round)
        {
            startingPlayerIndex = GetPlayerIndexAtPosition(position);
            roundNumber = round;
            Debug.Log($"[GameManager] Set starting player to {position} (index {startingPlayerIndex}) for round {round}");
        }

        /// <summary>
        /// Start a new round
        /// </summary>
        public void StartNewRound()
        {
            roundNumber++;
            tricksPlayedThisRound = 0;
            queenOfSpadesTaker = null; // Reset queen taker tracking
            isProcessingRemotePlay = false; // Safety reset between rounds
            Lekha.AI.AIPlayer.ResetRoundTracking();

            // Reset pass state for new round (host path - non-host resets in NotifyCardsDealt)
            localPassSubmitted = false;
            bufferedPassCards = null;
            receivedPassFrom.Clear();

            Debug.Log($"=== ROUND {roundNumber} ===");

            // Reset deck and deal
            deck.Reset();
            SetState(GameState.Dealing);

            List<Card>[] hands = deck.DealToPlayers();

            for (int i = 0; i < 4; i++)
            {
                players[i].ReceiveCards(hands[i]);
            }

            // In online game, host broadcasts dealt cards to all players
            bool isOnlineGameNow = NetworkGameSync.Instance != null && NetworkGameSync.Instance.IsOnlineGame;
            if (isOnlineGameNow && NetworkGameSync.Instance.IsHost)
            {
                Debug.Log("[GameManager] Broadcasting dealt cards to all players");
                PlayerPosition startingPos = players[startingPlayerIndex].Position;
                for (int i = 0; i < 4; i++)
                {
                    NetworkGameSync.Instance.SendDealCards(players[i].Position, players[i].Hand.ToList(), startingPos, roundNumber);
                }
            }

            OnCardsDealt?.Invoke();

            // Move to pass phase
            SetState(GameState.PassingCards);
            StartPassPhaseTimeout();
        }

        /// <summary>
        /// Handle card passing (each player passes 3 cards to their right)
        /// </summary>
        public void ExecutePassPhase(Dictionary<Player, List<Card>> cardsToPass)
        {
            if (currentState != GameState.PassingCards)
            {
                Debug.LogError("Not in passing phase!");
                return;
            }

            // First, remove all cards from hands
            foreach (var kvp in cardsToPass)
            {
                kvp.Key.RemoveCards(kvp.Value);
            }

            // Then, give cards to player on the right
            foreach (var kvp in cardsToPass)
            {
                PlayerPosition rightPosition = Player.GetPlayerToRight(kvp.Key.Position);
                Player rightPlayer = GetPlayerAtPosition(rightPosition);
                rightPlayer.AddPassedCards(kvp.Value);

                // Notify UI which cards were passed to the local player
                if (rightPlayer.IsHuman && (!isOnlineGame || (localPlayerPosition.HasValue && rightPosition == localPlayerPosition.Value)))
                {
                    OnPassCardsReceived?.Invoke(kvp.Key.Position, kvp.Value);
                }
            }

            Debug.Log("Pass phase complete");
            OnPassPhaseComplete?.Invoke();

            // Start playing
            StartTrickPhase();
        }

        /// <summary>
        /// Begin the trick-taking phase
        /// </summary>
        private void StartTrickPhase()
        {
            // Set current player BEFORE changing state so UI knows who should play
            currentPlayerIndex = startingPlayerIndex;
            trickLeaderIndex = startingPlayerIndex;

            Debug.Log($"{CurrentPlayer.PlayerName} leads the first trick");

            // Now set state - UI will check CurrentPlayer which is already set
            SetState(GameState.PlayingTricks);
            StartNewTrick();
        }

        /// <summary>
        /// Start a new trick
        /// </summary>
        private void StartNewTrick()
        {
            currentTrick.Clear();
            ledSuit = null;
            currentPlayerIndex = trickLeaderIndex;

            Debug.Log($"--- Trick {tricksPlayedThisRound + 1} ---");
            Debug.Log($"{CurrentPlayer.PlayerName} to lead");

            // Notify UI that a new trick has started - this allows UI to schedule AI play
            Debug.Log($"[GameManager] Firing OnTrickStarted event for {CurrentPlayer.PlayerName}");
            OnTrickStarted?.Invoke(CurrentPlayer);
            Debug.Log($"[GameManager] OnTrickStarted event fired (subscribers: {(OnTrickStarted != null ? "yes" : "no")})");
        }

        /// <summary>
        /// Player plays a card
        /// </summary>
        public bool PlayCard(Player player, Card card)
        {
            if (currentState != GameState.PlayingTricks)
            {
                Debug.LogError("Not in trick-playing phase!");
                return false;
            }

            // For remote plays, be more lenient with validation
            if (!isProcessingRemotePlay)
            {
                if (player != CurrentPlayer)
                {
                    Debug.LogError($"Not {player.PlayerName}'s turn! Current: {CurrentPlayer.PlayerName}");
                    return false;
                }

                // Validate the play
                List<Card> playableCards = player.GetPlayableCards(ledSuit);
                if (!playableCards.Contains(card))
                {
                    Debug.LogError($"Cannot play {card.GetUnoName()} - must follow suit!");
                    return false;
                }
            }
            else
            {
                // For remote plays, advance to the correct player if needed
                if (player != CurrentPlayer)
                {
                    Debug.Log($"[PlayCard] Remote play from {player.PlayerName}, advancing from {CurrentPlayer.PlayerName}");
                    currentPlayerIndex = GetPlayerIndexAtPosition(player.Position);
                }
            }

            // Play the card
            player.PlayCard(card);
            currentTrick.Add(card);

            // Track for AI intelligence
            Lekha.AI.AIPlayer.TrackPlayedCard(card);
            Lekha.AI.AIPlayer.UpdateSuitCount(player.Position, card.Suit);
            // Detect void: if player didn't follow suit, they're void
            if (ledSuit.HasValue && card.Suit != ledSuit.Value)
            {
                Lekha.AI.AIPlayer.TrackVoid(player.Position, ledSuit.Value);
            }

            // Set led suit if this is first card
            if (ledSuit == null)
            {
                ledSuit = card.Suit;
                // Track who led which suit for AI partner coordination
                Lekha.AI.AIPlayer.TrackSuitLed(player.Position, card.Suit);
                Debug.Log($"Led suit: {ledSuit} ({card.GetUnoColor()})");
            }

            // Send card play to network (for online games)
            if (isOnlineGame && !isProcessingRemotePlay)
            {
                NetworkGameSync.Instance.SendCardPlayed(player.Position, card, tricksPlayedThisRound);
            }
            else if (isOnlineGame && isProcessingRemotePlay)
            {
                Debug.Log($"[PlayCard] Skipping SendCardPlayed for {player.Position} - isProcessingRemotePlay=true (remote play relay)");
            }

            // Move to next player or end trick BEFORE firing event
            // so UI knows who plays next
            if (currentTrick.Count < 4)
            {
                currentPlayerIndex = (currentPlayerIndex + 1) % 4;
                Debug.Log($"{CurrentPlayer.PlayerName}'s turn");
                OnCardPlayed?.Invoke(player, card);
            }
            else
            {
                // Trick complete - fire event first, then end trick
                OnCardPlayed?.Invoke(player, card);
                EndTrick();
            }

            return true;
        }

        /// <summary>
        /// End the current trick and determine winner
        /// </summary>
        private void EndTrick()
        {
            // Find winning card (highest of led suit)
            int winningIndex = 0;
            Card winningCard = currentTrick[0];

            for (int i = 1; i < 4; i++)
            {
                Card card = currentTrick[i];
                // Only cards of led suit can win
                if (card.Suit == ledSuit && card.CompareRank(winningCard) > 0)
                {
                    winningCard = card;
                    winningIndex = i;
                }
            }

            // Calculate which player won (based on who led + position in trick)
            int winnerPlayerIndex = (trickLeaderIndex + winningIndex) % 4;
            Player winner = players[winnerPlayerIndex];

            Debug.Log($"{winner.PlayerName} wins trick with {winningCard.GetUnoName()}");

            // Give cards to winner
            winner.WinTrick(new List<Card>(currentTrick));
            OnTrickWon?.Invoke(winner, new List<Card>(currentTrick));

            // Track dangerous suits for AI (suits that caused points)
            int trickPoints = currentTrick.Sum(c => c.GetPoints());
            if (trickPoints > 0 && ledSuit.HasValue)
            {
                Lekha.AI.AIPlayer.TrackDangerousSuit(ledSuit.Value, trickPoints);
            }

            // Track if winner got the Queen of Spades (Blue +2)
            if (currentTrick.Any(c => c.IsQueenOfSpades()))
            {
                queenOfSpadesTaker = winner;
                Debug.Log($"*** {winner.PlayerName} took the Queen of Spades (Blue +2)! ***");
            }

            tricksPlayedThisRound++;

            // Check if round is over (13 tricks)
            if (tricksPlayedThisRound >= 13)
            {
                EndRound();
            }
            else
            {
                // Winner leads next trick
                trickLeaderIndex = winnerPlayerIndex;
                StartNewTrick();
            }
        }

        // Events for game over with player info
        public System.Action<Player> OnPlayerLost; // Fired when a specific player reaches 101

        /// <summary>
        /// End the round and calculate scores
        /// </summary>
        private void EndRound()
        {
            Debug.Log($"=== ROUND {roundNumber} END ===");

            // End round for all players (transfers round points to total)
            foreach (var player in players)
            {
                player.EndRound();
            }

            // Log individual player scores
            foreach (var player in players)
            {
                Debug.Log($"{player.PlayerName} ({player.Position}): {player.TotalPoints} total points");
            }

            OnRoundEnded?.Invoke(players);

            // Check for game over - ANY individual player reaching 101 loses
            Player losingPlayer = null;
            foreach (var player in players)
            {
                if (player.TotalPoints >= LOSING_SCORE)
                {
                    // If multiple players hit 101, the one with the highest score loses
                    if (losingPlayer == null || player.TotalPoints > losingPlayer.TotalPoints)
                    {
                        losingPlayer = player;
                    }
                }
            }

            if (losingPlayer != null)
            {
                SetState(GameState.GameOver);
                Debug.Log($"{losingPlayer.PlayerName} LOSES with {losingPlayer.TotalPoints} points! (reached 101+)");

                // Fire the player-specific loss event
                OnPlayerLost?.Invoke(losingPlayer);

                // The losing player's team loses
                Team losingTeam = losingPlayer.Team;
                Team winningTeam = losingTeam == Team.NorthSouth ? Team.EastWest : Team.NorthSouth;
                OnGameOver?.Invoke(winningTeam);
                return;
            }

            // Set starting player for next round
            if (queenOfSpadesTaker != null)
            {
                // Player to the right of queen taker starts
                PlayerPosition rightPos = Player.GetPlayerToRight(queenOfSpadesTaker.Position);
                startingPlayerIndex = GetPlayerIndexAtPosition(rightPos);
                Debug.Log($"{queenOfSpadesTaker.PlayerName} took Queen of Spades. {players[startingPlayerIndex].PlayerName} starts next round.");
            }
            else
            {
                // This shouldn't happen in normal play, but keep current starter
                Debug.LogWarning("Queen of Spades not tracked this round - keeping current starter");
            }

            // Continue to next round
            SetState(GameState.RoundEnd);
        }

        /// <summary>
        /// Get individual player score
        /// </summary>
        public int GetPlayerScore(PlayerPosition position)
        {
            return GetPlayerAtPosition(position).TotalPoints;
        }

        /// <summary>
        /// Get the player who lost (reached 101+), or null if no one has
        /// </summary>
        public Player GetLosingPlayer()
        {
            return players.FirstOrDefault(p => p.TotalPoints >= LOSING_SCORE);
        }

        /// <summary>
        /// Get player at a specific position
        /// </summary>
        public Player GetPlayerAtPosition(PlayerPosition position)
        {
            return players.First(p => p.Position == position);
        }

        /// <summary>
        /// Get player index at a specific position
        /// </summary>
        private int GetPlayerIndexAtPosition(PlayerPosition position)
        {
            for (int i = 0; i < 4; i++)
            {
                if (players[i].Position == position)
                    return i;
            }
            return 0;
        }

        /// <summary>
        /// Get team total score
        /// </summary>
        public int GetTeamScore(Team team)
        {
            return players.Where(p => p.Team == team).Sum(p => p.TotalPoints);
        }

        /// <summary>
        /// Change game state
        /// </summary>
        private void SetState(GameState newState)
        {
            currentState = newState;
            Debug.Log($"Game state: {newState}");
            OnGameStateChanged?.Invoke(newState);
        }

        /// <summary>
        /// Public state setter for network sync scenarios
        /// </summary>
        public void SetState_Public(GameState newState)
        {
            SetState(newState);
        }

        /// <summary>
        /// Get the human player (local player in online games)
        /// </summary>
        public Player GetHumanPlayer()
        {
            // In online games, return the local player
            if (isOnlineGame && localPlayerPosition.HasValue)
            {
                return GetPlayerAtPosition(localPlayerPosition.Value);
            }
            // In local games, return South (traditional human position)
            return players.First(p => p.IsHuman);
        }

        /// <summary>
        /// Check if this is an online game
        /// </summary>
        public bool IsOnlineGame => isOnlineGame;

        /// <summary>
        /// Get local player position (for online games)
        /// </summary>
        public PlayerPosition? LocalPlayerPosition => localPlayerPosition;

        /// <summary>
        /// Convert a server-assigned position to the visual screen position
        /// relative to the local player. In online games, the local player
        /// always appears at South (bottom), partner at North (top).
        /// In offline games, returns the position unchanged.
        /// </summary>
        public PlayerPosition GetVisualPosition(PlayerPosition serverPos)
        {
            if (!isOnlineGame || !localPlayerPosition.HasValue)
                return serverPos;

            // Clockwise order: South(0), East(1), North(2), West(3)
            int serverIndex = PositionToIndex(serverPos);
            int localIndex = PositionToIndex(localPlayerPosition.Value);
            int visualIndex = (serverIndex - localIndex + 4) % 4;
            return IndexToPosition(visualIndex);
        }

        private static int PositionToIndex(PlayerPosition pos)
        {
            return pos switch
            {
                PlayerPosition.South => 0,
                PlayerPosition.East => 1,
                PlayerPosition.North => 2,
                PlayerPosition.West => 3,
                _ => 0
            };
        }

        private static PlayerPosition IndexToPosition(int index)
        {
            return (index % 4) switch
            {
                0 => PlayerPosition.South,
                1 => PlayerPosition.East,
                2 => PlayerPosition.North,
                3 => PlayerPosition.West,
                _ => PlayerPosition.South
            };
        }

        /// <summary>
        /// Configure for online multiplayer game
        /// </summary>
        public void ConfigureForOnlineGame(PlayerPosition localPosition)
        {
            isOnlineGame = true;
            localPlayerPosition = localPosition;

            // In online games, mark ALL players as human (controlled by real people)
            // except the local player has special handling for input
            foreach (var player in players)
            {
                // Mark all as human so AI doesn't play for them
                player.SetIsHuman(true);
            }

            Debug.Log($"[GameManager] Configured for online game. Local position: {localPosition}");
        }

        /// <summary>
        /// Check if it's the local player's turn
        /// </summary>
        public bool IsLocalPlayerTurn()
        {
            if (!isOnlineGame) return CurrentPlayer.IsHuman;
            return localPlayerPosition.HasValue && CurrentPlayer.Position == localPlayerPosition.Value;
        }

        /// <summary>
        /// Reset online game configuration
        /// </summary>
        public void ResetOnlineConfig()
        {
            isOnlineGame = false;
            localPlayerPosition = null;
            disconnectedPositions.Clear();
            botReplacedPositions.Clear();
            // Restore default human state
            foreach (var player in players)
            {
                player.SetIsHuman(player.Position == PlayerPosition.South);
            }
        }

        // --- Disconnect tracking for online games ---

        public void MarkPlayerDisconnected(PlayerPosition pos)
        {
            disconnectedPositions.Add(pos);
            Debug.Log($"[GameManager] Player at {pos} marked as disconnected");
        }

        public void MarkPlayerReconnected(PlayerPosition pos)
        {
            disconnectedPositions.Remove(pos);
            Debug.Log($"[GameManager] Player at {pos} reconnected");
        }

        public void ReplacePlayerWithBot(PlayerPosition pos)
        {
            disconnectedPositions.Remove(pos);
            botReplacedPositions.Add(pos);
            Player player = GetPlayerAtPosition(pos);
            if (player != null) player.SetIsHuman(false);
            Debug.Log($"[GameManager] Player at {pos} replaced by bot");
        }

        public bool IsPlayerDisconnectedOrBot(PlayerPosition pos)
        {
            return disconnectedPositions.Contains(pos) || botReplacedPositions.Contains(pos);
        }

        public bool ShouldHostPlayForPosition(PlayerPosition pos)
        {
            if (!isOnlineGame) return false;
            if (NetworkGameSync.Instance == null || !NetworkGameSync.Instance.IsHost) return false;
            return IsPlayerDisconnectedOrBot(pos);
        }

        public IReadOnlyCollection<PlayerPosition> DisconnectedPositions => disconnectedPositions;
        public IReadOnlyCollection<PlayerPosition> BotReplacedPositions => botReplacedPositions;

        /// <summary>
        /// Force-set the current player from server turn authority.
        /// Used when server sends TurnUpdate to keep clients in sync.
        /// </summary>
        public void ForceSetCurrentPlayer(PlayerPosition position)
        {
            // Don't force during non-playing states
            if (currentState != GameState.PlayingTricks) return;
            // Don't force if a remote play is being processed right now
            if (isProcessingRemotePlay) return;

            int idx = GetPlayerIndexAtPosition(position);
            if (idx != currentPlayerIndex)
            {
                Debug.Log($"[GameManager] Server turn authority: advancing to {position} (was {CurrentPlayer.PlayerName})");
                currentPlayerIndex = idx;
            }
        }

        /// <summary>
        /// Set player names from network player data for online games
        /// </summary>
        public void SetNetworkPlayerNames(Dictionary<PlayerPosition, string> playerNames)
        {
            if (players == null) return;

            foreach (var player in players)
            {
                if (playerNames.TryGetValue(player.Position, out string name))
                {
                    player.PlayerName = name;
                    Debug.Log($"[GameManager] Set network player name: {player.Position} = {name}");
                }
            }
        }

        /// <summary>
        /// Get player name at a position (for display)
        /// </summary>
        public string GetPlayerDisplayName(PlayerPosition position)
        {
            var player = GetPlayerAtPosition(position);
            return player?.PlayerName ?? position.ToString();
        }
    }
}
