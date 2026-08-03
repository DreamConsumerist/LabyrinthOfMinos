# Labyrinth of Minos

Co-op multiplayer maze game. Players spawn into a procedurally generated maze, collect keys, and reach the exit while a server-authoritative AI (the Minotaur) hunts them using vision and hearing checks.

## Current State

- Server-authoritative maze generation, seeded deterministically per lobby (randomized Prim's algorithm plus a braiding pass for loop shaping). Flat, uniform corridor-grid layout.
- Key-collection and exit win condition.
- Minotaur FSM with Patrol and Chase states; aggro driven by vision and hearing checks; A* pathfinding for navigation. `M_InvestigateState` and `M_KillsPlayerState` exist as unwired stubs (see [CODE_REVIEW.md](CODE_REVIEW.md) H3).
- Lobby → Relay-based host/join → gameplay scene flow.
- Audio, settings, and menu UI.
- Designed around 3-4 players (`maxConnections` default in `RelayManager.cs`); not currently enforced server-side.

See [CODE_REVIEW.md](CODE_REVIEW.md) for known bugs and tech debt, [TODO.md](TODO.md) for planned features and design notes not yet implemented, and [AGENTS.md](AGENTS.md) for the design rationale/constraints behind existing systems.

## Tech Stack

- **Engine:** Unity 6000.5.6f1
- **Multiplayer:** Unity Netcode for GameObjects 2.13.0, Unity Relay 1.0.5
- **Render pipeline:** URP 17.5.0
- **Input:** Unity Input System 1.20.0

## Getting Started

1. Install Unity **6000.5.6f1** via Unity Hub.
2. Open the `Labyrinth of Minos DEVELOPMENT/` folder as the project root in Unity Hub (not the git repo root).
3. Open the `Gameplay Scene` or `MainMenu` scene under `Assets/Scenes/` to run locally.
4. For multiplayer testing, use Unity's Multiplayer Play Mode package (already included) to simulate multiple clients from one editor instance.
