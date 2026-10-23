# Project ledger — hero-shooter

Updated: 2026-10-23 13:05 (-03) · session 20261023_105517_ed63d44c · owner default profile
Sprint: **one long block today from 13:00**, then 10-24 → 10-29 as buffer (plan § 6–7).

## Goal

A buildable Unity 6 third-person hero shooter that satisfies all 11 minimum mechanics in
`docs/ASSIGNMENT.md`, submitted to the LMS by 2026-10-30 22:00 and defensible out loud in the
20:20–22:00 source-code session.

## Done when

- Linux **and** Windows builds run from a clean folder and every checklist row in
  `docs/ASSIGNMENT.md` is demonstrable in them.
- `docs/CODE_DEFENSE.md` has one block per system, each verified by opening the named file.
- LMS shows a submitted file before 2026-10-30 22:00.

## Status

**Paused for documentation, 2026-10-23 ~16:xx** (Chloe: "halt for now, writing docs and updating plans").
Code state: S1 + S2 closed and verified; S3 code + wiring complete and selection verified end to end
(`CharacterSelect` authored prefab, commit `56c3b31`). The six ability effects are **written but not measured**,
and the ability designs have since been revised — the old S3 ability gate is superseded by S3b.
Tree clean, 13 commits.

## Next action

**Blocked on design answers** (asked 2026-10-23, listed under *Open questions*). Once answered, the next
code block is S3b (ability rework) + S4 (HUD + game feel), which run together:
`docs/tasks/2026-10-23_S3b-ability-rework.md` and `docs/tasks/2026-10-23_S4-hud-and-feel.md`.

**Stopping point:** the S3b gate rows (Bulwark meter, Slam knockback, passive Climb) and the S4 gate rows
(authored HUD showing cooldowns/meters, enemy bars below max, aiming FOV/offset, ability VFX, Paper tracer) are
each observed in play mode, not inferred.

## Open questions

Asked 2026-10-23 — every "open" line in `docs/HERO_ROSTER.md` maps to one of these.

- **Aiming**: FOV values (hip → aim), shoulder offset amount, does aiming slow movement, does it tighten spread?
- **Slam**: jump height, descent speed multiplier, radius/damage (5 m / 40 today), steering during the jump,
  can it start mid-air?
- **Bulwark**: damage reduction value while shielded, can Rock shoot while shielded, movement penalty, length of
  the short cooldown after an early release, length of the longer cooldown after depletion, refill delay.
- **Mark / Snare feedback**: world marker above the enemy + HUD chip with the remaining time — enough, or do you
  want an outline/highlight through walls?
- **Climb**: max height (6 m today), climb speed, direction of the exit jump, does it cost anything?
- **Overclock**: bullet/trail colour for the active window.
- **Guns and projectiles**: what specifically is unsatisfying (feel of the shot, tracer look, spread, rate,
  hitscan vs projectile, sound) — design questions still to be asked in the chat, answers to be recorded here.
- Git remote `dollythecamus/hero-shooter`: push or stay local? — Chloe

## Decisions

- 2026-10-23 — Unity 6000.6.3f1 + URP — only editor installed; lets `shootshoot` code be reused.
- 2026-10-23 — **no art assets**: ProBuilder primitives + URP Lit materials (textured/coloured).
- 2026-10-23 — **levels are ProBuilder**; level design is a separate part of the discipline, not part of
  this brief, so the maps double as that evidence.
- 2026-10-23 — builds: **Windows + Linux**.
- 2026-10-23 — schedule: **one sprint today from 13:00**, remaining days are buffer + defense rehearsal.
- 2026-10-23 — free-choice mechanics and character designs are decided *inside* the sprint, once the
  mandatory systems stand (Chloe's call).
- 2026-10-23 — packages trimmed to inputsystem / URP / AI Navigation / ProBuilder / ugui / test-framework
  / IDE support; collab-proxy, visual scripting and timeline removed (fewer things to explain, faster import).
- 2026-10-23 — single generic `EnemyBrain` FSM with per-type attack strategy — "one brain, two bodies".

## Evidence

- `docs/reference/assignment.jpeg` — the brief as received.
- `docs/ASSIGNMENT.md` — verbatim pt-BR + translation + 11-row checklist.
- `docs/tasks/2026-10-23_S1-player-shooting.md` — S1 file map, wiring actually performed, gate table.
- `curl 127.0.0.1:8080/health` → `{"status":"healthy","version":"10.0.0"}` — MCP bridge live; agent drove
  the editor for all wiring after this point (see `docs/features/editor-mcp-tooling.md`).
- Play-mode probes through the bridge: pool `created=64` → 61 shots → `created=64`, `active=0`;
  `[DamageLog] Target_2 hit for 20 → 40/60`, `… → 0/60`.
- `Packages/packages-lock.json` — 32 packages incl. `com.coplaydev.unity-mcp`; ProBuilder 6.1.2.
- `Assets/Scenes/Level.unity` — Chloe's own ProBuilder scene (cube + `ProBuilderMesh`), left untouched.
- `~/projects/shootshoot/Assets/shootshoot/Scripts/{PlayerControl,ShootPool,Projectile}.cs` — reuse source.
- `PlaybackEngines/` → `WindowsStandaloneSupport` (965 MB) installed 2026-10-23; `windows-il2cpp` does not
  exist in this Hub, so Windows is Mono-backend.


## Uncommitted at last checkpoint

none — `git status --short` empty at commit `f82e489`.

## Handoff

S1 landed and was verified in a live play session, not by inspection. The editor bridge (MCP for Unity
v10 over HTTP) did all wiring; `Assets/Scenes/Level.unity` — Chloe's ProBuilder scene — was restored as
the active scene afterwards and is clean. Verified: pool warm-up 64, zero growth across 61 shots, every
bullet returned to the pool, damage reached `Health`, no self-damage, console free of compile errors.
NOT verified: real mouse/keyboard input path end to end (fire was driven by calling `Weapon.TryFire()`
directly, and movement/jump through `InputReader` was never exercised), Windows build, and any visual
check of the arena (the agent cannot see the Game view). Next: S2 enemy FSM.


## Dispatches

none

## Requirements log

- 2026-10-23 (session 20261023_105517_ed63d44c) — read the assignment image, translate it to English,
  create the project directory, write the plan — `docs/ASSIGNMENT.md`, `docs/PLAN.md`, this ledger.
- 2026-10-23 — revision: one sprint today from 13:00; mechanics decided when the mandatory work stands;
  characters decided when being built; no art, textured materials on simple shapes; ProBuilder levels;
  Windows + Linux builds; approve headless Unity project creation — `docs/PLAN.md` § 5–9, commit
  `caf7525`, project created.
- 2026-10-23 — add Unity MCP (same package + pin as `UnityXR_3DMuseu`) and begin S1 — landed:
  `com.coplaydev.unity-mcp` in `Packages/manifest.json`, `docs/features/editor-mcp-tooling.md`, S1 code
  + wiring + gate, commits `c15a2bd` / `f82e489`.
- 2026-10-23 (session 20261023_105517_ed63d44c) — **halt code, docs only**. New sprint step: HUD *and* better
  visual feedback (ability particles, bullet-hit feedback, enemy health bars below max, ability cooldown/meter
  HUD) — `docs/tasks/2026-10-23_S4-hud-and-feel.md`. Ability specs revised in detail (Slam jump+descent+knockback,
  Bulwark becomes a 5 s channelled meter, Climb becomes a passive wall traversal with no cooldown, Mark/Snare
  need visible state, Overclock needs VFX) — `docs/HERO_ROSTER.md`, `docs/tasks/2026-10-23_S3b-ability-rework.md`.
  RMB aiming for all characters (FOV + offset + crosshair, sniper reticle for Paper). Guns/projectiles rework
  requested, design questions pending — `docs/PLAN.md` § 6 (S3b + S4 blocks).
- 2026-10-23 — Chloe: **UI must be authored prefabs, never built at runtime** — applied to the character
  select (`Prefabs/UI/CharacterSelect.prefab`, commit `56c3b31`), and binding for the S4 HUD.
- 2026-10-23 — Chloe: level geometry lives on **Structure** (layer 3); the `8` / `9` masks are intentional —
  `docs/LAYERS.md`.


