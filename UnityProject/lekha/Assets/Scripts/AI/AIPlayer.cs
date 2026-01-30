using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Lekha.Core;
using Lekha.GameLogic;

namespace Lekha.AI
{
    /// <summary>
    /// Intelligent AI for computer-controlled players in Lekha
    /// Strategy: Avoid taking points, force opponents to take points
    /// </summary>
    public static class AIPlayer
    {
        // Track cards that have been played (for smarter decisions)
        private static HashSet<Card> playedCards = new HashSet<Card>();
        private static Dictionary<PlayerPosition, List<Card>> passedCardsTo = new Dictionary<PlayerPosition, List<Card>>();
        private static Dictionary<PlayerPosition, List<Card>> receivedCardsFrom = new Dictionary<PlayerPosition, List<Card>>();

        // Track which suits each player is void in
        private static Dictionary<PlayerPosition, HashSet<Suit>> knownVoids = new Dictionary<PlayerPosition, HashSet<Suit>>();

        // Track who led the current trick (set externally before AI plays)
        private static bool queenOfSpadesPlayed = false;
        private static bool tenOfDiamondsPlayed = false;

        // Track suits that caused us to receive points (avoid re-leading)
        private static HashSet<Suit> dangerousSuitsToLead = new HashSet<Suit>();

        // Track the last suit led by each player
        private static Dictionary<PlayerPosition, Suit?> lastSuitLedBy = new Dictionary<PlayerPosition, Suit?>();

        // Track estimated remaining cards per player per suit (starts at ~3-4 each)
        private static Dictionary<PlayerPosition, Dictionary<Suit, int>> estimatedSuitCounts = new Dictionary<PlayerPosition, Dictionary<Suit, int>>();

        // Track if each AI player passed the Queen of Spades or 10 of Diamonds (for flushing strategy)
        private static Dictionary<PlayerPosition, bool> passedQueenOfSpades = new Dictionary<PlayerPosition, bool>();
        private static Dictionary<PlayerPosition, bool> passedTenOfDiamonds = new Dictionary<PlayerPosition, bool>();

        /// <summary>
        /// Reset tracking at start of new round
        /// </summary>
        public static void ResetRoundTracking()
        {
            playedCards.Clear();
            passedCardsTo.Clear();
            receivedCardsFrom.Clear();
            knownVoids.Clear();
            queenOfSpadesPlayed = false;
            tenOfDiamondsPlayed = false;
            dangerousSuitsToLead.Clear();
            lastSuitLedBy.Clear();
            passedQueenOfSpades.Clear();
            passedTenOfDiamonds.Clear();

            // Initialize estimated suit counts (each player starts with ~3 cards per suit on average)
            estimatedSuitCounts.Clear();
            foreach (PlayerPosition pos in System.Enum.GetValues(typeof(PlayerPosition)))
            {
                estimatedSuitCounts[pos] = new Dictionary<Suit, int>();
                foreach (Suit suit in System.Enum.GetValues(typeof(Suit)))
                {
                    estimatedSuitCounts[pos][suit] = 3; // Average starting count
                }
            }
        }

        /// <summary>
        /// Track a played card and detect voids
        /// </summary>
        public static void TrackPlayedCard(Card card)
        {
            playedCards.Add(card);

            if (card.IsQueenOfSpades()) queenOfSpadesPlayed = true;
            if (card.Suit == Suit.Diamonds && card.Rank == Rank.Ten) tenOfDiamondsPlayed = true;
        }

        /// <summary>
        /// Track when a player doesn't follow suit (they're void)
        /// </summary>
        public static void TrackVoid(PlayerPosition position, Suit suit)
        {
            if (!knownVoids.ContainsKey(position))
                knownVoids[position] = new HashSet<Suit>();
            knownVoids[position].Add(suit);

            // Set their estimated count for this suit to 0
            if (estimatedSuitCounts.ContainsKey(position) && estimatedSuitCounts[position].ContainsKey(suit))
                estimatedSuitCounts[position][suit] = 0;
        }

        /// <summary>
        /// Track when a suit causes us to receive points (opponent dumped on us)
        /// </summary>
        public static void TrackDangerousSuit(Suit ledSuit, int pointsReceived)
        {
            if (pointsReceived > 0)
            {
                dangerousSuitsToLead.Add(ledSuit);
            }
        }

        /// <summary>
        /// Track who led which suit (for partner coordination)
        /// </summary>
        public static void TrackSuitLed(PlayerPosition leader, Suit suit)
        {
            lastSuitLedBy[leader] = suit;
            // Note: suit count is already decremented by UpdateSuitCount called for every card
        }

        /// <summary>
        /// Update estimated suit count when a card is played
        /// </summary>
        public static void UpdateSuitCount(PlayerPosition position, Suit suit)
        {
            if (estimatedSuitCounts.ContainsKey(position) &&
                estimatedSuitCounts[position].ContainsKey(suit) &&
                estimatedSuitCounts[position][suit] > 0)
            {
                estimatedSuitCounts[position][suit]--;
            }
        }

        /// <summary>
        /// Check if opponent likely has only 1 card in a suit
        /// </summary>
        private static bool OpponentLikelyShortInSuit(Suit suit, PlayerPosition myPosition)
        {
            PlayerPosition leftOpp = GetNextPosition(myPosition); // Opponent to left
            PlayerPosition rightOpp = GetNextPosition(GetPartner(myPosition)); // Opponent to right

            // Check if either opponent is known void or has low count
            if (IsKnownVoid(leftOpp, suit) || IsKnownVoid(rightOpp, suit))
                return true;

            // Check estimated counts (default to 3 if not tracked yet)
            int leftCount = 3;
            int rightCount = 3;
            if (estimatedSuitCounts.ContainsKey(leftOpp) && estimatedSuitCounts[leftOpp].ContainsKey(suit))
                leftCount = estimatedSuitCounts[leftOpp][suit];
            if (estimatedSuitCounts.ContainsKey(rightOpp) && estimatedSuitCounts[rightOpp].ContainsKey(suit))
                rightCount = estimatedSuitCounts[rightOpp][suit];

            return leftCount <= 1 || rightCount <= 1;
        }

        /// <summary>
        /// Track passed cards for strategic play
        /// </summary>
        public static void TrackPassedCards(PlayerPosition from, PlayerPosition to, List<Card> cards)
        {
            passedCardsTo[to] = new List<Card>(cards);
            receivedCardsFrom[from] = new List<Card>(cards);
        }

        /// <summary>
        /// Check if a player is known to be void in a suit
        /// </summary>
        private static bool IsKnownVoid(PlayerPosition position, Suit suit)
        {
            return knownVoids.ContainsKey(position) && knownVoids[position].Contains(suit);
        }

        /// <summary>
        /// Check if a Spades card is above the Queen (King or Ace)
        /// Rule 11: Never play these if Queen hasn't been played
        /// </summary>
        private static bool IsAboveQueenOfSpades(Card card)
        {
            return card.Suit == Suit.Spades && card.GetRankValue() > 11; // Queen is 11, so K=12, A=13
        }

        /// <summary>
        /// Check if a Diamonds card is above the 10 (Jack, Queen, King, Ace)
        /// Rule 11: Never play these if 10D hasn't been played
        /// </summary>
        private static bool IsAboveTenOfDiamonds(Card card)
        {
            return card.Suit == Suit.Diamonds && card.GetRankValue() > 9; // 10 is value 9, so J=10, Q=11, K=12, A=13
        }

        /// <summary>
        /// Check if a card is safe to lead (not above Queen/10D if they haven't been played)
        /// Rule 11 implementation
        /// </summary>
        private static bool IsSafeToLead(Card card)
        {
            // Rule 11: Never play K/A of Spades if Queen not played
            if (!queenOfSpadesPlayed && IsAboveQueenOfSpades(card))
                return false;

            // Rule 11: Never play J/Q/K/A of Diamonds if 10D not played
            if (!tenOfDiamondsPlayed && IsAboveTenOfDiamonds(card))
                return false;

            return true;
        }

        /// <summary>
        /// Count how many cards of a suit remain unplayed (not in our hand, not played)
        /// </summary>
        private static int CountRemainingInSuit(Suit suit, List<Card> myHand)
        {
            int total = 13; // 13 cards per suit
            int played = playedCards.Count(c => c.Suit == suit);
            int inHand = myHand.Count(c => c.Suit == suit);
            return total - played - inHand;
        }

        /// <summary>
        /// Check if a card is the highest remaining in its suit
        /// </summary>
        private static bool IsHighestRemaining(Card card, IReadOnlyList<Card> myHand)
        {
            int myRank = card.GetRankValue();
            // Check if any higher card exists that hasn't been played and isn't in my hand
            for (int r = myRank + 1; r <= 13; r++)
            {
                Rank rank = (Rank)GetRankFromValue(r);
                Card higher = new Card(card.Suit, rank);
                if (!playedCards.Contains(higher) && !myHand.Any(c => c.Suit == card.Suit && c.GetRankValue() == r))
                {
                    return false; // A higher card is still out there
                }
            }
            return true;
        }

        private static int GetRankFromValue(int value)
        {
            // Reverse of GetRankValue: value 1=Two, 2=Three, ..., 9=Ten, 10=Jack, 11=Queen, 12=King, 13=Ace
            return value switch
            {
                1 => (int)Rank.Two,
                2 => (int)Rank.Three,
                3 => (int)Rank.Four,
                4 => (int)Rank.Five,
                5 => (int)Rank.Six,
                6 => (int)Rank.Seven,
                7 => (int)Rank.Eight,
                8 => (int)Rank.Nine,
                9 => (int)Rank.Ten,
                10 => (int)Rank.Jack,
                11 => (int)Rank.Queen,
                12 => (int)Rank.King,
                13 => (int)Rank.Ace,
                _ => (int)Rank.Two
            };
        }

        /// <summary>
        /// Choose 3 cards to pass to the right
        /// Enhanced strategies:
        /// - Pass high hearts (dangerous points)
        /// - Pass Queen only if exposed (few low cards to protect)
        /// - Keep cards below Queen in Spades/Diamonds if have 3+ protection
        /// - If Queen has only high cards (K,A), keep Queen and void another suit
        /// </summary>
        public static List<Card> ChooseCardsToPass(Player player)
        {
            List<Card> hand = new List<Card>(player.Hand);
            List<Card> toPass = new List<Card>();

            // Group cards by suit and count
            var suitGroups = hand.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.ToList());

            // Analyze Queen of Spades situation
            Card queenOfSpades = hand.FirstOrDefault(c => c.IsQueenOfSpades());
            bool shouldPassQueen = false;
            if (queenOfSpades != null)
            {
                var otherSpades = hand.Where(c => c.Suit == Suit.Spades && !c.IsQueenOfSpades()).ToList();
                int cardsBelow = otherSpades.Count(c => c.GetRankValue() < 11); // Below Queen
                int cardsAbove = otherSpades.Count(c => c.GetRankValue() > 11); // King, Ace

                // Rule 7: If we have 3+ cards below Queen, we can duck - keep Queen and flush
                if (cardsBelow >= 3)
                {
                    shouldPassQueen = true; // Pass Queen, then lead high to flush
                }
                // Rule 7b: If we only have cards ABOVE Queen (K, A), keep Queen and void other suit
                else if (cardsAbove > 0 && cardsBelow == 0)
                {
                    shouldPassQueen = false; // Keep Queen, she's protected by higher cards
                }
                // If exposed (few cards to protect), pass it
                else if (cardsBelow < 2)
                {
                    shouldPassQueen = true;
                }
            }

            // Priority 1: Pass Queen of Spades if exposed
            if (queenOfSpades != null && shouldPassQueen && CanPassCard(queenOfSpades, hand, suitGroups))
            {
                toPass.Add(queenOfSpades);
                hand.Remove(queenOfSpades);
                UpdateSuitGroups(suitGroups, queenOfSpades);
                passedQueenOfSpades[player.Position] = true; // Track for Rule 12: only lead low Spades later
            }

            // Analyze 10 of Diamonds situation (similar logic)
            Card tenOfDiamonds = hand.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
            bool shouldPassTen = false;
            if (tenOfDiamonds != null)
            {
                var otherDiamonds = hand.Where(c => c.Suit == Suit.Diamonds && c.Rank != Rank.Ten).ToList();
                int cardsBelow = otherDiamonds.Count(c => c.GetRankValue() < 9); // Below 10
                int cardsAbove = otherDiamonds.Count(c => c.GetRankValue() > 9); // J,Q,K,A

                if (cardsBelow >= 3)
                {
                    shouldPassTen = true;
                }
                else if (cardsAbove > 0 && cardsBelow == 0)
                {
                    shouldPassTen = false;
                }
                else if (cardsBelow < 2)
                {
                    shouldPassTen = true;
                }
            }

            // Priority 2: Pass 10 of Diamonds if exposed
            if (tenOfDiamonds != null && shouldPassTen && toPass.Count < 3 && CanPassCard(tenOfDiamonds, hand, suitGroups))
            {
                toPass.Add(tenOfDiamonds);
                hand.Remove(tenOfDiamonds);
                UpdateSuitGroups(suitGroups, tenOfDiamonds);
                passedTenOfDiamonds[player.Position] = true; // Track for Rule 12: only lead low Diamonds later
            }

            // Priority 3: Pass HIGH HEARTS (Rule 5 - always dangerous)
            var highHearts = hand.Where(c => c.Suit == Suit.Hearts)
                                 .OrderByDescending(c => c.GetRankValue())
                                 .ToList();
            foreach (var card in highHearts)
            {
                if (toPass.Count >= 3) break;
                if (CanPassCard(card, hand, suitGroups))
                {
                    toPass.Add(card);
                    hand.Remove(card);
                    UpdateSuitGroups(suitGroups, card);
                }
            }

            // Rule 6: Don't pass cards below Queen in Spades/Diamonds if we have protection
            // Instead, pass from Hearts or Clubs to try voiding those suits

            // Priority 4: Try to void Hearts or Clubs (pass entire short suit)
            var voidableSuits = suitGroups
                .Where(g => (g.Key == Suit.Hearts || g.Key == Suit.Clubs) && g.Value.Count <= 3 && g.Value.Count > 0)
                .OrderBy(g => g.Value.Count)
                .ToList();

            foreach (var suitGroup in voidableSuits)
            {
                if (toPass.Count >= 3) break;
                foreach (var card in suitGroup.Value.OrderByDescending(c => c.GetRankValue()).ToList())
                {
                    if (toPass.Count >= 3) break;
                    if (CanPassCard(card, hand, suitGroups))
                    {
                        toPass.Add(card);
                        hand.Remove(card);
                        UpdateSuitGroups(suitGroups, card);
                    }
                }
            }

            // Priority 5: Pass high Clubs (safe suit, no points but can void to dump later)
            var highClubs = hand.Where(c => c.Suit == Suit.Clubs)
                                .OrderByDescending(c => c.GetRankValue())
                                .ToList();
            foreach (var card in highClubs)
            {
                if (toPass.Count >= 3) break;
                if (CanPassCard(card, hand, suitGroups))
                {
                    toPass.Add(card);
                    hand.Remove(card);
                    UpdateSuitGroups(suitGroups, card);
                }
            }

            // Priority 6: Only now consider passing high Spades (Ace, King) if we didn't keep Queen
            if (!hand.Any(c => c.IsQueenOfSpades()))
            {
                var highSpades = hand.Where(c => c.Suit == Suit.Spades && c.GetRankValue() >= 12)
                                     .OrderByDescending(c => c.GetRankValue())
                                     .ToList();
                foreach (var card in highSpades)
                {
                    if (toPass.Count >= 3) break;
                    if (CanPassCard(card, hand, suitGroups))
                    {
                        toPass.Add(card);
                        hand.Remove(card);
                        UpdateSuitGroups(suitGroups, card);
                    }
                }
            }

            // Priority 7: Pass any remaining high non-protected cards
            var remaining = hand
                .Where(c => c.GetPoints() == 0) // Non-point cards first
                .OrderByDescending(c => c.GetRankValue())
                .ToList();
            foreach (var card in remaining)
            {
                if (toPass.Count >= 3) break;
                if (CanPassCard(card, hand, suitGroups))
                {
                    toPass.Add(card);
                    hand.Remove(card);
                    UpdateSuitGroups(suitGroups, card);
                }
            }

            // Fallback
            if (toPass.Count < 3)
            {
                var anyCards = hand.OrderByDescending(c => c.GetRankValue()).ToList();
                foreach (var card in anyCards)
                {
                    if (toPass.Count >= 3) break;
                    toPass.Add(card);
                }
            }

            Debug.Log($"AI {player.PlayerName} passing: {string.Join(", ", toPass.Select(c => c.GetUnoName()))}");
            return toPass;
        }

        /// <summary>
        /// Check if a card can be passed without violating the "cannot empty color" rule
        /// </summary>
        private static bool CanPassCard(Card card, List<Card> currentHand, Dictionary<Suit, List<Card>> suitGroups)
        {
            if (!suitGroups.ContainsKey(card.Suit)) return true;

            List<Card> suitCards = suitGroups[card.Suit];
            int countInSuit = suitCards.Count;

            if (countInSuit > 3) return true;

            if (countInSuit == 1)
            {
                return CanEmptySuit(card.Suit, suitCards);
            }

            return true;
        }

        /// <summary>
        /// Check if it's legal to empty a suit by passing
        /// </summary>
        private static bool CanEmptySuit(Suit suit, List<Card> suitCards)
        {
            if (suit == Suit.Hearts || suit == Suit.Clubs)
                return false;

            if (suit == Suit.Diamonds)
            {
                return suitCards.All(c =>
                    c.Rank == Rank.Ten || c.Rank == Rank.Jack ||
                    c.Rank == Rank.Queen || c.Rank == Rank.King || c.Rank == Rank.Ace);
            }

            if (suit == Suit.Spades)
            {
                return suitCards.All(c =>
                    c.Rank == Rank.Queen || c.Rank == Rank.King || c.Rank == Rank.Ace);
            }

            return false;
        }

        private static void UpdateSuitGroups(Dictionary<Suit, List<Card>> suitGroups, Card removedCard)
        {
            if (suitGroups.ContainsKey(removedCard.Suit))
            {
                suitGroups[removedCard.Suit].Remove(removedCard);
                if (suitGroups[removedCard.Suit].Count == 0)
                    suitGroups.Remove(removedCard.Suit);
            }
        }

        /// <summary>
        /// Choose a card to play in the current trick
        /// </summary>
        public static Card ChooseCardToPlay(Player player, Suit? ledSuit, List<Card> currentTrick)
        {
            List<Card> playableCards = player.GetPlayableCards(ledSuit);

            if (playableCards.Count == 0)
            {
                Debug.LogError($"AI {player.PlayerName} has no playable cards!");
                return null;
            }

            if (playableCards.Count == 1)
            {
                return playableCards[0];
            }

            Card chosen;

            if (ledSuit == null)
            {
                chosen = ChooseLeadCard(player, playableCards);
            }
            else if (playableCards.Any(c => c.Suit == ledSuit))
            {
                chosen = ChooseFollowCard(player, playableCards, currentTrick, ledSuit.Value);
            }
            else
            {
                chosen = ChooseDumpCard(player, playableCards, currentTrick);
            }

            Debug.Log($"AI {player.PlayerName} plays: {chosen.GetUnoName()}");
            return chosen;
        }

        /// <summary>
        /// Get the partner position for a given player
        /// </summary>
        private static PlayerPosition GetPartner(PlayerPosition pos)
        {
            return Player.GetPartnerPosition(pos);
        }

        /// <summary>
        /// Determine which player position is currently winning the trick
        /// </summary>
        private static PlayerPosition GetCurrentTrickWinner(List<Card> currentTrick, Suit ledSuit)
        {
            if (currentTrick.Count == 0) return PlayerPosition.South; // shouldn't happen

            // Get the trick leader position from GameManager
            PlayerPosition leaderPos = PlayerPosition.South;
            if (GameManager.Instance != null)
            {
                leaderPos = GameManager.Instance.TrickLeaderPosition;
            }

            // Find highest card of led suit
            int bestIndex = 0;
            int bestRank = 0;
            for (int i = 0; i < currentTrick.Count; i++)
            {
                if (currentTrick[i].Suit == ledSuit && currentTrick[i].GetRankValue() > bestRank)
                {
                    bestRank = currentTrick[i].GetRankValue();
                    bestIndex = i;
                }
            }

            // Map index to position (leader is index 0, then clockwise)
            PlayerPosition pos = leaderPos;
            for (int i = 0; i < bestIndex; i++)
            {
                pos = GetNextPosition(pos);
            }
            return pos;
        }

        private static PlayerPosition GetNextPosition(PlayerPosition pos)
        {
            return pos switch
            {
                PlayerPosition.South => PlayerPosition.East,
                PlayerPosition.East => PlayerPosition.North,
                PlayerPosition.North => PlayerPosition.West,
                PlayerPosition.West => PlayerPosition.South,
                _ => PlayerPosition.South
            };
        }

        /// <summary>
        /// Check if partner is currently winning the trick
        /// </summary>
        private static bool IsPartnerWinning(Player player, List<Card> currentTrick, Suit ledSuit)
        {
            if (currentTrick.Count == 0) return false;

            PlayerPosition winnerPos = GetCurrentTrickWinner(currentTrick, ledSuit);
            PlayerPosition partnerPos = GetPartner(player.Position);
            return winnerPos == partnerPos;
        }

        /// <summary>
        /// Check if an opponent is currently winning the trick
        /// </summary>
        private static bool IsOpponentWinning(Player player, List<Card> currentTrick, Suit ledSuit)
        {
            if (currentTrick.Count == 0) return false;

            PlayerPosition winnerPos = GetCurrentTrickWinner(currentTrick, ledSuit);
            PlayerPosition partnerPos = GetPartner(player.Position);
            return winnerPos != player.Position && winnerPos != partnerPos;
        }

        /// <summary>
        /// Choose a card to lead with
        /// Enhanced with strategic rules:
        /// - Don't lead suits where opponents are short (they'll dump)
        /// - Don't re-lead suits that caused point dumps
        /// - Follow partner's lead when safe
        /// - Don't lead opponent's favorite suit toward partner
        /// - Rule 10: Never lead with high Spades/Diamonds if Queen/10D not played
        /// - Rule 12: If we passed Queen, only lead Spades with cards below Queen
        /// </summary>
        private static Card ChooseLeadCard(Player player, List<Card> playableCards)
        {
            var hand = player.Hand;
            PlayerPosition partnerPos = GetPartner(player.Position);
            PlayerPosition rightOpponent = GetNextPosition(partnerPos);

            // Rule 10 & 11: Filter out unsafe cards (high Spades/Diamonds when Queen/10D not played)
            var safePlayableCards = playableCards.Where(c => IsSafeToLead(c)).ToList();

            // Rule 12: If we passed Queen of Spades, only lead Spades with cards below Queen (2-Jack)
            bool iPassedQueen = passedQueenOfSpades.ContainsKey(player.Position) && passedQueenOfSpades[player.Position];
            if (iPassedQueen && !queenOfSpadesPlayed)
            {
                safePlayableCards = safePlayableCards.Where(c =>
                    c.Suit != Suit.Spades || c.GetRankValue() < 11 // Only spades below Queen (Queen=11)
                ).ToList();
            }

            // Rule 12: If we passed 10 of Diamonds, only lead Diamonds with cards below 10 (2-9)
            bool iPassedTen = passedTenOfDiamonds.ContainsKey(player.Position) && passedTenOfDiamonds[player.Position];
            if (iPassedTen && !tenOfDiamondsPlayed)
            {
                safePlayableCards = safePlayableCards.Where(c =>
                    c.Suit != Suit.Diamonds || c.GetRankValue() < 9 // Only diamonds below 10 (10=9 in value)
                ).ToList();
            }

            // If all cards are filtered out, fall back to original list (must play something)
            if (safePlayableCards.Count == 0)
            {
                safePlayableCards = playableCards;
            }

            // Get suits that are safe to lead (not dangerous, opponents not short)
            var safeSuits = new List<Suit> { Suit.Clubs, Suit.Hearts, Suit.Diamonds, Suit.Spades }
                .Where(s => !dangerousSuitsToLead.Contains(s))
                .Where(s => !OpponentLikelyShortInSuit(s, player.Position))
                .ToList();

            // Strategy 0: Avoid leading the same suit opponent on right just led (rule 4)
            if (lastSuitLedBy.ContainsKey(rightOpponent) && lastSuitLedBy[rightOpponent].HasValue)
            {
                safeSuits.Remove(lastSuitLedBy[rightOpponent].Value);
            }

            // Strategy 1: Follow partner's lead if we have cards in that suit (rule 3)
            if (lastSuitLedBy.ContainsKey(partnerPos) && lastSuitLedBy[partnerPos].HasValue)
            {
                Suit partnerSuit = lastSuitLedBy[partnerPos].Value;
                var partnerSuitCards = safePlayableCards.Where(c => c.Suit == partnerSuit && c.GetPoints() == 0).ToList();
                if (partnerSuitCards.Count > 0 && safeSuits.Contains(partnerSuit))
                {
                    // Lead low in partner's suit
                    return partnerSuitCards.OrderBy(c => c.GetRankValue()).First();
                }
            }

            // Strategy 2: If Queen of Spades hasn't been played, try to flush it out
            // (Only if we passed the Queen - we want to flush the person we passed it to)
            if (iPassedQueen && !queenOfSpadesPlayed && safeSuits.Contains(Suit.Spades))
            {
                // Rule 12: Lead LOW spades only (cards below Queen) to flush out the Queen
                var lowSpades = safePlayableCards.Where(c => c.Suit == Suit.Spades && c.GetRankValue() < 11)
                                                  .OrderBy(c => c.GetRankValue()).ToList();
                if (lowSpades.Count > 0)
                {
                    return lowSpades[0]; // Lead lowest spade to flush Queen
                }
            }

            // Strategy 3: Lead from safe suits where we have the highest remaining card
            foreach (var suit in safeSuits.Where(s => s != Suit.Hearts))
            {
                if (suit == Suit.Diamonds && !tenOfDiamondsPlayed) continue;
                if (suit == Suit.Spades && !queenOfSpadesPlayed) continue;

                var suitCards = safePlayableCards.Where(c => c.Suit == suit).OrderByDescending(c => c.GetRankValue()).ToList();
                if (suitCards.Count > 0 && IsHighestRemaining(suitCards[0], hand))
                {
                    if (suitCards[0].GetPoints() == 0)
                    {
                        return suitCards[0];
                    }
                }
            }

            // Strategy 4: Lead from longest safe suit (low card)
            var suitCounts = safePlayableCards
                .Where(c => safeSuits.Contains(c.Suit))
                .GroupBy(c => c.Suit)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (suitCounts.Count > 0)
            {
                var bestSuit = suitCounts[0];
                var lowestInLongest = bestSuit.OrderBy(c => c.GetRankValue()).ToList();
                var safe = lowestInLongest.Where(c => c.GetPoints() == 0).ToList();
                if (safe.Count > 0) return safe[0];
                if (lowestInLongest.Count > 0) return lowestInLongest[0];
            }

            // Strategy 5: Lead low clubs (safest)
            var clubs = safePlayableCards.Where(c => c.Suit == Suit.Clubs).OrderBy(c => c.GetRankValue()).ToList();
            if (clubs.Count > 0) return clubs[0];

            // Strategy 6: Lead low from any non-point suit
            var safeLead = safePlayableCards.Where(c => c.GetPoints() == 0).OrderBy(c => c.GetRankValue()).ToList();
            if (safeLead.Count > 0) return safeLead[0];

            // Fallback: lowest card from safe list
            return safePlayableCards.OrderBy(c => c.GetRankValue()).First();
        }

        /// <summary>
        /// Choose a card when following suit
        /// Rule 11: Never play K/A of Spades if Queen not played
        /// Rule 11: Never play J/Q/K/A of Diamonds if 10D not played
        /// </summary>
        private static Card ChooseFollowCard(Player player, List<Card> playableCards, List<Card> currentTrick, Suit ledSuit)
        {
            var suitCards = playableCards.Where(c => c.Suit == ledSuit).ToList();
            if (suitCards.Count == 0) return playableCards[0];

            // Rule 11: Filter out dangerous cards when following suit
            var safeSuitCards = suitCards.ToList();

            // Rule 11a: Don't play K/A of Spades if Queen hasn't been played
            if (ledSuit == Suit.Spades && !queenOfSpadesPlayed)
            {
                var filtered = safeSuitCards.Where(c => !IsAboveQueenOfSpades(c)).ToList();
                if (filtered.Count > 0) safeSuitCards = filtered;
                // If all cards are K/A, we must play one (fallback to original)
            }

            // Rule 11b: Don't play J/Q/K/A of Diamonds if 10D hasn't been played
            if (ledSuit == Suit.Diamonds && !tenOfDiamondsPlayed)
            {
                var filtered = safeSuitCards.Where(c => !IsAboveTenOfDiamonds(c)).ToList();
                if (filtered.Count > 0) safeSuitCards = filtered;
                // If all cards are above 10, we must play one (fallback to original)
            }

            var trickSuitCards = currentTrick.Where(c => c.Suit == ledSuit).ToList();
            int highestRankInTrick = trickSuitCards.Count > 0 ? trickSuitCards.Max(c => c.GetRankValue()) : 0;

            int trickPoints = currentTrick.Sum(c => c.GetPoints());
            bool partnerWinning = IsPartnerWinning(player, currentTrick, ledSuit);
            bool opponentWinning = IsOpponentWinning(player, currentTrick, ledSuit);
            bool isLastToPlay = currentTrick.Count == 3;

            // Use safeSuitCards for winning/losing calculations
            var winningCards = safeSuitCards.Where(c => c.GetRankValue() > highestRankInTrick).OrderBy(c => c.GetRankValue()).ToList();
            var losingCards = safeSuitCards.Where(c => c.GetRankValue() <= highestRankInTrick).OrderByDescending(c => c.GetRankValue()).ToList();

            // Special: Spades led and Queen of Spades not yet played
            if (ledSuit == Suit.Spades && !queenOfSpadesPlayed)
            {
                bool hasQueen = suitCards.Any(c => c.IsQueenOfSpades());
                if (hasQueen)
                {
                    // We have the Queen - try to play under if possible
                    var underCards = suitCards.Where(c => !c.IsQueenOfSpades() && c.GetRankValue() < highestRankInTrick).ToList();
                    if (underCards.Count > 0)
                    {
                        return underCards.OrderByDescending(c => c.GetRankValue()).First();
                    }
                    // If we're last and no one played higher than Queen, we're stuck
                    if (isLastToPlay && highestRankInTrick < 11) // Queen is rank value 11
                    {
                        // Must play Queen, but try lowest non-Queen first
                        var nonQueen = suitCards.Where(c => !c.IsQueenOfSpades()).OrderBy(c => c.GetRankValue()).ToList();
                        if (nonQueen.Count > 0) return nonQueen[0];
                    }
                }
            }

            // Special: Diamonds led and 10 of Diamonds not yet played
            if (ledSuit == Suit.Diamonds && !tenOfDiamondsPlayed)
            {
                bool hasTen = suitCards.Any(c => c.Rank == Rank.Ten);
                if (hasTen && losingCards.Any(c => c.Rank != Rank.Ten))
                {
                    // Play under without using the 10
                    return losingCards.Where(c => c.Rank != Rank.Ten).OrderByDescending(c => c.GetRankValue()).First();
                }
            }

            if (isLastToPlay)
            {
                // Last to play - we know exactly what we're getting
                int totalPoints = trickPoints + safeSuitCards.Min(c => c.GetPoints()); // minimum points we add

                if (trickPoints == 0)
                {
                    // No points in trick - safe to win, dump highest card
                    if (winningCards.Count > 0)
                    {
                        // Win with highest to clear high cards from hand
                        return winningCards.Last();
                    }
                    return safeSuitCards.OrderByDescending(c => c.GetRankValue()).First();
                }
                else
                {
                    // Trick has points - avoid winning!
                    if (losingCards.Count > 0)
                    {
                        return losingCards[0]; // Highest card that still loses
                    }
                    // Forced to win - play lowest winner
                    return winningCards[0];
                }
            }

            // Not last to play
            if (partnerWinning)
            {
                if (trickPoints > 0)
                {
                    // Partner winning a trick with points - play under partner
                    if (losingCards.Count > 0) return losingCards[0]; // Highest under
                    // Have to overtake partner - play lowest
                    return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
                }
                else
                {
                    // Partner winning clean trick - dump high card safely
                    // But don't dump point cards on partner!
                    var safeHigh = safeSuitCards.Where(c => c.GetPoints() == 0).OrderByDescending(c => c.GetRankValue()).ToList();
                    if (safeHigh.Count > 0) return safeHigh[0];
                    return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
                }
            }

            if (trickPoints > 0)
            {
                // Trick has points and opponent winning - try to lose
                if (losingCards.Count > 0)
                {
                    return losingCards[0]; // Highest losing card
                }
                // Forced to potentially win - play lowest
                return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
            }

            // No points yet, opponent or no one winning
            // Play conservatively - low card to stay under
            return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
        }

        /// <summary>
        /// Choose a card to dump when can't follow suit
        /// </summary>
        private static Card ChooseDumpCard(Player player, List<Card> playableCards, List<Card> currentTrick)
        {
            bool partnerWinning = false;
            if (currentTrick.Count > 0 && GameManager.Instance != null)
            {
                Suit? led = GameManager.Instance.LedSuit;
                if (led.HasValue)
                    partnerWinning = IsPartnerWinning(player, currentTrick, led.Value);
            }

            // Priority 1: Dump Queen of Spades (13 points!) on opponent
            if (!partnerWinning)
            {
                var queen = playableCards.FirstOrDefault(c => c.IsQueenOfSpades());
                if (queen != null) return queen;
            }

            // Priority 2: Dump 10 of Diamonds (10 points!) on opponent
            if (!partnerWinning)
            {
                var ten = playableCards.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
                if (ten != null) return ten;
            }

            // Priority 3: Dump high hearts on opponent
            if (!partnerWinning)
            {
                var hearts = playableCards.Where(c => c.Suit == Suit.Hearts).OrderByDescending(c => c.GetRankValue()).ToList();
                if (hearts.Count > 0) return hearts[0];
            }

            // If partner is winning, don't dump points on them!
            if (partnerWinning)
            {
                // Dump highest non-point card to clean up hand
                var nonPoint = playableCards.Where(c => c.GetPoints() == 0).OrderByDescending(c => c.GetRankValue()).ToList();
                if (nonPoint.Count > 0) return nonPoint[0];
                // All cards are point cards - dump lowest point card
                return playableCards.OrderBy(c => c.GetPoints()).ThenBy(c => c.GetRankValue()).First();
            }

            // Priority 4: Dump high spades (dangerous for Queen)
            var spades = playableCards.Where(c => c.Suit == Suit.Spades && !c.IsQueenOfSpades())
                                      .OrderByDescending(c => c.GetRankValue()).ToList();
            if (spades.Count > 0) return spades[0];

            // Priority 5: Dump highest non-point card
            var safeCards = playableCards.Where(c => c.GetPoints() == 0).OrderByDescending(c => c.GetRankValue()).ToList();
            if (safeCards.Count > 0) return safeCards[0];

            // Fallback: dump lowest point card
            return playableCards.OrderBy(c => c.GetPoints()).ThenBy(c => c.GetRankValue()).First();
        }
    }
}
