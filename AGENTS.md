# Agent Notes

Design intent and constraints for anyone (human or AI) implementing features in this codebase. [README.md](README.md) says what's built, [TODO.md](TODO.md) says what's planned, [CODE_REVIEW.md](CODE_REVIEW.md) says what's broken — this file says *why* certain things are built the way they are, so an implementation decision on an ambiguous point doesn't accidentally undermine the design. Treat these as constraints to respect, not flavor text.

## Tone

Thriller, not horror-comedy. Tension comes from isolation and sound, not jumpscares. Silence is the default state. Don't add jump-scare stingers, forced camera shakes, or similar horror-genre tells that aren't earned by the hunt mechanics themselves.

## No UI compass, no waypoints

The maze is deliberately disorienting — a large, uniform, undifferentiated space has no *pull* (nothing tells you where to go) and no *push* once the Minotaur is far away (the group just wanders and never interacts with the threat). Fixes to the orientation problem should be structural (room layout as legible landmarks, task-room ambient audio cues) or diegetic (chalk marking), never a UI waypoint/compass/minimap. That would remove the tension the room-based redesign and audio design are both built to solve.

## Sound is one pipeline, not per-source special cases

Footsteps, task-minigame noise, thrown rocks, and player voice chat are all meant to feed the *same* noise/hearing pipeline into the Minotaur's aggro system. When implementing any new noise-producing feature, route it through that shared system rather than writing bespoke detection logic per source — the design assumes the Minotaur reacts identically regardless of what made the sound.

Voice chat specifically must be in-engine and positional/distance-attenuated, not an external app (Discord etc.). An external voice channel can't be heard by the Minotaur, which breaks the "whisper when it's close" mechanic entirely — this is a hard constraint on any voice chat implementation, not a preference.

## Minotaur: evidence-based only, never ground-truth

`Patrol` and the planned adaptive/traffic-weighted routing must only ever react to actual noise/sighting events — never read a player's real position directly. This is the line between "hunting intelligently" and "cheating" (see *Alien Isolation*'s director AI for the reference point on that balance). Weights/hints should decay over time so old information goes cold. If an implementation of Patrol or Investigate ever needs a player's live transform to work, that's a sign it's violating this constraint.

`Investigate` (currently an unwired stub, see CODE_REVIEW.md H3) is the escalation tier that carries "it's getting closer" dread *before* a chase starts — it's the single piece most worth protecting design-wise when doing AI work here.

Evasion needs more than one viable method (line-of-sight occlusion is the only one that exists today). Avoid building toward a single memorized counter-play.

## Interruption is the point, not a bug

Task minigames are meant to be noisy and interruptible — that's intentional, not something to smooth over. It's what creates the natural role-split (one player heads-down on a task, another actively distracts/redirects the Minotaur). Don't make tasks silent or uninterruptible as a "quality of life" fix; that removes the mechanic.

## Downed, not dead

Getting caught should down a player with a teammate rescue window, not immediately end their run. The tension is the rescue decision itself (go get them vs. the Minotaur is still right there), not just a softer death. Any work on catch/death handling should preserve a window where the outcome is still undecided.

## Party size is a constraint, not a default

Designed around 3-4 players specifically so that losing track of any one person is immediately noticeable. Don't scale connection limits or party-based systems up without treating that as a deliberate design change, not just a config tweak.

## Findings get permanent reference IDs — treat CODE_REVIEW.md like a lightweight tracker

Every finding in [CODE_REVIEW.md](CODE_REVIEW.md) has a stable ID (`C1`, `H3`, `M12`, `L5`, ...) assigned once and never reused or renumbered. The file is split into **Current Issues** (open, organized by severity) and **Resolved** (closed, same ID, cites the original finding plus what fix actually landed). When a finding gets fixed:

- Move its entry from Current Issues to Resolved under the same ID — don't delete it and don't just edit it in place.
- In Resolved, keep a short restatement of the original finding plus a **Fixed:** date and what changed, referencing real files/methods touched.
- If a fix is partial (mitigates but doesn't eliminate the impact, e.g. C1/C6), it stays in Current Issues with a **Status:** note describing what changed and what's still open — don't mark something Resolved just because it was touched.
- Other findings can cite a resolved ID directly (e.g. "see C3") instead of re-explaining — that's the point of keeping IDs permanent.

This keeps Current Issues an accurate, trustworthy "what's actually still broken" list that can be cleaned up as things get fixed without losing the history of what was found or how it got fixed.

## UI buttons are wired via the Inspector, not code

Every button `OnClick` in this project is wired through the Inspector's persistent-call list (drag a target object, pick a public method), not `button.onClick.AddListener(...)` in code. Keep new UI buttons consistent with that, even though it means the handler method must be `public` (Inspector dropdowns can't target private methods). This was deliberately chosen over the code-wired alternative after weighing it directly: Inspector-wiring is fragile in ways that have caused real bugs here (duplicate/dangling `OnClick` entries surviving a refactor undetected, since nothing in the C# source shows the wiring), but it matches how the rest of the project already works and how the user builds UI by hand in the Editor — consistency and matching the human's tools won out over the marginal robustness of code-wiring for this specific case. Don't silently switch a component to code-wired listeners just because it's more robust; if the tradeoff seems worth revisiting for a specific case, raise it rather than deciding it alone.

## Advisory role — don't make direct edits in the Unity project

The user wants to personally perform all Unity Editor actions and code edits in this project themselves. Stay advisory: explain what needs to change and exactly where (file, method, specific lines), but don't use edit tools to make the change directly, and don't perform Unity Editor actions (scene/prefab edits, component wiring, Inspector changes) on their behalf — describe them clearly enough to follow instead.

This applies to the Unity project itself — `Assets/`, scenes, prefabs, C# scripts. It doesn't extend to this repo's top-level docs (`README.md`, `TODO.md`, `CODE_REVIEW.md`, this file) unless the user says otherwise; those remain collaborative as before.
