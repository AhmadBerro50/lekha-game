using UnityEngine;

namespace Lekha.Core
{
    /// <summary>
    /// Represents the four suits in the game, mapped to Uno colors
    /// </summary>
    public enum Suit
    {
        Hearts,    // Red - 1 point each
        Diamonds,  // Yellow - contains the 10 (Yellow 0) worth 10 points
        Spades,    // Blue - contains the Queen (Blue +2) worth 13 points
        Clubs      // Green - no point cards
    }

    /// <summary>
    /// Card ranks from Ace (1) to King (13)
    /// </summary>
    public enum Rank
    {
        Ace = 1,
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13
    }

    /// <summary>
    /// Represents a single card in the Lekha game.
    /// Cards are displayed as Uno cards but follow traditional card game rules.
    /// </summary>
    [System.Serializable]
    public class Card
    {
        public Suit Suit { get; private set; }
        public Rank Rank { get; private set; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// Get the point value of this card
        /// </summary>
        public int GetPoints()
        {
            // Blue +2 (Queen of Spades) = 13 points
            if (Suit == Suit.Spades && Rank == Rank.Queen)
                return 13;

            // Yellow 0 (10 of Diamonds) = 10 points
            if (Suit == Suit.Diamonds && Rank == Rank.Ten)
                return 10;

            // All Hearts = 1 point each
            if (Suit == Suit.Hearts)
                return 1;

            // All other cards = 0 points
            return 0;
        }

        /// <summary>
        /// Get the Uno color name for this card's suit
        /// </summary>
        public string GetUnoColor()
        {
            return Suit switch
            {
                Suit.Hearts => "Red",
                Suit.Diamonds => "Yellow",
                Suit.Spades => "Blue",
                Suit.Clubs => "Green",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// Get the Uno display symbol for this card's rank
        /// </summary>
        public string GetUnoSymbol()
        {
            return Rank switch
            {
                Rank.Ace => "1",
                Rank.Two => "2",
                Rank.Three => "3",
                Rank.Four => "4",
                Rank.Five => "5",
                Rank.Six => "6",
                Rank.Seven => "7",
                Rank.Eight => "8",
                Rank.Nine => "9",
                Rank.Ten => "0",        // 10 displays as 0 in Uno
                Rank.Jack => "⟲",       // Reverse
                Rank.Queen => "+2",     // Draw Two
                Rank.King => "⊘",       // Skip/Block
                _ => "?"
            };
        }

        /// <summary>
        /// Get the full Uno-style name (e.g., "Blue +2", "Yellow 0")
        /// </summary>
        public string GetUnoName()
        {
            return $"{GetUnoColor()} {GetUnoSymbol()}";
        }

        /// <summary>
        /// Get the rank value for comparison purposes.
        /// In Lekha, Ace is HIGHEST, order from low to high:
        /// 2,3,4,5,6,7,8,9,10(0),Jack(Reverse),Queen(+2),King(Skip),Ace(1)
        /// </summary>
        public int GetRankValue()
        {
            return Rank switch
            {
                Rank.Two => 1,
                Rank.Three => 2,
                Rank.Four => 3,
                Rank.Five => 4,
                Rank.Six => 5,
                Rank.Seven => 6,
                Rank.Eight => 7,
                Rank.Nine => 8,
                Rank.Ten => 9,      // 0 in Uno
                Rank.Jack => 10,    // Reverse
                Rank.Queen => 11,   // +2
                Rank.King => 12,    // Skip/Block
                Rank.Ace => 13,     // 1 in Uno - HIGHEST
                _ => 0
            };
        }

        /// <summary>
        /// Get a value for sorting cards in hand (by suit, then by rank)
        /// </summary>
        public int GetSortValue()
        {
            return (int)Suit * 100 + GetRankValue();
        }

        /// <summary>
        /// Check if this card is a point card
        /// </summary>
        public bool IsPointCard()
        {
            return GetPoints() > 0;
        }

        /// <summary>
        /// Check if this card is the dangerous Queen of Spades (Blue +2)
        /// </summary>
        public bool IsQueenOfSpades()
        {
            return Suit == Suit.Spades && Rank == Rank.Queen;
        }

        /// <summary>
        /// Compare two cards of the same suit to determine which is higher
        /// Returns positive if this card is higher, negative if other is higher
        /// In Lekha: Ace is HIGHEST, 2 is lowest
        /// </summary>
        public int CompareRank(Card other)
        {
            return GetRankValue() - other.GetRankValue();
        }

        public override string ToString()
        {
            return $"{Rank} of {Suit} ({GetUnoName()})";
        }

        public override bool Equals(object obj)
        {
            if (obj is Card other)
            {
                return Suit == other.Suit && Rank == other.Rank;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return (int)Suit * 13 + (int)Rank;
        }
    }
}
