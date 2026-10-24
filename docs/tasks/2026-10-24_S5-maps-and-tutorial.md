# S5 — two maps, map select, tutorial (started 2026-10-24)

Scope from `docs/PLAN.md` § 6: **two ProBuilder maps + `MapSelectUI`/`MapDefinition`, tutorial scene with
gated steps**. Gate: *the tutorial completes from a fresh launch and both maps load from the menu.*

Two standing rules apply unchanged: **UI is authored as prefabs, never built at runtime**, and **no runtime
`Instantiate`** (`grep -rn "Instantiate(" Assets/_Project/Scripts` → only `Core/ObjectPool.cs`). Levels are
ProBuilder meshes (decision 2026-10-23: the maps double as the level-design evidence for the discipline).

## 1. Flow this block introduces

Nothing in the tree today routes between scenes: `90_TestArena` is simultaneously the menu, the arena and the
match. S5 splits that into scenes, with one singleton carrying the player's two choices across the load.

```
00_Boot (build index 0)
  MainMenu canvas        Play | Tutorial | Quit          (authored Prefabs/UI/MainMenu.prefab)
    Play      → CharacterSelect canvas   (existing authored prefab, reused)
                  pick a hero → MapSelect canvas        (new    Prefabs/UI/MapSelect.prefab)
                                  pick a map → SceneManager.LoadScene(map.sceneName)
    Tutorial  → SceneManager.LoadScene("10_Tutorial")
    Quit      → Application.Quit
  GameFlow  (DontDestroyOnLoad) — holds the roster, the map catalog and the two choices

20_Map_Arena / 21_Map_Alley / 10_Tutorial
  the same match rig `90_TestArena` already carries (player, pools, HUD, bars, Vfx, camera rig, Match),
  `MatchBootstrap` applies `GameFlow.SelectedCharacter` at Start instead of opening the select screen.
```

`MatchBootstrap` keeps its dev behaviour: when `GameFlow` has no selection (i.e. the scene is opened directly
in the editor, as `90_TestArena` always was) it runs the character select exactly as today. That keeps the
existing dev scene working untouched, which is also the fallback if a map is opened standalone.

**Map scenes are copies of `90_TestArena`'s rig, not a new rig prefab.** Reason: every reference in the rig
(`HudController` → `HeroLoadout`, `Match` → `HeroLoadout`/camera rig/`SpawnPoint`) is a same-scene reference.
Extracting a `MatchRig.prefab` means re-owning those references inside one prefab — a wiring risk with no
gate that would catch it before the demo. Duplication is the cheaper risk; the divergence cost is written
down here so a later block either extracts the prefab deliberately or knows to edit all three scenes.

## 2. Files

| Status | Path | What |
|---|---|---|
| new | `Scripts/Data/MapDefinition.cs` | SO: `displayName`, `sceneName`, `blurb`, `sizeLabel`, `accentColor` |
| new | `Scripts/Systems/GameFlow.cs` | `DontDestroyOnLoad` singleton: roster, map catalog, `SelectedCharacter`, `SelectedMap`, `StartMatch()`, `StartTutorial()`, `ReturnToMenu()`, `Quit()` |
| new | `Scripts/UI/MainMenuUI.cs` | controller for the authored main menu: `Play()`, `Tutorial()`, `Quit()` as persistent listeners |
| new | `Scripts/UI/MapSelectUI.cs` | controller for the authored map select: fills N `MapCardUI` from the catalog, `Select(int)` |
| new | `Scripts/UI/MapCardUI.cs` | one authored map card: name, blurb, size, tint, `Click()` (mirrors `CharacterCardUI`) |
| new | `Scripts/Systems/TutorialDirector.cs` | ordered steps, each gated by an observed event, drives `TutorialPanelUI`, returns to the menu |
| new | `Scripts/UI/TutorialPanelUI.cs` | controller for the authored tutorial panel: step title, body, progress, done state |
| edit | `Scripts/Systems/MatchBootstrap.cs` | apply `GameFlow.SelectedCharacter` at `Start` when present; extract `ApplySelection` |
| edit | `Scripts/Player/PlayerAbilities.cs` | `public event Action<int> Activated` on a successful `TryActivate` (the tutorial's ability gate; S6's ultimate meter wants the same event) |
| new | `Data/Maps/Map_Arena.asset`, `Data/Maps/Map_Alley.asset` | the two `MapDefinition` instances |
| new | `Prefabs/UI/MainMenu.prefab`, `MapSelect.prefab`, `TutorialPanel.prefab` | authored UI |
| new | `Scenes/00_Boot.unity` | menu scene: camera, EventSystem, `GameFlow`, main menu + both select canvases (hidden) |
| new | `Scenes/10_Tutorial.unity` | arena rig + tutorial geometry + `Tutorial` object (`TutorialDirector` + panel) + 2 target dummies |
| new | `Scenes/20_Map_Arena.unity` | open symmetric courtyard: raised centre, 4 cover blocks, 2 ramps, perimeter walls |
| new | `Scenes/21_Map_Alley.unity` | narrow L corridor network with a second floor, stairs up, two chokepoints |
| edit | `ProjectSettings/EditorBuildSettings.asset` | via `manage_build(action='scenes')` — **approved by Chloe 2026-10-24**: `00_Boot`, `10_Tutorial`, `20_Map_Arena`, `21_Map_Alley`, `90_TestArena` last; `SampleScene` removed |

Untouched on purpose: `Prefabs/Player`, `Prefabs/Enemies`, `Prefabs/UI/CharacterSelect.prefab`, all ability and
weapon assets, `docs/HERO_ROSTER.md` tuning. Enemy placement is per map (each scene carries its own instances,
as the arena does today) — a spawner is S6's wave director.

## 3. Tutorial steps

| # | Step | Gate (observed, not inferred) |
|---|---|---|
| 1 | Move — `WASD` | `PlayerMotor` accumulates ≥ 8 m of planar travel |
| 2 | Look — mouse | camera yaw accumulates ≥ 60° |
| 3 | Shoot — left click | `Weapon.ShotsFired` increases |
| 4 | Ability — `Q`/`E` | `PlayerAbilities.Activated` |
| 5 | Kill — destroy the dummy | `Health.Died` on a tutorial target |
| 6 | Done | panel shows completion; return to `00_Boot` |

Movement/look/weapon are polled (`PlanarSpeed`, camera yaw, `ShotsFired`) because no event exists for them;
the ability step uses the new event. Steps run in order — a later step cannot complete before an earlier one,
which is the property the gate measures.

## 4. Gate table (to be filled with measurements)

| Gate row | How it is measured |
|---|---|
| Fresh launch reaches the menu | play mode from `00_Boot`: `SceneManager.GetActiveScene().name`, `MainMenu` canvas active, roster + catalog lengths |
| Character select → map select | `MainMenuUI.Play()` → pick → `MapSelectUI` visible with 2 cards filled from the assets |
| Both maps load from the menu | `MapBench.Select(0)` → active scene `20_Map_Arena`; `Select(1)` → `21_Map_Alley`, and in each: `HeroLoadout.Current.displayName` == the pick, spawn position == `SpawnPoint` |
| Tutorial completes from a fresh launch | from `00_Boot`: `Tutorial()` → active scene `10_Tutorial` → drive the five gates → `TutorialDirector.Completed == true`, panel shows the done state, `ReturnToMenu()` → active scene `00_Boot` |
| Static | `grep -rn "Instantiate(" Assets/_Project/Scripts` → one hit; `grep -c "error CS" Logs/Editor.log` → 0 after recompile; `git status --short` only intended paths |

Play-mode probes run through the MCP bridge with the method proven in earlier blocks (`EditorApplication.update`
callback + `Debug.Log`, read back from `Logs/Editor.log`) because sleeping in the shell does not advance game
time here.

---

## Landed 2026-10-24 10:4x–11:2x

**Built, all of it.** Six new scripts (`MapDefinition`, `GameFlow`, `MainMenuUI`, `MapSelectUI`, `MapCardUI`,
`TutorialDirector`, `TutorialPanelUI`, `ICharacterPicker`), two edits to existing flow code (`MatchBootstrap`
gained the `GameFlow` entry path and `ICharacterPicker`; `PlayerAbilities` gained the `Activated` event), two
`MapDefinition` assets, three authored UI prefabs, four scenes, and the Build Settings list (Chloe's sign-off
2026-10-24, option 1: Boot first, `90_TestArena` last, `SampleScene` dropped).

**Probe method.** Same bridge method as S3b/S4: an `EditorApplication.update` callback drives the flow and
`Debug.Log`s `[Probe]` lines, read back from `Logs/Editor.log` — sleeping in the shell does not advance game
time here. Each run entered play mode from `00_Boot`, which is what "fresh launch" means in this project.

### Gate table — measured, not inferred

| Gate row | Observation |
|---|---|
| Fresh launch reaches the menu | `[Probe] A scene=00_Boot playing=True menuUI=1 menuCanvasActive=True selectVisible=False mapSelectVisible=False` and `A flow=True roster=3 maps=2 map0=ARENA/20_Map_Arena` |
| Play → character select | `[Probe] B play() -> selectVisible=True cards=3 card0Name=ROCK menuCanvasActive=False` |
| Hero pick → map select, filled from the assets | `[Probe] C pickPaper -> mapSelectVisible=True mapCards=2 names=ARENA|ALLEY| heroLabel='FIGHTING AS PAPER' selectedCharacter=Paper` |
| Map 1 loads from the menu | `[Probe] E scene=20_Map_Arena selectedMap=ARENA hero=Paper maxHealth=90 playerPos=(0.00, 0.08, -22.00) spawnPos=(0.00, 1.20, -22.00) hud=True selectCanvasPresent=0`, `[Probe] E navmesh agents=4 onNavMesh=4` |
| Map 2 loads from the menu | `[Probe2] C scene=21_Map_Alley map=ALLEY hero=Scissors pos=(-20.00, 0.08, 12.00) spawn=(-20.00, 1.20, 12.00) geometry=20 agentsOnNavMesh=4/4`, `[Probe2] C stair/ramp present=True doorway=True balcony=True` |
| Back to menu keeps the flow alive | `[Probe2] D back to menu scene=00_Boot`, then a second Play → pick → load cycle completed (above) |
| Tutorial starts from the menu | `[Probe5] A loaded: step=0/5 gate=Move progress=0.00 …` (scene `10_Tutorial`, panel visible, hero applied) |
| Tutorial step 1 Move, and a teleport does **not** count | `[Probe4] C after teleport: step=0/5 gate=Move progress=0.00` after `+10 m`; `[Probe5] B frame=45 step=1/5 gate=Look planarSpeed=6.00 pos=(0.0, 0.1, 1.0)` under a sustained 6 m/s drive |
| Steps 2–4 in order | `[Probe5] D after look: step=2/5 gate=Shoot` · `E shoot: 0 -> 1` · `F after shoot: step=3/5 gate=Ability` · `G ability Q=True E=True` · `H after ability: step=4/5 gate=Kill` |
| Step 5 Kill + completion | `[Probe5] I damaged 3 targets` → `J COMPLETED=True step=5/5 panelTitle='TRAINING COMPLETE' doneRootVisible=True` |
| Done button returns to the menu | `[Probe5] K after done button: scene=00_Boot menuVisible=True` |
| Static | `grep -rn "Instantiate("` → `Core/ObjectPool.cs:43` only; `grep -rn attackRange` → declaration only; `grep -rn SceneManager.LoadScene` → 3 sites in `GameFlow.cs`; no `error CS` after the last Tundra build; `git diff --check` clean |

**How the tutorial was driven (labelled, per the pipeline's call-site rule).** Move and Look were not driven by a
held key: injected keyboard state is overwritten by the editor every input update (S3b note). Move was driven at
`PlayerMotor.Launch` — the same velocity path the climb exit uses, re-issued each frame so `PlanarSpeed` stays
above the gate's 0.3 m/s — and Look by advancing the camera rig's own `_yaw` field 15°/frame. Shoot used
`Weapon.TryFire`, Ability `PlayerAbilities.TryActivate`, Kill `Health.TakeDamage`. **Not proven:** the same five
steps under a human at the keyboard (and the visual look of any screen).

### Two defects the gate found, both fixed and re-measured

1. **`MainMenuUI` held a dead `GameFlow` after returning from a map.** The boot scene's own `GameFlow` destroys
   itself when an instance survived the load, so the serialized reference in `MainMenuUI` was a destroyed object
   and `Tutorial()`/`Quit()` silently did nothing — `[Probe2] EXCEPTION frame=65` with the tutorial never loading.
   Fixed with `RefreshFlow()` (`GameFlow.Instance` wins, called from `Awake` and `Start`). Re-measured: the second
   Play → pick → load cycle and the tutorial launch both work.
2. **The tutorial's Move step completed itself on scene load.** The gate compared raw position deltas and
   `MatchBootstrap` teleports the hero to the spawn point at `Start`, so step 1 was already done before the player
   moved: `[Probe3] B … step=1/5` at the first frame sampled. Fixed by accumulating travel only while
   `PlayerMotor.PlanarSpeed > 0.3`. Re-measured: `[Probe4] C after teleport … progress=0.00`, then
   `[Probe5]` completes it under real planar speed.

### What is not covered here

- **Nothing was looked at.** No one has seen the menu, the map select, the two maps or the tutorial panel on
  screen — every row above is a number reaching the right object, not a judgement of the layout.
- **No physical click was tested.** The probes call the same methods the buttons' persistent listeners call
  (`MainMenuUI.Play`, `CharacterCardUI.Click`, `MapCardUI.Click`), so the graphics-raycast/EventSystem path from a
  real mouse click to those listeners is **unverified**.
- Enemies in both maps are the same 4 hand-placed prefab instances as the dev arena; the two maps differ in
  geometry and spawn positions only. A spawner/wave director is S6.
- No pause, no score, no death handling, no restart after death (S6), and `90_TestArena` still runs the old
  in-scene character select on purpose.
