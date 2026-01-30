# Getting Started with Lekha Development

## Step 1: Install Unity

### Download Unity Hub
1. Go to https://unity.com/download
2. Click "Download Unity Hub"
3. Open the downloaded `.dmg` file
4. Drag Unity Hub to Applications

### Install Unity Editor
1. Open Unity Hub
2. Sign in or create a Unity account (free)
3. Go to **Installs** → **Install Editor**
4. Select **Unity 2022.3.x LTS** (Long Term Support)
5. Check these modules:
   - ✅ iOS Build Support
   - ✅ Android Build Support
   - ✅ WebGL Build Support
6. Click Install (this will take ~30-60 minutes)

## Step 2: Create the Project

### In Unity Hub:
1. Go to **Projects** tab
2. Click **New Project**
3. Select **2D (Built-in Render Pipeline)** template
4. Project name: `LekhaGame`
5. Location: `/Users/ahmad/Desktop/PersonalProjects/lekha/UnityProject`
6. Click **Create Project**

### Initial Setup in Unity:
1. Wait for Unity to open (first time takes a few minutes)
2. You'll see the Unity Editor with a sample 2D scene

## Step 3: Set Up Project Structure

In Unity's **Project** window (bottom panel):

1. Right-click in Assets folder
2. Create the following folder structure:
```
Assets/
├── Scripts/
│   ├── Core/
│   ├── GameLogic/
│   ├── UI/
│   ├── Animation/
│   ├── AI/
│   └── Network/
├── Sprites/
│   ├── Cards/
│   ├── UI/
│   └── Table/
├── Prefabs/
├── Scenes/
├── Audio/
│   ├── SFX/
│   └── Music/
└── Resources/
```

## Step 4: Get Uno Card Assets

### Option A: Create Simple Cards (Faster)
We'll create basic Uno-style cards using Unity's sprite tools and text.

### Option B: Use Free Assets (Better Looking)
1. Go to Unity Asset Store (Window → Asset Store)
2. Search for "card game UI" or "playing cards"
3. Download free card assets
4. We'll customize colors to match Uno

### Option C: Custom Art (Best)
Commission or create custom Uno-style card sprites.

## Step 5: First Script - Card.cs

Create your first script:

1. In Project window, navigate to `Assets/Scripts/Core`
2. Right-click → Create → C# Script
3. Name it `Card`
4. Double-click to open in your code editor

## Development Order

### Week 1: Foundation
1. Card data model
2. Deck creation and shuffle
3. Display cards on screen
4. Player hand management

### Week 2: Game Logic
1. Dealing cards
2. Pass phase
3. Trick-taking
4. Score calculation

### Week 3: AI & Polish
1. Simple AI opponent
2. Animations
3. Sound effects
4. UI polish

### Week 4+: Networking
1. Lobby system
2. Online multiplayer
3. Testing

## Useful Unity Shortcuts

| Shortcut | Action |
|----------|--------|
| Cmd + S | Save scene |
| Cmd + P | Play/Stop game |
| Cmd + Shift + B | Build settings |
| F | Focus on selected object |
| W | Move tool |
| E | Rotate tool |
| R | Scale tool |

## Next Steps

Once Unity is installed and project is created:

1. Create the `Card.cs` script (I'll provide the code)
2. Create card sprites or import assets
3. Build the deck system
4. Test displaying cards

## Helpful Resources

### Unity Learn (Free)
- https://learn.unity.com/
- "2D Game Development" pathway

### C# Basics
- If you know JavaScript, C# is similar
- Main differences: static typing, classes everywhere

### Card Game Tutorials
- Search YouTube: "Unity 2D card game tutorial"
- Focus on: sprite rendering, hand management, animations

---

**Ready to start?** Let me know when Unity is installed and I'll walk you through creating your first scripts!
