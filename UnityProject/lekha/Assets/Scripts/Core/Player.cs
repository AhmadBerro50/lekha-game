using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lekha.GameLogic;

namespace Lekha.Core
{
    /// <summary>
    /// Player positions at the table
    /// </summary>
    public enum PlayerPosition
    {
        South,  // Bottom - Human player
        West,   // Left
        North,  // Top - Partner of South
        East    // Right
    }

    /// <summary>
    /// Teams in the game (partners sit opposite each other)
    /// </summary>
    public enum Team
    {
        NorthSouth,  // North and South are partners
        EastWest     // East and West are partners
    }

    /// <summary>
    /// Represents a player in the Lekha game
    /// </summary>
    public class Player
    {
        public int PlayerId { get; private set; }
        public string PlayerName { get; set; }
        public PlayerPosition Position { get; private set; }
        public Team Team { get; private set; }
        public bool IsHuman { get; private set; }

        /// <summary>
        /// Set whether this player is human (used for online games)
        /// </summary>
        public void SetIsHuman(bool human)
        {
            IsHuman = human;
        }

        private List<Card> hand;
        public IReadOnlyList<Card> Hand => hand;

        // Points collected this round
        public int RoundPoints { get; private set; }

        // Total points across all rounds
        public int TotalPoints { get; private set; }

        // Cards won this round (for scoring)
        private List<Card> wonCards;
        public IReadOnlyList<Card> WonCards => wonCards;

        public Player(int id, string name, PlayerPosition position, bool isHuman)
        {
            PlayerId = id;
            PlayerName = name;
            Position = position;
            IsHuman = isHuman;

            // Assign team based on position
            Team = (position == PlayerPosition.North || position == PlayerPosition.South)
                ? Team.NorthSouth
                : Team.EastWest;

            hand = new List<Card>(13);
            wonCards = new List<Card>();
            RoundPoints = 0;
            TotalPoints = 0;
        }

        /// <summary>
        /// Give cards to this player (at start of round)
        /// </summary>
        public void ReceiveCards(List<Card> cards)
        {
            hand.AddRange(cards);
            SortHand();
            Debug.Log($"{PlayerName} received {cards.Count} cards. Hand size: {hand.Count}");
        }

        /// <summary>
        /// Add cards received from passing phase
        /// </summary>
        public void AddPassedCards(List<Card> cards)
        {
            hand.AddRange(cards);
            SortHand();
            Debug.Log($"{PlayerName} received {cards.Count} passed cards");
        }

        /// <summary>
        /// Remove cards from hand (for passing phase)
        /// </summary>
        public void RemoveCards(List<Card> cardsToRemove)
        {
            foreach (var card in cardsToRemove)
            {
                hand.Remove(card);
            }
        }

        /// <summary>
        /// Play a card from hand
        /// </summary>
        public Card PlayCard(Card card)
        {
            if (!hand.Contains(card))
            {
                Debug.LogError($"{PlayerName} tried to play {card} but doesn't have it!");
                return null;
            }

            hand.Remove(card);
            Debug.Log($"{PlayerName} played {card.GetUnoName()}");
            return card;
        }

        /// <summary>
        /// Get all cards that are legal to play given the led suit
        /// Enforces the forced point card rule when void in led suit
        /// Also forces dumping penalty cards when a higher card of the same suit was played
        /// </summary>
        public List<Card> GetPlayableCards(Suit? ledSuit)
        {
            // If no suit led yet (first card of trick), can play anything
            if (ledSuit == null)
            {
                return new List<Card>(hand);
            }

            // Must follow suit if possible
            List<Card> suitCards = hand.Where(c => c.Suit == ledSuit).ToList();

            if (suitCards.Count > 0)
            {
                // Check for forced dump of penalty cards when following suit
                // If a higher card of the same suit was played, must dump the penalty card
                var currentTrick = GameManager.Instance?.CurrentTrick;
                if (currentTrick != null && currentTrick.Count > 0)
                {
                    // Check Queen of Spades forced dump (when led suit is Spades)
                    if (ledSuit == Suit.Spades)
                    {
                        Card qos = suitCards.FirstOrDefault(c => c.IsQueenOfSpades());
                        if (qos != null)
                        {
                            // Check if King or Ace of Spades was already played
                            bool higherSpadePlayed = currentTrick.Any(c =>
                                c.Suit == Suit.Spades && c.GetRankValue() > qos.GetRankValue());
                            if (higherSpadePlayed)
                            {
                                return new List<Card> { qos }; // FORCED dump
                            }
                        }
                    }

                    // Check 10 of Diamonds forced dump (when led suit is Diamonds)
                    if (ledSuit == Suit.Diamonds)
                    {
                        Card tod = suitCards.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
                        if (tod != null)
                        {
                            // Check if J, Q, K, or A of Diamonds was already played
                            bool higherDiamondPlayed = currentTrick.Any(c =>
                                c.Suit == Suit.Diamonds && c.GetRankValue() > tod.GetRankValue());
                            if (higherDiamondPlayed)
                            {
                                return new List<Card> { tod }; // FORCED dump
                            }
                        }
                    }
                }

                return suitCards; // Must play one of these
            }

            // Can't follow suit - MUST play point cards in priority order
            // Priority 1: Blue +2 (Queen of Spades) - MUST play if you have it
            Card queenOfSpades = hand.FirstOrDefault(c => c.IsQueenOfSpades());
            if (queenOfSpades != null)
            {
                return new List<Card> { queenOfSpades }; // FORCED to play this
            }

            // Priority 2: Yellow 0 (10 of Diamonds) - MUST play if you have it
            Card tenOfDiamonds = hand.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
            if (tenOfDiamonds != null)
            {
                return new List<Card> { tenOfDiamonds }; // FORCED to play this
            }

            // No forced cards - can play anything
            return new List<Card>(hand);
        }

        /// <summary>
        /// Check if player has any cards of the specified suit
        /// </summary>
        public bool HasSuit(Suit suit)
        {
            return hand.Any(c => c.Suit == suit);
        }

        /// <summary>
        /// Win a trick and collect the cards
        /// </summary>
        public void WinTrick(List<Card> trickCards)
        {
            wonCards.AddRange(trickCards);

            int points = trickCards.Sum(c => c.GetPoints());
            RoundPoints += points;

            Debug.Log($"{PlayerName} won trick worth {points} points. Round total: {RoundPoints}");
        }

        /// <summary>
        /// End the round - add round points to total
        /// </summary>
        public void EndRound()
        {
            TotalPoints += RoundPoints;
            Debug.Log($"{PlayerName} round ended. Round: {RoundPoints}, Total: {TotalPoints}");

            // Clear for next round
            RoundPoints = 0;
            wonCards.Clear();
            hand.Clear();
        }

        /// <summary>
        /// Reset player for a brand new game
        /// </summary>
        public void ResetForNewGame()
        {
            TotalPoints = 0;
            RoundPoints = 0;
            wonCards.Clear();
            hand.Clear();
            Debug.Log($"{PlayerName} reset for new game");
        }

        /// <summary>
        /// Sort hand by suit then by rank
        /// </summary>
        public void SortHand()
        {
            hand = hand.OrderBy(c => c.GetSortValue()).ToList();
        }

        /// <summary>
        /// Get the player to the right (for passing cards)
        /// </summary>
        public static PlayerPosition GetPlayerToRight(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.South => PlayerPosition.East,
                PlayerPosition.East => PlayerPosition.North,
                PlayerPosition.North => PlayerPosition.West,
                PlayerPosition.West => PlayerPosition.South,
                _ => PlayerPosition.South
            };
        }

        /// <summary>
        /// Get the partner's position (opposite)
        /// </summary>
        public static PlayerPosition GetPartnerPosition(PlayerPosition position)
        {
            return position switch
            {
                PlayerPosition.South => PlayerPosition.North,
                PlayerPosition.North => PlayerPosition.South,
                PlayerPosition.East => PlayerPosition.West,
                PlayerPosition.West => PlayerPosition.East,
                _ => PlayerPosition.South
            };
        }

        public override string ToString()
        {
            return $"{PlayerName} ({Position}, Team {Team})";
        }
    }
}
