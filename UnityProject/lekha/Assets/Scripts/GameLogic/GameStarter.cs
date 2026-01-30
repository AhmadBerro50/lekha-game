using UnityEngine;
using UnityEngine.InputSystem;
using Lekha.Core;
using Lekha.AI;
using System.Collections.Generic;

namespace Lekha.GameLogic
{
    /// <summary>
    /// Temporary test script to start the game and show debug info
    /// </summary>
    public class GameStarter : MonoBehaviour
    {
        private bool gameStarted = false;
        private bool passPhaseComplete = false;

        private void Start()
        {
            // Subscribe to game events
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnCardsDealt += OnCardsDealt;
                GameManager.Instance.OnCardPlayed += OnCardPlayed;
                GameManager.Instance.OnTrickWon += OnTrickWon;
                GameManager.Instance.OnRoundEnded += OnRoundEnded;
                GameManager.Instance.OnGameOver += OnGameOver;
            }
        }

        private void Update()
        {
            // Press SPACE to start game
            if (!gameStarted && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                Debug.Log("========== STARTING GAME ==========");
                GameManager.Instance.StartGame();
                gameStarted = true;
            }

            // Press P to execute pass phase (with AI selections)
            if (gameStarted && !passPhaseComplete &&
                GameManager.Instance.CurrentState == GameState.PassingCards &&
                Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
            {
                ExecuteAIPassPhase();
            }

            // Press ENTER to play next card (AI plays for everyone in test mode)
            if (passPhaseComplete &&
                GameManager.Instance.CurrentState == GameState.PlayingTricks &&
                Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame)
            {
                PlayNextCard();
            }
        }

        private void ExecuteAIPassPhase()
        {
            Debug.Log("========== PASS PHASE ==========");

            var cardsToPass = new Dictionary<Player, List<Card>>();

            foreach (var player in GameManager.Instance.Players)
            {
                List<Card> selectedCards = AIPlayer.ChooseCardsToPass(player);
                cardsToPass[player] = selectedCards;
            }

            GameManager.Instance.ExecutePassPhase(cardsToPass);
            passPhaseComplete = true;

            Debug.Log("Pass phase complete! Press ENTER to play cards.");
        }

        private void PlayNextCard()
        {
            Player currentPlayer = GameManager.Instance.CurrentPlayer;

            // Get current trick info from GameManager
            Suit? ledSuit = GameManager.Instance.LedSuit;
            List<Card> currentTrick = new List<Card>(GameManager.Instance.CurrentTrick);

            // AI chooses card to play
            List<Card> playable = currentPlayer.GetPlayableCards(ledSuit);

            if (playable.Count > 0)
            {
                Card cardToPlay = AIPlayer.ChooseCardToPlay(currentPlayer, ledSuit, currentTrick);
                GameManager.Instance.PlayCard(currentPlayer, cardToPlay);
            }
        }

        private void OnCardsDealt()
        {
            Debug.Log("========== CARDS DEALT ==========");

            foreach (var player in GameManager.Instance.Players)
            {
                Debug.Log($"\n{player.PlayerName}'s hand ({player.Hand.Count} cards):");
                foreach (var card in player.Hand)
                {
                    string pointInfo = card.IsPointCard() ? $" [{card.GetPoints()} pts]" : "";
                    Debug.Log($"  - {card.GetUnoName()}{pointInfo}");
                }
            }

            Debug.Log("\nPress P to execute pass phase.");
        }

        private void OnCardPlayed(Player player, Card card)
        {
            string pointInfo = card.IsPointCard() ? $" [{card.GetPoints()} pts]" : "";
            Debug.Log($">>> {player.PlayerName} played {card.GetUnoName()}{pointInfo}");
        }

        private void OnTrickWon(Player winner, List<Card> cards)
        {
            int points = 0;
            foreach (var card in cards)
            {
                points += card.GetPoints();
            }

            Debug.Log($"*** {winner.PlayerName} won the trick! ({points} points) ***");
            Debug.Log("Press ENTER for next card...");
        }

        private void OnRoundEnded(Player[] players)
        {
            Debug.Log("========== ROUND ENDED ==========");

            int nsScore = GameManager.Instance.GetTeamScore(Team.NorthSouth);
            int ewScore = GameManager.Instance.GetTeamScore(Team.EastWest);

            Debug.Log($"Team North-South (You + Partner): {nsScore} points");
            Debug.Log($"Team East-West: {ewScore} points");

            // Reset for next round
            passPhaseComplete = false;
        }

        private void OnGameOver(Team winningTeam)
        {
            Debug.Log("========================================");
            Debug.Log($"GAME OVER! Team {winningTeam} WINS!");
            Debug.Log("========================================");
        }

        private void OnGUI()
        {
            // Show simple instructions on screen
            GUIStyle style = new GUIStyle();
            style.fontSize = 24;
            style.normal.textColor = Color.white;

            string instructions = "";

            if (!gameStarted)
            {
                instructions = "Press SPACE to start game";
            }
            else if (GameManager.Instance.CurrentState == GameState.PassingCards)
            {
                instructions = "Press P to pass cards";
            }
            else if (GameManager.Instance.CurrentState == GameState.PlayingTricks)
            {
                instructions = $"Press ENTER to play card\nCurrent player: {GameManager.Instance.CurrentPlayer.PlayerName}";
            }
            else if (GameManager.Instance.CurrentState == GameState.GameOver)
            {
                instructions = "GAME OVER!";
            }

            GUI.Label(new Rect(20, 20, 400, 100), instructions, style);

            // Show scores
            if (gameStarted)
            {
                int nsScore = GameManager.Instance.GetTeamScore(Team.NorthSouth);
                int ewScore = GameManager.Instance.GetTeamScore(Team.EastWest);

                string scores = $"Your Team: {nsScore} | Opponent: {ewScore}";
                GUI.Label(new Rect(20, 80, 400, 50), scores, style);
            }
        }
    }
}
