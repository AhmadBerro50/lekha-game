# Lekha Game Rules

## Overview
Lekha is a 4-player trick-taking card game using Uno-styled cards. Players are divided into 2 teams (partners sit across from each other: North-South vs East-West).

## Card Mapping (Uno to Traditional)
| Uno Card | Traditional Card | Points |
|----------|-----------------|--------|
| Red 1-9, Reverse, +2, Skip | Hearts (Ace-King) | 1 point each (13 total) |
| Yellow 0 | 10 of Diamonds | 10 points |
| Yellow 1-9, Reverse, +2, Skip | Diamonds (Ace-King) | 0 points (except Yellow 0) |
| Blue +2 | Queen of Spades | 13 points |
| Blue 1-9, Reverse, 0, Skip | Spades (Ace-King) | 0 points (except Blue +2) |
| Green 1-9, Reverse, +2, Skip, 0 | Clubs (Ace-King) | 0 points |

**Total points per round: 36** (13 Hearts + 10 Yellow 0 + 13 Blue +2)

## Setup
1. 4 players, 2 teams (North-South vs East-West)
2. Each player receives 13 cards
3. Game is played over multiple rounds until a player reaches 101 points (their team loses)

## Card Passing Phase (Every Round)
At the beginning of EACH round, each player passes 3 cards to the player on their **right** (always right).

### Passing Restrictions
Players cannot empty their hand of a color by passing, with exceptions:

**For Red (Hearts) and Green (Clubs):**
- If you have 3 or fewer cards of a color, you CANNOT pass all of them
- You must keep at least 1 card of that color

**For Yellow (Diamonds) - Special Exception:**
- You CAN pass all your yellow cards (even emptying the color) if ALL your yellow cards are:
  - Yellow 0 (10 of Diamonds)
  - Yellow Reverse (Jack)
  - Yellow +2 (Queen)
  - Yellow Skip (King)
  - Yellow 1 (Ace)
- If you have any yellow 2-9, you cannot empty your hand of yellow

**For Blue (Spades) - Special Exception:**
- You CAN pass all your blue cards (even emptying the color) if ALL your blue cards are:
  - Blue +2 (Queen of Spades)
  - Blue Skip (King)
  - Blue 1 (Ace)
- If you have any other blue cards (2-9, 0, Reverse), you cannot empty your hand of blue

## Gameplay

### Trick Play
1. Leader plays any card (no restrictions - can lead with any color including Hearts/Red)
2. Other players MUST follow suit (play same color) if they have it
3. If a player cannot follow suit (void in that color), they must play point cards in this priority:
   - **Blue +2 (Queen of Spades)** - MUST play first if you have it
   - **Yellow 0 (10 of Diamonds)** - MUST play if you don't have Blue +2
   - Any other card if you have neither
4. Highest card of the LED SUIT wins the trick (off-suit cards never win)
5. Winner of the trick leads the next trick

**Important:** You MUST play Blue +2 or Yellow 0 even if it gives points to your own teammate!

### Card Ranking (High to Low)
Skip (King) > +2 (Queen) > Reverse (Jack) > 0 (10) > 9 > 8 > 7 > 6 > 5 > 4 > 3 > 2 > 1 (Ace)

**Note:** Ace is the LOWEST card, not the highest.

## Scoring
- Each Red/Heart card: 1 point (13 total)
- Yellow 0: 10 points
- Blue +2: 13 points
- All other cards: 0 points

**Points are tracked per PLAYER, not per team.** Each player accumulates their own points from tricks they win.

## Round Progression
- **First round:** Random player starts
- **Subsequent rounds:** The player to the RIGHT of whoever took the Blue +2 (Queen of Spades) starts

## Winning/Losing
- When any player reaches **101 points**, their TEAM loses
- The other team wins
- No "shooting the moon" - points are simply accumulated as eaten

## Turn Order
Play proceeds **LEFT to RIGHT** (clockwise): South → East → North → West → South...

## Special Rules Summary
1. **No Lead Restrictions:** Any card can be led at any time (no "breaking hearts" rule)
2. **Must Follow Suit:** If you have the led color, you MUST play it
3. **Forced Point Card Play:** When void in the led suit, you MUST play Blue +2 first, then Yellow 0 (even on your teammate!)
4. **Passing Restrictions:** Cannot empty a color unless only "passable" high cards remain. If you have 2 cards of a color, you can pass 1 and keep 1.
5. **Individual Scoring:** Each player tracks their own points, team loses when any member hits 101
6. **Pass Direction:** Always pass to the right, every round
7. **Point Cards Can Be Passed:** Yellow 0 and Blue +2 CAN be passed to the player on your right

---

## AI Strategy Notes
The AI should play intelligently to AVOID taking points:

### Passing Strategy
- Pass HIGH cards to force the receiver into taking tricks
- Keep LOW cards to duck under opponents
- Strategic goal: Pass cards that will force the receiver to win tricks containing point cards

### Playing Strategy
- Lead with low cards to avoid winning tricks
- When following suit, play lowest card if teammate is winning, highest safe card if opponent is winning
- Dump point cards (Blue +2, Yellow 0) when void in led suit
- Track which cards have been played to make informed decisions
- Remember what cards were passed to anticipate opponent hands
