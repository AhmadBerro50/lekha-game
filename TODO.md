# Lekha - Future Implementation List

## Networking & Multiplayer

- [x] **Emoji Network Sync** - When a player sends an emoji, broadcast to all other players in the room (EmojiReaction message type on server + client)
- [ ] **Spectator Game View** - Full spectator mode: read-only game view showing all hands, no card interaction, "Stop Watching" button, proper GameController handling for spectators with no LocalPlayerPosition
- [x] **Reconnection Resume** - Client-side game state restoration when a player reconnects mid-game (server sends game state, client rebuilds hand/trick/scores)
- [ ] **Matchmaking** - Auto-match system: queue up and get matched with other waiting players instead of manual room create/join
- [ ] **Private Rooms** - Password-protected rooms or invite-link based joining
- [ ] **Chat Messages** - In-game text chat between players (quick messages or free text)

## Gameplay

- [ ] **Game Variants** - Support different rule sets (e.g., no-pass rounds, different point thresholds)
- [ ] **Statistics & History** - Track win/loss record, games played, average score per player profile
- [ ] **Leaderboard** - Global or friends leaderboard based on win rate / games played
- [ ] **Tutorial** - Interactive tutorial for new players explaining game rules, passing, trick-taking

## UI/UX

- [x] **Animations Polish** - Card dealing animation, trick collection sweep, score counting animation
- [x] **Sound Effects** - Card play sounds, trick win, round end, game over fanfare (procedural SoundManager with all effects)
- [x] **Haptic Feedback** - Vibration on card play, trick win, emoji received (mobile) - HapticManager with Android/iOS support
- [ ] **Landscape/Portrait** - Ensure layout works well in both orientations or lock to one
- [ ] **Accessibility** - Color-blind friendly card indicators, font size options

## Infrastructure

- [ ] **iOS Build** - TestFlight deployment and testing
- [ ] **Android Build** - Google Play internal testing track
- [ ] **Server Scaling** - Multiple server instances, load balancing if player count grows
- [ ] **Server Monitoring** - Health checks, error alerting, player count dashboard
- [ ] **Analytics** - Track game completions, disconnects, average game duration

## Known Issues / Polish

- [ ] **Agora macOS Binary** - 59MB file in git; consider Git LFS or .gitignore for platform-specific binaries
- [ ] **Server Submodule** - Server directory shows as modified submodule in git status; consider making it a proper submodule or moving into the repo
