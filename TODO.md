# TODO

Planned features, design notes, and rough sequencing, kept separate from [CODE_REVIEW.md](CODE_REVIEW.md) (bugs/tech debt on existing code) and [README.md](README.md) (what's actually built). Nothing here is committed or scheduled. See [AGENTS.md](AGENTS.md) for the design rationale/constraints behind why these are shaped the way they are.

Bug/stabilization work is tracked entirely in CODE_REVIEW.md's Suggested Priority Order, not duplicated here.

## Roadmap

Ordered by dependency (what has to exist before the next thing makes sense), not by priority.

1. **Downed, not dead** — replace the current full death-screen catch (`PlayerDeathDetector` / `DeathScreenManager`) with an incapacitated state and a teammate revive window. Include spectate: a downed/dead player can switch between the POVs of living players instead of sitting at a static screen.
2. **Room-based labyrinth topology** — BSP-site a handful of significant rooms (4-5 task rooms + one central exit room) with real interior volume and at least one obstacle each, connected by the existing corridor-maze algorithm tuned denser/loopier than the current default. Needed so dead-ends have hiding/evasion value instead of being uniform 1-tile pockets, and so the traffic-weighted patrol below has multiple real routes to reason over.
3. **Minotaur: Investigate state + general noise pipeline + adaptive patrol.** Wire up `M_InvestigateState` (third escalation tier between Patrol and full Chase — closes in on last-known noise/sighting without full lock-on) against a general noise-event system, rather than bespoke footstep-only logic. The same event stream should also feed a slower-decaying traffic weight onto the room-graph edges from item 2, biasing Patrol toward recently-used routes without ever reading ground-truth player position. Everything below becomes "a new thing that emits a noise event" once this exists.
4. **Items: rocks, chalk, phoenix dust.** Rocks are throwable distractions that plug into the noise pipeline (item 3). Chalk marks paths/leaves wall notes (path-marking/UI, low coupling to the rest). Phoenix dust is a panic-button blind-on-capture escape tool — only worth building after item 1, since it assumes getting caught is escapable rather than a foregone loss.
5. **Interruptible task minigames** — task puzzles get a noisy minigame component that feeds the noise pipeline (item 3), creating a distract-vs-solve role split between teammates. Depends on item 3 to matter and benefits from item 2's rooms existing as places for tasks to live in.
6. **Voice chat.** Basic in-engine proximity voice chat can land any time, independent of the rest of this list, purely for usability. Routing voice as a noise source into the Minotaur's hearing system depends on item 3's pipeline existing first.
7. **God/favor variety + run structure** — multiple gods with varied task sets per run, and whether a "run" is one labyrinth start-to-finish or multiple escalating stages. Deliberately last: cheaper to decide run-level meta-structure after the core hunt/hide/task loop has been played and feels good.

## Open Design Questions

- Run structure: single labyrinth solved start-to-finish, or multiple stages/floors of escalating depth within a session? Unresolved — don't assume either when building systems that care about run boundaries.
- Whether/how choice of god changes task types, maze generation, or Minotaur behavior per run is unexplored.
- Room count, size, and obstacle variety (item 2 above) are starting targets, not locked numbers — needs playtesting, including room-size-vs-Minotaur-turn-speed tuning.

## Also under consideration

- Task-room ambient audio cue — task rooms broadcast a discoverable, directional sound as a third noise-pipeline emitter type, giving players a pull toward objectives without a UI compass/map.
- Multiple/varied hiding-spot mechanics beyond basic line-of-sight occlusion, so evasion isn't a single memorized trick.
- **Full server-authoritative player movement.** CODE_REVIEW.md's C1 is being mitigated with server-side speed-cap detection on top of the existing owner-authoritative `ClientNetworkTransform`, not fixed outright — a modified client still can't be stopped from lying about its own position, only caught after the fact by exceeding a speed threshold. The actual fix is the server running movement simulation from client *input* and broadcasting the resulting position to everyone, including the owner (who'd then need client-side prediction/reconciliation to avoid feeling laggy). Real scope: touches core movement feel, not a bolt-on. Worth it if this ever needs to hold up against determined cheaters; not worth it for friends-only play.
- **Main menu background: chase-cam cutscene.** A looping background behind the main menu UI — camera moving through the maze as if being chased, veering around corners with the Minotaur constantly bearing down. Would use Cinemachine (not currently installed) driving a dolly/path camera along a hand-authored spline through corridor geometry, with a noise profile layered on for an unsteady, frantic feel. Purely presentational, no gameplay dependency — can happen any time.
