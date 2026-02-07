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

        // ===== NEW: Team Score Awareness =====
        // Track team scores for strategic decisions
        private static int myTeamScore = 0;
        private static int opponentTeamScore = 0;
        private static int partnerScore = 0;
        private static int myScore = 0;

        // End-game threshold - when to switch to ultra-safe play
        private const int END_GAME_THRESHOLD = 80;
        private const int PARTNER_DANGER_THRESHOLD = 70; // Partner needs protection

        // Track what cards were passed to whom (for predicting opponent hands)
        private static Dictionary<PlayerPosition, List<Card>> cardsPassedToOpponent = new Dictionary<PlayerPosition, List<Card>>();

        // ===== ATTACK AFTER PASS - Key Strategy! =====
        // Track suits where we passed HIGH cards - we should LEAD LOW in these to attack!
        private static Dictionary<PlayerPosition, HashSet<Suit>> suitsWePassedHighIn = new Dictionary<PlayerPosition, HashSet<Suit>>();
        // Track who we passed cards to (our victim to attack)
        private static Dictionary<PlayerPosition, PlayerPosition> myPassTarget = new Dictionary<PlayerPosition, PlayerPosition>();

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
            cardsPassedToOpponent.Clear();
            suitsWePassedHighIn.Clear();
            myPassTarget.Clear();

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

            // Update scores from GameManager
            UpdateTeamScores();
        }

        /// <summary>
        /// Update team scores from GameManager for strategic decisions
        /// </summary>
        public static void UpdateTeamScores()
        {
            if (GameManager.Instance == null) return;

            var players = GameManager.Instance.Players;
            if (players == null || players.Length < 4) return;

            // Calculate team scores (NorthSouth vs EastWest)
            int northSouthScore = 0;
            int eastWestScore = 0;

            foreach (var player in players)
            {
                if (player.Position == PlayerPosition.North || player.Position == PlayerPosition.South)
                    northSouthScore += player.TotalPoints;
                else
                    eastWestScore += player.TotalPoints;
            }

            // Store scores based on which team AI is on
            // Note: AI players are East and West (opponents of human South)
            // and North (partner of human South)
            myTeamScore = northSouthScore; // Will be adjusted per AI player
            opponentTeamScore = eastWestScore;

            // Get individual scores for partner protection
            var southPlayer = players.FirstOrDefault(p => p.Position == PlayerPosition.South);
            var northPlayer = players.FirstOrDefault(p => p.Position == PlayerPosition.North);
            var eastPlayer = players.FirstOrDefault(p => p.Position == PlayerPosition.East);
            var westPlayer = players.FirstOrDefault(p => p.Position == PlayerPosition.West);

            // Store for access during play
            if (southPlayer != null) myScore = southPlayer.TotalPoints;
            if (northPlayer != null) partnerScore = northPlayer.TotalPoints;
        }

        /// <summary>
        /// Check if we're in end-game mode (team score high)
        /// </summary>
        private static bool IsEndGame(PlayerPosition myPosition)
        {
            UpdateTeamScores();

            // Get actual team score for this player
            int teamScore = GetTeamScore(myPosition);
            return teamScore >= END_GAME_THRESHOLD;
        }

        /// <summary>
        /// Check if partner needs protection (high score)
        /// </summary>
        private static bool PartnerNeedsProtection(PlayerPosition myPosition)
        {
            UpdateTeamScores();

            if (GameManager.Instance == null) return false;
            var players = GameManager.Instance.Players;
            if (players == null) return false;

            PlayerPosition partnerPos = GetPartner(myPosition);
            var partner = players.FirstOrDefault(p => p.Position == partnerPos);

            if (partner == null) return false;

            return partner.TotalPoints >= PARTNER_DANGER_THRESHOLD;
        }

        /// <summary>
        /// Get team score for a player position
        /// </summary>
        private static int GetTeamScore(PlayerPosition pos)
        {
            if (GameManager.Instance == null) return 0;
            var players = GameManager.Instance.Players;
            if (players == null) return 0;

            int score = 0;
            PlayerPosition partnerPos = GetPartner(pos);

            foreach (var player in players)
            {
                if (player.Position == pos || player.Position == partnerPos)
                    score += player.TotalPoints;
            }
            return score;
        }

        /// <summary>
        /// Get partner's current score
        /// </summary>
        private static int GetPartnerScore(PlayerPosition myPosition)
        {
            if (GameManager.Instance == null) return 0;
            var players = GameManager.Instance.Players;
            if (players == null) return 0;

            PlayerPosition partnerPos = GetPartner(myPosition);
            var partner = players.FirstOrDefault(p => p.Position == partnerPos);
            return partner?.TotalPoints ?? 0;
        }

        /// <summary>
        /// Check if this player's hand is "dangerous" (has high point cards)
        /// Used in end-game to decide if we should avoid all tricks
        /// </summary>
        private static bool HasDangerousHand(List<Card> hand)
        {
            // Dangerous if we have Queen of Spades, 10 of Diamonds, or multiple hearts
            bool hasQueen = hand.Any(c => c.IsQueenOfSpades());
            bool hasTen = hand.Any(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
            int heartCount = hand.Count(c => c.Suit == Suit.Hearts);

            return hasQueen || hasTen || heartCount >= 3;
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
        /// ENHANCED: Also track what we passed to opponents for flush strategy
        /// </summary>
        public static void TrackPassedCards(PlayerPosition from, PlayerPosition to, List<Card> cards)
        {
            passedCardsTo[to] = new List<Card>(cards);
            receivedCardsFrom[from] = new List<Card>(cards);

            // Track what we (AI) passed to opponents
            // Cards are passed to the RIGHT, so check if 'to' is an opponent
            PlayerPosition partnerOfFrom = GetPartner(from);
            if (to != partnerOfFrom)
            {
                // 'to' is an opponent - track what we passed to them
                cardsPassedToOpponent[to] = new List<Card>(cards);
                Debug.Log($"[AI] Tracking: {from} passed to opponent {to}: {string.Join(", ", cards.Select(c => c.GetUnoName()))}");
            }
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
        /// Choose 3 cards to pass to the right - SMART STRATEGY
        ///
        /// KEY INSIGHT: Pass HIGH cards in suits where you have LOW cards!
        /// Then LEAD LOW in those suits to make the receiver take tricks.
        ///
        /// Strategy priority:
        /// 1. Pass dangerous cards (Queen of Spades, 10 of Diamonds) if exposed
        /// 2. Pass HIGH cards in suits where we have LOW cards (attack setup)
        /// 3. Create voids to dump points later
        /// </summary>
        public static List<Card> ChooseCardsToPass(Player player)
        {
            List<Card> hand = new List<Card>(player.Hand);
            List<Card> toPass = new List<Card>();

            // Track who we're passing to (right neighbor)
            PlayerPosition passTarget = Player.GetPlayerToRight(player.Position);
            myPassTarget[player.Position] = passTarget;

            // Initialize attack tracking for this player
            if (!suitsWePassedHighIn.ContainsKey(player.Position))
                suitsWePassedHighIn[player.Position] = new HashSet<Suit>();
            suitsWePassedHighIn[player.Position].Clear();

            // Group cards by suit
            var suitGroups = hand.GroupBy(c => c.Suit).ToDictionary(g => g.Key, g => g.ToList());

            Debug.Log($"[AI PASS] {player.PlayerName} analyzing hand for passing...");

            // ===== PRIORITY 1: Pass dangerous cards if exposed =====

            // Queen of Spades analysis
            Card queenOfSpades = hand.FirstOrDefault(c => c.IsQueenOfSpades());
            if (queenOfSpades != null)
            {
                var otherSpades = hand.Where(c => c.Suit == Suit.Spades && !c.IsQueenOfSpades()).ToList();
                int cardsBelow = otherSpades.Count(c => c.GetRankValue() < 11);

                // Pass Queen if we don't have enough low cards to duck
                if (cardsBelow < 3 && CanPassCard(queenOfSpades, hand, suitGroups))
                {
                    Debug.Log($"[AI PASS] {player.PlayerName}: Passing Queen of Spades (exposed - only {cardsBelow} cards below)");
                    toPass.Add(queenOfSpades);
                    hand.Remove(queenOfSpades);
                    UpdateSuitGroups(suitGroups, queenOfSpades);
                    passedQueenOfSpades[player.Position] = true;
                }
            }

            // 10 of Diamonds analysis
            Card tenOfDiamonds = hand.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
            if (tenOfDiamonds != null && toPass.Count < 3)
            {
                var otherDiamonds = hand.Where(c => c.Suit == Suit.Diamonds && c.Rank != Rank.Ten).ToList();
                int cardsBelow = otherDiamonds.Count(c => c.GetRankValue() < 9);

                if (cardsBelow < 3 && CanPassCard(tenOfDiamonds, hand, suitGroups))
                {
                    Debug.Log($"[AI PASS] {player.PlayerName}: Passing 10 of Diamonds (exposed)");
                    toPass.Add(tenOfDiamonds);
                    hand.Remove(tenOfDiamonds);
                    UpdateSuitGroups(suitGroups, tenOfDiamonds);
                    passedTenOfDiamonds[player.Position] = true;
                }
            }

            // ===== PRIORITY 2: ATTACK SETUP - Pass HIGH cards where we have LOW cards =====
            // This is the KEY strategy: pass high, keep low, then lead low to attack!

            foreach (Suit suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Spades, Suit.Hearts })
            {
                if (toPass.Count >= 3) break;
                if (!suitGroups.ContainsKey(suit) || suitGroups[suit].Count < 2) continue;

                var suitCards = suitGroups[suit].OrderBy(c => c.GetRankValue()).ToList();
                int lowestRank = suitCards.First().GetRankValue();
                int highestRank = suitCards.Last().GetRankValue();

                // If we have BOTH high and low cards in this suit, pass the HIGH ones
                // So we can lead LOW and force opponent to take with HIGH
                if (highestRank - lowestRank >= 5 && suitCards.Count >= 3)
                {
                    // We have good spread - pass high, keep low
                    var highCards = suitCards.Where(c => c.GetRankValue() >= 8).OrderByDescending(c => c.GetRankValue()).ToList();
                    foreach (var card in highCards)
                    {
                        if (toPass.Count >= 3) break;
                        // Don't pass Queen of Spades or 10 of Diamonds here (handled above)
                        if (card.IsQueenOfSpades() || (card.Suit == Suit.Diamonds && card.Rank == Rank.Ten)) continue;
                        if (!CanPassCard(card, hand, suitGroups)) continue;

                        Debug.Log($"[AI PASS] {player.PlayerName}: Passing {card.GetUnoName()} (attack setup - keeping low {suit})");
                        toPass.Add(card);
                        hand.Remove(card);
                        UpdateSuitGroups(suitGroups, card);
                        suitsWePassedHighIn[player.Position].Add(suit); // Track for attack!
                    }
                }
            }

            // ===== PRIORITY 3: Pass HIGH Hearts (always dangerous to have) =====
            if (toPass.Count < 3 && suitGroups.ContainsKey(Suit.Hearts))
            {
                var highHearts = suitGroups[Suit.Hearts]
                    .OrderByDescending(c => c.GetRankValue())
                    .ToList();

                foreach (var card in highHearts)
                {
                    if (toPass.Count >= 3) break;
                    if (CanPassCard(card, hand, suitGroups))
                    {
                        Debug.Log($"[AI PASS] {player.PlayerName}: Passing {card.GetUnoName()} (high heart - dangerous)");
                        toPass.Add(card);
                        hand.Remove(card);
                        UpdateSuitGroups(suitGroups, card);
                        suitsWePassedHighIn[player.Position].Add(Suit.Hearts);
                    }
                }
            }

            // ===== PRIORITY 4: Create voids (for dumping points later) =====
            if (toPass.Count < 3)
            {
                // Find suits with 1-3 cards that we can empty
                var shortSuits = suitGroups
                    .Where(g => g.Value.Count > 0 && g.Value.Count <= 3 - toPass.Count + g.Value.Count)
                    .Where(g => CanEmptySuit(g.Key, g.Value) || g.Key == Suit.Clubs) // Clubs is safest to void
                    .OrderBy(g => g.Value.Count)
                    .ToList();

                foreach (var suitGroup in shortSuits)
                {
                    if (toPass.Count >= 3) break;
                    foreach (var card in suitGroup.Value.OrderByDescending(c => c.GetRankValue()).ToList())
                    {
                        if (toPass.Count >= 3) break;
                        if (CanPassCard(card, hand, suitGroups))
                        {
                            Debug.Log($"[AI PASS] {player.PlayerName}: Passing {card.GetUnoName()} (creating void in {suitGroup.Key})");
                            toPass.Add(card);
                            hand.Remove(card);
                            UpdateSuitGroups(suitGroups, card);
                        }
                    }
                }
            }

            // ===== PRIORITY 5: Pass remaining high cards from any suit =====
            if (toPass.Count < 3)
            {
                var highCards = hand
                    .Where(c => c.GetPoints() == 0) // Non-point cards only
                    .Where(c => !c.IsQueenOfSpades() && !(c.Suit == Suit.Diamonds && c.Rank == Rank.Ten))
                    .OrderByDescending(c => c.GetRankValue())
                    .ToList();

                foreach (var card in highCards)
                {
                    if (toPass.Count >= 3) break;
                    if (CanPassCard(card, hand, suitGroups))
                    {
                        Debug.Log($"[AI PASS] {player.PlayerName}: Passing {card.GetUnoName()} (high card)");
                        toPass.Add(card);
                        hand.Remove(card);
                        UpdateSuitGroups(suitGroups, card);
                    }
                }
            }

            // Priority 5: Pass high Clubs (safe suit, no points but can void to dump later)
            // ===== FALLBACK: Just pass highest remaining cards =====
            if (toPass.Count < 3)
            {
                var anyCards = hand.OrderByDescending(c => c.GetRankValue()).ToList();
                foreach (var card in anyCards)
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

            // Final summary
            Debug.Log($"[AI PASS] {player.PlayerName} FINAL: Passing {string.Join(", ", toPass.Select(c => c.GetUnoName()))}");
            if (suitsWePassedHighIn[player.Position].Count > 0)
            {
                Debug.Log($"[AI PASS] {player.PlayerName} will ATTACK in suits: {string.Join(", ", suitsWePassedHighIn[player.Position])}");
            }
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
        /// ENHANCED with strategic rules:
        /// - Help partner dump by leading suits they're void in (if safe)
        /// - Active flushing: Lead to force opponents to play Queen/10D
        /// - End-game awareness: Play safer when team score is high
        /// - Don't lead suits where opponents are short (they'll dump)
        /// - Don't re-lead suits that caused point dumps
        /// - Follow partner's lead when safe
        /// - Rule 10: Never lead with high Spades/Diamonds if Queen/10D not played
        /// - Rule 12: If we passed Queen, only lead Spades with cards below Queen
        /// </summary>
        private static Card ChooseLeadCard(Player player, List<Card> playableCards)
        {
            var hand = player.Hand;
            PlayerPosition partnerPos = GetPartner(player.Position);
            PlayerPosition rightOpponent = GetNextPosition(partnerPos);
            PlayerPosition leftOpponent = GetNextPosition(player.Position);

            // NEW: Check game state
            bool inEndGame = IsEndGame(player.Position);
            bool protectPartner = PartnerNeedsProtection(player.Position);

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

            // ===== PRIORITY 0: ATTACK AFTER PASS =====
            // Lead LOW in suits where we passed HIGH cards - this is our key attack strategy!
            if (suitsWePassedHighIn.ContainsKey(player.Position) && suitsWePassedHighIn[player.Position].Count > 0)
            {
                foreach (Suit attackSuit in suitsWePassedHighIn[player.Position])
                {
                    var attackCards = safePlayableCards.Where(c => c.Suit == attackSuit).ToList();
                    if (attackCards.Count > 0)
                    {
                        // Lead LOWEST card in this suit - opponent has our high cards!
                        var lowest = attackCards.OrderBy(c => c.GetRankValue()).First();

                        // Make sure we're not leading a point card
                        if (lowest.GetPoints() == 0)
                        {
                            Debug.Log($"[AI ATTACK] {player.PlayerName}: Leading LOW {lowest.GetUnoName()} - passed HIGH cards in {attackSuit}!");
                            return lowest;
                        }
                    }
                }
            }

            // Strategy 0b: Avoid leading the same suit opponent on right just led (rule 4)
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
                    Debug.Log($"[AI] {player.PlayerName}: Flushing Queen with low Spade");
                    return lowSpades[0]; // Lead lowest spade to flush Queen
                }
            }

            // NEW Strategy 2.5: Help partner dump - lead suits partner is void in (if safe)
            // Only do this if we won't get stuck with points
            if (IsKnownVoid(partnerPos, Suit.Hearts) || IsKnownVoid(partnerPos, Suit.Spades) ||
                IsKnownVoid(partnerPos, Suit.Diamonds))
            {
                // Find a suit partner is void in where we have low cards
                foreach (Suit voidSuit in new[] { Suit.Spades, Suit.Diamonds, Suit.Hearts })
                {
                    if (!IsKnownVoid(partnerPos, voidSuit)) continue;
                    if (!safeSuits.Contains(voidSuit)) continue; // Don't lead dangerous suits

                    // Check if opponent is also void (they would dump on us)
                    if (IsKnownVoid(leftOpponent, voidSuit) || IsKnownVoid(rightOpponent, voidSuit))
                        continue; // Opponent would dump on us

                    var suitCards = safePlayableCards.Where(c => c.Suit == voidSuit).ToList();
                    if (suitCards.Count > 0)
                    {
                        // Lead LOW so we don't win the trick ourselves
                        var lowCard = suitCards.OrderBy(c => c.GetRankValue()).First();
                        if (lowCard.GetPoints() == 0)
                        {
                            Debug.Log($"[AI] {player.PlayerName}: Leading {voidSuit} to help partner dump");
                            return lowCard;
                        }
                    }
                }
            }

            // NEW Strategy 2.6: Active flush - force opponents to play dangerous cards
            // If we know opponent has Queen/10D (from pass tracking), lead that suit
            if (!queenOfSpadesPlayed)
            {
                // Check if we passed Queen to someone - lead spades to flush them
                foreach (var passed in cardsPassedToOpponent)
                {
                    if (passed.Value != null && passed.Value.Any(c => c.IsQueenOfSpades()))
                    {
                        // We passed Queen to this opponent - lead low spades
                        var lowSpades = safePlayableCards.Where(c => c.Suit == Suit.Spades && c.GetRankValue() < 11)
                                                          .OrderBy(c => c.GetRankValue()).ToList();
                        if (lowSpades.Count > 0)
                        {
                            Debug.Log($"[AI] {player.PlayerName}: Flushing Queen from opponent");
                            return lowSpades[0];
                        }
                    }
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

            // Strategy 4: Lead from suits where we have ALL LOW cards (SAFEST!)
            // If our highest card in a suit is low (7 or below), we can't win the trick!
            foreach (var suit in new[] { Suit.Clubs, Suit.Diamonds, Suit.Spades, Suit.Hearts })
            {
                if (!safeSuits.Contains(suit)) continue;
                var suitCards = safePlayableCards.Where(c => c.Suit == suit && c.GetPoints() == 0).ToList();
                if (suitCards.Count == 0) continue;

                int highestInSuit = suitCards.Max(c => c.GetRankValue());
                // If our highest is 7 or lower (rank value 6), we're safe - likely to lose!
                if (highestInSuit <= 6)
                {
                    var lowestCard = suitCards.OrderBy(c => c.GetRankValue()).First();
                    Debug.Log($"[AI] {player.PlayerName}: Leading from ALL-LOW suit {suit} (highest={highestInSuit})");
                    return lowestCard;
                }
            }

            // Strategy 5: Lead from longest safe suit (low card)
            var suitCounts = safePlayableCards
                .Where(c => safeSuits.Contains(c.Suit) && c.GetPoints() == 0) // Only safe non-point cards
                .GroupBy(c => c.Suit)
                .OrderByDescending(g => g.Count())
                .ToList();

            if (suitCounts.Count > 0)
            {
                var bestSuit = suitCounts[0];
                var lowestInLongest = bestSuit.OrderBy(c => c.GetRankValue()).First();
                Debug.Log($"[AI] {player.PlayerName}: Leading low from longest suit {bestSuit.Key}");
                return lowestInLongest;
            }

            // Strategy 6: Lead low clubs (safest - no points)
            var clubs = safePlayableCards.Where(c => c.Suit == Suit.Clubs).OrderBy(c => c.GetRankValue()).ToList();
            if (clubs.Count > 0)
            {
                Debug.Log($"[AI] {player.PlayerName}: Leading low club (safe)");
                return clubs[0];
            }

            // Strategy 7: Lead lowest non-point card
            var safeLead = safePlayableCards.Where(c => c.GetPoints() == 0).OrderBy(c => c.GetRankValue()).ToList();
            if (safeLead.Count > 0)
            {
                return safeLead[0];
            }

            // Fallback: lowest card (avoid point cards if possible)
            var nonPoint = safePlayableCards.Where(c => c.GetPoints() == 0).ToList();
            if (nonPoint.Count > 0)
            {
                return nonPoint.OrderBy(c => c.GetRankValue()).First();
            }
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
            // CRITICAL: NEVER play 10D unless it's the ONLY option!
            if (ledSuit == Suit.Diamonds)
            {
                bool hasTen = suitCards.Any(c => c.Rank == Rank.Ten);
                if (hasTen)
                {
                    // Filter out 10D from our options if we have ANY other diamond
                    var nonTenCards = suitCards.Where(c => c.Rank != Rank.Ten).ToList();
                    if (nonTenCards.Count > 0)
                    {
                        // Use only non-10D cards
                        Debug.Log($"[AI] {player.PlayerName}: Protecting 10 of Diamonds - playing other diamond");
                        return nonTenCards.OrderBy(c => c.GetRankValue()).First(); // Play lowest non-10D
                    }
                    // Only have 10D - must play it (forced)
                    Debug.Log($"[AI] {player.PlayerName}: FORCED to play 10 of Diamonds (only diamond)");
                }
            }

            if (isLastToPlay)
            {
                // Last to play - we know exactly what we're getting
                int totalPoints = trickPoints + safeSuitCards.Min(c => c.GetPoints()); // minimum points we add

                if (trickPoints == 0)
                {
                    // No points in trick - safe to win, dump highest card
                    // BUT: Never play 10D or QoS unless forced!
                    var safeWinners = winningCards.Where(c => c.GetPoints() == 0).ToList();
                    if (safeWinners.Count > 0)
                    {
                        // Win with highest SAFE card
                        return safeWinners.Last();
                    }
                    // All winners are point cards - play lowest winner
                    if (winningCards.Count > 0)
                    {
                        return winningCards.First();
                    }
                    // Can't win - play highest safe card
                    var safeCards = safeSuitCards.Where(c => c.GetPoints() == 0).ToList();
                    if (safeCards.Count > 0) return safeCards.OrderByDescending(c => c.GetRankValue()).First();
                    return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
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

            // NEW: Check if partner needs protection
            bool protectPartner = PartnerNeedsProtection(player.Position);
            bool inEndGame = IsEndGame(player.Position);

            // Not last to play
            if (partnerWinning)
            {
                if (trickPoints > 0)
                {
                    // Partner winning a trick with points
                    // NEW: If partner needs protection, consider taking the trick ourselves
                    if (protectPartner && winningCards.Count > 0)
                    {
                        // Partner is in danger - we should take these points instead!
                        Debug.Log($"[AI] {player.PlayerName}: Taking points to protect partner!");
                        return winningCards[0]; // Win with lowest winner
                    }

                    // Normal play - play under partner
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
            // NEW: In end-game with dangerous hand, play extra safe
            if (inEndGame && HasDangerousHand(player.Hand.ToList()))
            {
                // Play lowest to avoid winning
                return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
            }

            // Play conservatively - low card to stay under
            return safeSuitCards.OrderBy(c => c.GetRankValue()).First();
        }

        /// <summary>
        /// Choose a card to dump when can't follow suit
        /// SMART DUMPING STRATEGY: Maximize damage to opponents!
        ///
        /// Key principles:
        /// 1. NEVER dump points on partner (unless absolutely forced)
        /// 2. When opponent wins, dump MAXIMUM points (QoS > 10D > Hearts highest-first)
        /// 3. Target the opponent with HIGHER score (closer to 101 = lose)
        /// 4. When last to play, we know exactly who wins - be aggressive!
        /// 5. After dumping points, get rid of dangerous high cards
        /// </summary>
        private static Card ChooseDumpCard(Player player, List<Card> playableCards, List<Card> currentTrick)
        {
            Suit? ledSuit = null;
            bool partnerWinning = false;
            bool isLastToPlay = currentTrick.Count == 3;
            PlayerPosition winnerPos = player.Position; // Default

            if (currentTrick.Count > 0 && GameManager.Instance != null)
            {
                ledSuit = GameManager.Instance.LedSuit;
                if (ledSuit.HasValue)
                {
                    partnerWinning = IsPartnerWinning(player, currentTrick, ledSuit.Value);
                    winnerPos = GetCurrentTrickWinner(currentTrick, ledSuit.Value);
                }
            }

            // Check game state
            bool protectPartner = PartnerNeedsProtection(player.Position);
            PlayerPosition partnerPos = GetPartner(player.Position);

            // Get opponent scores to target the higher one
            int leftOppScore = GetPlayerScore(GetNextPosition(player.Position));
            int rightOppScore = GetPlayerScore(GetNextPosition(partnerPos));

            Debug.Log($"[AI DUMP] {player.PlayerName}: Partner winning={partnerWinning}, LastToPlay={isLastToPlay}, " +
                      $"LeftOpp={leftOppScore}pts, RightOpp={rightOppScore}pts");

            // ===== RULE 1: NEVER dump points on partner =====
            if (partnerWinning)
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: Partner winning - dumping SAFE cards only!");

                // Dump highest non-point card (helps clear dangerous cards)
                var safeCards = playableCards.Where(c => c.GetPoints() == 0)
                    .OrderByDescending(c => c.GetRankValue()).ToList();
                if (safeCards.Count > 0)
                {
                    // Prefer dumping high spades (King/Ace) to get rid of Queen risk
                    var safeHighSpades = safeCards.Where(c => c.Suit == Suit.Spades && c.GetRankValue() >= 12).ToList();
                    if (safeHighSpades.Count > 0)
                    {
                        Debug.Log($"[AI DUMP] {player.PlayerName}: Dumping high spade {safeHighSpades[0].GetUnoName()} on partner (safe)");
                        return safeHighSpades[0];
                    }

                    // Prefer dumping high diamonds (J/Q/K/A) to get rid of 10D risk
                    var safeHighDiamonds = safeCards.Where(c => c.Suit == Suit.Diamonds && c.GetRankValue() >= 10).ToList();
                    if (safeHighDiamonds.Count > 0)
                    {
                        Debug.Log($"[AI DUMP] {player.PlayerName}: Dumping high diamond {safeHighDiamonds[0].GetUnoName()} on partner (safe)");
                        return safeHighDiamonds[0];
                    }

                    return safeCards[0]; // Highest non-point
                }

                // ALL cards are point cards - dump LOWEST points (minimize damage to partner)
                Debug.Log($"[AI DUMP] {player.PlayerName}: All cards are points! Dumping minimum points on partner.");
                return playableCards.OrderBy(c => c.GetPoints()).ThenBy(c => c.GetRankValue()).First();
            }

            // ===== RULE 2: OPPONENT WINNING - MAXIMUM DAMAGE! =====
            Debug.Log($"[AI DUMP] {player.PlayerName}: OPPONENT winning - DUMPING MAXIMUM POINTS!");

            // Priority 1: Queen of Spades (13 points - BIGGEST DUMP!)
            var queen = playableCards.FirstOrDefault(c => c.IsQueenOfSpades());
            if (queen != null)
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: DUMPING QUEEN OF SPADES! (+13 points to opponent!)");
                return queen;
            }

            // Priority 2: 10 of Diamonds (10 points)
            var tenD = playableCards.FirstOrDefault(c => c.Suit == Suit.Diamonds && c.Rank == Rank.Ten);
            if (tenD != null)
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: DUMPING 10 OF DIAMONDS! (+10 points to opponent!)");
                return tenD;
            }

            // Priority 3: ALL Hearts - dump HIGHEST first (each = 1 point)
            var hearts = playableCards.Where(c => c.Suit == Suit.Hearts)
                .OrderByDescending(c => c.GetRankValue()).ToList();
            if (hearts.Count > 0)
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: Dumping heart {hearts[0].GetUnoName()} (+1 point to opponent)");
                return hearts[0];
            }

            // ===== RULE 3: No point cards - dump dangerous high cards =====

            // Priority 4: Dump high Spades (King/Ace) - they're dangerous if Queen comes
            var highSpades = playableCards.Where(c => c.Suit == Suit.Spades && !c.IsQueenOfSpades())
                .OrderByDescending(c => c.GetRankValue()).ToList();
            if (highSpades.Count > 0 && highSpades[0].GetRankValue() >= 12) // King or Ace
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: Dumping dangerous high spade {highSpades[0].GetUnoName()}");
                return highSpades[0];
            }

            // Priority 5: Dump high Diamonds (J/Q/K/A) - dangerous if 10D comes
            var highDiamonds = playableCards.Where(c => c.Suit == Suit.Diamonds && c.Rank != Rank.Ten)
                .OrderByDescending(c => c.GetRankValue()).ToList();
            if (highDiamonds.Count > 0 && highDiamonds[0].GetRankValue() >= 10) // Jack or higher
            {
                Debug.Log($"[AI DUMP] {player.PlayerName}: Dumping dangerous high diamond {highDiamonds[0].GetUnoName()}");
                return highDiamonds[0];
            }

            // Priority 6: Dump any high cards to clean up hand
            var anyHighCards = playableCards.Where(c => c.GetPoints() == 0)
                .OrderByDescending(c => c.GetRankValue()).ToList();
            if (anyHighCards.Count > 0)
            {
                return anyHighCards[0];
            }

            // Fallback: dump lowest card
            return playableCards.OrderBy(c => c.GetRankValue()).First();
        }

        /// <summary>
        /// Get a player's current total points
        /// </summary>
        private static int GetPlayerScore(PlayerPosition position)
        {
            if (GameManager.Instance == null) return 0;
            var players = GameManager.Instance.Players;
            if (players == null) return 0;

            var player = players.FirstOrDefault(p => p.Position == position);
            return player?.TotalPoints ?? 0;
        }
    }
}
