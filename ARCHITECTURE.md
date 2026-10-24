# ARCHITECTURE — hero-shooter

A single-player, wave-based third-person hero shooter: the player picks 1 of 3 heroes (Rock / Paper /
Scissors), fights FSM-driven ranged and melee enemies with pooled projectiles and two abilities. Built for
UNICAP *Level Design e Programação 3D*, individual project, due **2026-10-30 22:00**.
"Working" means: both builds run from a clean folder, all 11 checklist rows in `docs/ASSIGNMENT.md` are
demonstrable, and every system can be opened and defended out loud in the 20:20 code session.

This file describes **what exists**. The sprint plan and requirement→system map live in `docs/PLAN.md`;
its § 3 file listing is partly aspirational (see *Not built yet*).

## Stack

- **Unity 6000.6.3f1** (`ProjectSettings/ProjectVersion.txt`), URP 17.6.0, PC renderer asset under `Assets/Settings/`
- **C#**, single implicit assembly `Assembly-CSharp` — there are no `.asmdef` files; editing any script recompiles everything
- Packages (`Packages/manifest.json`): Input System 1.19.0, AI Navigation 2.0.12, ProBuilder 6.1.2, uGUI 2.6.0,
  Test Framework 1.8.0, Rider/VS IDE support, `com.coplaydev.unity-mcp` v10.0.0 (editor bridge)
- Build targets: Linux x86_64 and Windows x86_64 (Windows module installed, Mono backend)
- No imported art: ProBuilder geometry + URP Lit materials; a hero is a capsule scale plus a colour

Input System is installed but **no Input Actions asset is used** — `InputReader` reads the low-level
`Keyboard.current` / `Mouse.current` / `Gamepad.current` devices at call time.

## Components

- **Core primitives** — `IDamageable` + `DamageInfo` (one hit, struct), `Health` (value + `Damaged`/`Died`
  events, damage multiplier), `IPooled` + `ObjectPool<T>` (warm-up pool). Everything else builds on these.
  → `docs/features/damage-and-pooling.md`
- **Player** — `InputReader` (the only file that touches hardware) feeding `PlayerMotor` (CharacterController
  motion), `PlayerCombat` (trigger → weapon), `PlayerAbilities` (Q/E → ability slots). `ThirdPersonCameraRig`
  owns the camera, `PlayerStatus` is the single sink for timed multipliers. → `player-movement-and-camera.md`
- **Weapons** — `Weapon` reads a `WeaponDefinition` SO and fires through `ProjectilePool`; `Projectile` flies a
  per-frame `Physics.SphereCast` and returns itself to the pool. One bullet prefab serves player and enemies.
  → `weapons-and-projectiles.md`
- **Enemies** — one `EnemyBrain` (NavMeshAgent + `StateMachine` + perception + every transition in one method),
  five states, and a swappable `IEnemyAttack` strategy (`RangedAttack` / `MeleeAttack`) plus an `EnemyStats` SO.
  "One brain, two bodies". → `enemy-fsm.md`
- **Characters & abilities** — `CharacterDefinition` is the whole hero (stats, body, one weapon, two ability
  SOs); `AbilityShapes` maps `AbilityKind` → instant / channelled / passive; instant kinds are plain C#
  `IAbility` classes from `AbilityFactory`, the three stateful ones are components (`SlamDriver`,
  `BulwarkShield`, `WallClimbPassive`); `PlayerAbilities` routes Q/E and owns the instant cooldowns.
  → `characters-and-abilities.md`
- **Match flow & UI** — `MatchBootstrap` holds the roster, freezes the hero, hands the pick to `HeroLoadout`.
  `CharacterSelectUI` / `CharacterCardUI` drive an **authored** prefab. → `characters-and-abilities.md`
- **Planned, not built** — HUD, enemy health bars, RMB aiming, VFX, wave director, scoring, minimap, map select,
  tutorial. → `hud-aiming-and-feedback.md`, `docs/tasks/`

Scene reality: `Assets/_Project/Scenes/90_TestArena.unity` holds the ProBuilder arena, the `ProjectilePool`
object, the `Match` bootstrap, the camera rig, the authored `CharacterSelect` canvas, 3 static test targets,
and **4 hand-placed enemies** (2 ranged, 2 melee) as prefab instances. There is no runtime spawner.

## Core conventions

- **Namespaces mirror folders**: `HeroShooter.<Area>` (`Core`, `Data`, `Player`, `Player.Abilities`, `Weapons`,
  `Enemies`, `Enemies.FSM`, `Enemies.States`, `Systems`). `Scripts/UI/*.cs` are in `HeroShooter.Systems`.
- **Data over hardcode**: every tunable number lives on a ScriptableObject (`CharacterDefinition`,
  `WeaponDefinition`, `AbilityDefinition`, `EnemyStats`). Classes hold behaviour only. A balance change is an
  asset edit and must be demonstrable live.
- **UI is authored as prefabs, never built at runtime.** Layout, widgets and button listeners are visible in
  the inspector; a controller script may only fill and show/hide.
- **No runtime `Instantiate`.** Only `ObjectPool<T>.CreateInstance` may allocate
  (`grep -rn "Instantiate(" Assets/_Project/Scripts` → one hit). Warm-up count must cover a whole firefight.
- **One mover**: only one component may call `CharacterController.Move` in a frame. Currently violated by
  `PlayerStatus`'s climb path — see the one-mover rule in the ledger decisions.
- **Layers** (fixed by Chloe): Structure = 3, Player = 6, Enemy = 7. Masks are serialized integers and the
  `8` / `9` values are correct, not bugs. Table and derived mask values: `docs/LAYERS.md`.
- **State ownership**: a component owns its own state and announces changes with C# events
  (`Health.Damaged`, `Health.Died`, `StateMachine.Transitioned`, `AllyCall.Broadcasted`); callers subscribe
  rather than poll. Optional references are resolved in `Awake` with a `GetComponent` / `Camera.main` fallback
  because a prefab cannot hold a scene reference; `Weapon` re-resolves its pool on every use for ordering reasons.
- **Error handling**: no exceptions for gameplay. Missing data or a dead target means a silent early return —
  a null `WeaponDefinition` fires nothing, an ability with no target does nothing. Nothing logs a warning.
- **Testing**: no automated tests exist. `com.unity.test-framework` is installed but there is no test assembly,
  so the done-criteria are play-mode gates, console probes and greps (`AGENTS.md`).

## Directory map

| Path | Purpose |
|---|---|
| `Assets/_Project/Scripts/Core/` | `Health`, `DamageInfo`/`IDamageable`, `IPooled`, `ObjectPool<T>` |
| `Assets/_Project/Scripts/Data/` | `CharacterDefinition`, `WeaponDefinition`, `AbilityDefinition` (+ `AbilityKind`) |
| `Assets/_Project/Scripts/Player/` | `InputReader`, `PlayerMotor`, `ThirdPersonCameraRig`, `PlayerCombat`, `PlayerStatus`, `PlayerAbilities`, `AbilityShapes`, `SlamDriver`, `BulwarkShield`, `WallClimbPassive`, `HeroLoadout`, `PlayerAppearance` |
| `Assets/_Project/Scripts/Player/Abilities/` | `IAbility`/`AbilityContext`, `AbilityBase`, `AbilityFactory`, `AimRaycast`, 3 instant ability classes (`MarkTarget`, `Snare`, `Overclock`) |
| `Assets/_Project/Scripts/Weapons/` | `Weapon`, `Projectile`, `ProjectilePool` |
| `Assets/_Project/Scripts/Enemies/` | `EnemyBrain`, `EnemyStats`, `Spyglass`, `AllyCall`, `IEnemyAttack`, `RangedAttack`, `MeleeAttack` |
| `Assets/_Project/Scripts/Enemies/FSM/` | `EnemyState`, `IState`, `StateMachine` |
| `Assets/_Project/Scripts/Enemies/States/` | `Search`, `Chase`, `Attack`, `Flee`, `CallAllies` states |
| `Assets/_Project/Scripts/Systems/` | `MatchBootstrap`, `DamageLog` (S1 console probe, delete when the HUD lands) |
| `Assets/_Project/Scripts/UI/` | `CharacterSelectUI`, `CharacterCardUI` (controllers for the authored prefab) |
| `Assets/_Project/Data/` | the SO instances: `Characters/`, `Weapons/`, `Abilities/`, `Enemies/` |
| `Assets/_Project/Prefabs/` | `Player/`, `Enemies/`, `Projectiles/`, `UI/` (`VFX/` is empty) |
| `Assets/_Project/Scenes/` | `90_TestArena.unity` — the only gameplay scene; NavMesh baked into it |
| `Assets/_Project/Art/Materials/` | `M_Floor`, `M_Block`, `M_Player`, `M_Enemy`, `M_Projectile` |
| `docs/` | assignment, plan, ledger, roster, layers, per-block task files, feature docs |

## Feature docs

| Feature | Doc | Owns |
|---|---|---|
| Damage & pooling | `docs/features/damage-and-pooling.md` | `Health`, `DamageInfo`, `ObjectPool<T>`, `IPooled` |
| Player movement & camera | `docs/features/player-movement-and-camera.md` | `InputReader`, `PlayerMotor`, `ThirdPersonCameraRig`, `PlayerStatus` |
| Weapons & projectiles | `docs/features/weapons-and-projectiles.md` | `Weapon`, `WeaponDefinition`, `Projectile`, `ProjectilePool` |
| Enemy FSM | `docs/features/enemy-fsm.md` | `EnemyBrain`, states, `Spyglass`, `AllyCall`, attack strategies, `EnemyStats` |
| Characters & abilities | `docs/features/characters-and-abilities.md` | `CharacterDefinition`, `AbilityDefinition`, 6 abilities, `PlayerAbilities`, `HeroLoadout`, `MatchBootstrap`, select UI |
| HUD, aiming & feedback | `docs/features/hud-aiming-and-feedback.md` | **planned only** — design note, no code |
| Editor bridge | `docs/features/editor-mcp-tooling.md` | MCP for Unity v10 setup and pitfalls |

## Not built yet (listed here so it is not mistaken for existing code)

`docs/PLAN.md` § 3 names files that do not exist: `GameFlow`, `WaveDirector`, `ScoreService`, `SpawnPoint`,
`TutorialDirector`, `HUD`, `MinimapCamera`, `FragBoardUI`, `AbilityCooldownUI`, `MapSelectUI`, `PauseMenu`,
`HitFeedback`, `EnemyHealth`, `MapDefinition`, `WaveDefinition`, `PlayerHealth`. Health is one `Health`
component for player and enemies; there is no wave spawning, no death/score handling, no menu scenes
(`00_Boot`…`21_Map_Alley` do not exist) and no VFX prefab.

## External constraints

- Hard deadline **2026-10-30 22:00**; presentation 18:30–20:00 and source walkthrough 20:20–22:00 on that day.
  Half the grade is the spoken walkthrough, so designs must stay explainable in ~3 minutes per system.
- The brief (`docs/ASSIGNMENT.md`) fixes the 11 minimum mechanics; `+3` free mechanics are Chloe's choice.
- Design intent for heroes and abilities is owned by Chloe in `docs/HERO_ROSTER.md` — where it disagrees with
  the code, the roster wins and the code is wrong.
- The GUI editor holds `Temp/UnityLockfile` while open; batchmode runs and a second editor instance must not
  share the project. Agent-side wiring goes through the MCP bridge on `127.0.0.1:8080` instead.
