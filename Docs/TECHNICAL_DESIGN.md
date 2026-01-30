# Lekha - Technical Design Document

## Technology Stack

### Game Engine
- **Unity 2022.3 LTS** (2D)
- **C#** for game logic

### Networking (Future Online Play)
- **Unity Netcode for GameObjects** or **Photon PUN 2** (recommended for card games)
- WebSocket support for real-time multiplayer

### Platforms
- iOS (via Xcode)
- Android (via Android Studio)
- WebGL (browser play)

## Project Structure

```
LekhaGame/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/
│   │   │   ├── Card.cs              # Card data model
│   │   │   ├── Deck.cs              # Deck management
│   │   │   ├── Player.cs            # Player state
│   │   │   └── Team.cs              # Team management
│   │   │
│   │   ├── GameLogic/
│   │   │   ├── GameManager.cs       # Main game controller
│   │   │   ├── TrickManager.cs      # Trick-taking logic
│   │   │   ├── PassPhaseManager.cs  # Card passing phase
│   │   │   ├── ScoreManager.cs      # Point calculation
│   │   │   └── RoundManager.cs      # Round flow control
│   │   │
│   │   ├── UI/
│   │   │   ├── CardUI.cs            # Card visual representation
│   │   │   ├── HandUI.cs            # Player hand display
│   │   │   ├── TableUI.cs           # Center table display
│   │   │   ├── ScoreboardUI.cs      # Score display
│   │   │   └── MenuUI.cs            # Menu screens
│   │   │
│   │   ├── Animation/
│   │   │   ├── CardAnimator.cs      # Card movement animations
│   │   │   ├── DealAnimator.cs      # Dealing animation
│   │   │   └── TrickAnimator.cs     # Trick collection animation
│   │   │
│   │   ├── AI/
│   │   │   ├── AIPlayer.cs          # AI controller
│   │   │   ├── AIStrategy.cs        # Decision making
│   │   │   └── CardEvaluator.cs     # Card value assessment
│   │   │
│   │   └── Network/
│   │       ├── NetworkManager.cs    # Connection handling
│   │       ├── LobbyManager.cs      # Room/lobby system
│   │       ├── GameSync.cs          # State synchronization
│   │       └── MessageTypes.cs      # Network message definitions
│   │
│   ├── Sprites/
│   │   ├── Cards/
│   │   │   ├── Red/                 # Hearts (1-13)
│   │   │   ├── Yellow/              # Diamonds (1-13)
│   │   │   ├── Blue/                # Spades (1-13)
│   │   │   ├── Green/               # Clubs (1-13)
│   │   │   └── CardBack.png
│   │   │
│   │   ├── UI/
│   │   │   ├── Buttons/
│   │   │   ├── Panels/
│   │   │   └── Icons/
│   │   │
│   │   └── Table/
│   │       └── Background.png
│   │
│   ├── Prefabs/
│   │   ├── Card.prefab
│   │   ├── PlayerHand.prefab
│   │   └── TrickArea.prefab
│   │
│   ├── Scenes/
│   │   ├── MainMenu.unity
│   │   ├── Game.unity
│   │   ├── Lobby.unity              # For online play
│   │   └── Loading.unity
│   │
│   ├── Audio/
│   │   ├── SFX/
│   │   │   ├── CardPlace.wav
│   │   │   ├── CardShuffle.wav
│   │   │   ├── TrickWin.wav
│   │   │   └── GameEnd.wav
│   │   └── Music/
│   │       └── Background.mp3
│   │
│   └── Resources/
│       └── GameConfig.asset         # Game settings
│
├── Packages/
└── ProjectSettings/
```

## Core Classes Design

### Card.cs
```csharp
public enum Suit { Hearts, Diamonds, Spades, Clubs }
public enum Rank { Ace=1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }

public class Card
{
    public Suit Suit { get; }
    public Rank Rank { get; }
    public int Points { get; }
    public Sprite FrontSprite { get; }

    public int GetSortValue();      // For hand sorting
    public bool IsPointCard();      // Has points?
    public string GetUnoName();     // "Blue +2", "Yellow 0", etc.
}
```

### Player.cs
```csharp
public class Player
{
    public int PlayerId { get; }
    public string PlayerName { get; }
    public Team Team { get; }
    public List<Card> Hand { get; }
    public int RoundPoints { get; }
    public bool IsAI { get; }

    public List<Card> GetPlayableCards(Suit ledSuit);
    public void AddCards(List<Card> cards);
    public void RemoveCards(List<Card> cards);
}
```

### GameManager.cs
```csharp
public class GameManager : MonoBehaviour
{
    public GameState CurrentState { get; }
    public Player CurrentPlayer { get; }
    public int RoundNumber { get; }

    public void StartGame();
    public void StartPassPhase();
    public void StartTrickPhase();
    public void PlayCard(Player player, Card card);
    public void EndRound();
    public void CheckGameEnd();
}

public enum GameState
{
    WaitingForPlayers,
    Dealing,
    PassingCards,
    Playing,
    RoundEnd,
    GameEnd
}
```

## Game Flow State Machine

```
┌──────────────────┐
│  Main Menu       │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Start Game      │
│  (Deal Cards)    │
└────────┬─────────┘
         │
         ▼
┌──────────────────┐
│  Pass Phase      │◄──────────────────┐
│  (3 cards right) │                   │
└────────┬─────────┘                   │
         │                             │
         ▼                             │
┌──────────────────┐                   │
│  Trick Phase     │                   │
│  (13 tricks)     │                   │
└────────┬─────────┘                   │
         │                             │
         ▼                             │
┌──────────────────┐     No            │
│  Round End       ├───────────────────┘
│  (Score < 101?)  │
└────────┬─────────┘
         │ Yes (≥101)
         ▼
┌──────────────────┐
│  Game Over       │
│  (Show Winner)   │
└──────────────────┘
```

## Animation System

### Card Animations
1. **Deal Animation**: Cards fly from deck to each player's hand
2. **Pass Animation**: Selected cards slide to player on right
3. **Play Animation**: Card moves from hand to center table
4. **Collect Animation**: Won cards slide to winner's side
5. **Shuffle Animation**: Deck shuffle visual

### Timing
- Deal: 0.1s per card
- Pass: 0.5s slide
- Play: 0.3s to table
- Collect: 0.5s after last card played

## Uno Card Visual Mapping

| Card | Uno Visual | Number Display |
|------|------------|----------------|
| Ace | "1" | 1 |
| 2-9 | "2"-"9" | 2-9 |
| 10 | "0" | 0 |
| Jack | Reverse symbol | ⟲ |
| Queen | +2 symbol | +2 |
| King | Block/Skip symbol | ⊘ |

## Network Architecture (Future)

### Client-Server Model
- Host player acts as server
- All game logic validated on host
- Clients send actions, receive state updates

### Messages
```
CLIENT → SERVER:
- JoinGame(playerId)
- PassCards(card1, card2, card3)
- PlayCard(card)

SERVER → CLIENT:
- GameState(fullState)
- PlayerJoined(playerId)
- CardsDealt(hand)
- CardPlayed(playerId, card)
- TrickWon(playerId, cards)
- RoundEnd(scores)
- GameEnd(winningTeam)
```

## Assets Needed

### Card Sprites (52 + 1)
- 13 Red cards (Hearts)
- 13 Yellow cards (Diamonds)
- 13 Blue cards (Spades)
- 13 Green cards (Clubs)
- 1 Card back

### UI Sprites
- Play button
- Pass button
- Sort button
- Menu button
- Score panel
- Player avatar frames

### Audio
- Card place sound
- Card shuffle sound
- Trick win sound
- Round end sound
- Game win/lose sounds
- Background music

## Development Phases

### Phase 1: Offline Single-Player (vs AI)
- [ ] Basic card rendering
- [ ] Hand management
- [ ] Trick-taking logic
- [ ] Simple AI
- [ ] Scoring system
- [ ] Game flow

### Phase 2: Polish & Animations
- [ ] Card animations
- [ ] UI polish
- [ ] Sound effects
- [ ] Visual feedback

### Phase 3: Online Multiplayer
- [ ] Network infrastructure
- [ ] Lobby system
- [ ] Real-time sync
- [ ] Reconnection handling

### Phase 4: Release
- [ ] iOS build
- [ ] Android build
- [ ] WebGL build
- [ ] Testing & optimization
