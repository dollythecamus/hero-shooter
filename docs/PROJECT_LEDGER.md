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

**Docs only, 2026-10-23.** Code is exactly at `dced7ba`: S1 + S2 closed and verified, S3 code + wiring complete
(with the authored character select), S3's six ability *effects* never measured. The ability rework is planned in
`docs/tasks/2026-10-23_S3b-ability-rework.md` and **not built** — an implementation pass was started at ~18:10
and reverted at Chloe's instruction before any compile verification. Tree clean, working tree == `dced7ba`.

## Next action

**Waiting for Chloe's explicit go.** The next code block is S3b (ability rework) + S4 (HUD + game feel), in this
order, per the *Planned implementation* table in `docs/tasks/2026-10-23_S3b-ability-rework.md`:
input hold/release → `BulwarkShield` → `SlamDriver` → `WallClimbPassive` → aiming → authored HUD → enemy bars →
VFX.

**Stopping point:** the per-ability verification table in that file, measured in play mode rather than inferred,
plus the S4 gates. Nothing is declared closed on the strength of "it compiles".


## Open questions

**Answered 2026-10-23** (recorded in `docs/HERO_ROSTER.md`, each under its own "Decided" table):

- Aiming — FOV **60 → 42**, blend 8/s, offset **+0.55 right / −0.20 down**, movement **×0.9** all heroes,
  **×0.6** Paper, spread **−50 %**, nothing visible through walls.
- Slam — jump **4 m**, descent **2.5× gravity**, radius **5 m**, damage **40**, cooldown **8 s**, **no steering**,
  **grounded start only**.
- Bulwark — **5 s** meter, damage **×0.4**, **no shooting** while shielded, movement **×0.75**, **0.5 s** cooldown
  after early release, **8 s** after depletion, refill **1/s** starting **1 s** after release.
- Mark / Snare — marker above the enemy + HUD chip with remaining seconds, **never through walls**.
- Climb — cap **6 m**, **3 m/s**, exit jump **wall-normal + 2 m up**, no cost, reset on floor, **mantle over a
  wall whose top is below the cap** instead of jumping off.
- Overclock — **green** bullets/trails/particles.
- Enemy bars — world-space, hidden at full, **amber** when marked, **blue-grey** when snared.

Still open:

- **Guns and projectiles rework** — Chloe is answering this later; nothing on the weapon feel starts before that.
- Git remote `dollythecamus/hero-shooter`: push or stay local?


## Decisions

- 2026-10-23 — **A "halt" stops every write, including code that looks like the obvious next step.** Chloe's
  "halt for now, writing docs and updating plans" meant docs only; the agent read the docs phase as permission to
  start the rework and had to revert an unverified implementation pass. From here: one explicit go per phase.
- 2026-10-23 — Ability shapes: instant (Slam, Mark, Snare, Overclock), channelled (Bulwark), passive (Wall Climb).
  `AbilityDefinition.kind` decides; the assignment's "2 habilidades" stays satisfied with a passive slot.
- 2026-10-23 — One-mover rule: only one component may call `CharacterController.Move` in a frame. Slam and the
  wall climb suspend `PlayerMotor` while they drive the controller.

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
- 2026-10-23 18:1x (session 20261023_105517_ed63d44c) — Chloe halted execution after the docs pass; an
  implementation pass had already started and was **reverted** at her instruction: working tree restored to
  `dced7ba`, the two new components and the two deleted ability files undone, nothing verified or committed. The
  rework is now documented as planned-only in `docs/tasks/2026-10-23_S3b-ability-rework.md` (shapes, file table,
  one-mover rule, verification table).



