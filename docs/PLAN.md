# Hero Shooter — build plan

Facts this plan is built on (verified 2026-10-23):
- Deadline **2026-10-30 22:00**; presentation 18:30–20:00, code walkthrough 20:20–22:00 that same evening.
- Unity **6000.6.3f1** is the only editor installed (`~/Unity/Hub/Editor/6000.6.3f1`), URP blank template `17.2.1` ships with it.
- `~/projects/shootshoot` (Unity 6000.6.3f1, URP 17.6.0, Input System 1.20.0) already contains working
  third-person movement (`PlayerControl.cs`, 132 lines) and a correct object-pool shooter
  (`ShootPool.cs` + `Projectile.cs`, 124 lines). Both are directly reusable — that is roughly
  requirements 2 and 5 already solved, and it is the fastest defensible base.
- 7 calendar days, minus classes and sleep. Everything below assumes evenings + one long weekend block.

## 1. One-sentence goal

A single-player, wave-based third-person hero shooter in Unity 6 where the player picks 1 of 3
characters on 1 of 2 maps, survives escalating waves of FSM-driven ranged and melee enemies using
pooled projectiles and 2 abilities per character, with minimap, Tab frag board, and a scripted
tutorial — each requirement individually openable in code.

## 2. Project identity

| | |
|---|---|
| Path | `~/projects/hero-shooter` |
| Unity | 6000.6.3f1, URP (3D), Input System, Cinemachine **not** required |
| Git | local repo, `origin` = `github.com/dollythecamus/hero-shooter` (created on first push) |
| Docs | `docs/ASSIGNMENT.md` (brief), `docs/PLAN.md` (this), `docs/PROJECT_LEDGER.md` (state), `docs/CODE_DEFENSE.md` (written during the polish phase) |

## 3. Architecture

```
Assets/_Project/
  Art/            characters, enemies, environment (Blender exports)
  Audio/
  Data/           ScriptableObject instances: Characters/, Abilities/, Maps/, Waves/, Weapons/
  Prefabs/        Player/, Enemies/, Projectiles/, VFX/, UI/
  Scenes/         00_Boot, 01_MainMenu, 02_CharacterSelect, 03_MapSelect, 10_Tutorial, 20_Map_Arena, 21_Map_Alley
  Scripts/
    Core/         GameFlow (scene routing), ServiceLocator, ObjectPool<T>, IPooled
    Player/       PlayerMotor, ThirdPersonCameraRig, PlayerHealth, PlayerAbilities
    Weapons/      Weapon, Projectile, ProjectilePool, DamageInfo, HitFeedback
    Enemies/      EnemyBrain, FSM/ (StateMachine, IState, 5 states), RangedEnemy, MeleeEnemy, EnemyHealth
    Systems/      WaveDirector, ScoreService, SpawnPoint, MapDefinition, TutorialDirector
    UI/           HUD, MinimapCamera, FragBoardUI, AbilityCooldownUI, CharacterSelectUI, MapSelectUI
    Data/         CharacterDefinition, AbilityDefinition, MapDefinition, WaveDefinition
  Settings/       URP assets, Input actions
```

Rules that keep it defensible in the 20:20 code session:
- **One responsibility per file**, no "Manager" catch-alls beyond `GameFlow`.
- **FSM is generic and data-driven**: `EnemyBrain` owns the state machine; ranged and melee differ only
  by an attack strategy + one `EnemyStats` SO, so the walkthrough is "one brain, two bodies", not two
  duplicated AIs. States: `SearchState`, `ChaseState`, `AttackState`, `FleeState`, `CallAlliesState`.
  Transitions keyed off distance, line-of-sight, health %, and `AllyCall` signal — every transition one line.
- **Pooling**: `ObjectPool<T>` warm-up count in the inspector, `Get()`/`Release()`, projectiles and hit VFX
  pooled. Zero `Instantiate` inside `Update`/`Fire` — grep for it at the end and show it is only in warm-up.
- **Data over hardcode**: characters, abilities, maps and waves are ScriptableObjects, so the presentation
  can add a balancing change live without a code edit.

## 4. Requirement → system map

| Req | How it is satisfied | Files |
|---|---|---|
| Third-person camera | Lifted from `shootshoot/PlayerControl.cs`: CharacterController motor + pivot camera with pitch clamp, mouse look, sprint | `PlayerMotor`, `ThirdPersonCameraRig` |
| Pooled shooting | `ObjectPool<T>` + `ProjectilePool`; weapon asks pool for a projectile, projectile releases itself on collision/lifetime | `ObjectPool`, `ProjectilePool`, `Weapon`, `Projectile` |
| Ranged FSM | Same brain, `RangedAttack` (LOS + fire rate + retreat when close) | `EnemyBrain`, `RangedEnemy` |
| Melee FSM | Same brain, `MeleeAttack` (wind-up telegraph + lunge + recover) | `EnemyBrain`, `MeleeEnemy` |
| Call allies | Enemy broadcasts on `AllyCall` within radius → allies enter `CallAlliesState` → converge on caller's target | `CallAlliesState`, `AllySignal` |
| 3 characters | `CharacterDefinition` SO (prefab, stats, 2 ability refs); selection stored in `GameFlow` | `CharacterDefinition`, `CharacterSelectUI` |
| 2 abilities each | `AbilityDefinition` SO + one class per ability, cooldown handled in `PlayerAbilities` | 6 classes under `Player/Abilities/` |
| Minimap | Ortho camera above player, culling mask = minimap icons only, 256² RenderTexture into a `RawImage` with a rotating player arrow | `MinimapCamera`, `HUD` |
| Tab frags | `ScoreService` counts player kills / enemy kills / deaths; holding Tab shows board | `ScoreService`, `FragBoardUI` |
| Map select | `MapDefinition` SO (scene name, thumbnail, description) → `SceneManager.LoadScene` | `MapSelectUI`, `MapDefinition` |
| Tutorial | Scripted step list: move → look → shoot → ability → kill → done, each gated by an event | `TutorialDirector`, `TutorialStep` |

## 5. The three self-chosen mechanics (proposed — her call)

Ranked by grade-per-hour. Recommendation: **A + B + E**.

- **A. Wave director with escalating difficulty** (~3 h) — spawn table SO, wave count, composition ramp,
  breather between waves, HUD wave counter. Turns a shooting range into a *game*, and doubles as level-design
  evidence for the 18:30 presentation.
- **B. Ultimate meter** (~2 h) — kills charge a meter; full meter unlocks a third, stronger ability per
  character. Costs almost nothing (one field + one UI bar) and gives the presentation a hook.
- **C. Damage numbers + hit markers, with headshot multiplier** (~2 h) — crit colliders, floating pooled
  damage labels. Shows the projectile/damage pipeline in detail.
- **D. Destructible cover + jump pads** (~3 h) — level-design flourish, reusable across both maps.
- **E. Pause menu with settings** (sensitivity, invert Y, volume, quit) (~1.5 h) — cheap, expected by
  any evaluator, and one clear UI system to explain.

Cut candidates (do not start before P5): bot allies, weapon pickups, meta progression.

## 6. Schedule (2026-10-23 → 10-30)

| When | Phase | Output | Verify |
|---|---|---|---|
| **Fri 23** (tonight, ~1 h) | P0 — scaffold | Unity project created from URP blank template, folder layout, git init + first commit, `shootshoot` scripts copied in | editor opens, play mode runs, `git log` shows commit 1 |
| **Sat 24** (4–5 h) | P1 — player + shooting | `PlayerMotor`, camera rig, `ObjectPool`, `ProjectilePool`, `Weapon`, hit feedback. Grey-box arena | shoot 50 rounds, pool never grows after warm-up (log the count) |
| **Sun 25** (4–5 h) | P2 — FSM enemies | `EnemyBrain` + 5 states, ranged + melee prefabs, health, death, `AllyCall` | both enemy types visibly cycle all 5 states; grep transition log |
| **Mon 26** (2–3 h) | P3 — characters + abilities | 3 `CharacterDefinition`s, 6 abilities, character-select screen | all 6 abilities fire from the input, cooldowns tick |
| **Tue 27** (2–3 h) | P4 — UI systems | HUD, minimap, Tab frag board, wave counter | minimap tracks player, Tab shows kills |
| **Wed 28** (3–4 h) | P5 — maps, tutorial, custom mechanics | 2 maps dressed, tutorial scene, mechanics A+B+E | tutorial completes end to end; map select loads both |
| **Thu 29** (3–4 h) | P6 — build + defense prep | Windows/Linux build, `docs/CODE_DEFENSE.md`, balance pass, playtest by a second person | fresh build runs from a clean folder; every file in the defense doc opens |
| **Fri 30** (morning) | P7 — freeze | Final build submitted to LMS (multiple submissions allowed), 20-min demo rehearsed, script of the code walkthrough | demo timed with a stopwatch; submission confirmation page |

Buffer rule: anything unfinished at the end of its day moves to the **cut list** the same evening, not forward.

## 7. Code-defense preparation (the half of the grade that is easy to lose)

`docs/CODE_DEFENSE.md` gets one block per system, written *while building*, each with:
what it does · why this design · the one file to open · the 30-second explanation in pt-BR · the likely
question ("why not `Instantiate`?", "why a state machine and not behaviour trees?", "why ScriptableObjects?").

Rehearse out loud once on Thu 29 and once on Fri 30 — target: 6 systems × ~3 min ≈ 20 min of the 100-minute slot.

## 8. Decisions owed by Chloe

1. **Custom mechanics**: confirm A+B+E (or swap) — blocks P5.
2. **Characters**: 3 concrete heroes with a theme each (e.g. gunner / brawler / support) and what the 2
   abilities are. Design input, not code — needed by Mon 26.
3. **Art source**: reuse `shootshoot`/store primitives and grey-boxing (fast, ugly) or model in Blender
   (better grade, ~1 extra day). Default assumption if unanswered: **grey-box with clear silhouettes**.
4. **Maps**: 2 grey-boxed maps (default) or 3 if time allows.
5. **Build target**: Windows x86_64 for the class machines, or WebGL for a link submission?

## 9. Risks

| Risk | Mitigation |
|---|---|
| Scope is a full game in 7 days | Grey-box art, one arena geometry reused across both maps, cut list survives |
| Unity project creation/licence friction on NixOS | `unityhub` present; editor 6000.6.3f1 installed; batchmode build recipe known from `UnityXR_3DMuseu` |
| FSMs look like copy-paste in the code session | single `EnemyBrain`, per-type attack strategy — rehearsed talking point |
| Build machine has no Windows target | build Linux standalone + WebGL, or install the Windows module on Sat 24 |
| Dirty-tree habit | commit at the end of every phase; ledger updated in the same turn |
