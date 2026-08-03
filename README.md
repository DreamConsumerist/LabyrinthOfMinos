# Labyrinth of Minos

A co-op multiplayer thriller. You and a small squad of friends are dropped into a shifting stone labyrinth to earn a god's favor — the only way out. Split up, listen for each other in the dark, and don't be the one who goes quiet.

Think *Lethal Company* / *Peak* in social structure — friends splitting off, regrouping, calling out to each other, staying alive together — crossed with the dread of *Alien Isolation*: a single hunter that learns where you are instead of respawning to find you.

## Vision

### Atmosphere
This is a thriller, not a horror-comedy. The tension comes from isolation and sound, not jumpscares. Silence is the default state — you should be able to hear your own footsteps, your own breathing, the labyrinth settling. When a friend is far away, you might catch their scream echoing down a corridor with no way to tell how far or which direction. When they've been gone too long, the silence where that sound used to be is its own kind of dread.

### The Core Loop
A run starts with the group together, seeking the favor of one of the gods (which god is being courted can vary run to run — a hook for variety in tone, tasks, and maybe the labyrinth's own behavior). Favor isn't handed over; it's earned by completing a scattered set of tasks throughout the labyrinth. Each task is its own small puzzle — remembering where you've been, figuring out what a room wants from you, coordinating with people who are somewhere else in the maze. Once enough favor is earned, the god grants a way out.

This inherently forces the group to split up (tasks are spread out, and solving them together in one clump is slower and riskier) while giving them every reason to want to stay together (the Minotaur, getting lost, the second item mechanic below). That tension — *we should split up* vs *I don't want to be alone* — is the emotional core of the moment-to-moment game.

### The Labyrinth's Shape
The maze generator currently produces what's functionally a dungeon-crawler layout, not an environment: a flat grid of uniform 1-tile corridor cells (randomized Prim's algorithm, then a braiding pass that carves extra connections to control how many dead-ends survive). That topology is actually good — it gives you a mostly-looped maze with real alternate routes rather than one linear path, plus a controlled number of true dead-ends to use for objectives — but every node in it, dead-end or corridor, gets built as an identical pocket. There's nothing to hide behind, and a dead-end with nothing in it is just an inescapable trap rather than a space with any tension of its own.

The direction to move in is a hybrid, closer to BSP than not, but sparse rather than filling the map: use BSP-style partitioning to site a handful of *significant* rooms — on the order of 4-5 task rooms plus one central exit room in a large level — scattered irregularly rather than evenly gridded, each carved with real interior volume and at least one obstacle to circle around. That's what actually delivers the "duck behind a pillar and let it stomp past" evasion beat described above: the fix isn't making dead-ends topologically stop being dead-ends, it's giving them depth. A single-doorway room with something to break line-of-sight around still lets you juke back out the way you came while the Minotaur commits to the wrong side of the obstacle.

Most of the map should stay unclaimed labyrinth — the existing corridor-maze algorithm keeps doing its job as the connective tissue weaving between and around the sited rooms, which is what keeps this reading as "a labyrinth with a few significant places in it" rather than "a building with hallways." That corridor mesh should be tuned denser/loopier than the current default, and not just for flavor: real, multiple distinct routes between rooms are a hard requirement for the adaptive patrol behavior described below to have any alternate path to push players toward in the first place.

> **Note — orientation problem:** a large, uniform, undifferentiated maze has no *pull* (nothing tells you where to go) and, once the Minotaur is too far away to realistically find anyone, no *push* either — the group just wanders aimlessly and never meaningfully interacts with the threat. That's why the current dev/test maze is kept small (40×40 cells): at that scale the group stays close enough to actually collide with the Minotaur and with each other, but it's a stopgap, not the target size. The room-based redesign above is a structural fix for this, not just an environment upgrade — a handful of distinct rooms are legible landmarks by construction, and if Patrol reasons over the room/route graph (see Adaptive Patrol, below) instead of wandering the raw tile grid, its effective coverage scales with the finite set of meaningful routes rather than with total map area, so a bigger maze doesn't automatically dilute its presence the way it does today.

### Sound Is a Core Mechanic, Not Set Dressing
Voice chat is part of the game, not a side channel players happen to be using over Discord. It's intended to run in-engine, on the same positional/proximity audio system that carries footsteps, task noise, and screams across the labyrinth — which means the Minotaur hears you talk exactly as it hears anything else you make noise doing.

That single rule is meant to produce the game's signature moment for free: players naturally start whispering when the Minotaur is close, not because a UI prompt told them to, but because they understand the rule and it's their own voice on the line. It turns ordinary co-op chatter — "wait, is that it?", "I found one, come here" — into a real risk decision made in real time, and it reinforces the isolation atmosphere even when the group is nominally still "together" but spread out enough that a raised voice won't reach a friend, only the Minotaur.

Design implications this puts on everything else:
- Voice needs to be positional/attenuated by distance like other world sound, not a flat always-audible party-chat channel — otherwise "whisper" has no mechanical meaning.
- The Minotaur's hearing system (see below) needs to treat player voice input as just another noise source feeding the same aggro/investigate pipeline, not a separate special case.
- This is a strong argument for keeping voice chat native/in-engine rather than assuming an external app — an external channel can't be heard by the Minotaur, which breaks the entire mechanic.

> **Note — task rooms as a third listener:** the same propagation pipeline has an obvious third use beyond "Minotaur hears players" and "players hear each other": task rooms broadcasting their own discoverable ambient cue, audible and directional within some range and growing as you approach. That gives players a diegetic pull toward objectives — solving the orientation problem noted under [The Labyrinth's Shape](#the-labyrinths-shape) — without a UI compass or map, consistent with chalk being the only sanctioned memory aid. Not a separate system to build, just a third emitter type on the same sound pipeline.

### The Minotaur
Inspired directly by the Xenomorph in *Alien Isolation*: one hunter, always present in the world (not spawned in reactively), whose behavior escalates based on what it actually knows about you rather than a simple aggro-radius switch.

- **Patrol** — wandering the maze with no knowledge of the players. Low tension baseline.
- **Investigate** — has picked up a hint (a noise, a recent scent/trail, a half-glimpse, a voice) and starts closing in on last-known information without full lock-on. This is the state that's supposed to carry the escalating dread of "it's getting closer" before the chase actually starts — and it's the piece most worth protecting design-wise (see [Implementation Status](#implementation-status) below). Noise sources feeding this — footsteps, task minigames, thrown rocks, and player voice chat alike — should all route through the same hearing system rather than being bolted on as special cases.
- **Chase** — full lock-on. It roars, and it's coming for you specifically.

For the player's side of that fight: you can't fight back, only evade. Hiding should work — ducking behind a pillar and letting the Minotaur stomp past because it's lost precise track of you is the intended example, not the only one. There should be more than one kind of hiding spot / evasion trick so it doesn't become a single memorized solution.

### Adaptive Patrol: The Minotaur Learns Your Routes
Once the labyrinth is structured as a handful of rooms connected by multiple distinct routes (see [The Labyrinth's Shape](#the-labyrinths-shape) above), Patrol shouldn't be blind wandering forever — it should reason over that structure directly. Picture a small region graph sitting on top of the maze: rooms as nodes, the routes between them as edges. Every noise event a player generates — footsteps, task noise, voice chat, thrown rocks, the same signal driving the Investigate state moment-to-moment — also nudges a slower-decaying traffic weight onto whichever edge it happened near. Patrol then leans toward whichever routes have seen recent activity, not deterministically camping them, which creates real pressure to find and use a colder alternate path instead of just repeating whatever's worked so far.

This has to stay evidence-based and telegraphed rather than reading as the AI simply knowing where you are — weights should decay so an old route cools off, and they should only build from actual noise/sighting events, never ground-truth position. That's the difference between "it's hunting intelligently" and "it's cheating," and it's worth protecting deliberately (see *Alien Isolation*'s "director" AI for the reference point on that balance). Mechanically, this is a second, coarser layer of reasoning sitting above the existing state machine and tile-level A* pathfinding — the region graph decides which route to lean toward, the existing pathfinding still handles the actual walk there.

### Tools & Items
Items exist to give players options in both the puzzle-solving and the evasion halves of the loop:

- **Glowing chalk** — mark your path, leave notes on the walls for yourself or teammates. Answers "where have I been / what did I already figure out" without requiring an out-of-game map.
- **Phoenix dust** (or equivalent) — a panic-button escape tool that blinds/stuns the Minotaur momentarily when it's already caught up to you, buying a few seconds to break away.
- **Rocks** — throwable distractions down a corridor, either to redirect a patrolling/investigating Minotaur away from a task in progress, or to pull it off a teammate it's currently bearing down on.

### Puzzles as a Team Pressure Valve
Task puzzles should have a minigame component that can be interrupted — and that's the point, not a bug. Working the puzzle makes noise, which is exactly the kind of hint that feeds the Minotaur's Investigate state. That creates natural role-splitting in the moment: one player heads-down on a static, noisy task while another goes and actively distracts or redirects the Minotaur (rocks, movement, noise of their own) to buy the first player time. Neither role is passive — the "distraction" player is making a real-time risk decision, not just standing guard.

### Stakes: Downed, Not Dead
Getting caught isn't an instant, permanent removal from the run — it downs you, and your teammates have a window to reach you and revive you before it's too late. This is meant to create rescue-under-pressure as its own tension beat (do we go get them, knowing the Minotaur is still right there?) rather than just ending a caught player's run outright. **Note:** the current codebase's death handling (`PlayerDeathDetector` / `DeathScreenManager`) treats a catch as a full death screen with no rescue window — that flow needs rework to match this vision (see below).

### Party Size
Designed around a small squad — 3-4 players, in the spirit of *Lethal Company*'s crew size, where losing track of any one person is immediately noticeable. Scaling to larger groups is a possible future direction but not a current design target.

### Open Design Questions
- **Run structure isn't locked yet.** Is a "run" one labyrinth solved start-to-finish, or are there multiple stages/floors of escalating depth within a session? Both are on the table; don't assume either when building systems that care about run boundaries.
- Whether/how the choice of god changes task types, labyrinth generation, or Minotaur behavior per run is unexplored.
- **Room count, size, and obstacle variety are starting targets, not locked numbers.** "4-5 task rooms plus a central exit room" needs playtesting, as does the room-size-vs-Minotaur-turn-speed tuning that decides whether the pillar-circling evasion trick actually feels fair rather than either trivial or impossible.

---

## Implementation Status

This section exists to keep the vision and the actual code honest with each other. See [CODE_REVIEW.md](CODE_REVIEW.md) for the full technical review this is drawn from.

**Built and working:**
- Server-authoritative maze generation, seeded deterministically per lobby — currently a flat, uniform corridor-grid maze (randomized Prim's algorithm plus a braiding pass), not yet the room-based topology described in [The Labyrinth's Shape](#the-labyrinths-shape).
- Key-collection + exit win condition (currently a stand-in for the "earn the god's favor" loop).
- Minotaur FSM with working **Patrol** and **Chase** states, aggro built from vision + hearing checks.
- A* pathfinding for the Minotaur to navigate the maze.
- Basic multiplayer session flow: lobby, relay-based connection, host/client spawn.
- Audio, settings, and menu UI flow.

**Stubbed or unfinished — and central to the vision, not safe to delete:**
- **`M_InvestigateState`** exists as an empty no-op and is never transitioned into — right now the Minotaur jumps straight from Patrol to full Chase with no "it's getting closer" middle gear. This is the single most vision-critical gap in the current build; it's what turns the Minotaur from a light switch into a hunter.
- **`M_KillsPlayerState`** is also unwired/unused. Given the vision calls for "downed, rescuable" rather than a hard kill, this state likely needs to be redesigned (a downed/incapacitated state with a revive window) rather than simply finished as originally scoped.

**Not yet implemented:**
- Chalk marking / wall notes.
- Phoenix dust (or equivalent) blind-on-capture escape tool.
- Throwable rocks / distraction objects.
- The interruptible task-minigame system, and the noise-attracts-Minotaur link that's supposed to connect it to AI behavior.
- Downed/revive mechanic (current catch behavior is a full death screen, not a rescue window).
- Multiple/varied hiding-spot mechanics beyond basic occlusion.
- Room-based labyrinth topology (BSP-sited rooms with real interior volume and obstacles, connected by a denser corridor maze) — every dead-end in the current generator is still a uniform 1-tile pocket with nothing to evade around.
- The Minotaur's adaptive, traffic-weighted patrol (the region graph over rooms and routes described above) — the current Patrol state has no memory of player activity at all.
- In-engine positional voice chat, and routing player voice as a noise source into the Minotaur's hearing system (no voice package is currently in `Packages/manifest.json`). This is a load-bearing design pillar, not a nice-to-have polish item — worth prioritizing a voice solution (e.g. Unity Vivox, or another proximity-voice package) early since a lot of the atmosphere design assumes it exists.

---

## Roadmap

This is a hypothetical sequencing, not a committed schedule — ordered by dependency (what has to exist before the next thing makes sense) rather than by feature excitement. It's worth revisiting as the design solidifies, especially the open questions above.

**Phase 1 — Stabilize for playtesting.** Fix the bugs that would actively ruin a session with friends before anything else: the host dying or quitting currently tears down the whole network session (`NetworkManager.Shutdown()` with no host check), the server can crash when a player is caught or disconnects mid-chase, and players who join after the initial spawn pass never get a character. All three are small, isolated fixes, but they're a hard prerequisite — there's no point layering new systems on a foundation that falls over the first time the group actually plays it.

**Phase 2 — Rebuild catch as Downed + Rescue.** Replace the current permanent death screen with the incapacitated/revive-window mechanic described above. Worth doing right after Phase 1 because it touches the same catch-handling code the crash fixes just went through — better to redesign it once than patch a flow that's about to be gutted. It's also a prerequisite for later features (phoenix dust only matters if getting caught is escapable).

**Phase 3 — Redesign the labyrinth as rooms + a denser connecting maze.** BSP-site a handful of significant rooms (4-5 task rooms plus a central exit room), carve real interior volume with at least one obstacle to circle in each, and re-tune the existing corridor algorithm denser/loopier than the current default — it now needs to guarantee multiple distinct routes between rooms, not just a controlled dead-end percentage. Worth doing before Phase 4: the adaptive patrol work below needs a room/route graph to reason over, and there's no point building a route-learning system before there are routes to learn.

**Phase 4 — Finish the Minotaur: Investigate state + a general noise pipeline + adaptive patrol.** The single most vision-critical unfinished piece, now with a second layer folded in. Build a general noise-event system that `M_InvestigateState` listens to — one pipeline that footsteps, hearing checks, and (later) rocks, task noise, and voice chat all feed into as producers, rather than bespoke footstep-only logic. That same event stream also feeds a slower-decaying traffic weight on the Phase 3 region graph's edges, biasing Patrol toward routes with recent activity — fast/local consumption for Investigate, slow/global consumption for patrol pressure, off one shared signal. Everything in Phases 5-7 becomes "a new thing that emits a noise event" instead of a bespoke integration each time.

**Phase 5 — Items: rocks, chalk, phoenix dust.** Rocks plug straight into the Phase 4 noise pipeline and are a good way to prove it out with real player-facing content. Chalk is lower-coupling (mostly path-marking/UI) and can happen in parallel. Phoenix dust belongs after Phase 2 specifically — a panic-button blind only matters once getting caught is a rescuable situation worth escaping, not a foregone death.

**Phase 6 — Interruptible task minigames.** Depend on Phase 4's noise pipeline to matter (the point is that working the task is loud) and benefit from Phase 3's rooms existing as actual places for tasks to live in. Likely deserves its own design pass once the AI escalation feels right in isolation.

**Phase 7 — Voice chat.** Split in two: basic in-engine proximity voice could land any time (even Phase 1) purely for usability, so friends aren't relying on an external app while testing. But wiring voice as a noise source into the Minotaur's hearing — the actual "it can hear you talking" mechanic — depends on Phase 4's pipeline existing first.

**Phase 8 — God/favor variety and run structure.** Multiple gods, varied task sets per run, and the single-labyrinth-vs-multi-stage question are run-level meta-structure. Deliberately last: it's cheaper to decide how many times you do the loop and what flavor it takes *after* the core hunt/hide/task loop has been played and feels good, than to build structure around a loop that hasn't been validated yet.

---

## Tech Stack

- **Engine:** Unity 6000.2.6f2
- **Multiplayer:** Unity Netcode for GameObjects 2.6.0, Unity Relay 1.0.5
- **Render pipeline:** URP 17.2.0
- **Input:** Unity Input System 1.14.2

## Getting Started

1. Install Unity **6000.2.6f2** (via Unity Hub — this exact version is what the project targets).
2. Open the `Labyrinth of Minos Unity Project/` folder as the project root in Unity Hub (not the git repo root).
3. Open the `Gameplay Scene` or `MainMenu` scene under `Assets/Scenes/` to run locally.
4. For multiplayer testing, use Unity's Multiplayer Play Mode package (already included) to simulate multiple clients from one editor instance.
