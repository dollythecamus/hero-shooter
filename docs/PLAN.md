# Hero Shooter — build plan

Revised 2026-10-23 12:55 after Chloe's decisions: **one long sprint today, starting 13:00**,
no art assets (textured materials on simple shapes), levels built with **ProBuilder**, targets
**Windows + Linux**, and both the "+3 mechanics" and the character designs get decided *inside*
the sprint once the mandatory systems stand.

Facts this plan is built on (verified 2026-10-23):
- Deadline **2026-10-30 22:00**; presentation 18:30–20:00, code walkthrough 20:20–22:00 that evening.
  Nothing is developed on 10-30.
- Unity **6000.6.3f1** (`~/Unity/Hub/Editor/6000.6.3f1`), URP 17.6.0, Input System 1.19.0,
  AI Navigation 2.0.12, ProBuilder 6.1.2 (`Packages/manifest.json`, trimmed of collab / visual scripting / timeline).
- Installed build support: **LinuxStandaloneSupport only** — `Editor/Data/PlaybackEngines/` holds
  `AndroidPlayer` + `LinuxStandaloneSupport`, **no Windows support**. Windows build support has to be
  installed through `unityhub install-modules` before a Windows build can exist.
- `~/projects/shootshoot` (same editor) holds reusable code Chloe already wrote:
  `PlayerControl.cs` (132 lines, third-person CharacterController + pivot camera),
  `ShootPool.cs` + `Projectile.cs` (124 lines, correct pooled projectiles).

## 1. One-sentence goal

A single-player, wave-based third-person hero shooter in Unity 6 where the player picks 1 of 3
characters on 1 of 2 ProBuilder maps, survives escalating waves of FSM-driven ranged and melee
enemies using pooled projectiles and 2 abilities per character, with minimap, Tab frag board and a
scripted tutorial — every requirement openable and defensible in the 20:20 code session.

## 2. Project identity

| | |
|---|---|
| Path | `/home/chloe/projects/hero-shooter` |
| Engine | Unity 6000.6.3f1, URP 17.6.0 (PC renderer), Input System, AI Navigation, ProBuilder |
| Art | **none imported** — ProBuilder/primitive shapes with URP Lit materials (flat colour + simple textures) |
| Git | local repo, one commit per block; `origin` (github.com/dollythecamus/hero-shooter) on decision |
| Builds | Linux x86_64 (possible today) + Windows x86_64 (blocked on the module install) |
| Docs | `docs/ASSIGNMENT.md`, `docs/PLAN.md` (this), `docs/PROJECT_LEDGER.md`, `docs/CODE_DEFENSE.md` (written in S7) |

## 3. Architecture

```
Assets/_Project/            (folders created — Unity writes their .meta on import)
  Scenes/                   00_Boot, 01_MainMenu, 02_CharacterSelect, 03_MapSelect, 10_Tutorial, 20_Map_Arena, 21_Map_Alley
  Scripts/Core/             GameFlow (scene routing + selected character/map), ObjectPool<T>, IPooled, DamageInfo
  Scripts/Player/           PlayerMotor, ThirdPersonCameraRig, PlayerHealth, PlayerAbilities
  Scripts/Player/Abilities/ 6 ability classes (2 per character)
  Scripts/Weapons/          Weapon, Projectile, ProjectilePool, HitFeedback
  Scripts/Enemies/          EnemyBrain, EnemyHealth, Spyglass (perception), AllyCall
  Scripts/Enemies/FSM/      StateMachine, IState, SearchState, ChaseState, AttackState, FleeState, CallAlliesState
  Scripts/Systems/          WaveDirector, ScoreService, SpawnPoint, TutorialDirector
  Scripts/UI/               HUD, MinimapCamera, FragBoardUI, AbilityCooldownUI, CharacterSelectUI, MapSelectUI, PauseMenu
  Scripts/Data/             CharacterDefinition, AbilityDefinition, MapDefinition, WaveDefinition, EnemyStats (ScriptableObjects)
  Data/                     the SO instances (Characters/, Abilities/, Maps/, Waves/, Weapons/)
  Prefabs/                  Player/, Enemies/, Projectiles/, VFX/, UI/
  Settings/                 URP assets stay at Assets/Settings (from the template)
```

Defensibility rules (this is what the 20:20 session grades):
- One responsibility per file; a generic base plus a per-variant strategy instead of duplicated copies.
- **One `EnemyBrain`, five states, two bodies.** Ranged and melee differ by an attack strategy
  (`RangedAttack` / `MeleeAttack`) plus an `EnemyStats` SO. Talking point: "one brain, two bodies".
- **Pooling**: `ObjectPool<T>` with an inspector warm-up count, `Get()` / `Release()`; projectiles,
  muzzle flash and hit VFX pooled. `Instantiate` appears only in warm-up — provable with one grep.
- **Data over hardcode**: characters, abilities, maps, waves and enemy stats are ScriptableObjects, so a
  balance change can be shown live during the presentation without touching code.
- **Levels are ProBuilder meshes** in their own scenes; nothing gameplay-owned lives in scene data that
  a prefab could carry.

## 4. Requirement → system map

| Req | How it is satisfied | Files |
|---|---|---|
| Third-person camera | Port of `shootshoot/PlayerControl.cs`: CharacterController motor + pivot camera, pitch clamp, mouse look, sprint | `PlayerMotor`, `ThirdPersonCameraRig` |
| Pooled shooting | `ObjectPool<T>` + `ProjectilePool`; weapon requests, projectile releases itself on hit/lifetime | `ObjectPool`, `ProjectilePool`, `Weapon`, `Projectile` |
| Ranged FSM | Shared brain + `RangedAttack` (line of sight, fire rate, backs off when too close) | `EnemyBrain`, `RangedEnemy` |
| Melee FSM | Shared brain + `MeleeAttack` (wind-up telegraph, lunge, recover) | `EnemyBrain`, `MeleeEnemy` |
| Call allies | `AllyCall` broadcast in radius → allies enter `CallAlliesState`, converge on the caller's target | `CallAlliesState`, `AllyCall` |
| 3 characters | `CharacterDefinition` SO (prefab, stats, 2 ability refs); choice held in `GameFlow` | `CharacterDefinition`, `CharacterSelectUI` |
| 2 abilities each | `AbilityDefinition` SO + one class per ability; cooldown owned by `PlayerAbilities` | 6 classes under `Player/Abilities/` |
| Minimap | Ortho camera above the player, culling mask = minimap icons, 256² RenderTexture → `RawImage` + rotating arrow | `MinimapCamera`, `HUD` |
| Tab frags | `ScoreService` counts kills/deaths; holding Tab shows the board | `ScoreService`, `FragBoardUI` |
| Map select | `MapDefinition` SO (scene name, thumbnail, blurb) → `SceneManager.LoadScene` | `MapSelectUI`, `MapDefinition` |
| Tutorial | Ordered steps gated by events: move → look → shoot → ability → kill → done | `TutorialDirector` |

## 5. The three self-chosen mechanics — decided at 21:30, not now

Ranked by grade-per-hour. Chloe picks three at S6, once the mandatory systems stand.
Recommended set in bold.

- **A. Wave director with escalating difficulty** (~3 h) — spawn table SO, composition ramp, breather
  between waves, HUD wave counter. Turns the arena into a game and doubles as level-design evidence.
- **B. Ultimate meter** (~2 h) — kills charge a meter; full meter unlocks a third, stronger ability.
- **C. Damage numbers + hit markers with headshot multiplier** (~2 h) — shows the projectile/damage pipeline.
- **E. Pause menu with settings** (sensitivity, invert Y, volume, quit) (~1.5 h) — cheap, expected.
- D. Destructible cover + jump pads (~3 h) — level-design flourish, reusable across both maps.

**Default if the clock runs out: A + B + E**; C and D go to the cut list.

## 6. Sprint schedule — today, 2026-10-23, from 13:00

Every block ends with its own verification gate; a block with no gate gets declared done on vibes.

| Block | Work | Gate |
|---|---|---|
| **S0 13:00–13:20** | Confirm the headless import, ProBuilder in the package list, `Assets/_Project` folders, commit the Unity project | editor opens with no console errors; `git log` shows the Unity-project commit |
| **S1 13:20–15:10** | `PlayerMotor` + `ThirdPersonCameraRig` (ported), `ObjectPool`/`ProjectilePool`/`Weapon`/`Projectile`, damage + hit feedback, ProBuilder grey-box test arena | fire 60 rounds: pool count identical before/after; `grep -rn "Instantiate(" Assets/_Project/Scripts` returns warm-up only |
| **S2 15:10–17:10** | `EnemyBrain` + 5 states, `Spyglass` perception, `RangedEnemy`, `MeleeEnemy`, `EnemyHealth`, `AllyCall`, NavMesh bake on the test arena | both enemy types visit all five states (transition log); a called ally turns to the caller's target |
| **S3 17:10–18:10** | `CharacterDefinition` + 3 characters (designed here), `AbilityDefinition` + 6 abilities, character select | all six abilities fire from input, cooldowns tick, the choice carries into the map |
| **S4 18:10–19:00** | HUD, `MinimapCamera` + RenderTexture, `FragBoardUI` (Tab), wave counter | minimap follows the player; holding Tab shows the counts |
| **— 19:00–20:00** | break / food | — |
| **S5 20:00–21:30** | Two ProBuilder maps + `MapSelectUI`/`MapDefinition`, tutorial scene with gated steps | tutorial completes from a fresh launch; both maps load from the menu |
| **S6 21:30–22:30** | The three chosen mechanics (default A+B+E) | each demonstrable in under 30 s |
| **S7 22:30–23:30** | Linux build, Windows build if the module has landed, `docs/CODE_DEFENSE.md` skeleton, commit + ledger close | a build runs from a clean folder; every defense block opens the file it names |

Anything unfinished at the end of its block goes to the **cut list** that night, never forward.

## 7. After the sprint — 10-24 → 10-30 (buffer, not development)

| Day | Use |
|---|---|
| Sat 24 – Sun 25 | Fix what the sprint cut or broke; playtest with a second person; balance waves/abilities |
| Mon 26 – Wed 28 | Finish and rehearse `docs/CODE_DEFENSE.md` out loud (6 systems × ~3 min); Windows build once the module lands |
| Thu 29 | Final builds for both targets, submit to the LMS (multiple submissions allowed), demo rehearsal with a stopwatch |
| Fri 30 | Freeze. 18:30 demo, 20:20 code walkthrough. No edits after 18:00. |

## 8. Code-defense preparation

`docs/CODE_DEFENSE.md` gets one block per system, started in S7 and finished on 10-26:
what it does · why this design · the file to open · the 30-second pt-BR explanation · the likely
question ("why not `Instantiate`?", "why a state machine and not behaviour trees?", "why
ScriptableObjects?", "why one brain for two enemies?").

## 9. Decisions owed by Chloe

1. **+3 mechanics** — at 21:30, options A–E on screen. Default A+B+E if time is short.
2. **The 3 characters and their 2 abilities** — at S3, 17:10: theme per hero + how each ability behaves.
3. **Git remote** — push to `github.com/dollythecamus/hero-shooter`, or keep local? (local is the default)
4. **Windows build** — install the Windows build-support module (~1.5–3 GB; the last Unity registry
   download ran at ~90 KB/s, so it starts as a background job now).

## 10. Risks

| Risk | Mitigation |
|---|---|
| One ~10-hour sprint for a full game | blocks ordered by grade weight; cut list pre-written; 10-24 → 10-29 absorbs the rest |
| Windows build blocked by a missing module | module download starts now; Linux is the guaranteed deliverable |
| Slow Unity package registry (~90 KB/s observed) | packages already resolved into `Library/PackageCache` — add nothing mid-sprint |
| FSMs read as copy-paste in the code session | one `EnemyBrain`, per-type attack strategy — rehearsed |
| Sprint fatigue | the 19:00–20:00 break is part of the schedule, not a reward |
| Dirty tree | commit at the end of every block; ledger updated in the same turn |
