using System.Collections.Generic;
using UnityEngine;

namespace Lekha.Core
{
    /// <summary>
    /// Manages the deck of 52 cards used in Lekha
    /// </summary>
    public class Deck
    {
        private List<Card> cards;
        private System.Random random;

        public int CardsRemaining => cards.Count;

        public Deck()
        {
            random = new System.Random();
            CreateDeck();
        }

        /// <summary>
        /// Create a fresh deck with all 52 cards
        /// </summary>
        public void CreateDeck()
        {
            cards = new List<Card>(52);

            // Create all 52 cards (4 suits x 13 ranks)
            foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in System.Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(suit, rank));
                }
            }

            Debug.Log($"Deck created with {cards.Count} cards");
        }

        /// <summary>
        /// Shuffle the deck using Fisher-Yates algorithm
        /// </summary>
        public void Shuffle()
        {
            int n = cards.Count;
            for (int i = n - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                // Swap cards[i] and cards[j]
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }

            Debug.Log("Deck shuffled");
        }

        /// <summary>
        /// Draw a single card from the top of the deck
        /// </summary>
        public Card DrawCard()
        {
            if (cards.Count == 0)
            {
                Debug.LogError("Attempting to draw from empty deck!");
                return null;
            }

            Card card = cards[0];
            cards.RemoveAt(0);
            return card;
        }

        /// <summary>
        /// Draw multiple cards from the deck
        /// </summary>
        public List<Card> DrawCards(int count)
        {
            List<Card> drawnCards = new List<Card>(count);

            for (int i = 0; i < count; i++)
            {
                Card card = DrawCard();
                if (card != null)
                {
                    drawnCards.Add(card);
                }
            }

            return drawnCards;
        }

        /// <summary>
        /// Deal cards to all 4 players (13 cards each)
        /// </summary>
        public List<Card>[] DealToPlayers()
        {
            // Shuffle before dealing
            Shuffle();

            List<Card>[] hands = new List<Card>[4];

            for (int i = 0; i < 4; i++)
            {
                hands[i] = new List<Card>(13);
            }

            // Deal one card at a time to each player, rotating
            for (int cardNum = 0; cardNum < 13; cardNum++)
            {
                for (int player = 0; player < 4; player++)
                {
                    hands[player].Add(DrawCard());
                }
            }

            Debug.Log("Cards dealt to all 4 players (13 each)");
            return hands;
        }

        /// <summary>
        /// Return cards to the deck (for reshuffling between rounds)
        /// </summary>
        public void ReturnCards(List<Card> returnedCards)
        {
            cards.AddRange(returnedCards);
        }

        /// <summary>
        /// Reset the deck to a fresh 52-card state
        /// </summary>
        public void Reset()
        {
            CreateDeck();
        }
    }
}
