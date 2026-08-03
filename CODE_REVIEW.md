# Code Review — Labyrinth of Minos

**Scope:** All first-party C# scripts (~8,600 lines across `Assets/` root and `Assets/Scripts/`). Vendored/third-party code (StarterAssets, TextMesh Pro examples, TutorialInfo, and the auto-generated `InputSystem_Actions.cs`) was excluded as out of scope.
**Stack:** Unity, Netcode for GameObjects (NGO), Unity Relay. Co-op multiplayer maze game: players collect keys and escape while a server-authoritative Minotaur AI hunts them.

Every finding has a permanent reference ID (`C1`, `H3`, `M12`, `L5`, ...). IDs are never renumbered or reused — when an issue is fixed, its entry moves to [Resolved](#resolved) under the same ID, so it stays citable from commit messages, other findings, or conversation history. Gaps in the numbering under Current Issues are expected once things close; that's not a bug in the list.

## Summary (Current Issues only)

| Severity | Open | Resolved | Theme                                                             |
| -------- | ---- | -------- | ----------------------------------------------------------------- |
| Critical | 2    | 5        | Multiplayer cheating vectors, server crashes, session-ending bug  |
| High     | 8    | 0        | Desync/data-corruption bugs, dead AI states, perf hot-path issues |
| Medium   | 13   | 1        | Real bugs with limited blast radius, duplicated logic             |
| Low      | 13   | 2        | Dead code, code quality, minor inefficiencies                     |

---

## Current Issues

### Critical

#### C1. Player movement is fully client-authoritative — no server-side anti-cheat validation

**File:** `Assets/ClientNetworkTransform.cs:5-8`

```csharp
protected override bool OnIsServerAuthoritative() => false;
```

Combined with `Assets/ClientPlayerMove.cs` (which just enables `FirstPersonController` for the owning client), nothing on the server ever validates incoming position/rotation against max speed, teleport distance, or wall collisions. A modified client can write arbitrary transform values and the server will broadcast them to everyone else as ground truth.
**Impact:** Teleport-to-exit, walk-through-walls, and instant key grabs are all trivially achievable by a cheating client. This is the single highest-impact issue on this list for a competitive/co-op game.
**Status (2026-08-02):** Mitigated, not fixed. `ClientNetworkTransform` now overrides `OnNetworkTransformStateUpdated` (fires server-side even under owner authority) and logs a warning when implied speed exceeds a configurable cap. This only detects after the fact — a modified client still fully controls its own reported position, and a client that stays under the cap is invisible to this check. The actual fix (server-authoritative movement) is tracked in [TODO.md](TODO.md).

#### C7. Getting caught while your own pause menu is open leaves the game in an unrecoverable stuck state

**Files:** `Assets/Scripts/UI/DeathScreenManager.cs` (`ShowDeath`, `DisablePauseRelays`), `Assets/Scripts/UI/LocalPauseMenu.cs`, `Assets/Scripts/Player/PlayerDeathDetector.cs`
The server-side catch detection is correct — `PlayerDeathDetector.OnTriggerEnter`'s `IsServer` gate and cooldown fire properly even while a client's local pause menu is open, and the death `ClientRpc` is dispatched. The bug is entirely client-side: `DeathScreenManager.ShowDeath()` calls `DisablePauseRelays()`, which disables every `PauseInputRelay` (the component that listens for Pause/Escape and drives `LocalPauseMenu.Toggle()`/`Close()`), but never calls `LocalPauseMenu.Instance.Close()` itself. If death happens while pause is already open, the pause panel is never told to close, its input relay is cut out from under it, and the death screen's own cursor/action-map takeover doesn't reliably win against pause's already-active state.
**Impact:** From the player's perspective this looks like invulnerability while paused — the catch registers correctly server-side, but the pause screen just stays up with no visible death screen and no working input, since the one thing that could close it was just disabled. Reported reproduction required a force-quit to recover. Reachable through completely normal play (open pause, get caught by the Minotaur), no malicious intent needed. `WinScreenManager.ShowWin()` has the identical structure and likely the same bug on the win path.
**Likely fix:** `ShowDeath()`/`ShowWin()` should explicitly call `LocalPauseMenu.Instance?.Close();` as part of taking over, so death/win always cleanly supersede an open pause menu instead of trying to coexist with it.

### High

#### H1. Late-joining clients never get a player object spawned

**File:** `Assets/Scripts/Player/PlayerSpawnControl.cs:71-98`
A `_hasSpawnedInThisScene` flag gates `SpawnAllPlayers()` to run exactly once per scene load, even though `SpawnAllPlayers` itself already safely skips clients that already have a `PlayerObject` (line 177) — implying it was designed to be safely re-callable. With NGO's own auto-spawn also disabled (`autoCreatePlayerObjects = false`, lines 106-109), a client connecting after the first spawn pass gets no player object, permanently, for that session.

#### H2. Consumed keys are never restored when progress is reset

**Files:** `Assets/Scripts/Player/KeyPickupNetwork.cs:38-63`, `Assets/Scripts/Network/GlobalKeyProgress.cs`
`KeyPickupNetwork` permanently despawns/deactivates a key on pickup with no code path that ever reactivates it, yet a progress-reset path exists specifically to reset progress without a full scene reload (see `GlobalKeyProgress.ResetProgress()`, formerly `ResetProgressServerRpc` — C3). If that reset is ever wired up and used as apparently intended, the maze becomes permanently uncompletable (keys gone, but progress counter reset) until a hard scene reload.

#### H3. `M_KillsPlayerState` and `M_InvestigateState` are dead FSM states

**Files:** `Assets/Scripts/Minotaur/M_KillsPlayerState.cs`, `Assets/Scripts/Minotaur/M_InvestigateState.cs`
Confirmed via grep: no `ChangeState(...)` call anywhere in the project ever transitions into either state — only `Patrol ⇄ Chase` exists. Actual player death is handled entirely outside the FSM by `PlayerDeathDetector.cs`. `M_Parameters.investigateThreshold` (value 40) is consequently a dead tuning knob — aggro between 40 and the chase threshold (80) does nothing, contradicting the apparent three-tier escalation design. Either wire these states up or delete them. (Also tracked from the design side in [TODO.md](TODO.md) item 3 and [AGENTS.md](AGENTS.md)'s Minotaur section — this is the single most vision-critical gap in the current build, not just a code-quality item.)

#### H4. Player permanently drops out of Minotaur aggro tracking after being caught

**File:** `Assets/Scripts/Minotaur/M_BehaviorController.cs:144-160`
`OnTriggerEnter` deletes the player from `aggroValues` (`.Remove(...)`) rather than resetting their aggro to 0. They're only re-added via a full `OnPlayerSpawned` event. If a caught player keeps playing after a death screen (rather than a full respawn), they become permanently invisible to the AI's vision/hearing for the rest of the match. This was also the trigger path for C5 (now fixed — the crash symptom is gone, but this root cause, the permanent drop from tracking, is untouched).

#### H5. Unthrottled, allocation-heavy sensing every frame

**File:** `Assets/Scripts/Minotaur/M_AggroHandler.cs:14-23,79-109`
`Physics.OverlapSphere` (GC-allocates a new array every call instead of `OverlapSphereNonAlloc`) plus a `Raycast` per player, running unconditionally every `Update()`, plus `Debug.Log` calls on every invocation of `AggroUpdate`/`IncreaseAggro`/`HearingCheck`/`VisionUpdate`. Should be throttled to a fixed cadence (e.g. every 0.1–0.2s) and logging gated behind a debug flag before shipping.

#### H6. Full A\* re-search allocates fresh state almost every tick during Chase

**File:** `Assets/Scripts/Minotaur/M_Movement.cs:125-143`, `Assets/Scripts/A_StarPathfinding.cs:78-144`
`RecalculatePath` reruns whenever the target's grid cell changes, which happens essentially every time the fleeing player crosses a tile boundary — i.e., many times per second during the most performance-sensitive state. Each run allocates a new `Dictionary`, heap-backed priority queue, and `Node` objects with no reuse. Real GC pressure exactly where frame time matters most.

#### H7. Un-gated maze "reroll" hotkey can duplicate/orphan networked objects

**File:** `Assets/Scripts/Initialization/MazeBootstrap.cs:38-42,59-68`
The rebuild hotkey has no `IsServer`/`IsHost` guard and no RPC coordination. If the host presses it mid-session, `ContentGenerator.Generate()` spawns brand-new key/exit/minotaur `NetworkObject`s without despawning the old ones — duplicate/orphaned objects and inconsistent key-progress bookkeeping. If a non-host presses it, it just causes a pointless full local rebuild hitch (the seed is deterministic from the lobby join code, so the layout doesn't even change in a networked session).

#### H8. Player's own spawn tile is never excluded from key/exit placement

**File:** `Assets/Scripts/Initialization/ContentGenerator.cs:62-64,140-142`

```csharp
//Vector2Int playerPos2D = PlayerGen(maze, s);
Vector2Int playerPos2D = default;
...
if (playerTile != default)   // always false since playerPos2D is hard-coded to default
    used.Add(playerTile);
```

A commented-out call left `playerPos2D` hard-coded to `Vector2Int.zero`, silently disabling the player-exclusion check. A key or the exit can spawn on/adjacent to a player's actual spawn tile.

### Medium

- **M1. `GameplayAutoStartHost.cs:54-57`** — anonymous lambda event subscriptions to `NetworkManager` events with no matching unsubscription; re-entering the gameplay scene (host/join again) stacks duplicate log-emitting handlers each time.
- **M2. `ContentGenerator.cs:72-87`** (`SpawnMinotaurWhenPlayerExists`) — unbounded `while` loop calling `FindAnyObjectByType` every frame with no timeout; if a player object never appears, the Minotaur silently never spawns with nothing logged.
- **M3. `AutonomousPatrol.cs`** — plain `MonoBehaviour` (not network-aware) driving physics movement via local `Random`; referenced by `MinotaurPlaceholder.prefab` and `M_DebugPlayer.prefab`. Should be confirmed unreachable from any shipped networked prefab, or each client will compute a different position independently.
- **M4. `AutonomousPatrol.cs:32`** — `Random.Range(0, destOptions.Count - 1)` double-excludes the last index (off-by-one vs. the correct `Random.Range(0, destOptions.Count)` used two lines away at line 62); also throws on an empty list.
- **M5. `GetTilePosition.cs:56-72`** (`WithinEdgeMargin`) — missing the empty-list guard its sibling method `OpenInRange` has; throws `IndexOutOfRangeException` on a maze/margin combo with no valid edge tiles.
- **M6. `PlayerNameData.cs:40`** — `playerNameText.text = ...` with no null check on the `[SerializeField]`; NRE during `OnNetworkSpawn` on any prefab variant missing the reference, aborting the rest of spawn setup for that player.
- **M7. `PlayerSpawnControl.cs:100-115`** (`OnConnectionApproval`) — unconditionally approves every connection with no player-count cap, compounding C1.
- **M8. `M_Parameters.cs:17`** (`maxChaseTime`) — defined and tuned per-prefab but never read anywhere; chase only ends via aggro decay, so this dead field misleadingly looks like a working timeout.
- **M9. `M_BehaviorController.cs:12,76`** — `Instance` static singleton has no duplicate-guard (`Destroy` on second instance), unlike every other singleton in the codebase (`AudioManager`, `WinScreenManager`, `RelayManager`, etc.). `WorldAudio.cs` reads `Instance.aggro` directly, so an accidental double-spawn silently rebinds global hearing/vision to whichever `Awake()` ran last.
- **M10. `TransitionManager.cs:39-84`** — `Go()` has no re-entrancy guard; double-clicking Play/Host/Back within the fade window starts two overlapping fade+scene-load coroutines, risking a stuck black screen.
- **M11. `AudioManager.cs:193-198`** (`SetVolume`) — dereferences `masterMixer` with no null check, unlike every other public method in the same class; NRE on scene boot or volume-slider Apply if the mixer reference is ever unassigned.
- **M13. (Added 2026-08-02, post-6.5-migration)** Migrate off the deprecated `com.unity.services.relay` package to the unified `com.unity.services.multiplayer` package (Lobby + Matchmaker + Relay under one API). Not urgent — the deprecated package still functions — but do it as its own isolated pass, after `RelayManager.cs`'s post-6.5 `RelayServerData` rewrite has more real-world (not just single-session) verification. Bundling the two Relay-touching changes together would make a regression hard to attribute.
- **M14. (Added 2026-08-02)** `Assets/Scripts/Player/StaminaBarUI.cs` doesn't consistently hide the stamina HUD when a menu (pause/death/win/leave-confirmation) is open. A `GameplayMenuState.AnyMenuOpen` facade was built and wired into `StaminaBarUI.Update()` to gate `staminaCanvas.enabled`, and one failure mode (the bar not appearing at all) turned out to be stale static state that cleared after an Editor restart — but the original symptom (bar staying visible over an open menu) was not confirmed fixed before testing moved on. Root cause not yet isolated; re-test once C7 is fixed, since C7's pause/death interaction may have been muddying earlier test results.

### Low / Code Quality

**Dead code to remove:**

- **L1.** `Assets/ClientNetworkAnimator.cs` — misnamed empty stub (class is actually `ClientNetwork`, no Animator/Netcode logic, nothing references it).
- **L2.** `Assets/Scripts/Initialization/Legacy/OldContentGenerator.cs` — confirmed unreferenced anywhere in the project.
- **L3.** `Assets/Scripts/Minotaur/Legacy/M_Senses.cs` — confirmed unreferenced (its vision logic was forked into `M_AggroHandler.VisionUpdate` and never cleaned up); still sits on `MinotaurPlaceholder.prefab` via `[RequireComponent]`.
- **L4.** `Assets/Scripts/A_StarPathfinding.cs:152-309` — ~155 lines of commented-out legacy A\* implementation.
- **L6.** `Assets/Scripts/Player/Billboard.cs`, `Assets/Scripts/Player/UI Rotator.cs` — empty/fully-commented-out stub files.

**Other:**

- **L7.** `WinScreenManager.cs` and `DeathScreenManager.cs` are ~200-line near-duplicates (identical Awake/cursor/input-map/reselect logic). **Status (2026-08-02):** the C6-related duplication (network shutdown + scene load) is gone — both now call the shared `NetworkSessionLifecycle.LeaveSession()`. The rest (`HideImmediate`, `ShowDeath`/`ShowWin`, `EnsureLocalPlayerInput`, `SwitchToUIActionMap`, `ForceUICursor`, `ReselectDefault`, pause-relay enable/disable, `LateUpdate`) is still fully duplicated between the two files and should share a base class.
- **L8.** PlayerPrefs keys and audio-mixer parameter names (`vol_music`, `MusicVol`, `KEY_FULLSCREEN`, etc.) are redeclared as separate string literals in multiple files (`AudioManager.cs` vs `AudioSettingsUI.cs`; `SettingsBootstrap.cs` vs `VideoSettingsUI.cs`) with no shared constants source — a typo in one silently desyncs the two paths.
- **L9.** `MainMenuLobbyUI.cs` calls `SceneManager.LoadScene` directly, bypassing the `TransitionManager` fade used everywhere else in the menu flow.
- **L10.** `MazeGenerator.cs` — `frontier.RemoveAt(i)` on a `List<>` (O(n) per removal) and a full grid rescan (`DeadEnds`) inside the braiding loop; fine at the current 40×40 default but scales roughly quadratically if grid size increases.
- **L11.** Magic numbers/string-tag comparisons (`obj.tag == "Player"` instead of `CompareTag`) in `M_AggroHandler.cs:88`, `M_Movement.cs:53`, `M_PatrolState.cs:117-123`.
- **L12.** Hardcoded `"MainMenu"` scene-name literals in three files vs. Inspector-configurable scene-name fields used elsewhere — inconsistent and silently fails if the scene is renamed.
- **L13.** Several files have spaces in their filenames (`Stamina System.cs`, `UI Rotator.cs`, `Player audio.cs`) — cosmetic only.
- **L14.** Per-frame `Debug.Log` calls throughout `M_AggroHandler.cs` will ship into production builds unless gated behind a debug flag.

---

## Suggested Priority Order

1. **C7** (caught-while-paused leaves an unrecoverable stuck state) — reachable through completely normal play, needs a force-quit to escape. Likely a small, targeted fix (have `ShowDeath`/`ShowWin` close the pause menu).
2. **C1** (client-authoritative movement) — mitigated with detection-only logging; still the real anti-cheat gap. Worth scoping as its own dedicated pass if this ever needs to hold up against determined cheaters (see TODO.md).
3. **H1/H2/H7/H8** — spawn/late-join and maze content-generation correctness bugs; each is a small, isolated fix.
4. **H3/H4** — decide whether Investigate/KillsPlayer states are meant to ship; either wire them up or delete them along with the aggro-removal-on-touch bug.
5. **M14** (stamina bar not hiding over menus) — cosmetic, low blast radius; re-test after C7 since the two may be entangled.
6. Everything else (H5/H6 perf, remaining Medium/Low) can be cleaned up incrementally — none are blocking, but H5/H6 are worth doing before a playtest with real player counts since they're the biggest perf hot spots found.
7. **M13** (Relay package migration) — not urgent, do as its own isolated pass.

---

## Resolved

#### C2. Client-side stamina/sprint system — speed-hack vector

**Fixed:** 2026-08-02
**Original finding:** `currentStamina` and `_isSprinting` in `Stamina System.cs` were plain (non-networked) fields computed entirely inside `Update()`, gated only by `IsOwner`. `CanSprint()` — consulted by `FirstPersonController` to gate sprint speed — trusted this purely local value with no server round-trip, so a modified client could force unlimited sprint.
**Fix:** Rewrote around server-write `NetworkVariable<float>` (stamina) and `NetworkVariable<bool>` (sprinting), driven by an owner-write `NetworkVariable<bool>` "wants to sprint" signal instead of the client deciding sprint state itself. `CanSprint()` now reads server-computed state. No changes needed to consumers (`FirstPersonController.cs`, `StaminaBarUI.cs`) — public API preserved.

#### C3. Unauthenticated key-progress reset RPC (two copies)

**Fixed:** 2026-08-02
**Original finding:** `[ServerRpc(RequireOwnership = false)]` on `GlobalKeyProgress.ResetProgressServerRpc`, with no caller/authority check, meant any connected client could wipe the whole team's key progress mid-run. `PlayerKeyProgress.cs` was a separately dead duplicate (see L5).
**Fix:** Confirmed the RPC had zero callers anywhere in the project. Removed it as an RPC entirely — `GlobalKeyProgress.ResetProgress()` is now a plain server-only C# method, unreachable from the network. `PlayerKeyProgress.cs` deleted along with its now-orphaned component on `PlayerCapsule_Networked.prefab` (see L5).

#### C4. NullReferenceException on server when chase target dies/disconnects mid-tick

**Fixed:** 2026-08-02
**Original finding:** `M_Movement.cs` dereferenced `controller.currentTarget.transform.position` one line before the null check that was meant to guard it, so a target going null between ticks threw a server-side NRE every fixed tick.
**Fix:** Null check moved into the guarding `if` condition so the dereference on the next line never executes when `currentTarget` is null.

#### C5. KeyNotFoundException on server when a caught/disconnected player re-triggers aggro

**Fixed:** 2026-08-02
**Original finding:** `M_AggroHandler.IncreaseAggro` guarded the dictionary _write_ with `ContainsKey`, but an unconditional `Debug.Log` right after it read `aggroValues[player]` regardless, throwing when the key had just been removed (see H4 for the still-open root cause).
**Fix:** Log line moved inside the same guard as the write.

#### C6. Any "return to menu" action (quit, death, or win) unconditionally shuts down the entire network session

**Fixed:** 2026-08-02
**Original finding:** `LocalPauseMenu`, `DeathScreenManager`, and `WinScreenManager` all called `NetworkSessionLifecycle.LeaveSession()` (previously four separate duplicated implementations, see L7/L15) with no confirmation and no host migration — the host dying or quitting instantly disconnected every other still-alive player, with no warning and no way to back out.
**Fix:** Built `LeaveConfirmationDialog`, a panel gating `LeaveSession()` behind an actual Confirm/Cancel step, with extra warning text shown when the leaver is host. Wired into all three call sites; buttons wired via Inspector `OnClick` matching the rest of the project's convention (not code-side `AddListener`, which was tried first and reverted for consistency — see [AGENTS.md](AGENTS.md)). Does not fix host migration itself (a host leaving still ends the session for everyone) — that's an accepted, deliberate scope limit for a friends-only co-op game, not an oversight; tracked as its own option in [TODO.md](TODO.md) if it's ever needed.

#### M12. `BackToMenu.cs` never actually loads a scene

**Fixed:** 2026-08-02
**Original finding:** Only "worked" because it was chained after `LocalPauseMenu.OnQuitToMainMenu` in one specific button's Inspector `OnClick` list order — fragile, and silently broke if reordered or reused standalone.
**Fix:** Resolved as a side effect of the C6 consolidation. `BackToMenu.ReturnToMenu()` now calls the shared `NetworkSessionLifecycle.LeaveSession()`, which always loads the menu scene itself — works standalone regardless of button wiring.

#### L5. `Assets/Scripts/Player/PlayerKeyProgress.cs` — dead duplicate of `GlobalKeyProgress`

**Fixed:** 2026-08-02
Deleted, along with its orphaned (disabled) component on `PlayerCapsule_Networked.prefab`, as part of the C3 fix.

#### L15. `Assets/Scripts/UI/BackToMenu.cs` — dead duplicate of the C6 leave-session flow

**Fixed:** 2026-08-02
**Original finding:** `BackToMenu.ReturnToMenu()` became a redundant duplicate of `LocalPauseMenu.OnQuitToMainMenu()` once both were consolidated onto `NetworkSessionLifecycle.LeaveSession()` for C6 — three buttons in `Gameplay Scene.unity` had both wired to the same click, so the leave flow fired twice per click, and the duplicate would have bypassed the new confirmation dialog entirely if left in place.
**Fix:** Removed the redundant `BackToMenu.ReturnToMenu` `OnClick` entry from all three buttons (two plain, one prefab-override) and deleted the now-fully-unreferenced `BackToMenu.cs` and its `.meta` file. Confirmed zero remaining references outside a stray, non-live `Assets/_Recovery/0 (4).unity` scene.
