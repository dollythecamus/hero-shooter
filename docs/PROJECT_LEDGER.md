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

**Docs retrofit landed 2026-10-23 ~19:2x** (`docs-driven-dev-pipeline` stage 0): `ARCHITECTURE.md`, `AGENTS.md`,
and six feature docs under `docs/features/` now describe the code as it actually is, including the drift against
`docs/HERO_ROSTER.md`. No source file, prefab, scene or data asset was touched by that pass.

## Next action

**Code block not started.** Chloe gave a go at 19:10 for this order, then at ~19:2x asked for the
documentation/architecture pass instead — only docs were written (`ARCHITECTURE.md`, `AGENTS.md`, six feature
docs). Starting the code block needs its own explicit go; a docs phase is not permission for the next step
(decision below). Order she fixed: **ability rework → HUD → VFX last.** Working
scope, in this order:

1. ability rework (input press/hold/release, `BulwarkShield`, `SlamDriver`, `WallClimbPassive`, knockback fix)
2. aiming (FOV/offset/spread/speed) + authored `HUD.prefab` (health, Q/E cooldowns, Bulwark meter, Mark/Snare
   chips, crosshair/reticle, hit marker) + world-space enemy bars
3. VFX last (impact sparks, Paper trails, Overclock green, ability particles)

Deferred out of this block and recorded there: minimap + Tab frag board (both need systems that do not exist
yet — `ScoreService`, a minimap mask/layer split), and the guns/projectiles rework (Chloe has not answered).

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
- `Assets/Scenes/Level.unity` — Chloe's own ProBuilder scene, left untouched during S1; **no longer present in
  the repo at `dced7ba`** (the only scenes now are `90_TestArena` and the template `SampleScene`).
- `~/projects/shootshoot/Assets/shootshoot/Scripts/{PlayerControl,ShootPool,Projectile}.cs` — reuse source.
- `PlaybackEngines/` → `WindowsStandaloneSupport` (965 MB) installed 2026-10-23; `windows-il2cpp` does not
  exist in this Hub, so Windows is Mono-backend.
- Docs retrofit probes, 2026-10-23: `curl 127.0.0.1:8080/health` → healthy v10.0.0; editor running as
  `Unity -projectpath /home/chloe/projects/hero-shooter` (pid 126381) with `Temp/UnityLockfile` present;
  `Library/ScriptAssemblies/Assembly-CSharp.dll` present; `grep -c "error CS" Logs/Editor.log` → **0**;
  `grep -rn "Instantiate(" Assets/_Project/Scripts` → **one** hit (`Core/ObjectPool.cs:43`);
  `grep -rn "attackRange"` → declaration only; `git diff --check` clean. Scene inventory from the YAML:
  `90_TestArena` holds 20 objects incl. 2 × `RangedEnemy`, 2 × `MeleeEnemy`, 1 × `Player`, the
  `CharacterSelect` canvas, `ProjectilePool` (`warmUpCount: 64`) and a baked NavMesh.


## Uncommitted at last checkpoint

none — `git status --short` empty at commit `f82e489`, and empty again after the docs retrofit, which is
committed together with this line. No source, prefab, scene or data asset changed in that pass.

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
- 2026-10-23 ~19:2x (session `20261023_1920…`, this TUI session) — Chloe: **apply `docs-driven-dev-pipeline` and
  create documentation + architecture for the features** — done as stage 0 retrofit, docs only, no code:
  `ARCHITECTURE.md`, `AGENTS.md`, `docs/features/{damage-and-pooling,player-movement-and-camera,
  weapons-and-projectiles,enemy-fsm,characters-and-abilities,hud-aiming-and-feedback}.md`, README status
  corrected. Drift found and written down rather than fixed: `EnemyStats.attackRange` is dead data; the aim ray
  in `Weapon.SolveAimDirection` uses mask `~0` and can hit the hero's own capsule; `InputReader.Ability1Pressed`
  binds RMB, which collides with the planned RMB aiming; `PLAN.md` § 3 lists ~16 files that do not exist;
  `Health.IncomingDamageMultiplier` / `EnemyBrain.SpeedMultiplier` restores are not stack-safe.



