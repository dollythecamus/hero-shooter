# Project ledger — hero-shooter

Updated: 2026-10-24 11:25 (-03) · session 20261023_1941… · owner default profile
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

**Audit-fix step landed 2026-10-23 ~20:05** (item 1 below, code phase, Chloe's go at 19:41): the three defects the
docs audit turned up are fixed and each one was measured in play mode, not inferred —
`PlayerAbilities.CooldownRatio` (now 0 = just used → 1 = ready), `WallClimb` (asset `magnitude` is now the climb
speed in m/s, `PlayerStatus` reads it), `EnemyBrain.CallTarget` + `CallAlliesState` (a called ally converges on
the caller's target). Five files under `Assets/_Project/Scripts/`, no prefab, scene or data asset touched.
Still open in this block: the HUD and VFX (items 3–4).

**Ability rework landed 2026-10-23 ~21:2x** (item 2, Chloe's go at 20:40 "mcp is on now, go for 2"): the three
shapes are in, the six abilities were re-measured in play mode against the gate table (numbers in
`docs/tasks/2026-10-23_S3b-ability-rework.md`), RMB is no longer an ability button, and enemy knockback now
moves the body. That closes the S3b block; the HUD/aiming block (item 3) is next and needs its own go.

**HUD/aiming/bars landed 2026-10-23 ~22:5x** (item 3, Chloe's go at 21:30): RMB aiming on the camera rig,
`PlayerMotor` aim speed from `CharacterDefinition`, `Weapon` spread halving, authored `HUD.prefab` +
`EnemyHealthBar.prefab` with `HudController` / `AbilitySlotUI` / `EnemyHealthBar` / `EnemyHealthBarPool` /
`EnemyBarBinder`, and Mark/Snare moved to deadline-based multipliers on `Health` / `EnemyBrain`. Every gate row
was measured in play mode (numbers in the S4 task file) and the block is committed as **`5c40a99`** (code,
prefabs, scene, `Hero_Paper` asset). VFX (item 4) is not started.

**VFX landed 2026-10-24 ~11:1x** (item 4, the last of S4): one pooled burst prefab (`Prefabs/VFX/Blast.prefab` +
`VfxOneShot`/`VfxPool`), per-hit sparks from `Projectile.Impact`, Paper's tracer and Overclock's green through the
projectile's `TrailRenderer` + material swap, the slam landing burst, climb dust, and the Bulwark shell on
`Player.prefab`. All measured in play mode (probe lines in the S4 task file). It needed one manifest change:
`com.unity.modules.particlesystem` — the project had no built-in modules enabled at all, so `ParticleSystem`
did not compile; Chloe approved it.

## Next action

**S4 is closed.** What is left before the deadline is the submission work in `docs/PLAN.md` § 6–7:

1. ~~correctness fixes from the docs audit~~ — landed 2026-10-23 ~20:05.
2. ~~ability rework (S3b)~~ — landed 2026-10-23 ~21:2x, committed `1960b0a`/`aad17ae`.
3. ~~aiming + HUD + enemy bars~~ — landed 2026-10-23, committed `5c40a99`/`5a115ba`.
4. ~~VFX (rest of S4)~~ — landed and measured 2026-10-24 ~11:1x, committed with its docs.
5. **Builds, next** — Linux x86_64 and Windows x86_64 from `File → Build Profiles`, then **run each from a clean
   folder** and check the checklist in `docs/ASSIGNMENT.md` against the built game. This is GUI work in the
   editor; the agent can drive the dialogs through the bridge but cannot verify a built binary's behaviour, so a
   human run is required. No executeMethod build class exists (see `AGENTS.md`).
6. **`docs/CODE_DEFENSE.md`** — one block per system, each verified by opening the named file, for the 20:20
   walkthrough on 2026-10-30.

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

- **VFX gate, 2026-10-24 10:5x–11:1x** — play mode, same bridge method; full table in the S4 task file. The
  load-bearing lines: pool `created=24` at start and **still 24** after a shot, a slam, a Bulwark cycle and a
  climb; Rock hip fire `emitting=False body='M_Projectile'`; Paper `emitting=True trailCol=(0.60,0.92,1.00,0.55)`;
  impact `bullet=no-active-bullet bursts=[scale=1.00 playing=True pos=(2.3,0.0,-5.5)] created=24`; Overclock
  `trailCol=(0.30,1.00,0.40,0.65) body='M_Projectile_Overclock'`; slam `[scale=2.25 playing=True]` at the feet;
  Bulwark shell `enabled=False → True (1.43 @ meter 0.72) → False`; climb dust call sites `Stop() 0→1`,
  `Climb() 1→2`. Not measured end-to-end: the dust stream during a held-key climb — the editor overwrites
  injected keyboard state every input update, so the climb had to be driven at its call sites instead.
- **Particle System module was missing** (found 2026-10-24): `Packages/manifest.json` carried no
  `com.unity.modules.*` entries, so `UnityEngine.ParticleSystem` failed to compile (`error CS1069`). Adding
  `"com.unity.modules.particlesystem": "1.0.0"` (Chloe's sign-off) fixed it; `TrailRenderer`/`LineRenderer` were
  never affected (they live in `CoreModule`).
- **HUD / aiming / enemy-bar gate, 2026-10-23 22:2x–22:5x** — play mode through the MCP bridge, same injected-input
  method as the ability gate; every number is a probe line and the full table is in
  `docs/tasks/2026-10-23_S4-hud-and-feel.md` § *Landed*. Load-bearing ones:
  - Aiming: `fov=60.0 → fov@0.2s=45.7 → fov@0.6s=42.2 → fov@1.2s=42.0`, release `59.9`; Paper while aiming
    `hip=False aim=False sniper=True`, after release `hip=True aim=False sniper=False`.
  - HUD: Rock `health='200 / 200' Q='Rock Slam' E='Rock Bulwark' meterRootE=True meterFillE=1.00`, shield raised
    → `meterE=0.74`; Paper `Q='Paper Mark' E='Paper Snare' chargeRoot=True passiveQ=False`; Scissors
    `key='passive' passiveQ=True cdRootQ=False`.
  - Bars: full health `vis=False`; hurt `vis=True fill=0.60 rgb=(0.85,0.20,0.20)`; marked
    `rgb=(1.00,0.72,0.20) st='MARK 5.5' → 'MARK 4.6'`; snared `rgb=(0.55,0.66,0.78) st='SNARE 2.4'`; a wall
    between camera and bar → `vis=False`, wall removed → `vis=True`.
  - Mark / Snare through the real abilities: `markOk=True dmgMult=1.35 left=6.0`, `SNARE ok=True speedMult=0.25`,
    chip `'SNARE 2.5s' → 'SNARE 1.5s'`.
  - Hit marker: `hitMarkerAlpha=1.00` on a hit whose `DamageInfo.Source` is the hero (window patched to 1.5 s in
    the probe only; authored 0.12 s).
  - Static gates: `grep -rn "Instantiate("` → one hit (`Core/ObjectPool.cs:43`); `attackRange` → declaration
    only; `Assembly-CSharp.dll` 21:34 and no `error CS` after line 18251 (the 9 `CS2001` are the older
    deleted-file lines).
  - **`git diff --check` is not clean any more** (superseded 2026-10-23): it reports 9 trailing-space lines, all
    Unity serializer output in the saved arena (`m_Name: ` / `value: ` for empty strings). Engine artifact, the
    same class as the prefab note below; not hand-editable while the editor holds the project.
  - Two defects the pass introduced and then fixed: a stale `m_IsActive: 0` override on the `CharacterSelect`
    prefab instance (inherited from the previous session's in-memory play state) was written into the scene by
    the wiring save, so the select screen would not have appeared — restored to `m_IsActive: 1`; and
    `ProjectSettings/TimeManager.asset` was re-serialized by a probe's `Time.maximumDeltaTime` write — reverted
    with `git checkout --`.
- **Ability-rework gate, 2026-10-23 ~20:45–21:20** — play mode, with the *real* input path: `KeyboardState` /
  `MouseState` injected through `InputSystem.QueueStateEvent`, so `InputReader` saw Q/E/Space/LMB exactly as it
  would from the keyboard. Full table with every number is in `docs/tasks/2026-10-23_S3b-ability-rework.md`;
  the load-bearing ones:
  - Slam: `apexAboveTakeOff=3.96`, `IMPACT t=59.140 ... enemyHp=50 enemyMoved=2.12 knocked=True` (90 hp enemy,
    40 damage, body displaced) and `airborne press Q: ... slamActive=False Q-ready=True` (no cooldown spent).
  - Bulwark: `raised=True meter=4.94 dmgMult=0.40`, `damage-while-raised: hp 180 -> 176 (10 raw)`,
    `same-frame E+fire held 1 s: raised=True shots 0 -> 0`, `shield down, fired 1 s: shots 0 -> 1`,
    `depleted after 5.34 s ... canRaise=False cooldownRatio=0.00` then `+8.6 s ... canRaise=True`.
  - Mark / Snare: `IncomingDamageMultiplier=1.35` (10 damage landed as 13.5, restored at 6 s),
    `SpeedMultiplier=0.25` (restored at 3 s).
  - Climb: `climbing 0.08 -> 2.19 m in 0.7 s` (3.0 m/s), `mantle result: finalY=2.05 grounded=True`,
    `exit jump: climbed=6.04`, `apexAfterExit=8.06 riseAboveCap=1.94 outward=4.90`.
  - Overclock: `fireRate=1.80 speed=1.25` still up at 2.8 s, `1.00 / 1.00` after the window.
  - Two defects the probes caught and the code now carries fixes for: a same-frame E+LMB leak (fixed with
    `[DefaultExecutionOrder(-100)]` on `PlayerAbilities` — re-measured 0 shots) and an exit jump that travelled
    0.11 m because `PlayerMotor`'s ground acceleration ate the impulse (fixed with the `_launched` momentum rule —
    re-measured 4.90 m).
  - Compile after the last edit: `Assembly-CSharp.dll` 21:12:30, and the 9 `error CS` lines in `Logs/Editor.log`
    are all `CS2001` from the moment the three deleted ability files left the generated `.csproj` before the
    asset refresh — **0 errors after the last of them** (checked by line number, not by count).
- **Audit-fix gate, 2026-10-23 19:5x–20:0x** — play-mode probes through the MCP bridge. Method: register an
  `UnityEditor.EditorApplication.update` callback that samples and `Debug.Log`s, let it run, then read
  `Logs/Editor.log` from disk (`grep -a '\[Probe\]'`). Sleeping in the shell does **not** advance game time here —
  the clamp is `Time.maximumDeltaTime` and one `execute_code` call buys about one frame.
  - **Cooldown direction** (Scissors' 3 s Climb, slot 0): `A activate slot0 ok=True t=25.131`,
    `A t=25.131 ratio=0.000 ready=False` → `0.107 / 0.210 / 0.312 / 0.418 / 0.520 / 0.621 / 0.726 / 0.831 /
    0.939` → `A t=28.142 ratio=1.000 ready=True`. Rise toward ready, `IsReady` true exactly at 1.000.
  - **Climb responds to the asset** (`PlayerMotor` disabled so the climb is the only mover):
    `B1 start mag=6.0 y0=0.080` → `B1 end rise=5.260 dt=1.272`; `B2 start mag=3.0 y0=5.340` →
    `B2 end rise=2.376 dt=1.191`. Measured in continuous frames: `y=0.592 → 2.935 → 5.340`.
  - **Climb with the motor running (honest number, not the gate):** `rise` came out 0.000 / 0.049 / 0.356 / 1.222 /
    1.369 across runs at magnitudes 6 and 3 — gravity is the second mover, so the apex is not reproducible. This is
    the rework's problem (one-mover rule), not the plumbing's.
  - **Called ally converges on the caller's target** — the ally's own `Spyglass.Target` was pointed at a decoy:
    `D5 setup ally=RangedEnemy@(5.7, 1.2, 13.9) ownTarget(decoy)=(20.7, 1.2, 23.9) player=(5.7, 1.2, 1.9)
    startDist=12.00`, `D5 broadcast: state=CallAllies callTarget=Player`, then `dest=(4.2, 0.1, 3.1)
    dPlayer=10.48 dOwn=18.37` → … → `dest=(2.9, 0.1, 2.3) dPlayer=5.63 dOwn=22.98`: the destination is the
    player's position and the distance to the player falls while the distance to its own target grows.
  - Compile after the five edits: `Library/ScriptAssemblies/Assembly-CSharp.dll` rebuilt 19:43,
    `grep -c "error CS" Logs/Editor.log` → **0**; `grep -rn "Instantiate(" Assets/_Project/Scripts` → one hit
    (`Core/ObjectPool.cs:43`); `grep -rn attackRange` → declaration only; `git diff --check` clean.
- **Play-mode side effects had to be reverted** (2026-10-23, same session): the project runs with *Reload Scene*
  and *Reload Domain* disabled, so the play sessions wrote `90_TestArena.unity` (camera rig position 0,1,-10 →
  0,1.5,-8.5 and distance 4.5 → 2.5), `CharacterSelect.prefab` (Canvas render mode 1 → 0) and
  `ProjectSettings/TimeManager.asset` (the probe's `timeScale: 4`, `maximumDeltaTime: 8`). All three were restored
  with `git checkout --` and the scene reloaded from disk; the pitfall and the probing method are written into
  `docs/features/editor-mcp-tooling.md`.
- **Editor bridge dropped at the end of the session (20:12).** After the scene was reloaded from disk the
  editor-side MCP client's websocket died (`McpLog` send exceptions in `Logs/Editor.log`;
  `mcpforunity://editor/state` → "Unity session not ready (ping not answered)"), while the Python server
  (pid 34863) still listens on `127.0.0.1:8080` — `curl /health` → healthy. A retry ~10 minutes later still
  failed. Nothing was left running: play mode stopped, `Time.timeScale` 1, `maximumDeltaTime` 0.333, scene not
  dirty, working tree clean at `32aba1d`. The next drive session needs Window → MCP for Unity → Auto-Connect
  (or an editor restart) before the bridge answers again.
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

**none** — the VFX half landed as `4da3ef1` (3 new `Scripts/VFX` classes, 7 hooked scripts, the generated
`T_Spark` + three materials, `Blast.prefab`, the projectile's trail/refs, the player's `ShieldVfx` shell, the
scene's `Vfx` pool, and `com.unity.modules.particlesystem` in the manifest + lock). Its docs land in the commit
that follows this line, which is the only reason the tree is not empty at the moment of writing.

Earlier in this sprint: `5c40a99` (aiming + HUD + bars), `5a115ba` (their docs).

## Handoff

**Where the project stands (2026-10-24 11:2x).** S4 is closed: aiming, the authored HUD, the enemy bars and the
VFX layer are all built, measured in play mode and committed. `docs/ASSIGNMENT.md`'s 11 mechanics are covered by
code that exists and runs in the editor. What has **not** been done is everything after the editor: no build has
ever been produced, and there is no `docs/CODE_DEFENSE.md` yet.

**Next.** Builds (Linux + Windows from `File → Build Profiles`) and a human run from a clean folder — the agent
can drive the build dialogs through the bridge but cannot judge a built binary. Then the code-defense doc.

**Not verified anywhere in this sprint.** No one has looked at the Game view: HUD layout, world-space bars, VFX
sizing and the Bulwark shell are all "the numbers reach the renderer", not "it looks right". One play session by
Chloe covers it. The climb's dust stream likewise has never been seen while actually climbing.

**Known gaps that are deliberate, not forgotten:** minimap and the Tab frag board (need `ScoreService` and a
layer split), the guns/projectiles rework (Chloe has not answered), the Scissors fire-rate-window readout,
`DamageLog` still in the scene, no automated tests, no pause menu, no wave spawning.

## Handoff (earlier, S1)

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

- 2026-10-24 10:25 (same session) — Chloe: **"editor ok"** → the VFX half finished: the block had actually been
  failing on a disabled Particle System module (`error CS1069`), not a wedged editor. Asked for sign-off, she
  approved adding `com.unity.modules.particlesystem` to `manifest.json`; after that the nine VFX scripts compiled
  and every gate was measured (probe lines in the S4 task file). Code + assets committed as `4da3ef1`.
- 2026-10-24 10:1x (same session) — Chloe: **"go for continuing your tasks finishing S4"**. Committed the
  measured HUD/aiming block (`5c40a99`) and its docs (`5a115ba`), then started item 4 (VFX): the three
  `Scripts/VFX/` classes and all seven gameplay hooks are written. **Blocked** — the editor's compile after the
  10:10 restart never completed, so nothing from VFX is compiled or measured; the asset list and gates are in
  `docs/tasks/2026-10-23_S4-hud-and-feel.md` § *VFX half*.
- 2026-10-23 22:5x (same session) — Chloe: **"interrupt and write the next steps you didn't complete in docs"**.
  Verification had finished (all S4 gate rows measured, static gates green apart from the engine's serializer
  whitespace); the interrupt stopped the docs-from-diff pass and the commit. Written down here and in
  `docs/tasks/2026-10-23_S4-hud-and-feel.md` § *Landed*: leaving the block uncommitted, item 4 (four feature
  docs + `LAYERS.md`) and item 5 (VFX) are the next session's work.
- 2026-10-23 21:30 (session `20261023_1941…`, this TUI session) — Chloe: **"let's continue executing next
  actions"** + explicit **go for item 3** (aiming + full authored HUD + enemy bars), and: her uncommitted
  `Projectile.prefab` edit (radius 0.14, hitMask 191, mesh scale 0.08) is **hers — keep it and commit it with
  this block**. Note for the docs pass: `docs/LAYERS.md` still states `Projectile.hitMask = -65`; the prefab on
  disk is now `191` (layers 0–5 + 7, i.e. everything except Player and the UI layers) — doc drift to correct.
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
- 2026-10-23 (same session, audit pass) — an independent subagent audited the eight docs against the sources
  (~240 claims). **11 claims were wrong or misleading and are corrected in the docs** (commit follows the
  retrofit): `CooldownRatio` is 1-immediately-after-use, not 1-when-ready; `Weapon.Fire` advances its rate only
  after the null guards; `WallClimb` passes a literal magnitude `1f` so the asset's `magnitude = 6` is dead;
  `Spyglass` retries acquisition every frame; `CallAlliesState` converges on the ally's own target, ignoring the
  caller's; `Push` replaces rather than accumulates knockback; a brain with no `EnemyStats` perceives nothing;
  plus three smaller precision fixes (`sqrMagnitude > 0.01` means length > 0.1; the `-2` grounded pin applies
  only while descending; `StatusEffect` is a top-level enum and the `AbilityBase` numbers are properties).
  Three of these are code defects, not doc defects, and belong to the rework block: the inverted
  `CooldownRatio` XML comment (any HUD trusting it draws backwards), the ignored climb magnitude, and
  "calls friends" allies walking to their own target instead of the caller's.
- 2026-10-23 19:41–20:10 (session `20261023_1941…`, this TUI session) — Chloe: **"let's execute next actions"** →
  executed item 1 of the block, the three audit fixes, and nothing else (item 2 still needs its own go). Five files
  changed: `PlayerAbilities.cs` (CooldownRatio returns the fill amount), `WallClimb.cs` + `PlayerStatus.cs`
  (the `Climb` effect's magnitude is m/s and comes from the asset), `EnemyBrain.cs` + `CallAlliesState.cs`
  (`CallTarget`). Verified in play mode with the quoted probe lines in Evidence, docs corrected in the same pass
  (`characters-and-abilities.md`, `enemy-fsm.md`, `hud-aiming-and-feedback.md`, `player-movement-and-camera.md`,
  `editor-mcp-tooling.md`), scene/prefab/ProjectSettings churn produced by play mode reverted.
- 2026-10-23 20:40–21:25 (same session) — Chloe: **"mcp is on now, go for 2"** → the ability rework (S3b) built and
  verified. Four new files (`AbilityShapes`, `BulwarkShield`, `SlamDriver`, `WallClimbPassive`), three deleted
  (`Abilities/Bulwark`, `WallClimb`, `GroundSlam`), eleven changed scripts, `Player.prefab` gained the three
  components, two ability assets aligned to the roster (Bulwark `duration 5` / `cooldown 0`; Climb `cooldown 0` /
  `duration 0`, `magnitude 6` = cap). All six gate rows measured with injected real input; the two defects the
  probes found (execution order for shield-vs-fire, launch momentum for the exit jump) are fixed and re-measured.
  Docs updated from the diff: `characters-and-abilities.md`, `player-movement-and-camera.md`, `enemy-fsm.md`,
  `hud-aiming-and-feedback.md`, the S3b task file, `ARCHITECTURE.md` and this ledger.



