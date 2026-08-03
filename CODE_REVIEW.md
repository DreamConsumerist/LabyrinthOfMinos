# Code Review — Labyrinth of Minos

**Date:** 2026-07-26
**Scope:** All first-party C# scripts (~8,600 lines across `Assets/` root and `Assets/Scripts/`). Vendored/third-party code (StarterAssets, TextMesh Pro examples, TutorialInfo, and the auto-generated `InputSystem_Actions.cs`) was excluded as out of scope.
**Stack:** Unity, Netcode for GameObjects (NGO), Unity Relay. Co-op multiplayer maze game: players collect keys and escape while a server-authoritative Minotaur AI hunts them.

This review was done by reading every in-scope file across four subsystems (Networking/Maze Generation, Player/Input, Minotaur AI/Pathfinding, UI/Audio) and cross-referencing call sites, prefabs, and scenes to confirm each finding is real and reachable, not theoretical.

## Summary

| Severity | Count | Theme |
|---|---|---|
| Critical | 6 | Multiplayer cheating vectors, server crashes, session-ending bug |
| High | 8 | Desync/data-corruption bugs, dead AI states, perf hot-path issues |
| Medium | 12 | Real bugs with limited blast radius, duplicated logic |
| Low | ~20 | Dead code, code quality, minor inefficiencies |

The overall architecture is sound — the death/win/key-pickup trigger logic and the Minotaur FSM are correctly server-authoritative (`IsServer` gates are used consistently), which is the hard part to get right in NGO. The critical issues below are concentrated in a few specific gaps rather than a systemic problem.

---

## Critical

### C1. Player movement is fully client-authoritative — no server-side anti-cheat validation
**File:** `Assets/ClientNetworkTransform.cs:5-8`
```csharp
protected override bool OnIsServerAuthoritative() => false;
```
Combined with `Assets/ClientPlayerMove.cs` (which just enables `FirstPersonController` for the owning client), nothing on the server ever validates incoming position/rotation against max speed, teleport distance, or wall collisions. A modified client can write arbitrary transform values and the server will broadcast them to everyone else as ground truth.
**Impact:** Teleport-to-exit, walk-through-walls, and instant key grabs are all trivially achievable by a cheating client. This is the single highest-impact issue in the review for a competitive/co-op game.

### C2. Client-side stamina/sprint system — speed-hack vector
**File:** `Assets/Scripts/Player/Stamina System.cs:35-82`
`currentStamina` and `_isSprinting` are plain (non-networked) fields computed entirely inside `Update()`, gated only by `IsOwner`. `CanSprint()` — presumably consulted to gate sprint speed — trusts this purely local value with no server round-trip.
**Impact:** A modified client can force `_isSprinting` true or stamina to never deplete, granting unlimited sprint speed with no server-side check ever catching it — directly load-bearing since outrunning the Minotaur is the core loop.

### C3. Unauthenticated key-progress reset RPC (two copies)
**Files:** `Assets/Scripts/Network/GlobalKeyProgress.cs:38-43`, `Assets/Scripts/Player/PlayerKeyProgress.cs:12-16`
```csharp
[ServerRpc(RequireOwnership = false)]
public void ResetProgressServerRpc()
{
    if (!IsServer) return;   // (missing entirely in PlayerKeyProgress's copy)
    NextKeyIndex.Value = 1;
}
```
`RequireOwnership = false` and no caller/authority check means **any** connected client can invoke this RPC at will. `GlobalKeyProgress` is the copy that actually matters — confirmed as the sole source of truth consulted by `KeyPickupNetwork.cs` and `ExitTriggerNetwork.cs` to gate the win condition (`GlobalKeyProgress.Instance`, `KeyPickupNetwork.cs:23`, `ExitTriggerNetwork.cs:20`).
**Impact:** Any client (no host/ownership privileges needed) can send this RPC directly over the wire to wipe the whole team's key progress mid-run as pure griefing, with no cooldown or attribution.
**Note:** `PlayerKeyProgress.cs` is separately dead code — it's attached to `Prefabs/PlayerCapsule_Networked.prefab` but its `NextKeyIndex`/`HasAllKeys`/RPC are never read by any gameplay logic (grep confirms only `GlobalKeyProgress.Instance` is used). Recommend deleting `PlayerKeyProgress.cs` entirely and fixing the auth gap on the surviving `GlobalKeyProgress` copy.

### C4. NullReferenceException on server when chase target dies/disconnects mid-tick
**File:** `Assets/Scripts/Minotaur/M_Movement.cs:48-58`
```csharp
if (controller.GetCurrState() == controller.ChaseState)
{
    chasePos = controller.currentTarget.transform.position;   // dereferenced first
    if ((controller.currentTarget != null) && (...))          // null-checked after
```
`controller.currentTarget` is set to `null` from two places (`M_BehaviorController.cs:132-142` on player disconnect, `M_BehaviorController.cs:145-160` on player-touch), both of which can fire on any frame — including between `Update()` and the next `FixedUpdate()` that calls this code.
**Impact:** Server-side NRE, every fixed tick, whenever a player disconnects or is caught while still the chase target. This throws on the server in a live session — the most severe stability bug found.

### C5. KeyNotFoundException on server when a caught/disconnected player re-triggers aggro
**File:** `Assets/Scripts/Minotaur/M_AggroHandler.cs:24-31`
```csharp
if (controller.aggroValues.ContainsKey(player))
{
    controller.aggroValues[player] += value * modifier;
}
Debug.Log(player.name + " has " + controller.aggroValues[player] + " aggro...");  // unconditional read
```
`M_BehaviorController.OnTriggerEnter` (`M_BehaviorController.cs:144-160`) removes a player from `aggroValues` the instant the Minotaur touches them, but the player GameObject isn't guaranteed to disappear immediately (death is handled separately by `PlayerDeathDetector.cs`). If that same player triggers `VisionUpdate`/`HearingCheck` again before a respawn event re-adds them, this throws.
**Impact:** Same class of bug as C4 — reachable server crash during normal play (a player getting caught), not just an edge case.

### C6. Any "return to menu" action (quit, death, or win) unconditionally shuts down the entire network session
**Files:** `Assets/Scripts/UI/LocalPauseMenu.cs:73-78`, `Assets/Scripts/UI/DeathScreenManager.cs:85-90`, `Assets/Scripts/UI/WinScreenManager.cs:96-101`, `Assets/Scripts/UI/BackToMenu.cs:10-16`
All four call `NetworkManager.Singleton.Shutdown()` with no check for whether the acting client is the host vs. a regular client, and no host migration.
**Impact:** This is a routine action, not an exploit — the host dying (completely normal in a chase game) and clicking "Return to Menu" on their death screen instantly disconnects every other still-alive player mid-match. Same for a host simply quitting or winning. Highest-likelihood-of-occurring bug in this list since it needs no malicious intent to trigger.

---

## High

### H1. Late-joining clients never get a player object spawned
**File:** `Assets/Scripts/Player/PlayerSpawnControl.cs:71-98`
A `_hasSpawnedInThisScene` flag gates `SpawnAllPlayers()` to run exactly once per scene load, even though `SpawnAllPlayers` itself already safely skips clients that already have a `PlayerObject` (line 177) — implying it was designed to be safely re-callable. With NGO's own auto-spawn also disabled (`autoCreatePlayerObjects = false`, lines 106-109), a client connecting after the first spawn pass gets no player object, permanently, for that session.

### H2. Consumed keys are never restored when progress is reset
**Files:** `Assets/Scripts/Player/KeyPickupNetwork.cs:38-63`, `Assets/Scripts/Network/GlobalKeyProgress.cs:38-43`
`KeyPickupNetwork` permanently despawns/deactivates a key on pickup with no code path that ever reactivates it, yet `ResetProgressServerRpc` exists specifically to reset progress without a full scene reload. If that RPC is ever used as apparently intended, the maze becomes permanently uncompletable (keys gone, but progress counter reset) until a hard scene reload.

### H3. `M_KillsPlayerState` and `M_InvestigateState` are dead FSM states
**Files:** `Assets/Scripts/Minotaur/M_KillsPlayerState.cs`, `Assets/Scripts/Minotaur/M_InvestigateState.cs`
Confirmed via grep: no `ChangeState(...)` call anywhere in the project ever transitions into either state — only `Patrol ⇄ Chase` exists. Actual player death is handled entirely outside the FSM by `PlayerDeathDetector.cs`. `M_Parameters.investigateThreshold` (value 40) is consequently a dead tuning knob — aggro between 40 and the chase threshold (80) does nothing, contradicting the apparent three-tier escalation design. Either wire these states up or delete them.

### H4. Player permanently drops out of Minotaur aggro tracking after being caught
**File:** `Assets/Scripts/Minotaur/M_BehaviorController.cs:144-160`
`OnTriggerEnter` deletes the player from `aggroValues` (`.Remove(...)`) rather than resetting their aggro to 0. They're only re-added via a full `OnPlayerSpawned` event. If a caught player keeps playing after a death screen (rather than a full respawn), they become permanently invisible to the AI's vision/hearing for the rest of the match. This is also the direct trigger path for C5.

### H5. Unthrottled, allocation-heavy sensing every frame
**File:** `Assets/Scripts/Minotaur/M_AggroHandler.cs:14-23,79-109`
`Physics.OverlapSphere` (GC-allocates a new array every call instead of `OverlapSphereNonAlloc`) plus a `Raycast` per player, running unconditionally every `Update()`, plus `Debug.Log` calls on every invocation of `AggroUpdate`/`IncreaseAggro`/`HearingCheck`/`VisionUpdate`. Should be throttled to a fixed cadence (e.g. every 0.1–0.2s) and logging gated behind a debug flag before shipping.

### H6. Full A* re-search allocates fresh state almost every tick during Chase
**File:** `Assets/Scripts/Minotaur/M_Movement.cs:125-143`, `Assets/Scripts/A_StarPathfinding.cs:78-144`
`RecalculatePath` reruns whenever the target's grid cell changes, which happens essentially every time the fleeing player crosses a tile boundary — i.e., many times per second during the most performance-sensitive state. Each run allocates a new `Dictionary`, heap-backed priority queue, and `Node` objects with no reuse. Real GC pressure exactly where frame time matters most.

### H7. Un-gated maze "reroll" hotkey can duplicate/orphan networked objects
**File:** `Assets/Scripts/Initialization/MazeBootstrap.cs:38-42,59-68`
The rebuild hotkey has no `IsServer`/`IsHost` guard and no RPC coordination. If the host presses it mid-session, `ContentGenerator.Generate()` spawns brand-new key/exit/minotaur `NetworkObject`s without despawning the old ones — duplicate/orphaned objects and inconsistent key-progress bookkeeping. If a non-host presses it, it just causes a pointless full local rebuild hitch (the seed is deterministic from the lobby join code, so the layout doesn't even change in a networked session).

### H8. Player's own spawn tile is never excluded from key/exit placement
**File:** `Assets/Scripts/Initialization/ContentGenerator.cs:62-64,140-142`
```csharp
//Vector2Int playerPos2D = PlayerGen(maze, s);
Vector2Int playerPos2D = default;
...
if (playerTile != default)   // always false since playerPos2D is hard-coded to default
    used.Add(playerTile);
```
A commented-out call left `playerPos2D` hard-coded to `Vector2Int.zero`, silently disabling the player-exclusion check. A key or the exit can spawn on/adjacent to a player's actual spawn tile.

---

## Medium

1. **`GameplayAutoStartHost.cs:54-57`** — anonymous lambda event subscriptions to `NetworkManager` events with no matching unsubscription; re-entering the gameplay scene (host/join again) stacks duplicate log-emitting handlers each time.
2. **`ContentGenerator.cs:72-87`** (`SpawnMinotaurWhenPlayerExists`) — unbounded `while` loop calling `FindAnyObjectByType` every frame with no timeout; if a player object never appears, the Minotaur silently never spawns with nothing logged.
3. **`AutonomousPatrol.cs`** — plain `MonoBehaviour` (not network-aware) driving physics movement via local `Random`; referenced by `MinotaurPlaceholder.prefab` and `M_DebugPlayer.prefab`. Should be confirmed unreachable from any shipped networked prefab, or each client will compute a different position independently.
4. **`AutonomousPatrol.cs:32`** — `Random.Range(0, destOptions.Count - 1)` double-excludes the last index (off-by-one vs. the correct `Random.Range(0, destOptions.Count)` used two lines away at line 62); also throws on an empty list.
5. **`GetTilePosition.cs:56-72`** (`WithinEdgeMargin`) — missing the empty-list guard its sibling method `OpenInRange` has; throws `IndexOutOfRangeException` on a maze/margin combo with no valid edge tiles.
6. **`PlayerNameData.cs:40`** — `playerNameText.text = ...` with no null check on the `[SerializeField]`; NRE during `OnNetworkSpawn` on any prefab variant missing the reference, aborting the rest of spawn setup for that player.
7. **`PlayerSpawnControl.cs:100-115`** (`OnConnectionApproval`) — unconditionally approves every connection with no player-count cap, compounding H1.
8. **`M_Parameters.cs:17`** (`maxChaseTime`) — defined and tuned per-prefab but never read anywhere; chase only ends via aggro decay, so this dead field misleadingly looks like a working timeout.
9. **`M_BehaviorController.cs:12,76`** — `Instance` static singleton has no duplicate-guard (`Destroy` on second instance), unlike every other singleton in the codebase (`AudioManager`, `WinScreenManager`, `RelayManager`, etc.). `WorldAudio.cs` reads `Instance.aggro` directly, so an accidental double-spawn silently rebinds global hearing/vision to whichever `Awake()` ran last.
10. **`TransitionManager.cs:39-84`** — `Go()` has no re-entrancy guard; double-clicking Play/Host/Back within the fade window starts two overlapping fade+scene-load coroutines, risking a stuck black screen.
11. **`AudioManager.cs:193-198`** (`SetVolume`) — dereferences `masterMixer` with no null check, unlike every other public method in the same class; NRE on scene boot or volume-slider Apply if the mixer reference is ever unassigned.
12. **`BackToMenu.cs`** — never actually loads a scene; only "works" today because it's chained after `LocalPauseMenu.OnQuitToMainMenu` in the Inspector's `OnClick` list order on one specific button. Fragile, silently breaks if reordered or reused standalone. Consolidate with C6's fix.

---

## Low / Code Quality

**Dead code to remove:**
- `Assets/ClientNetworkAnimator.cs` — misnamed empty stub (class is actually `ClientNetwork`, no Animator/Netcode logic, nothing references it).
- `Assets/Scripts/Initialization/Legacy/OldContentGenerator.cs` — confirmed unreferenced anywhere in the project.
- `Assets/Scripts/Minotaur/Legacy/M_Senses.cs` — confirmed unreferenced (its vision logic was forked into `M_AggroHandler.VisionUpdate` and never cleaned up); still sits on `MinotaurPlaceholder.prefab` via `[RequireComponent]`.
- `Assets/Scripts/A_StarPathfinding.cs:152-309` — ~155 lines of commented-out legacy A* implementation.
- `Assets/Scripts/Player/PlayerKeyProgress.cs` — dead duplicate of `GlobalKeyProgress` (see C3).
- `Assets/Scripts/Player/Billboard.cs`, `Assets/Scripts/Player/UI Rotator.cs` — empty/fully-commented-out stub files.

**Other:**
- `WinScreenManager.cs` and `DeathScreenManager.cs` are ~200-line near-duplicates (identical Awake/cursor/input-map/reselect logic); should share a base class so fixes (including C6) don't need to be applied twice.
- PlayerPrefs keys and audio-mixer parameter names (`vol_music`, `MusicVol`, `KEY_FULLSCREEN`, etc.) are redeclared as separate string literals in multiple files (`AudioManager.cs` vs `AudioSettingsUI.cs`; `SettingsBootstrap.cs` vs `VideoSettingsUI.cs`) with no shared constants source — a typo in one silently desyncs the two paths.
- `MainMenuLobbyUI.cs` calls `SceneManager.LoadScene` directly, bypassing the `TransitionManager` fade used everywhere else in the menu flow.
- `MazeGenerator.cs` — `frontier.RemoveAt(i)` on a `List<>` (O(n) per removal) and a full grid rescan (`DeadEnds`) inside the braiding loop; fine at the current 40×40 default but scales roughly quadratically if grid size increases.
- Magic numbers/string-tag comparisons (`obj.tag == "Player"` instead of `CompareTag`) in `M_AggroHandler.cs:88`, `M_Movement.cs:53`, `M_PatrolState.cs:117-123`.
- Hardcoded `"MainMenu"` scene-name literals in three files vs. Inspector-configurable scene-name fields used elsewhere — inconsistent and silently fails if the scene is renamed.
- Several files have spaces in their filenames (`Stamina System.cs`, `UI Rotator.cs`, `Player audio.cs`) — cosmetic only.
- Per-frame `Debug.Log` calls throughout `M_AggroHandler.cs` will ship into production builds unless gated behind a debug flag.

---

## Suggested Priority Order

1. **C6** (host quit kills everyone's session) and **C4/C5** (server crashes) — these hit during completely normal play, not just cheating, and are cheap, targeted fixes (a null/host check, an ordered check-then-read).
2. **C1/C2** (client-authoritative movement & stamina) — the real anti-cheat gap. Worth scoping as a dedicated pass: server-side movement validation and a server-owned stamina `NetworkVariable`.
3. **C3** (unauthenticated reset RPC) — quick fix (add a caller/host check, or remove the RPC and drive resets server-side only); also delete the dead `PlayerKeyProgress.cs`.
4. **H1/H2/H7/H8** — spawn/late-join and maze content-generation correctness bugs; each is a small, isolated fix.
5. **H3/H4** — decide whether Investigate/KillsPlayer states are meant to ship; either wire them up or delete them along with the aggro-removal-on-touch bug.
6. Everything else (H5/H6 perf, Medium/Low) can be cleaned up incrementally — none are blocking, but H5/H6 are worth doing before a playtest with real player counts since they're the biggest perf hot spots found.
